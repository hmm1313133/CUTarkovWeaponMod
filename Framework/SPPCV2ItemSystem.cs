using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Stich Profi V2 插板胸挂（黑色）
///
/// 防弹胸挂，占用胸部（outertorso）和弹挂（bandolier）槽位。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite + MultiWornSprites。
/// 主肢体 UpTorso 通过 desiredWearLimb + WornSprite，额外肢体 DownTorso 通过 MultiWornSprites。
/// 弹挂功能由 Container 组件提供（可装物品）。
/// </summary>
public static class SPPCV2ItemSystem
{
    public const string ItemKey = "sppcv2";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("sppcv2.name");
    public static string Description => I18n.Tr("sppcv2.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;
    private static Sprite? _cachedDownIcon;

    public static bool IsSPPCV2Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    // 减伤48.8%: 1/(1+a) = 0.512, a = 1/0.512 - 1 = 0.9531
    public static float WearableArmor = 0.9531f;
    public static float Weight = 2.7f;                  // 重量 2.7u
    public static float WearableHitDurabilityLossMultiplier = 0.17f; // 被击中耐久损失倍率
    public static float WearableIsolation = 0.11f;      // 保温值
    public static int Value = 44;                       // 价值
    public static int RecognitionMin = 5;               // 识别所需智力
    public static float ContainerCapacity = 3f;         // 容器容量 3u
    public static float ContainerMaxWeightPerItem = 2f; // 单物品最大重量 2u
    public static float ContainerEncumbranceReduction = 0.35f; // 重量减免 65%
    public static int WearableVisualOffset = 5;         // 穿戴时 sortingOrder 偏移

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsSPPCV2Request(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[SPPCV2] Set sprite to sppcv2 icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[SPPCV2] Icon load failed (icon={icon != null}, sr={sr != null}) - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[SPPCV2] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[SPPCV2] Registered '{ItemKey}' as wearable vest.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SPPCV2] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[SPPCV2] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

        if (wornIcon != null)
        {
            customInfo.WornSprite = wornIcon;
            customInfo.WornSpriteOffset = new Vector2(0f, -0.04f);
        }

        if (icon != null)
        {
            customInfo.Icon = icon;
        }

        // 多部位防护：DownTorso（腹部）通过 MultiWornSprites 注册
        var downSprite = TryLoadDownIcon();
        if (downSprite != null)
        {
            customInfo.MultiWornSprites["DownTorso"] = downSprite;
            customInfo.MultiWornSpriteOffsets["DownTorso"] = new Vector2(0f, 0.04f);
            Plugin.Log.LogInfo($"[SPPCV2] Set MultiWornSprites: DownTorso sprite loaded.");
        }
        else
        {
            Plugin.Log.LogWarning($"[SPPCV2] sppcv2_down.png not found, DownTorso protection will have no custom sprite.");
        }

        if (ContainerCapacity > 0)
        {
            customInfo.Container = new ContainerProperties
            {
                Capacity = ContainerCapacity,
                MaxWeightPerItem = ContainerMaxWeightPerItem > 0 ? ContainerMaxWeightPerItem : 3f,
                EncumbranceReduction = ContainerEncumbranceReduction,
            };
        }

        Plugin.Log.LogInfo($"[SPPCV2] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, MultiWornSprites={customInfo.MultiWornSprites?.Count ?? 0}, Container={customInfo.Container != null}.");
    }

    // === Icon ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sppcv2.png");

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
                _cachedIcon.name = "sppcv2-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SPPCV2] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sppcv2_up.png");

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
                _cachedWornIcon.name = "sppcv2-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SPPCV2] Failed to load worn icon: {ex.Message}");
        }

        return _cachedWornIcon;
    }

    private static Sprite? TryLoadDownIcon()
    {
        if (_cachedDownIcon != null) return _cachedDownIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sppcv2_down.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedDownIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 7f);
                _cachedDownIcon.name = "sppcv2-down";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SPPCV2] Failed to load down icon: {ex.Message}");
        }

        return _cachedDownIcon;
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
    public static class SPPCV2HoverPatch
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

    // === 多部位防护（UpTorso + DownTorso） ===
    //
    // 通过 CUCoreLib CustomItemInfo.MultiWornSprites 实现，
    // 由 CUCoreLib 的 CustomWearablePatches.ConfigureSecondarySprites
    // 在 Wearable.CreateSprites Prefix 中自动设置 secondaryLimbs。
    // 无需额外的 Harmony 补丁。

    // === 双槽位锁定 ===

    public static class SPPCV2DualSlotPatch
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
