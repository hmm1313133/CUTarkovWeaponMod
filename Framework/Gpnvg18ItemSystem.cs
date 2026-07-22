using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

public static class Gpnvg18ItemSystem
{
    public const string ItemKey = "gpnvg18";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "eyes";

    // Compatible helmets (same as NVG/PVS-14 - mounts on these helmets only)
    private static readonly string[] CompatibleHelmets = { "6b47", "calman", "fastmt" };

    public static string DisplayName => I18n.Tr("gpnvg18.name");
    public static string Description => I18n.Tr("gpnvg18.desc");

    private static Sprite? _cachedIcon;

    public static bool IsGpnvg18Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static float Weight = 0.44f;
    public static int Value = 35;
    public static int RecognitionMin = 7;
    public static int WearableVisualOffset = 6;

    public static bool IsCompatibleHelmet(string helmetId)
    {
        foreach (var h in CompatibleHelmets)
        {
            if (helmetId.Equals(h, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsGpnvg18Request(request)) return;
        item.id = ItemKey;

        // Remove flashlight base template components that cause inventory drag freeze
        var lightItem = item.GetComponent<LightItem>();
        if (lightItem != null) { UnityEngine.Object.Destroy(lightItem); Plugin.Log.LogInfo("[GPNVG18] Removed LightItem."); }
        var light2d = item.GetComponent<Light2D>();
        if (light2d != null) { UnityEngine.Object.Destroy(light2d); Plugin.Log.LogInfo("[GPNVG18] Removed Light2D."); }
        var childLight2d = item.GetComponentInChildren<Light2D>();
        if (childLight2d != null) { UnityEngine.Object.Destroy(childLight2d.gameObject); }

        // Manually add BatteryItem
        EnsureBatteryItem(item);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[GPNVG18] Set sprite ({icon.texture.width}x{icon.texture.height}).");
        }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[GPNVG18] Configured spawned item '{ItemKey}'.");
    }

    /// <summary>
    /// Ensure the item has a BatteryItem component configured for medium battery.
    /// </summary>
    public static void EnsureBatteryItem(Item item)
    {
        var bat = item.GetComponent<BatteryItem>();
        if (bat == null) bat = item.gameObject.AddComponent<BatteryItem>();
        bat.preset = BatteryItem.BatteryPreset.Medium;
        bat.maxAllowedCharge = 100f;
        bat.batteryType = "mediumbattery";
        bat.maxCharge = 100f;
        item.SetCondition(1f); // Full charge (0~1 scale)
        Plugin.Log.LogInfo(
            $"[GPNVG18] BatteryItem ensured: preset=Medium, maxCharge={bat.maxCharge}, condition={item.condition}.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey)) return false;
        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "custom",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                wearable = true,
                desiredWearLimb = "Head",
                wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                tags = "cangetwet",
                rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = 0f;
            info.wearableHitDurabilityLossMultiplier = 0f;
            info.wearableIsolation = 0f;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[GPNVG18] Registered '{ItemKey}' as wearable NVG (eyes slot).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GPNVG18] Failed: {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;

        // Use vanilla battery system: medium battery, spawn with full charge
        customInfo.Battery = new BatteryProperties
        {
            Preset = BatteryItem.BatteryPreset.Medium,
            SpawnWithBattery = true,
        };

        // No LightProperties - night vision light is managed by NightVisionController

        Plugin.Log.LogInfo(
            $"[GPNVG18] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Medium.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "gpnvg18.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 8.5f);
                _cachedIcon.name = "gpnvg18-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[GPNVG18] Icon: {ex.Message}"); }
        return _cachedIcon;
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

    /// <summary>
    /// Prevent wearing GPNVG-18 without a compatible helmet (6B47, Calman, FAST MT).
    /// </summary>
    [HarmonyPatch(typeof(Body), "WearWearable")]
    public static class Gpnvg18WearPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Body __instance, Item item)
        {
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                return true;

            var helmet = __instance.GetWearableBySlotID("hat");
            if (helmet == null || !IsCompatibleHelmet(helmet.id))
            {
                Plugin.Log.LogInfo(
                    $"[GPNVG18] Cannot wear GPNVG-18: no compatible helmet (has: {helmet?.id ?? "none"}).");
                return false;
            }

            Plugin.Log.LogInfo($"[GPNVG18] GPNVG-18 equipped with helmet '{helmet.id}'.");
            return true;
        }
    }

    // [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class Gpnvg18HoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
        return; // Disabled: replaced by UnifiedHoverPatch
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                return;
            if (!item.Stats.rec.recognizable) return;
            __result.Item1 = DisplayName;
        }
    }
}
