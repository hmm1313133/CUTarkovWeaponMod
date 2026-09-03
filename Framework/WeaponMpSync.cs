using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CUCoreLib.Networking;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 武器模组多人同步通道（基于 CUCoreLib MultiplayerApi JSON 消息）。
///
/// 架构：客户端本地预测 + 上报服务器权威执行。
/// - 客户端操作（退弹/刷卡/开锁/配件安装卸下）先本地执行（即时反馈），
///   再通过 SendToServer 上报；服务器 handler 在服务器端镜像上执行相同操作，
///   KrokMP 自身的周期状态同步（物品 rounds/condition、建筑 health/backgroundified）
///   会把权威状态传播到所有客户端。
/// - 物品创建（退弹子弹/卸下配件/地堡掉落）仅服务器端 Utils.Create
///   （自动 KrokMP 网络同步），客户端不本地创建，避免重复物品。
/// - 建筑操作（地堡门/武器箱）服务器处理后 Broadcast，其他客户端本地应用。
///
/// 消息协议（JSON，字段名缩短减小带宽）：
/// 客户端→服务器：
///   {"t":"ur","sid":弹匣syncId,"r":新rounds,"am":弹药id,"x":..,"y":..}     退弹
///   {"t":"kc","sid":钥匙卡syncId,"c":新condition,"dx":门x,"dy":门y}       刷卡开门
///   {"t":"bb","x":箱x,"y":箱y}                                            开锁武器箱
///   {"t":"ai","gs":枪syncId,"a":配件id,"is":配件物品syncId}               安装配件
///   {"t":"ad","gs":枪syncId,"a":配件id,"c":电量,"hb":有无电池,
///    "ft":feedType,"mc":magCapacity,"rm":roundsInMag}                     卸下配件（级联逐个上报）
/// 服务器→客户端广播：
///   {"t":"bd","x":门x,"y":门y}                                            门变背景
///   {"t":"bb","x":箱x,"y":箱y}                                            箱销毁
///   {"t":"ai","gs":枪syncId,"a":配件id,"ft":feedType,"mc":magCapacity,
///    "rm":roundsInMag}                                                    安装配件状态
///   {"t":"ad","gs":枪syncId,"a":配件id,"ft":feedType,"mc":magCapacity,
///    "rm":roundsInMag}                                                    卸下配件状态
/// </summary>
public static class WeaponMpSync
{
    private const string Channel = "cutarkovweapon.sync";

    // === KrokMP NetObjectRegistry 反射（CUCoreLib 未封装 syncId API） ===
    // 目标签名（KrokMP 4.0.1 反编译验证）：
    //   public class SyncInfo { public knetid syncId; public GameObject go; public Item item {get;} ... }
    //   public struct knetid  { public ushort id; public knetid(ushort id); }
    //   NetObjectRegistry.TryGetSyncInfo(Component obj, out SyncInfo si)
    //   NetObjectRegistry.TryGetSyncInfo(GameObject obj, out SyncInfo si)
    //   NetObjectRegistry.TryGetSyncInfo(knetid syncid, out SyncInfo si)
    //   ItemSync.TryGetItemSyncInfo(Item item, out SyncInfo si)  // 也可用，但 CUCoreLib
    //                                                            // 未封装 ItemSync，仍走反射
    private static MethodInfo _tryGetByItem;
    private static MethodInfo _getSyncInfoByItem;
    private static MethodInfo _tryGetById;
    private static FieldInfo _syncIdField;
    private static FieldInfo _knetidIdField;
    private static PropertyInfo _itemProperty;
    private static Type _knetidType;
    private static Type _itemSyncType;
    private static FieldInfo _clientLastInventoryStateField;
    private static bool _reflectionResolved;

    // === 直连 KrokMP 原始消息通道（不依赖 CUCoreLib MultiplayerBridge） ===
    // CUCoreLib 的 MultiplayerBridge 与当前 KrokMP 4.0.1 反射不兼容时，
    // MultiplayerApi.SendToServer/Broadcast 会返回 false。这里直接在武器模组内
    // 注册/发送 KrokMP 原始消息，避免改动 CUCoreLib。
    private const ushort RawServerMsgId = 58000; // 客户端 -> 服务器
    private const ushort RawClientMsgId = 58001; // 服务器 -> 客户端广播
    private static Type _rawNetType;
    private static Type _rawMpType;
    private static Type _rawClientMainType;
    private static Type _rawServerMainType;
    private static Type _rawReaderType;
    private static Type _rawWriterType;
    private static Type _rawDeliveryMethodType;
    private static Type _rawReceiverDelegateType;
    private static MethodInfo _rawCreateWriterMethod;
    private static MethodInfo _rawClientSendMethod;
    private static MethodInfo _rawSimpleSendStringMethod;
    private static MethodInfo _rawServerSendToClientsMethod;
    private static MethodInfo _rawRegisterServerReceiverMethod;
    private static MethodInfo _rawRegisterClientReceiverMethod;
    private static MethodInfo _rawPutStringMethod;
    private static MethodInfo _rawGetStringMethod;
    private static PropertyInfo _rawAllClientsExceptHost;
    private static object _rawReliableOrdered;
    private static bool _rawResolved;
    private static bool _rawRegistered;
    private static float _lastFullSyncTime = -999f;
    private const float FullSyncInterval = 3f;

    private static void ResolveReflection()
    {
        if (_reflectionResolved) return;
        _reflectionResolved = true;
        try
        {
            var registryType = AccessTools.TypeByName("KrokoshaCasualtiesMP.NetObjectRegistry");
            var syncInfoType = AccessTools.TypeByName("KrokoshaCasualtiesMP.SyncInfo");
            _itemSyncType = AccessTools.TypeByName("KrokoshaCasualtiesMP.ItemSync");
            _knetidType = AccessTools.TypeByName("KrokoshaCasualtiesMP.knetid");
            if (registryType == null || syncInfoType == null || _knetidType == null)
            {
                Plugin.Log.LogWarning("[WeaponMpSync] KrokMP types not found (KrokMP not installed?). MP sync disabled.");
                return;
            }

            _knetidIdField = AccessTools.Field(_knetidType, "id");
            _syncIdField = AccessTools.Field(syncInfoType, "syncId");
            _itemProperty = AccessTools.Property(syncInfoType, "item");

            // 实际 KrokMP 的 NetObjectRegistry.TryGetSyncInfo 接受 Component/GameObject，
            // 没有以 Item 为参数的重载。Item 继承自 MonoBehaviour(Component)，这里反射
            // Component 重载即可，调用时传入 Item 实例。
            // 同时兼容 ItemSync.TryGetItemSyncInfo(Item/knetid)，多版本下提高解析成功率。
            // 优先使用直接返回 SyncInfo 的 GetSyncInfo(Component)，
            // 少一层 out/ref 反射调用，兼容性更好。
            _getSyncInfoByItem = AccessTools.Method(registryType, "GetSyncInfo",
                new[] { typeof(Component) });

            _tryGetByItem = AccessTools.Method(registryType, "TryGetSyncInfo",
                new[] { typeof(Component), syncInfoType.MakeByRefType() });
            if (_tryGetByItem == null && _itemSyncType != null)
                _tryGetByItem = AccessTools.Method(_itemSyncType, "TryGetItemSyncInfo",
                    new[] { typeof(Item), syncInfoType.MakeByRefType() });

            _tryGetById = AccessTools.Method(registryType, "TryGetSyncInfo",
                new[] { _knetidType, syncInfoType.MakeByRefType() });
            if (_tryGetById == null && _itemSyncType != null)
                _tryGetById = AccessTools.Method(_itemSyncType, "TryGetItemSyncInfo",
                    new[] { _knetidType, syncInfoType.MakeByRefType() });

            if ((_tryGetByItem == null && _getSyncInfoByItem == null) || _tryGetById == null
                || _syncIdField == null || _knetidIdField == null || _itemProperty == null)
            {
                Plugin.Log.LogWarning("[WeaponMpSync] KrokMP sync API signature mismatch. MP sync disabled.");
                _tryGetByItem = null;
                _tryGetById = null;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Reflection resolve failed: {ex.Message}");
        }
    }

    /// <summary>获取物品的网络 syncId（客户端/服务器通用）。未注册网络对象时返回 null。</summary>
    public static ushort? GetSyncId(Item item)
    {
        if (item == null) return null;
        ResolveReflection();
        if (_tryGetByItem == null && _getSyncInfoByItem == null) return null;
        try
        {
            object syncInfo = null;
            if (_getSyncInfoByItem != null)
            {
                syncInfo = _getSyncInfoByItem.Invoke(null, new object[] { item });
            }
            else
            {
                var parameters = new object[] { item, null };
                if (!(bool)_tryGetByItem.Invoke(null, parameters) || parameters[1] == null) return null;
                syncInfo = parameters[1];
            }
            if (syncInfo == null) return null;

            // SyncInfo.syncId 是 knetid（struct 包装 ushort），取其 .id 字段
            var knetidObj = _syncIdField.GetValue(syncInfo);
            if (knetidObj == null) return null;
            var idObj = _knetidIdField.GetValue(knetidObj);
            return idObj != null ? (ushort?)Convert.ToUInt16(idObj) : null;
        }
        catch { return null; }
    }

    /// <summary>通过 syncId 查找本进程的物品镜像（服务器端用）。</summary>
    public static Item FindItemBySyncId(ushort syncId)
    {
        ResolveReflection();
        if (_tryGetById == null || _knetidType == null) return null;
        try
        {
            // knetid 是 struct，需按构造函数 knetid(ushort) 装箱后传入
            var knetidInstance = Activator.CreateInstance(_knetidType, (ushort)syncId);
            var parameters = new object[] { knetidInstance, null };
            if (!(bool)_tryGetById.Invoke(null, parameters) || parameters[1] == null) return null;
            return _itemProperty.GetValue(parameters[1]) as Item;
        }
        catch { return null; }
    }


    // === 直连 KrokMP 原始消息实现 ===

    private static Type NormalizeRawType(Type type)
    {
        return type != null && type.IsByRef ? type.GetElementType() : type;
    }

    private static bool IsUnsignedIntegerRaw(Type type)
    {
        return type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);
    }

    private static bool RawParamMatches(Type expected, Type actual)
    {
        if (expected == null) return true;
        var e = NormalizeRawType(expected);
        var a = NormalizeRawType(actual);
        if (e == a) return true;
        if (e == typeof(System.Collections.IEnumerable) &&
            typeof(System.Collections.IEnumerable).IsAssignableFrom(a)) return true;
        if (e.IsAssignableFrom(a)) return true;
        return IsUnsignedIntegerRaw(e) && IsUnsignedIntegerRaw(a);
    }

    private static MethodInfo ResolveRawServerSendToClients(Type netType, Type deliveryType, Type writerType,
        Type knetidType)
    {
        if (netType == null || deliveryType == null || writerType == null) return null;
        foreach (var method in netType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Static))
        {
            if (method.Name != "Server_SendToClients") continue;
            var parameters = method.GetParameters();
            if (parameters.Length != 3) continue;
            if (!RawParamMatches(deliveryType, parameters[0].ParameterType)) continue;
            if (!RawParamMatches(writerType, parameters[1].ParameterType)) continue;
            var third = NormalizeRawType(parameters[2].ParameterType);
            if (third == null) continue;
            if (third.IsGenericType && third.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var arg = third.GetGenericArguments()[0];
                if ((knetidType != null && arg == knetidType) || IsUnsignedIntegerRaw(arg))
                    return method;
            }
            if (third == typeof(System.Collections.IEnumerable)) return method;
        }
        return null;
    }

    private static void ResolveRawMessaging()
    {
        if (_rawResolved) return;
        _rawResolved = true;
        try
        {
            _rawNetType = AccessTools.TypeByName("KrokoshaCasualtiesMP.Net");
            _rawMpType = AccessTools.TypeByName("KrokoshaCasualtiesMP.KrokoshaScavMultiplayer");
            _rawClientMainType = AccessTools.TypeByName("KrokoshaCasualtiesMP.ClientMain");
            _rawServerMainType = AccessTools.TypeByName("KrokoshaCasualtiesMP.ServerMain");
            if (_rawNetType == null || _rawMpType == null || _rawClientMainType == null ||
                _rawServerMainType == null) return;

            var lite = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                string.Equals(a.GetName().Name, "LiteNetLib", StringComparison.OrdinalIgnoreCase));
            if (lite == null) return;
            _rawReaderType = lite.GetType("LiteNetLib.Utils.NetDataReader", false);
            _rawWriterType = lite.GetType("LiteNetLib.Utils.NetDataWriter", false);
            if (_rawReaderType == null || _rawWriterType == null) return;

            _rawCreateWriterMethod = AccessTools.Method(_rawNetType, "CreateWriter", new[] { typeof(ushort) });
            _rawClientSendMethod = _rawNetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                          BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Client_Send");
            _rawSimpleSendStringMethod = _rawMpType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                               BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Client_SendSimpleMessageToServer" &&
                                     m.GetParameters().Length == 3 &&
                                     NormalizeRawType(m.GetParameters()[0].ParameterType) == typeof(ushort) &&
                                     m.GetParameters()[1].ParameterType == typeof(string) &&
                                     m.GetParameters()[2].ParameterType == typeof(bool));
            if (_rawCreateWriterMethod == null || _rawClientSendMethod == null ||
                _rawSimpleSendStringMethod == null) return;

            _rawDeliveryMethodType = NormalizeRawType(_rawClientSendMethod.GetParameters()[0].ParameterType);
            var knetidType = AccessTools.TypeByName("KrokoshaCasualtiesMP.knetid");
            _rawServerSendToClientsMethod = ResolveRawServerSendToClients(
                _rawNetType, _rawDeliveryMethodType, _rawWriterType, knetidType);
            // KrokMP 的公开注册入口在 ServerMain / ClientMain 上，和 Skin Sync 等模组一致；
            // 它们会在后续 _RegisterServerReceivers/_RegisterClientReceivers 时统一包装。
            _rawRegisterServerReceiverMethod = _rawServerMainType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                                             BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "RegisterServerReceiver");
            _rawRegisterClientReceiverMethod = _rawClientMainType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                                             BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "RegisterClientReceiver");
            if (_rawServerSendToClientsMethod == null || _rawRegisterServerReceiverMethod == null ||
                _rawRegisterClientReceiverMethod == null) return;

            _rawReceiverDelegateType = _rawRegisterServerReceiverMethod.GetParameters()[1].ParameterType;
            var extType = AccessTools.TypeByName("KrokoshaCasualtiesMP.MyLiteNetLibExtensions");
            if (extType != null)
            {
                foreach (var method in extType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var ps = method.GetParameters();
                    if (method.Name == "Put" && ps.Length == 3 &&
                        ps[0].ParameterType == _rawWriterType &&
                        ps[1].ParameterType == typeof(string) &&
                        ps[2].ParameterType == typeof(bool))
                        _rawPutStringMethod = method;
                    if (method.Name == "Get" && ps.Length == 3 &&
                        ps[0].ParameterType == _rawReaderType &&
                        ps[1].IsOut &&
                        ps[1].ParameterType == typeof(string).MakeByRefType() &&
                        ps[2].ParameterType == typeof(bool))
                        _rawGetStringMethod = method;
                }
            }
            if (_rawPutStringMethod == null || _rawGetStringMethod == null) return;

            _rawAllClientsExceptHost = _rawServerMainType.GetProperty("AllClientIdsExceptHost",
                BindingFlags.Public | BindingFlags.Static);
            _rawReliableOrdered = _rawDeliveryMethodType != null
                ? Enum.Parse(_rawDeliveryMethodType, "ReliableOrdered")
                : null;
            if (_rawAllClientsExceptHost == null || _rawReliableOrdered == null) return;

            Plugin.Log.LogInfo("[WeaponMpSync] Raw KrokMP messaging resolved.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Raw KrokMP resolve failed: {ex.Message}");
        }
    }

    private static Delegate CreateRawReceiverDelegate(MethodInfo registerMethod, MethodInfo targetHelper)
    {
        if (registerMethod == null || targetHelper == null) return null;
        var delegateType = registerMethod.GetParameters()[1].ParameterType;
        var invoke = delegateType.GetMethod("Invoke");
        if (invoke == null) return null;
        var invokeParams = invoke.GetParameters();
        if (invokeParams.Length < 2) return null;

        var method = new DynamicMethod(
            "CUTarkovWeapon_RawReceiver",
            typeof(void),
            new[] { invokeParams[0].ParameterType, invokeParams[1].ParameterType },
            typeof(WeaponMpSync).Module,
            true);
        var il = method.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
        il.Emit(System.Reflection.Emit.OpCodes.Ldind_Ref);
        il.Emit(System.Reflection.Emit.OpCodes.Call, targetHelper);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        return method.CreateDelegate(delegateType);
    }

    private static void RegisterRawMessaging()
    {
        if (_rawRegistered) return;
        ResolveRawMessaging();
        if (_rawNetType == null || _rawRegisterServerReceiverMethod == null ||
            _rawRegisterClientReceiverMethod == null) return;
        try
        {
            var serverHelper = AccessTools.Method(typeof(WeaponMpSync), "RawServerHandle",
                new[] { typeof(object) });
            var clientHelper = AccessTools.Method(typeof(WeaponMpSync), "RawClientHandle",
                new[] { typeof(object) });
            var serverDelegate = CreateRawReceiverDelegate(_rawRegisterServerReceiverMethod, serverHelper);
            var clientDelegate = CreateRawReceiverDelegate(_rawRegisterClientReceiverMethod, clientHelper);
            if (serverDelegate == null || clientDelegate == null) return;

            _rawRegisterServerReceiverMethod.Invoke(null,
                new object[] { RawServerMsgId, serverDelegate });
            if (_rawRegisterClientReceiverMethod.GetParameters().Length >= 3)
                _rawRegisterClientReceiverMethod.Invoke(null,
                    new object[] { RawClientMsgId, clientDelegate, false });
            else
                _rawRegisterClientReceiverMethod.Invoke(null,
                    new object[] { RawClientMsgId, clientDelegate });

            _rawRegistered = true;
            Plugin.Log.LogInfo("[WeaponMpSync] Registered raw KrokMP message handlers.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Register raw handlers failed: {ex.Message}");
        }
    }

    private static void RawServerHandle(object reader)
    {
        Plugin.Log.LogInfo("[WeaponMpSync] RawServerHandle invoked.");
        var token = ReadRawJson(reader);
        if (token != null) HandleServerMessage(token);
    }

    private static void RawClientHandle(object reader)
    {
        Plugin.Log.LogInfo("[WeaponMpSync] RawClientHandle invoked.");
        var token = ReadRawJson(reader);
        if (token != null) HandleClientBroadcast(token);
    }

    private static JToken ReadRawJson(object reader)
    {
        if (reader == null || _rawGetStringMethod == null) return null;
        try
        {
            var args = new object[] { reader, null, true };
            _rawGetStringMethod.Invoke(null, args);
            var json = args[1] as string;
            if (string.IsNullOrEmpty(json)) return null;
            return JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Read raw message failed: {ex.Message}");
            return null;
        }
    }

    private static bool WriteRawJson(object writer, JObject msg)
    {
        if (writer == null || msg == null || _rawPutStringMethod == null) return false;
        try
        {
            var json = msg.ToString(Newtonsoft.Json.Formatting.None);
            _rawPutStringMethod.Invoke(null, new object[] { writer, json, true });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Write raw message failed: {ex.Message}");
            return false;
        }
    }

    private static bool SendToServerRaw(JObject msg)
    {
        ResolveRawMessaging();
        if (_rawSimpleSendStringMethod == null || _rawReliableOrdered == null) return false;
        try
        {
            var json = msg.ToString(Newtonsoft.Json.Formatting.None);
            // 使用 KrokMP 自带公开发送方法，内部使用它验证过的写入/投递方式。
            _rawSimpleSendStringMethod.Invoke(null, new object[] { RawServerMsgId, json, true });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Raw SendToServer failed: {ex.Message}");
            return false;
        }
    }

    private static bool BroadcastRaw(JObject msg)
    {
        ResolveRawMessaging();
        if (_rawCreateWriterMethod == null || _rawServerSendToClientsMethod == null ||
            _rawAllClientsExceptHost == null || _rawReliableOrdered == null) return false;
        try
        {
            var targets = _rawAllClientsExceptHost.GetValue(null, null);
            if (targets == null) return false;
            var writer = _rawCreateWriterMethod.Invoke(null, new object[] { RawClientMsgId });
            if (writer == null || !WriteRawJson(writer, msg)) return false;
            _rawServerSendToClientsMethod.Invoke(null,
                new object[] { _rawReliableOrdered, writer, targets });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Raw Broadcast failed: {ex.Message}");
            return false;
        }
    }

    // === 注册（插件初始化时调用） ===

    public static void Register()
    {
        try
        {
            MultiplayerApi.RegisterServerHandler(Channel, HandleServerMessage);
            MultiplayerApi.RegisterClientHandler(Channel, HandleClientBroadcast);
            Plugin.Log.LogInfo("[WeaponMpSync] Registered MP sync channel.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Register failed: {ex.Message}");
        }

        // CUCoreLib MultiplayerBridge 在当前 KrokMP 版本可能不可用，
        // 额外注册一套不经过 CUCoreLib 的原始 KrokMP 消息通道。
        RegisterRawMessaging();
    }

    /// <summary>主机端周期广播全量枪械自定义状态，用于重连/后加入客户端补状态。</summary>
    public static void Tick()
    {
        if (!KrokMpHelper.IsMultiplayer || !KrokMpHelper.IsHost) return;
        if (Time.time - _lastFullSyncTime < FullSyncInterval) return;
        _lastFullSyncTime = Time.time;
        try
        {
            BroadcastFullState();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Full state broadcast failed: {ex.Message}");
        }
    }

    private static void BroadcastFullState()
    {
        var holders = UnityEngine.Object.FindObjectsOfType<GunAttachmentHolder>();
        foreach (var holder in holders)
        {
            if (holder == null) continue;
            var item = holder.GetComponent<Item>();
            if (item == null) continue;
            var gs = item.GetComponent<GunScript>();
            if (gs == null) continue;
            var gsid = GetSyncId(item);
            if (gsid == null) continue;

            var msg = new JObject
            {
                ["t"] = "fs",
                ["gs"] = gsid.Value,
                ["att"] = new JArray(holder.attachmentIds ?? new List<string>()),
                ["mag"] = holder.currentMagId ?? "",
                ["hm"] = gs.hasMag,
                ["rm"] = gs.roundsInMag,
                ["ft"] = (int)gs.feedType,
                ["mc"] = gs.magCapacity,
            };
            if (holder.noBatteryAttachments != null && holder.noBatteryAttachments.Count > 0)
                msg["nb"] = new JArray(holder.noBatteryAttachments);
            if (holder.lasTacCharge < 1f) msg["lc"] = holder.lasTacCharge;
            if (holder.kleschCharge < 1f) msg["kc"] = holder.kleschCharge;
            if (holder.baldrCharge < 1f) msg["bc"] = holder.baldrCharge;
            if (holder.tblCharge < 1f) msg["tc"] = holder.tblCharge;

            // 只广播真正有自定义状态的枪，减少流量。
            bool hasState = (holder.attachmentIds != null && holder.attachmentIds.Count > 0)
                || !string.IsNullOrEmpty(holder.currentMagId)
                || gs.hasMag
                || holder.noBatteryAttachments.Count > 0
                || holder.lasTacCharge < 1f
                || holder.kleschCharge < 1f
                || holder.baldrCharge < 1f
                || holder.tblCharge < 1f;
            if (hasState) Broadcast(msg);
        }
    }

    // === 上报方法（客户端调用；主机无需上报，本地即权威） ===

    /// <summary>退弹上报：服务器更新弹匣 rounds 并创建子弹物品（同步回来）。返回是否上报成功。</summary>
    public static bool ReportUnloadRound(Item mag, int newRounds, string ammoId, Vector2 pos)
    {
        var sid = GetSyncId(mag);
        if (sid == null) return false;
        return SendToServer(new JObject
        {
            ["t"] = "ur",
            ["sid"] = sid.Value,
            ["r"] = newRounds,
            ["am"] = ammoId,
            ["x"] = pos.x,
            ["y"] = pos.y,
        });
    }

    /// <summary>刷卡开门上报：服务器扣钥匙卡耐久 + 开门 + 广播</summary>
    public static bool ReportKeycardUse(Item card, float newCondition, Vector2 doorPos)
    {
        var sid = GetSyncId(card);
        if (sid == null) return false;
        return SendToServer(new JObject
        {
            ["t"] = "kc",
            ["sid"] = sid.Value,
            ["c"] = newCondition,
            ["dx"] = doorPos.x,
            ["dy"] = doorPos.y,
        });
    }

    /// <summary>武器箱开锁上报：服务器销毁箱子+掉落 + 广播其他客户端</summary>
    public static void ReportBunkerBox(Vector2 boxPos)
    {
        // 去重：客户端本地销毁 OnDestroy 会触发上报；随后服务器广播回到本客户端时，
        // 若箱子仍在销毁流程中会再次触发 OnDestroy → 再次上报 → 无限循环。
        // 用位置+时间窗抑制重复上报（5 秒内的同一位置只上报一次）。
        var key = BoxKey(boxPos);
        var now = Time.time;
        if (_boxReportTimes.TryGetValue(key, out var last) && now - last < BoxDedupWindow) return;
        _boxReportTimes[key] = now;

        SendToServer(new JObject
        {
            ["t"] = "bb",
            ["x"] = boxPos.x,
            ["y"] = boxPos.y,
        });
    }

    /// <summary>物品能否被多人同步定位（有网络 syncId）。用于判断是否走"上报服务器"路径。</summary>
    public static bool CanSync(Item item) => GetSyncId(item).HasValue;

    /// <summary>
    /// 客户端本地消耗/销毁一个还在背包槽里的网络物品前调用，用来清除 KrokMP
    /// ItemSync 的本地槽位快照。否则 KrokMP 会发现“槽位从有物品变成空”，
    /// 并把服务器上的镜像物品当作“丢到地上”处理，产生多余的地上复制品。
    /// </summary>
    public static void SuppressLocalInventoryDrop(Item item)
    {
        if (item == null || !KrokMpHelper.IsMultiplayer || KrokMpHelper.IsHost) return;

        var syncId = GetSyncId(item);
        if (syncId == null) return;

        try
        {
            if (_itemSyncType == null)
                _itemSyncType = AccessTools.TypeByName("KrokoshaCasualtiesMP.ItemSync");
            if (_itemSyncType == null) return;
            if (_clientLastInventoryStateField == null)
                _clientLastInventoryStateField = AccessTools.Field(_itemSyncType, "Client_last_inventory_state");
            if (_clientLastInventoryStateField == null) return;

            var arr = _clientLastInventoryStateField.GetValue(null) as Array;
            if (arr == null) return;
            for (var i = 0; i < arr.Length; i++)
            {
                var syncInfo = arr.GetValue(i);
                if (syncInfo == null) continue;
                var knetidObj = _syncIdField.GetValue(syncInfo);
                if (knetidObj == null) continue;
                var idObj = _knetidIdField.GetValue(knetidObj);
                if (idObj != null && Convert.ToUInt16(idObj) == syncId.Value)
                {
                    arr.SetValue(null, i);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] SuppressLocalInventoryDrop failed: {ex.Message}");
        }
    }

    private static string BoxKey(Vector2 pos) => $"{pos.x:0.0},{pos.y:0.0}";
    private const float BoxDedupWindow = 5f;
    private static readonly Dictionary<string, float> _boxReportTimes = new(StringComparer.Ordinal);

    /// <summary>标记某位置的箱子已由远程广播处理（抑制本地再次上报）</summary>
    private static void MarkBoxHandled(Vector2 pos) => _boxReportTimes[BoxKey(pos)] = Time.time;

    /// <summary>配件安装上报：服务器端枪 attachmentIds.Add + 销毁配件镜像</summary>
    public static bool ReportAttachInstall(Item gun, string attachmentId, Item attachmentItem,
        int feedType, int magCapacity, int roundsInMag)
    {
        var gsid = GetSyncId(gun);
        if (gsid == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ReportAttachInstall: gun '{gun?.id}' has no network syncId.");
            return false;
        }
        var isid = GetSyncId(attachmentItem);
        if (isid == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ReportAttachInstall: attachment '{attachmentItem?.id}' has no network syncId.");
            return false;
        }
        var ok = SendToServer(new JObject
        {
            ["t"] = "ai",
            ["gs"] = gsid.Value,
            ["a"] = attachmentId,
            ["is"] = isid.Value,
            ["ft"] = feedType,
            ["mc"] = magCapacity,
            ["rm"] = roundsInMag,
        });
        Plugin.Log.LogInfo($"[WeaponMpSync] Client reported attach '{attachmentId}' to gun syncId={gsid.Value}, ok={ok}.");
        return ok;
    }

    /// <summary>配件卸下上报（级联卸下逐个上报）：服务器端移除 attachmentIds、创建配件物品、应用供弹状态</summary>
    public static bool ReportAttachDetach(Item gun, string detachedId, float charge, bool hadBattery,
        int feedType, int magCapacity, int roundsInMag, Vector2 spawnPos)
    {
        var gsid = GetSyncId(gun);
        if (gsid == null) return false;
        var msg = new JObject
        {
            ["t"] = "ad",
            ["gs"] = gsid.Value,
            ["a"] = detachedId,
            ["c"] = charge,
            ["ft"] = feedType,
            ["mc"] = magCapacity,
            ["rm"] = roundsInMag,
            ["x"] = spawnPos.x,
            ["y"] = spawnPos.y,
        };
        if (hadBattery) msg["hb"] = 1;
        return SendToServer(msg);
    }

    /// <summary>同步枪械当前弹匣/供弹状态（currentMagId 等自定义字段）。</summary>
    public static void SyncMagState(Item gun)
    {
        if (gun == null) return;
        var holder = gun.GetComponent<GunAttachmentHolder>();
        var gs = gun.GetComponent<GunScript>();
        var gsid = GetSyncId(gun);
        if (gsid == null) return;
        var msg = new JObject
        {
            ["t"] = "mg",
            ["gs"] = gsid.Value,
            ["mag"] = holder != null ? holder.currentMagId : "",
            ["hm"] = gs != null && gs.hasMag,
            ["rm"] = gs != null ? gs.roundsInMag : 0,
            ["ft"] = gs != null ? (int)gs.feedType : 0,
            ["mc"] = gs != null ? gs.magCapacity : 0,
        };

        if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost)
            SendToServer(msg);
        else if (KrokMpHelper.IsMultiplayer && KrokMpHelper.IsHost)
            Broadcast(msg);
    }

    /// <summary>同步战术设备（手电/激光）档位状态。</summary>
    public static void SyncTacticalState(Item gun, string attachmentId, int state)
    {
        if (gun == null || string.IsNullOrEmpty(attachmentId)) return;
        if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost)
        {
            var gsid = GetSyncId(gun);
            if (gsid == null) return;
            SendToServer(new JObject
            {
                ["t"] = "tl",
                ["gs"] = gsid.Value,
                ["a"] = attachmentId,
                ["m"] = state,
            });
        }
        else if (KrokMpHelper.IsMultiplayer && KrokMpHelper.IsHost)
        {
            BroadcastTacticalState(gun, attachmentId, state);
        }
    }

    /// <summary>
    /// 主机端安装配件后，把配件状态广播给所有非主机客户端。
    /// 客户端不需要销毁/创建物品；物品增删由 KrokMP 服务器权威同步处理，
    /// 这里只同步 KrokMP 不认识的 GunAttachmentHolder.attachmentIds 等自定义状态。
    /// </summary>
    public static void BroadcastAttachInstall(Item gun, string attachmentId,
        int feedType, int magCapacity, int roundsInMag)
    {
        var gsid = GetSyncId(gun);
        if (gsid == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] BroadcastAttachInstall: gun '{gun?.id}' has no network syncId.");
            return;
        }
        Broadcast(new JObject
        {
            ["t"] = "ai",
            ["gs"] = gsid.Value,
            ["a"] = attachmentId,
            ["ft"] = feedType,
            ["mc"] = magCapacity,
            ["rm"] = roundsInMag,
        });
        Plugin.Log.LogInfo($"[WeaponMpSync] Broadcasted attach '{attachmentId}' for gun syncId={gsid.Value}.");
    }

    private static void BroadcastTacticalState(Item gun, string attachmentId, int state)
    {
        var gsid = GetSyncId(gun);
        if (gsid == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] BroadcastTacticalState: gun '{gun?.id}' has no network syncId.");
            return;
        }
        Broadcast(new JObject
        {
            ["t"] = "tl",
            ["gs"] = gsid.Value,
            ["a"] = attachmentId,
            ["m"] = state,
        });
    }

    /// <summary>主机端卸下配件后，把配件状态广播给所有非主机客户端。</summary>
    public static void BroadcastAttachDetach(Item gun, string detachedId,
        int feedType, int magCapacity, int roundsInMag)
    {
        var gsid = GetSyncId(gun);
        if (gsid == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] BroadcastAttachDetach: gun '{gun?.id}' has no network syncId.");
            return;
        }
        Broadcast(new JObject
        {
            ["t"] = "ad",
            ["gs"] = gsid.Value,
            ["a"] = detachedId,
            ["ft"] = feedType,
            ["mc"] = magCapacity,
            ["rm"] = roundsInMag,
        });
        Plugin.Log.LogInfo($"[WeaponMpSync] Broadcasted detach '{detachedId}' for gun syncId={gsid.Value}.");
    }

    /// <summary>发送消息到服务器。返回是否成功（失败时调用方应退回本地行为，避免物品丢失）。</summary>
    private static bool SendToServer(JObject msg)
    {
        try
        {
            if (MultiplayerApi.SendToServer(Channel, msg)) return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] SendToServer failed: {ex.Message}");
        }

        // CUCoreLib 通道不可用时，走本模组自带的原始 KrokMP 消息通道。
        return SendToServerRaw(msg);
    }

    // === 服务器 handler（处理客户端上报） ===

    private static JToken HandleServerMessage(JToken payload)
    {
        try
        {
            if (payload is not JObject msg) return null;
            var type = msg.Value<string>("t");
            switch (type)
            {
                case "ur": ServerUnloadRound(msg); break;
                case "kc": ServerKeycardUse(msg); break;
                case "bb": ServerBunkerBox(msg); break;
                case "ai": ServerAttachInstall(msg); break;
                case "ad": ServerAttachDetach(msg); break;
                case "tl": ServerTacticalState(msg); break;
                case "mg": ServerMagState(msg); break;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Server handler error: {ex.Message}");
        }
        return null;
    }

    /// <summary>服务器端退弹：更新弹匣 rounds + 创建子弹（Utils.Create 自动同步）</summary>
    private static void ServerUnloadRound(JObject msg)
    {
        var mag = FindItemBySyncId(msg.Value<ushort>("sid"));
        var ammoId = msg.Value<string>("am");
        var newRounds = msg.Value<int>("r");
        var pos = new Vector2(msg.Value<float>("x"), msg.Value<float>("y"));

        if (mag != null)
        {
            var ammo = mag.GetComponent<AmmoScript>();
            if (ammo != null) ammo.rounds = newRounds;
        }

        if (string.IsNullOrEmpty(ammoId)) return;
        var go = Utils.Create(ammoId, pos, 0f);
        if (go != null)
        {
            go.AddComponent<FreshItemDrop>();
            Plugin.Log.LogInfo($"[WeaponMpSync] Server spawned round '{ammoId}' for client unload.");
        }
    }

    /// <summary>服务器端刷卡：扣钥匙卡耐久 + 开门 + 广播其他客户端</summary>
    private static void ServerKeycardUse(JObject msg)
    {
        var card = FindItemBySyncId(msg.Value<ushort>("sid"));
        var cond = msg.Value<float>("c");
        var doorPos = new Vector2(msg.Value<float>("dx"), msg.Value<float>("dy"));

        if (card != null)
            card.SetCondition(Mathf.Clamp(cond, 0f, 1f));

        BackgroundifyDoorAt(doorPos);
        Broadcast(new JObject { ["t"] = "bd", ["x"] = doorPos.x, ["y"] = doorPos.y });
    }

    /// <summary>服务器端武器箱开锁：销毁箱子（触发主机端掉落）+ 广播其他客户端</summary>
    private static void ServerBunkerBox(JObject msg)
    {
        var boxPos = new Vector2(msg.Value<float>("x"), msg.Value<float>("y"));
        DestroyBoxAt(boxPos);
        Broadcast(new JObject { ["t"] = "bb", ["x"] = boxPos.x, ["y"] = boxPos.y });
    }

    /// <summary>服务器端配件安装：枪镜像 attachmentIds.Add + 销毁配件镜像</summary>
    private static void ServerAttachInstall(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        var attachmentId = msg.Value<string>("a");
        var attachmentItem = FindItemBySyncId(msg.Value<ushort>("is"));
        if (gun == null || attachmentId == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ServerAttachInstall: gun={(gun == null ? "null" : gun.id)}, attachment={attachmentId ?? "null"}.");
            return;
        }

        // 服务器镜像枪可能还没有 GunAttachmentHolder（只有客户端本地在安装时才会
        // AddComponent）。如果不在这里补建，服务端就永远无法记录客户端安装的配件。
        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            holder = gun.gameObject.AddComponent<GunAttachmentHolder>();
        if (!holder.attachmentIds.Contains(attachmentId))
            holder.attachmentIds.Add(attachmentId);

        // 镜像客户端本地安装时的 SKS 供弹方式切换，保证主机上的 SKS 也是正确的
        // Direct/Mag 状态（否则主机后续开枪/装弹会按旧模式处理）。
        var gunScript = gun.GetComponent<GunScript>();
        if (gunScript != null)
        {
            if (string.Equals(attachmentId, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                gunScript.feedType = GunScript.FeedType.Mag;
                gunScript.magCapacity = SksA5MagItemSystem.MaxRounds;
                gunScript.roundsInMag = 0;
            }
            else if (string.Equals(attachmentId, SksIntegralMagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                gunScript.feedType = GunScript.FeedType.Direct;
                gunScript.magCapacity = SKSItemSystem.MagCapacity;
                gunScript.roundsInMag = 0;
            }
        }

        // 刷新主机端枪械合成贴图，让主机上看到的枪也有配件外观。
        try { SuppressorSystem.UpdateSuppressorVisual(gun); }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Refresh visual after attach failed: {ex.Message}"); }

        if (attachmentItem != null)
        {
            try { UnityEngine.Object.Destroy(attachmentItem.gameObject); }
            catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Destroy attachment mirror failed: {ex.Message}"); }
        }

        // 客户端上报安装后，把状态再广播给其他非主机客户端。
        try
        {
            BroadcastAttachInstall(gun, attachmentId,
                gunScript != null ? (int)gunScript.feedType : 0,
                gunScript != null ? gunScript.magCapacity : 0,
                gunScript != null ? gunScript.roundsInMag : 0);
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Broadcast attach failed: {ex.Message}"); }

        Plugin.Log.LogInfo($"[WeaponMpSync] Server applied attach '{attachmentId}'.");
    }

    /// <summary>服务器端配件卸下：移除 attachmentIds + 创建配件物品（同步回来）+ 应用供弹状态</summary>
    private static void ServerAttachDetach(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        if (gun == null)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ServerAttachDetach: gun not found for syncId={msg.Value<ushort>("gs")}.");
            return;
        }

        var id = msg.Value<string>("a");
        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            holder = gun.gameObject.AddComponent<GunAttachmentHolder>();
        if (id != null)
            holder.attachmentIds.Remove(id);

        // SKS 卸下 SKS-A5 弹匣：恢复默认 10 发弹仓改件（与客户端本地逻辑一致）
        if (id != null && string.Equals(id, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && !holder.attachmentIds.Contains(SksIntegralMagItemSystem.ItemKey))
        {
            holder.attachmentIds.Add(SksIntegralMagItemSystem.ItemKey);
        }

        if (string.IsNullOrEmpty(id)) return;

        // 服务器端创建卸下的配件物品（Utils.Create 自动同步到所有客户端，包括发起客户端）
        // 位置必须使用客户端上报的位置，而不是主机 PlayerCamera 的位置。
        var spawnPos = new Vector2(msg.Value<float>("x"), msg.Value<float>("y"));
        var spawned = Utils.Create(id, spawnPos, 0f);
        if (spawned != null && IsTacticalLightId(id))
        {
            // 战术手电电量写回（延迟写回，Utils.Create 内部流程会覆盖 condition）。
            // 只有战术灯才挂 TacticalLightDetachedCharge；普通配件挂这个会把耐久改成 1%。
            var charge = msg.Value<float>("c");
            var setter = spawned.AddComponent<TacticalLightDetachedCharge>();
            setter.lightId = id;
            setter.charge = Mathf.Max(charge, 0.01f);
            setter.hadBattery = msg["hb"] != null;
        }

        // 应用枪械供弹状态（SKS 弹匣切换等）
        var gunScript = gun.GetComponent<GunScript>();
        if (gunScript != null)
        {
            gunScript.feedType = (GunScript.FeedType)msg.Value<int>("ft");
            gunScript.magCapacity = msg.Value<int>("mc");
            gunScript.roundsInMag = msg.Value<int>("rm");
        }

        // 刷新主机端枪械合成贴图，让主机端显示的卸下状态与客户端一致。
        try { SuppressorSystem.UpdateSuppressorVisual(gun); }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Refresh visual after detach failed: {ex.Message}"); }

        // 客户端上报卸下后，把状态广播给其他非主机客户端。
        try
        {
            BroadcastAttachDetach(gun, id,
                gunScript != null ? (int)gunScript.feedType : 0,
                gunScript != null ? gunScript.magCapacity : 0,
                gunScript != null ? gunScript.roundsInMag : 0);
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Broadcast detach failed: {ex.Message}"); }

        Plugin.Log.LogInfo($"[WeaponMpSync] Server applied detach '{id}'.");
    }

    private static bool IsTacticalLightId(string id)
    {
        return string.Equals(id, LasTac2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, Klesch2UItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, BaldrProItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, TblItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
    }

    private static void ServerMagState(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        if (gun == null) return;
        ApplyMagState(gun, msg);
        Broadcast(msg);
    }

    private static void ClientMagState(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        if (gun == null) return;
        ApplyMagState(gun, msg);
    }

    private static void ClientFullState(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        if (gun == null) return;
        try
        {
            var holder = gun.GetComponent<GunAttachmentHolder>();
            if (holder == null)
                holder = gun.gameObject.AddComponent<GunAttachmentHolder>();
            if (msg["att"] is JArray arr)
                holder.attachmentIds = arr.OfType<JValue>()
                    .Select(v => v.Value<string>())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            holder.currentMagId = msg.Value<string>("mag") ?? "";

            if (msg["nb"] is JArray nbArr)
            {
                holder.noBatteryAttachments.Clear();
                foreach (var tok in nbArr.OfType<JValue>())
                {
                    var id = tok.Value<string>();
                    if (!string.IsNullOrEmpty(id)) holder.noBatteryAttachments.Add(id);
                }
            }
            if (msg["lc"] != null) holder.lasTacCharge = msg.Value<float>("lc");
            if (msg["kc"] != null) holder.kleschCharge = msg.Value<float>("kc");
            if (msg["bc"] != null) holder.baldrCharge = msg.Value<float>("bc");
            if (msg["tc"] != null) holder.tblCharge = msg.Value<float>("tc");

            var gs = gun.GetComponent<GunScript>();
            if (gs != null)
            {
                gs.hasMag = msg.Value<bool>("hm");
                gs.roundsInMag = msg.Value<int>("rm");
                gs.feedType = (GunScript.FeedType)msg.Value<int>("ft");
                gs.magCapacity = msg.Value<int>("mc");
            }

            // 恢复战术灯控制器，保证其他客户端有灯光组件。
            if (holder.attachmentIds != null)
            {
                if (holder.attachmentIds.Contains(LasTac2ItemSystem.ItemKey))
                    LasTac2Controller.Attach(gun, holder.lasTacCharge);
                if (holder.attachmentIds.Contains(Klesch2UItemSystem.ItemKey))
                    Klesch2UController.Attach(gun);
                if (holder.attachmentIds.Contains(BaldrProItemSystem.ItemKey))
                    BaldrProController.Attach(gun);
                if (holder.attachmentIds.Contains(TblItemSystem.ItemKey))
                    TblController.Attach(gun);
            }

            SuppressorSystem.UpdateSuppressorVisual(gun);
            SuppressorSystem.UpdateMagVisual(gun);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ClientFullState failed: {ex.Message}");
        }
    }

    private static void ApplyMagState(Item gun, JObject msg)
    {
        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            holder = gun.gameObject.AddComponent<GunAttachmentHolder>();
        holder.currentMagId = msg.Value<string>("mag") ?? "";
        var gs = gun.GetComponent<GunScript>();
        if (gs != null)
        {
            gs.hasMag = msg.Value<bool>("hm");
            gs.roundsInMag = msg.Value<int>("rm");
            gs.feedType = (GunScript.FeedType)msg.Value<int>("ft");
            gs.magCapacity = msg.Value<int>("mc");
        }
        try { SuppressorSystem.UpdateMagVisual(gun); }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Mag visual refresh failed: {ex.Message}"); }
    }

    private static void ServerTacticalState(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        if (gun == null) return;
        var id = msg.Value<string>("a");
        var state = msg.Value<int>("m");
        ApplyTacticalState(gun, id, state);
        BroadcastTacticalState(gun, id, state);
    }

    private static void ClientTacticalState(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        if (gun == null) return;
        ApplyTacticalState(gun, msg.Value<string>("a"), msg.Value<int>("m"));
    }

    private static void ApplyTacticalState(Item gun, string id, int state)
    {
        if (gun == null || string.IsNullOrEmpty(id)) return;
        try
        {
            if (string.Equals(id, LasTac2ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var ctrl = gun.GetComponent<LasTac2Controller>();
                if (ctrl == null) ctrl = LasTac2Controller.Attach(gun, 1f);
                ctrl.SetNetworkMode(state);
            }
            else if (string.Equals(id, Klesch2UItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var ctrl = gun.GetComponent<Klesch2UController>();
                if (ctrl == null) ctrl = Klesch2UController.Attach(gun);
                ctrl.SetNetworkOn(state != 0);
            }
            else if (string.Equals(id, BaldrProItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var ctrl = gun.GetComponent<BaldrProController>();
                if (ctrl == null) ctrl = BaldrProController.Attach(gun);
                ctrl.SetNetworkMode(state);
            }
            else if (string.Equals(id, TblItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                var ctrl = gun.GetComponent<TblController>();
                if (ctrl == null) ctrl = TblController.Attach(gun);
                ctrl.SetNetworkOn(state != 0);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ApplyTacticalState failed: {ex.Message}");
        }
    }

    // === 客户端广播应用（服务器把主机/其他客户端的配件状态广播到各客户端） ===

    private static void ClientAttachInstall(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        var id = msg.Value<string>("a");
        if (gun == null || string.IsNullOrEmpty(id))
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ClientAttachInstall: cannot find local gun for syncId={msg.Value<ushort>("gs")}.");
            return;
        }

        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            holder = gun.gameObject.AddComponent<GunAttachmentHolder>();
        if (!holder.attachmentIds.Contains(id))
            holder.attachmentIds.Add(id);

        var gunScript = gun.GetComponent<GunScript>();
        if (gunScript != null)
        {
            gunScript.feedType = (GunScript.FeedType)msg.Value<int>("ft");
            gunScript.magCapacity = msg.Value<int>("mc");
            gunScript.roundsInMag = msg.Value<int>("rm");
        }

        Plugin.Log.LogInfo($"[WeaponMpSync] Client applied broadcast attach '{id}' to gun syncId={msg.Value<ushort>("gs")}.");

        try { SuppressorSystem.UpdateSuppressorVisual(gun); }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Client visual refresh after attach failed: {ex.Message}"); }
    }

    private static void ClientAttachDetach(JObject msg)
    {
        var gun = FindItemBySyncId(msg.Value<ushort>("gs"));
        var id = msg.Value<string>("a");
        if (gun == null || string.IsNullOrEmpty(id))
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] ClientAttachDetach: cannot find local gun for syncId={msg.Value<ushort>("gs")}.");
            return;
        }

        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            holder = gun.gameObject.AddComponent<GunAttachmentHolder>();
        holder.attachmentIds.Remove(id);

        // 与本地卸下 SKS-A5 的逻辑保持一致：恢复默认 10 发弹仓改件。
        if (string.Equals(id, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase)
            && !holder.attachmentIds.Contains(SksIntegralMagItemSystem.ItemKey))
        {
            holder.attachmentIds.Add(SksIntegralMagItemSystem.ItemKey);
        }

        var gunScript = gun.GetComponent<GunScript>();
        if (gunScript != null)
        {
            gunScript.feedType = (GunScript.FeedType)msg.Value<int>("ft");
            gunScript.magCapacity = msg.Value<int>("mc");
            gunScript.roundsInMag = msg.Value<int>("rm");
        }

        Plugin.Log.LogInfo($"[WeaponMpSync] Client applied broadcast detach '{id}' from gun syncId={msg.Value<ushort>("gs")}.");

        try { SuppressorSystem.UpdateSuppressorVisual(gun); }
        catch (Exception ex) { Plugin.Log.LogWarning($"[WeaponMpSync] Client visual refresh after detach failed: {ex.Message}"); }
    }

    // === 客户端广播 handler（接收服务器广播的建筑操作） ===

    private static void HandleClientBroadcast(JToken payload)
    {
        try
        {
            // 主机不处理广播：Broadcast 默认 includeHost=false（只发非主机客户端），
            // 且主机端已在服务器 handler 中直接执行过。此检查是冗余安全网。
            if (KrokMpHelper.IsHost) return;
            if (payload is not JObject msg) return;

            switch (msg.Value<string>("t"))
            {
                case "bd":
                    BackgroundifyDoorAt(new Vector2(msg.Value<float>("x"), msg.Value<float>("y")));
                    break;
                case "bb":
                    var boxPos = new Vector2(msg.Value<float>("x"), msg.Value<float>("y"));
                    // 先标记已处理：防止本地 OnDestroy 再次上报触发循环
                    MarkBoxHandled(boxPos);
                    DestroyBoxAt(boxPos);
                    break;
                case "ai":
                    ClientAttachInstall(msg);
                    break;
                case "ad":
                    ClientAttachDetach(msg);
                    break;
                case "tl":
                    ClientTacticalState(msg);
                    break;
                case "mg":
                    ClientMagState(msg);
                    break;
                case "fs":
                    ClientFullState(msg);
                    break;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Client broadcast handler error: {ex.Message}");
        }
    }

    /// <summary>广播给所有非主机客户端（includeHost=false）。主机端由服务器 handler 直接执行。</summary>
    private static void Broadcast(JObject msg)
    {
        try
        {
            if (MultiplayerApi.Broadcast(Channel, msg, includeHost: false)) return;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[WeaponMpSync] Broadcast failed: {ex.Message}");
        }

        // CUCoreLib 通道不可用时，走本模组自带的原始 KrokMP 消息广播。
        BroadcastRaw(msg);
    }

    // === 建筑定位辅助（两端世界种子一致，位置可定位） ===

    private static void BackgroundifyDoorAt(Vector2 pos)
    {
        var door = FindBuildingAt(pos, WeaponCacheBunker.WeaponCacheDoorId);
        if (door != null)
        {
            door.Backgroundify();
            Plugin.Log.LogInfo("[WeaponMpSync] Door backgroundified.");
        }
    }

    private static void DestroyBoxAt(Vector2 pos)
    {
        var box = FindBuildingAt(pos, WeaponCacheBunker.WeaponCacheBoxId);
        if (box != null)
        {
            box.health = 0f; // 触发原版销毁逻辑（主机端 OnDestroy 掉落，客户端跳过）
            Plugin.Log.LogInfo("[WeaponMpSync] Box destroyed.");
        }
    }

    private static BuildingEntity FindBuildingAt(Vector2 pos, string buildingId)
    {
        var hits = Physics2D.OverlapCircleAll(pos, 1.5f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.TryGetComponent<BuildingEntity>(out var building)) continue;
            if (building.id != buildingId) continue;
            if ((Vector2)building.transform.position != pos) continue; // 精确位置匹配
            return building;
        }
        // 精确匹配失败时退回最近匹配
        BuildingEntity closest = null;
        var closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.TryGetComponent<BuildingEntity>(out var building)) continue;
            if (building.id != buildingId) continue;
            var dist = Vector2.Distance(building.transform.position, pos);
            if (dist < closestDist) { closestDist = dist; closest = building; }
        }
        return closest;
    }
}
