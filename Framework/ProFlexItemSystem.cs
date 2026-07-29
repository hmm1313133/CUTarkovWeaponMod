using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// CENS ProFlex DX5 战术耳塞 - 高端入耳式电子防护耳机。
/// wearSlotId = "ear"，desiredWearLimb = "Head"。
/// 有电：听力损伤 -70%，可听范围 +40%，环境音 +5.5dB，强主动降噪。
/// 无电：听力损伤 -60%，可听范围 -75%，环境音 -11dB。
/// 满电 15 分钟，价值 36。
/// </summary>
public static class ProFlexItemSystem
{
    public const string ItemKey = "proflextac";
    public const string DisplayName = "CENS ProFlex DX5";

    private const string WearSlotId = "ear";
    private const string DesiredWearLimb = "Head";
    private const float Weight = 0.05f;
    private const int Value = 36;
    private const int WearableVisualOffset = 1;
    private const int RecognitionMin = 7;

    private static Sprite? _cachedIcon;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest? request = null)
    {
        if (item == null) return;

        item.id = ItemKey;
        EnsureBatteryItem(item);

        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var icon = TryLoadIcon();
            if (icon != null) sr.sprite = icon;
        }

        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo(
            $"[ProFlex] Configured spawned item: condition={item.condition}.");
    }

    public static void EnsureBatteryItem(Item item)
    {
        var bat = item.GetComponent<BatteryItem>();
        if (bat == null) bat = item.gameObject.AddComponent<BatteryItem>();
        item.battery = bat;
        bat.preset = BatteryItem.BatteryPreset.Small;
        bat.maxAllowedCharge = 50f;
        bat.batteryType = "smallbattery";
        bat.maxCharge = 50f;
        item.SetCondition(1f);
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey)) return false;
        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = "",
                category = "custom",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                wearable = true,
                desiredWearLimb = DesiredWearLimb,
                wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = 0f;
            info.wearableHitDurabilityLossMultiplier = 0f;
            info.wearableIsolation = 0f;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo(
                $"[ProFlex] Registered '{ItemKey}' as wearable earplug (ear slot, Head limb).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ProFlex] Failed: {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null)
        {
            customInfo.Icon = icon;
            customInfo.WornSprite = icon;
            customInfo.WornSpriteOffset = Vector2.zero;
        }

        customInfo.Battery = new BatteryProperties
        {
            Preset = BatteryItem.BatteryPreset.Small,
            SpawnWithBattery = true,
        };

        Plugin.Log.LogInfo(
            $"[ProFlex] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Small.");
    }

    public static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "headset", "ProFlex.png");
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
                _cachedIcon.name = "proflextac-icon";
            }
            else
            {
                Plugin.Log.LogWarning($"[ProFlex] Icon not found: {iconPath}");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[ProFlex] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsProFlexRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

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
}
