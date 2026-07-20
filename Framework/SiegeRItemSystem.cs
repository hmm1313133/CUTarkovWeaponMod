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
/// First Spear Siege-R Optimized M.A.S.S. 插板胸挂 (黑色军团)
///
/// 防弹胸挂，占用胸部（outertorso）和弹挂（bandolier）槽位。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite + MultiWornSprites。
/// 主肢体 UpTorso，额外肢体 DownTorso + UpArmF/B 通过 MultiWornSprites。
/// 所有部位统一 67% 减伤。
/// </summary>
public static class SiegeRItemSystem
{
    public const string ItemKey = "sieger";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("sieger.name");
    public static string Description => I18n.Tr("sieger.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;
    private static Sprite? _cachedArmIcon;
    private static Sprite? _cachedDownIcon;

    public static bool IsSiegeRRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    // 减伤54.5%: 1/(1+a) = 0.455, a = 1/0.455 - 1 = 1.1978
    public static float WearableArmor = 1.1978f;
    public static float Weight = 5.2f;                  // 重量 5.2u
    public static float WearableHitDurabilityLossMultiplier = 0.13f; // 被击中耐久损失倍率
    public static float WearableIsolation = 0.13f;      // 保温值
    public static int Value = 66;                       // 价值
    public static int RecognitionMin = 7;               // 识别所需智力
    public static float ContainerCapacity = 5.5f;       // 容器容量 5.5u
    public static float ContainerMaxWeightPerItem = 2f; // 单物品最大重量 2u
    public static float ContainerEncumbranceReduction = 0.3f; // 重量减免 70%
    public static int WearableVisualOffset = 5;         // 穿戴时 sortingOrder 偏移

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsSiegeRRequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[SiegeR] Set sprite to sieger icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[SiegeR] Icon load failed - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[SiegeR] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[SiegeR] Registered '{ItemKey}' as wearable vest.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SiegeR] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[SiegeR] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

        if (wornIcon != null)
        {
            customInfo.WornSprite = wornIcon;
            customInfo.WornSpriteOffset = new Vector2(0f, 0f);
        }

        if (icon != null)
        {
            customInfo.Icon = icon;
        }

        // 多部位防护：DownTorso + UpArmF/B
        var downSprite = TryLoadDownIcon();
        if (downSprite != null)
        {
            customInfo.MultiWornSprites["DownTorso"] = downSprite;
        }
        else
        {
            Plugin.Log.LogWarning($"[SiegeR] sieger_down.png not found, DownTorso protection will have no custom sprite.");
        }

        var armSprite = TryLoadArmIcon();
        if (armSprite != null)
        {
            customInfo.MultiWornSprites["UpArmF"] = armSprite;
            customInfo.MultiWornSprites["UpArmB"] = armSprite;
        }
        else
        {
            Plugin.Log.LogWarning($"[SiegeR] sieger_arm.png not found, arm protection will have no custom sprite.");
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

        Plugin.Log.LogInfo($"[SiegeR] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, MultiWornSprites={customInfo.MultiWornSprites?.Count ?? 0}, Container={customInfo.Container != null}.");
    }

    // === Icon ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sieger.png");

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
                _cachedIcon.name = "sieger-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SiegeR] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sieger_up.png");

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
                _cachedWornIcon.name = "sieger-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SiegeR] Failed to load worn icon: {ex.Message}");
        }

        return _cachedWornIcon;
    }

    private static Sprite? TryLoadDownIcon()
    {
        if (_cachedDownIcon != null) return _cachedDownIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sieger_down.png");

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
                _cachedDownIcon.name = "sieger-down";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SiegeR] Failed to load down icon: {ex.Message}");
        }

        return _cachedDownIcon;
    }

    private static Sprite? TryLoadArmIcon()
    {
        if (_cachedArmIcon != null) return _cachedArmIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "sieger_arm.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedArmIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 7f);
                _cachedArmIcon.name = "sieger-arm";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SiegeR] Failed to load arm icon: {ex.Message}");
        }

        return _cachedArmIcon;
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
    public static class SiegeRHoverPatch
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

    // === 多部位防护（UpTorso + DownTorso + UpArmF/B） ===
    //
    // 通过 CUCoreLib CustomItemInfo.MultiWornSprites 实现 secondaryLimbs。
    // 所有部位统一使用 wearableArmor 减伤值（67%）。

    // === 双槽位锁定 ===

    public static class SiegeRDualSlotPatch
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
