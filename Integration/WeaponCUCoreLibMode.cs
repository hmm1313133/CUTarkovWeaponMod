using System;
using System.Collections.Generic;
using System.Reflection;
using CUCoreLib.Data;
using CUCoreLib.Helpers;
using CUCoreLib.Registries;
using CUCoreLib.Saving;
using CUTarkovMedicalMod.Framework;
using CUTarkovWeaponMod.Framework;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// CUCoreLib 集成模式（CUCoreLib 为硬依赖）。
///
/// - 不注册 QoLSaveFix，由 CUCoreLib CustomInstantiate 原生处理存档加载
/// - 构建 CustomItemInfo 注册到 CUCoreLib ItemRegistry
/// - 注册 IItemSaveProvider 持久化武器运行时状态（弹药数、弹匣状态）
///
/// 武器特有问题及解决方案：
/// CUCoreLib ChooseTemplateId 只认识 waterbottle/bandage/smallpack/flashlight 模板，
/// 不认识 rifle/pistol/shotgun 等武器模板。
/// 解决：在 OnItemsSetup 中预先创建正确的武器模板（基于 rifle/pistol/shotgun 等基础预制体），
/// 通过反射注入 CustomInstantiate._templateCache，使 GetOrCreateTemplate 直接返回正确模板，
/// 绕过 ChooseTemplateId。
///
/// 存档加载流程：
/// 1. CUCoreLib Transpiler 拦截 Resources.Load("akm") -> null -> GetOrCreateTemplate("akm")
/// 2. GetOrCreateTemplate 在 _templateCache 找到预创建的 rifle 模板 -> 返回
/// 3. InstantiateSavedItem 创建实例并激活
/// 4. Item.Start() -> CUCoreLib ApplyCustomItemRuntime（应用图标/缩放）
/// 5. SaveCoordinator.ApplyPendingRestore -> WeaponItemSaveProvider.Restore
///    -> 调用 ConfigureCustomItem 设置 GunScript 等 -> 恢复弹药/弹匣状态
///
/// KrokMP 多人联机图标修复：
/// 注册到 ItemRegistry 时通过反射调用各 ItemSystem 类的 TryLoadIcon() 获取图标 Sprite。
/// 客户端通过 KrokMP 同步创建的物品在 Item.Start() 时由 ApplyCustomItemRuntime
/// 用此图标替换精灵图，避免显示为原版步枪/弹匣外观。
/// 同时将图标设置到预创建模板的 SpriteRenderer 上作为即时外观。
/// </summary>
public sealed class WeaponCUCoreLibMode
{
    public void Initialize(Harmony harmony)
    {
        // 注册武器运行时状态保存提供者（弹药数、弹匣状态）
        SaveRegistry.RegisterItemProvider("cutarkovweapon.runtime", new WeaponItemSaveProvider());
        Plugin.Log.LogInfo("[WeaponCUCoreLib] Registered WeaponItemSaveProvider (ammo/mag state).");
    }

    public void OnItemsSetup()
    {
        if (Item.GlobalItems == null) return;

        // 获取 CustomInstantiate._templateCache（private static）
        var templateCacheField = typeof(CustomInstantiate).GetField(
            "_templateCache", BindingFlags.NonPublic | BindingFlags.Static);
        var templateCache = templateCacheField?.GetValue(null) as Dictionary<string, GameObject>;

        // 诊断日志：验证反射是否成功
        if (templateCacheField == null)
        {
            Plugin.Log.LogError("[WeaponCUCoreLib] CRITICAL: CustomInstantiate._templateCache field NOT FOUND via reflection! Template injection will fail, items will appear as bandage.");
        }
        else if (templateCache == null)
        {
            Plugin.Log.LogError("[WeaponCUCoreLib] CRITICAL: _templateCache is null! Template injection will fail.");
        }
        else
        {
            Plugin.Log.LogInfo($"[WeaponCUCoreLib] _templateCache resolved OK (current entries: {templateCache.Count}).");
        }

        // 构建 itemId -> ItemSystem 类型的映射，用于反射调用 TryLoadIcon()
        var itemTypeMap = BuildItemTypeMap();

        // 设置模板预创建标志，防止 CustomSpawnPatch 的世界生成逻辑在模板 Instantiate 时触发
        CustomSpawnPatch.InTemplateSetup = true;

        var registered = 0;
        var templatesInjected = 0;
        foreach (var itemId in WeaponItemRegistration.WeaponItemIds)
        {
            try
            {
                if (!Item.GlobalItems.ContainsKey(itemId)) continue;
                var plainInfo = Item.GlobalItems[itemId];
                if (plainInfo == null) continue;

                // --- 1. 构建 CustomItemInfo ---
                var customInfo = new CustomItemInfo();
                foreach (var field in GetPublicInstanceFields(plainInfo.GetType()))
                    field.SetValue(customInfo, field.GetValue(plainInfo));
                customInfo.capacity = 0;
                customInfo.autoFill = false;
                customInfo.defaultContents = null;

                // --- 2. 解析图标 ---
                Sprite icon = null;
                if (itemTypeMap.TryGetValue(itemId, out var itemSystemType))
                {
                    icon = ResolveIcon(itemSystemType, itemId);
                }
                if (icon == null)
                    Plugin.Log.LogWarning($"[WeaponCUCoreLib] Icon is null for '{itemId}', will keep base prefab sprite.");

                // --- 3. 预创建正确的武器模板 ---
                if (templateCache != null &&
                    ConsoleSpawnPatch.CustomItemPrefabs.TryGetValue(itemId, out var basePrefabId))
                {
                    var basePrefab = Resources.Load<GameObject>(basePrefabId);
                    if (basePrefab != null)
                    {
                        var template = Object.Instantiate(basePrefab);
                        template.SetActive(false);
                        template.name = itemId;
                        Object.DontDestroyOnLoad(template);

                        var itemComp = template.GetComponent<Item>();
                        if (itemComp != null) itemComp.id = itemId;

                        // 调用 ConfigureCustomItem 设置全部武器字段：
                        // - GunScript: normalSprite/rackedSprite/normalSpriteNoMag/rackedSpriteNoMag
                        //   （GunScript.Update 会用这些字段覆盖 sr.sprite，不设置则保持原版贴图）
                        // - GunScript: magCapacity/damage/spread/barrel位置等
                        // - AmmoScript: itemType/maxRounds 等
                        // - SpriteRenderer.sprite + collider
                        // 这样 KrokMP 同步创建的实例从模板继承全部正确配置。
                        try
                        {
                            var request = new MedicalGrantRequest(itemId, itemId, 1, "TemplateSetup", basePrefabId);
                            ConsoleSpawnPatch.ConfigureCustomItem(itemComp, request);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogWarning(
                                $"[WeaponCUCoreLib] ConfigureCustomItem on template '{itemId}' failed: {ex.Message}");
                        }

                        templateCache[itemId] = template;
                        templatesInjected++;
                    }
                    else
                    {
                        Plugin.Log.LogWarning(
                            $"[WeaponCUCoreLib] Base prefab '{basePrefabId}' not found for '{itemId}'.");
                    }
                }

                // --- 4. 注册到 CUCoreLib ItemRegistry ---
                // 对于枪械/弹匣/弹药/近战：不传 icon，回退到 sr.sprite
                // 对于护甲/胸挂：传 icon（背包缩略图），WornSprite 由 RegisterWithCUCoreLib 设置
                Sprite registerIcon = null;
                if (itemId == MBSSItemSystem.ItemKey)
                {
                    registerIcon = MBSSItemSystem.TryLoadIconPublic();
                    MBSSItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == TV115ItemSystem.ItemKey)
                {
                    registerIcon = TV115ItemSystem.TryLoadIconPublic();
                    TV115ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == TV110ItemSystem.ItemKey)
                {
                    registerIcon = TV110ItemSystem.TryLoadIconPublic();
                    TV110ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SPPCV2ItemSystem.ItemKey)
                {
                    registerIcon = SPPCV2ItemSystem.TryLoadIconPublic();
                    SPPCV2ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == MK4AItemSystem.ItemKey)
                {
                    registerIcon = MK4AItemSystem.TryLoadIconPublic();
                    MK4AItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SiegeRItemSystem.ItemKey)
                {
                    registerIcon = SiegeRItemSystem.TryLoadIconPublic();
                    SiegeRItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SixB516ItemSystem.ItemKey)
                {
                    registerIcon = SixB516ItemSystem.TryLoadIconPublic();
                    SixB516ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == TTSKItemSystem.ItemKey)
                {
                    registerIcon = TTSKItemSystem.TryLoadIconPublic();
                    TTSKItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == AVSTEItemSystem.ItemKey)
                {
                    registerIcon = AVSTEItemSystem.TryLoadIconPublic();
                    AVSTEItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == LV119ItemSystem.ItemKey)
                {
                    registerIcon = LV119ItemSystem.TryLoadIconPublic();
                    LV119ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SixB45ItemSystem.ItemKey)
                {
                    registerIcon = SixB45ItemSystem.TryLoadIconPublic();
                    SixB45ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == IDEAItemSystem.ItemKey)
                {
                    registerIcon = IDEAItemSystem.TryLoadIconPublic();
                    IDEAItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == BankRobberItemSystem.ItemKey)
                {
                    registerIcon = BankRobberItemSystem.TryLoadIconPublic();
                    BankRobberItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == Type56ItemSystem.ItemKey)
                {
                    registerIcon = Type56ItemSystem.TryLoadIconPublic();
                    Type56ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == WTChestRigItemSystem.ItemKey)
                {
                    registerIcon = WTChestRigItemSystem.TryLoadIconPublic();
                    WTChestRigItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == LBCRItemSystem.ItemKey)
                {
                    registerIcon = LBCRItemSystem.TryLoadIconPublic();
                    LBCRItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == CommandoItemSystem.ItemKey)
                {
                    registerIcon = CommandoItemSystem.TryLoadIconPublic();
                    CommandoItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == UmkaItemSystem.ItemKey)
                {
                    registerIcon = UmkaItemSystem.TryLoadIconPublic();
                    UmkaItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == BlackRockItemSystem.ItemKey)
                {
                    registerIcon = BlackRockItemSystem.TryLoadIconPublic();
                    BlackRockItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == PACAItemSystem.ItemKey)
                {
                    registerIcon = PACAItemSystem.TryLoadIconPublic();
                    PACAItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == MFUNItemSystem.ItemKey)
                {
                    registerIcon = MFUNItemSystem.TryLoadIconPublic();
                    MFUNItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == DRDItemSystem.ItemKey)
                {
                    registerIcon = DRDItemSystem.TryLoadIconPublic();
                    DRDItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == ThorItemSystem.ItemKey)
                {
                    registerIcon = ThorItemSystem.TryLoadIconPublic();
                    ThorItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == TrooperItemSystem.ItemKey)
                {
                    registerIcon = TrooperItemSystem.TryLoadIconPublic();
                    TrooperItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SixB13ItemSystem.ItemKey)
                {
                    registerIcon = SixB13ItemSystem.TryLoadIconPublic();
                    SixB13ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == HPCItemSystem.ItemKey)
                {
                    registerIcon = HPCItemSystem.TryLoadIconPublic();
                    HPCItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == GzhelKItemSystem.ItemKey)
                {
                    registerIcon = GzhelKItemSystem.TryLoadIconPublic();
                    GzhelKItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == RedutT5ItemSystem.ItemKey)
                {
                    registerIcon = RedutT5ItemSystem.TryLoadIconPublic();
                    RedutT5ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SlickItemSystem.ItemKey)
                {
                    registerIcon = SlickItemSystem.TryLoadIconPublic();
                    SlickItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == HGridItemSystem.ItemKey)
                {
                    registerIcon = HGridItemSystem.TryLoadIconPublic();
                    HGridItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SixB43ItemSystem.ItemKey)
                {
                    registerIcon = SixB43ItemSystem.TryLoadIconPublic();
                    SixB43ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == ArmorPlateItemSystem.CheapPlateKey)
                {
                    registerIcon = ArmorPlateItemSystem.TryLoadCheapIconPublic();
                    ArmorPlateItemSystem.RegisterCheapPlateWithCUCoreLib(customInfo);
                }
                else if (itemId == ArmorPlateItemSystem.AdvancedPlateKey)
                {
                    registerIcon = ArmorPlateItemSystem.TryLoadAdvancedIconPublic();
                    ArmorPlateItemSystem.RegisterAdvancedPlateWithCUCoreLib(customInfo);
                }
                else if (itemId == RysTItemSystem.ItemKey)
                {
                    registerIcon = RysTItemSystem.TryLoadIconPublic();
                    RysTItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == ExfilItemSystem.ItemKey)
                {
                    registerIcon = ExfilItemSystem.TryLoadIconPublic();
                    ExfilItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == UlachItemSystem.ItemKey)
                {
                    registerIcon = UlachItemSystem.TryLoadIconPublic();
                    UlachItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == B47ItemSystem.ItemKey)
                {
                    registerIcon = B47ItemSystem.TryLoadIconPublic();
                    B47ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == Ssh68ItemSystem.ItemKey)
                {
                    registerIcon = Ssh68ItemSystem.TryLoadIconPublic();
                    Ssh68ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == CalmanItemSystem.ItemKey)
                {
                    registerIcon = CalmanItemSystem.TryLoadIconPublic();
                    CalmanItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == LK3FItemSystem.ItemKey)
                {
                    registerIcon = LK3FItemSystem.TryLoadIconPublic();
                    LK3FItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == FastMtItemSystem.ItemKey)
                {
                    registerIcon = FastMtItemSystem.TryLoadIconPublic();
                    FastMtItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == Pvs14ItemSystem.ItemKey)
                {
                    registerIcon = Pvs14ItemSystem.TryLoadIconPublic();
                    Pvs14ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == Gpnvg18ItemSystem.ItemKey)
                {
                    registerIcon = Gpnvg18ItemSystem.TryLoadIconPublic();
                    Gpnvg18ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == Pvs31aItemSystem.ItemKey)
                {
                    registerIcon = Pvs31aItemSystem.TryLoadIconPublic();
                    Pvs31aItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == ReadyPackItemSystem.ItemKey)
                {
                    registerIcon = ReadyPackItemSystem.TryLoadIconPublic();
                    ReadyPackItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == PartizanItemSystem.ItemKey)
                {
                    registerIcon = PartizanItemSystem.TryLoadIconPublic();
                    PartizanItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == DayPackItemSystem.ItemKey)
                {
                    registerIcon = DayPackItemSystem.TryLoadIconPublic();
                    DayPackItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == BerkutItemSystem.ItemKey)
                {
                    registerIcon = BerkutItemSystem.TryLoadIconPublic();
                    BerkutItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == ScavPackItemSystem.ItemKey)
                {
                    registerIcon = ScavPackItemSystem.TryLoadIconPublic();
                    ScavPackItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == MysteryRanch2DayItemSystem.ItemKey)
                {
                    registerIcon = MysteryRanch2DayItemSystem.TryLoadIconPublic();
                    MysteryRanch2DayItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == PilgrimItemSystem.ItemKey)
                {
                    registerIcon = PilgrimItemSystem.TryLoadIconPublic();
                    PilgrimItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SsoAttack2ItemSystem.ItemKey)
                {
                    registerIcon = SsoAttack2ItemSystem.TryLoadIconPublic();
                    SsoAttack2ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == SH118ItemSystem.ItemKey)
                {
                    registerIcon = SH118ItemSystem.TryLoadIconPublic();
                    SH118ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                else if (itemId == LBT2670ItemSystem.ItemKey)
                {
                    registerIcon = LBT2670ItemSystem.TryLoadIconPublic();
                    LBT2670ItemSystem.RegisterWithCUCoreLib(customInfo);
                }
                ItemRegistry.Register(itemId, customInfo, registerIcon);
                registered++;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[WeaponCUCoreLib] Failed to register item '{itemId}': {ex.Message}");
            }
        }

        CustomSpawnPatch.InTemplateSetup = false;

        // 验证模板缓存注入结果
        if (templateCache != null)
        {
            var sampleId = AKMItemSystem.ItemKey;
            var hasSample = templateCache.ContainsKey(sampleId);
            Plugin.Log.LogInfo(
                $"[WeaponCUCoreLib] Registered {registered} items, injected {templatesInjected} templates. Cache now has {templateCache.Count} entries. Sample '{sampleId}' in cache: {hasSample}.");
        }
        else
        {
            Plugin.Log.LogError(
                $"[WeaponCUCoreLib] Registered {registered} items but templateCache is null - 0 templates injected. ALL weapon items will appear as bandage!");
        }
    }

    /// <summary>
    /// 遍历类型层级（从 type 到 ItemInfo）获取所有公共实例字段，去重。
    /// 与 CUCoreLib ItemRegistry.ToCustomItemInfo 的 GetPublicInstanceFields 逻辑一致。
    /// </summary>
    private static IEnumerable<FieldInfo> GetPublicInstanceFields(Type type)
    {
        var seen = new HashSet<string>();
        for (var current = type;
             current != null && typeof(ItemInfo).IsAssignableFrom(current);
             current = current.BaseType)
            foreach (var field in current.GetFields(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (seen.Add(field.Name))
                    yield return field;
    }

    /// <summary>
    /// 扫描武器模组程序集中所有带有 public const string ItemKey 字段的类型，
    /// 构建 itemId -> Type 映射，用于反射调用 TryLoadIcon()。
    /// </summary>
    private static Dictionary<string, Type> BuildItemTypeMap()
    {
        var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(WeaponCUCoreLibMode).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            var keyField = type.GetField("ItemKey",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (keyField == null || !keyField.IsLiteral) continue;
            var itemId = keyField.GetRawConstantValue() as string;
            if (!string.IsNullOrEmpty(itemId))
                map[itemId] = type;
        }
        return map;
    }

    /// <summary>
    /// 通过反射调用 ItemSystem 类的 TryLoadIcon() 方法获取图标 Sprite。
    /// 镜像医疗模组 MedicalItemDef.ResolveIcon() 的实现。
    /// </summary>
    private static Sprite ResolveIcon(Type itemSystemType, string itemId)
    {
        try
        {
            var method = itemSystemType.GetMethod("TryLoadIcon",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method?.Invoke(null, null) as Sprite;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(
                $"[WeaponCUCoreLib] Failed to resolve icon for '{itemId}': {ex.Message}");
            return null;
        }
    }
}
