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
/// Wartech TV-115 插板胸挂（橄榄绿）
///
/// 防弹胸挂，占用胸部（outertorso）和弹挂（bandolier）槽位。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite。
/// 胸部减伤由 desiredWearLimb=UpTorso 的 wearableArmor 提供。
/// 弹挂功能由 Container 组件提供（可装物品）。
/// </summary>
public static class TV115ItemSystem
{
    public const string ItemKey = "tv115";
    public const string BaseGameItemId = "bruisekit"; // 有 Resources prefab

    public static string DisplayName => I18n.Tr("tv115.name");
    public static string Description => I18n.Tr("tv115.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsTV115Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    // 减伤35.8%: 1/(1+a) = 0.642, a = 1/0.642 - 1 = 0.5576
    public static float WearableArmor = 0.5576f;
    public static float Weight = 1.3f;                  // 重量 1.3u
    public static float WearableHitDurabilityLossMultiplier = 0.2f; // 被击中耐久损失倍率
    public static float WearableIsolation = 0.1f;       // 保温值
    public static int Value = 37;                       // 价值
    public static int RecognitionMin = 5;               // 识别所需智力
    public static float ContainerCapacity = 2.2f;       // 容器容量 2.2u
    public static float ContainerMaxWeightPerItem = 1f; // 单物品最大重量 1u
    public static float ContainerEncumbranceReduction = 0.36f; // 重量减免 64%
    public static int WearableVisualOffset = 5;         // 穿戴时 sortingOrder 偏移

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsTV115Request(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[TV115] Set sprite to tv115 icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[TV115] Icon load failed (icon={icon != null}, sr={sr != null}) - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[TV115] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[TV115] Registered '{ItemKey}' as wearable vest.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[TV115] Failed to register '{ItemKey}': {ex}");
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

        Plugin.Log.LogInfo($"[TV115] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

        // 穿戴贴图
        if (wornIcon != null)
        {
            customInfo.WornSprite = wornIcon;
            customInfo.WornSpriteOffset = new Vector2(0f, 0f);
        }

        // Icon（确保背包缩略图正确）
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
                EncumbranceReduction = ContainerEncumbranceReduction,
            };
        }

        Plugin.Log.LogInfo($"[TV115] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, Container={customInfo.Container != null}.");
    }

    // === Icon ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "tv115.png");

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
                _cachedIcon.name = "tv115-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[TV115] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "tv115.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedWornIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "tv115-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[TV115] Failed to load worn icon: {ex.Message}");
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

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class TV115HoverPatch
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
    /// 让 TV-115 穿戴后同时锁定 bandolier 槽位，阻止穿戴其他弹挂。
    /// </summary>
    public static class TV115DualSlotPatch
    {
        private static bool _inPatch;

        public static void Postfix(Body __instance, string id, ref Item __result)
        {
            if (_inPatch) return;

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
