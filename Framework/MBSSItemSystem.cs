using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUCoreLib.Helpers;
using CUCoreLib.Registries;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Eagle Allied Industries MBSS 插板胸挂（狼棕色）
///
/// 防弹胸挂，占用胸部（outertorso）和弹挂（bandolier）槽位。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite。
/// 胸部减伤由 desiredWearLimb=UpTorso 的 wearableArmor 提供。
/// 弹挂功能由 Container 组件提供（可装物品）。
///
/// 注意：原版 wearSlotId 互斥只允许一个槽位。
/// 这里用 outertorso 槽位（与外套互斥，不影响背包/弹挂）。
/// 弹挂功能通过 Container 实现，不需要占 bandolier 槽位。
/// </summary>
public static class MBSSItemSystem
{
    public const string ItemKey = "mbss";
    public const string BaseGameItemId = "bruisekit"; // 有 Resources prefab

    public static string DisplayName => I18n.Tr("mbss.name");
    public static string Description => I18n.Tr("mbss.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsMBSSRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    public static float WearableArmor = 0.6863f;          // 减伤40.7%（按比例缩放）
    public static float Weight = 1.5f;                  // 重量 1.5u
    public static float WearableHitDurabilityLossMultiplier = 0.25f; // 被击中耐久损失倍率
    public static float WearableIsolation = 0.1f;       // 保温值
    public static int Value = 35;                       // 价值
    public static int RecognitionMin = 5;               // 识别所需智力
    public static float ContainerCapacity = 2f;         // 容器容量 2u
    public static float ContainerMaxWeightPerItem = 1f; // 单物品最大重量 1u
    public static int WearableVisualOffset = 5;         // 穿戴时 sortingOrder 偏移

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsMBSSRequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[MBSS] Set sprite to mbss icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[MBSS] Icon load failed (icon={icon != null}, sr={sr != null}) - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[MBSS] Configured spawned item '{ItemKey}'.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "utility",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                wearable = true,
                desiredWearLimb = "UpTorso",
                wearSlotId = "outertorso",
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                tags = "cangetwet",
                rec = new Recognition(RecognitionMin),
            };

            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;

            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[MBSS] Registered '{ItemKey}' as wearable vest.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[MBSS] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    /// <summary>
    /// 在 CUCoreLib 模式下注册到 ItemRegistry（由 WeaponCUCoreLibMode.OnItemsSetup 调用）。
    /// 设置 WornSprite、Container 等自定义属性。
    /// </summary>
    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[MBSS] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

        // 穿戴贴图
        if (wornIcon != null)
        {
            customInfo.WornSprite = wornIcon;
            customInfo.WornSpriteOffset = new Vector2(0f, 0f);
        }

        // 也设置 Icon（确保背包缩略图正确）
        if (icon != null)
        {
            customInfo.Icon = icon;
        }

        // 容器属性（弹挂功能）
        if (ContainerCapacity > 0)
        {
            customInfo.Container = new ContainerProperties
            {
                Capacity = ContainerCapacity,
                MaxWeightPerItem = ContainerMaxWeightPerItem > 0 ? ContainerMaxWeightPerItem : 3f,
                EncumbranceReduction = 0.3f, // 重量减免70%
            };
        }

        Plugin.Log.LogInfo($"[MBSS] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, Container={customInfo.Container != null}.");
    }

    // === Icon ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "mbss.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 6f);
                _cachedIcon.name = "mbss-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[MBSS] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "mbss.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                // 穿戴贴图用相同图片但不同 pivot/scale
                _cachedWornIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "mbss-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[MBSS] Failed to load worn icon: {ex.Message}");
        }

        return _cachedWornIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static void ResizeColliderToSprite(Item item)
    {
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        var col = item.GetComponent<BoxCollider2D>();
        if (col == null) col = item.gameObject.AddComponent<BoxCollider2D>();
        var bounds = sr.sprite.bounds;
        col.size = new Vector2(bounds.size.x, bounds.size.y);
        col.offset = Vector2.zero;
    }

    // === 悬停描述 ===

    /// <summary>
    /// MBSS 悬停描述补丁。仅覆盖名称，保留游戏原生详细页面。
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class MBSSHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                return;
            if (!item.Stats.rec.recognizable) return;
            __result.Item1 = DisplayName;
        }
    }

    // === 双槽位锁定 ===

    /// <summary>
    /// 让 MBSS 穿戴后同时锁定 bandolier 槽位，阻止穿戴其他弹挂。
    /// 通过 Plugin.cs 手动注册（GetWearableBySlotID 可能不是 public）。
    /// </summary>
    public static class MBSSDualSlotPatch
    {
        private static bool _inPatch;

        public static void Postfix(Body __instance, string id, ref Item __result)
        {
            if (_inPatch) return;

            // 查询 bandolier 槽位时，如果还没穿戴弹挂但已穿 MBSS，返回 MBSS 占用槽位
            if (id == "bandolier" && __result == null)
            {
                _inPatch = true;
                try
                {
                    var upTorsoItem = __instance.GetWearableBySlotID("outertorso");
                    if (upTorsoItem != null &&
                        upTorsoItem.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                    {
                        __result = upTorsoItem;
                    }
                }
                finally
                {
                    _inPatch = false;
                }
            }
        }
    }
}
