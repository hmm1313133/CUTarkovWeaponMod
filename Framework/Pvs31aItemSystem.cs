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

public static class Pvs31aItemSystem
{
    public const string ItemKey = "pvs31a";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "eyes";

    // Compatible helmets (same as PVS-14 / GPNVG-18)
    private static readonly string[] CompatibleHelmets = { "6b47", "calman", "fastmt" };

    public static string DisplayName => I18n.Tr("pvs31a.name");
    public static string Description => I18n.Tr("pvs31a.desc");

    private static Sprite? _cachedIcon;

    public static bool IsPvs31aRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static float Weight = 0.24f;
    public static int Value = 55;
    public static int RecognitionMin = 9;
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
        if (!IsPvs31aRequest(request)) return;
        item.id = ItemKey;

        // Remove flashlight base template components that cause inventory drag freeze
        var lightItem = item.GetComponent<LightItem>();
        if (lightItem != null) { UnityEngine.Object.Destroy(lightItem); Plugin.Log.LogInfo("[PVS31A] Removed LightItem."); }
        var light2d = item.GetComponent<Light2D>();
        if (light2d != null) { UnityEngine.Object.Destroy(light2d); Plugin.Log.LogInfo("[PVS31A] Removed Light2D."); }
        var childLight2d = item.GetComponentInChildren<Light2D>();
        if (childLight2d != null) { UnityEngine.Object.Destroy(childLight2d.gameObject); }

        // Manually add BatteryItem
        EnsureBatteryItem(item);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[PVS31A] Set sprite ({icon.texture.width}x{icon.texture.height}).");
        }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[PVS31A] Configured spawned item '{ItemKey}'.");
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
            $"[PVS31A] BatteryItem ensured: preset=Medium, maxCharge={bat.maxCharge}, condition={item.condition}.");
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
            Plugin.Log.LogInfo($"[PVS31A] Registered '{ItemKey}' as wearable NVG (eyes slot).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PVS31A] Failed: {ex}");
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

        Plugin.Log.LogInfo(
            $"[PVS31A] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Medium.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "pvs31a.png");
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
                _cachedIcon.name = "pvs31a-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[PVS31A] Icon: {ex.Message}"); }
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
    /// Prevent wearing PVS-31A without a compatible helmet (6B47, Calman, FAST MT).
    /// </summary>
    [HarmonyPatch(typeof(Body), "WearWearable")]
    public static class Pvs31aWearPatch
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
                    $"[PVS31A] Cannot wear PVS-31A: no compatible helmet (has: {helmet?.id ?? "none"}).");
                return false;
            }
            Plugin.Log.LogInfo($"[PVS31A] PVS-31A equipped with helmet '{helmet.id}'.");
            return true;
        }
    }
}
