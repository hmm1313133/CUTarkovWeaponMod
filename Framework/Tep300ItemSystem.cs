using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Peltor TEP-300 战术耳塞（狼棕色）- 入耳式电子防护耳机。
/// wearSlotId = "ear"（新槽位），desiredWearLimb = "Head"。
/// 有电：听力损伤 -55%，可听范围 +20%。
/// 无电：听力损伤 -50%，可听范围 -60%，环境音量 -50%。
/// 小型电池，满电 15 分钟。
/// </summary>
public static class Tep300ItemSystem
{
    public const string ItemKey = "tep300";
    public static string DisplayName => WModLoc.Tr("tep300.name", "Peltor TEP-300");
    public static string Description => WModLoc.Tr("tep300.desc", "为保护听力与通讯而特别设计的入耳式耳机，由 3M Peltor 公司设计制造。狼棕色版本。也只有这种耳机刚刚好能塞到你的耳洞了。使用小型电池进行供电。");

    private const string WearSlotId = "ear";
    private const string DesiredWearLimb = "Head";
    private const float Weight = 0.05f;
    private const int Value = 20;
    private const int WearableVisualOffset = 1;
    private const int RecognitionMin = 7;

    private static Sprite? _cachedIcon;

    /// <summary>
    /// Configure a spawned TEP-300 item: battery, icon, collider.
    /// Called from ConfigureSpawnedItem and ConfigureWeaponItem.
    /// </summary>
    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest? request = null)
    {
        if (item == null) return;

        item.id = ItemKey;

        // Battery: small preset
        EnsureBatteryItem(item);

        // Icon
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var icon = TryLoadIcon();
            if (icon != null) sr.sprite = icon;
        }

        // Collider to match sprite
        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo(
            $"[TEP300] Configured spawned item: condition={item.condition}.");
    }

    /// <summary>
    /// Ensure the item has a BatteryItem component configured for small battery.
    /// </summary>
    public static void EnsureBatteryItem(Item item)
    {
        var bat = item.GetComponent<BatteryItem>();
        if (bat == null) bat = item.gameObject.AddComponent<BatteryItem>();
        item.battery = bat; // Item.Awake() 在 BatteryItem 添加前运行，需手动绑定
        bat.preset = BatteryItem.BatteryPreset.Small;
        bat.maxAllowedCharge = 50f;
        bat.batteryType = "smallbattery";
        bat.maxCharge = 50f;
        item.SetCondition(1f); // Full charge (0~1 scale)
        Plugin.Log.LogInfo(
            $"[TEP300] BatteryItem ensured: preset=Small, maxCharge={bat.maxCharge}, condition={item.condition}.");
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
                $"[TEP300] Registered '{ItemKey}' as wearable earplug (ear slot, Head limb).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[TEP300] Failed: {ex}");
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

        // Small battery, spawn with full charge
        customInfo.Battery = new BatteryProperties
        {
            Preset = BatteryItem.BatteryPreset.Small,
            SpawnWithBattery = true,
        };

        Plugin.Log.LogInfo(
            $"[TEP300] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Small.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "headset", "TEP300.png");
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
                _cachedIcon.name = "tep300-icon";
            }
            else
            {
                Plugin.Log.LogWarning($"[TEP300] Icon not found: {iconPath}");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[TEP300] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>Check if a spawn request is for TEP-300.</summary>
    public static bool IsTep300Request(MedicalGrantRequest request) =>
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
