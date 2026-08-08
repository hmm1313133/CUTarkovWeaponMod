using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Ops-Core FAST 护目罩（面罩），可装备在 TK Fast MT / FAST MT 头盔上。
/// 与夜视仪共享 eyes 槽位，需穿戴兼容头盔才能佩戴。
/// </summary>
public static class FastVisorItemSystem
{
    public const string ItemKey = "fastvisor";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("fastvisor.name");
    public static string Description => I18n.Tr("fastvisor.desc");

    // 用户指定
    private const float Weight = 0.2f;
    private const int Value = 20;
    private const int RecognitionMin = 8;

    // 护甲值：5% 减伤（1 - 1/(1+0.0526) ≈ 5%）
    private const float WearableArmor = 0.0526f;

    // 保暖 0
    private const float WearableHitDurabilityLossMultiplier = 0.65f;
    private const float WearableIsolation = 0f;

    private const string WearSlotId = "eyes";
    private const string DesiredWearLimb = "Head";
    private const int WearableVisualOffset = 6;

    private static Sprite? _cachedIcon;

    public static bool IsFastVisorRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsFastVisorRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        item.Stats.wearable = true;
        item.Stats.wearSlotId = WearSlotId;
        item.Stats.desiredWearLimb = DesiredWearLimb;
        item.Stats.wearableArmor = WearableArmor;
        item.Stats.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
        item.Stats.wearableIsolation = WearableIsolation;
        item.Stats.wearableVisualOffset = WearableVisualOffset;
        item.Stats.weight = Weight;
        item.Stats.value = Value;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[FastVisor] Configured spawned item '{ItemKey}'.");
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
                destroyAtZeroCondition = true,
                wearable = true,
                desiredWearLimb = DesiredWearLimb,
                wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[FastVisor] Registered '{ItemKey}' as wearable visor (eyes slot).");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[FastVisor] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[FastVisor] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "fastvisor.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedIcon = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 8.5f);
                    _cachedIcon.name = "fastvisor-icon";
                }
            }
            else Plugin.Log.LogWarning($"[FastVisor] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[FastVisor] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    private static void ResizeColliderToSprite(Item item)
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
