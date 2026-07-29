using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// TK Fast MT 头盔 - FAST MT 头盔仿制品，防弹等级低但兼容配件。
/// 数值与原版 bikehelmet 一致。
/// </summary>
public static class TkFastMtItemSystem
{
    public const string ItemKey = "tkfastmt";
    public const string DisplayName = "TK Fast MT";

    private const float Weight = 0.8f;
    private const int Value = 15;
    private const int RecognitionMin = 6;
    private const float WearableArmor = 1f; // bikehelmet: 50% 减伤
    private const float WearableHitDurabilityLossMultiplier = 0.8f;
    private const float WearableIsolation = 0.08f;
    private const string WearSlotId = "hat";
    private const int WearableVisualOffset = 8;

    private static Sprite? _cachedIcon;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest? request = null)
    {
        if (item == null) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        // 移除 bruisekit 预制体的多余组件（导致库存拖拽冻结）
        var lightItem = item.GetComponent<LightItem>();
        if (lightItem != null) UnityEngine.Object.Destroy(lightItem);
        var light2d = item.GetComponent<Light2D>();
        if (light2d != null) UnityEngine.Object.Destroy(light2d);
        var childLight2d = item.GetComponentInChildren<Light2D>();
        if (childLight2d != null) UnityEngine.Object.Destroy(childLight2d.gameObject);
        // 同步所有 Stats 字段
        item.Stats.wearableArmor = WearableArmor;
        item.Stats.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
        item.Stats.wearableIsolation = WearableIsolation;
        item.Stats.wearableVisualOffset = WearableVisualOffset;
        item.Stats.weight = Weight;
        item.Stats.value = Value;
        item.Stats.wearable = true;
        item.Stats.desiredWearLimb = "Head";
        item.Stats.wearSlotId = WearSlotId;
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[TKFastMt] Configured spawned item.");
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
                destroyAtZeroCondition = true,
                wearable = true,
                desiredWearLimb = "Head",
                wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;
            // bikehelmet 不调用 SetTags()

            // 兼容与 FAST MT 相同的头盔（NVG 兼容检查使用 helmet id）
            // Pvs14/Gpnvg18/Pvs31a 的 CompatibleHelmets 不包含 tkfastmt，
            // 但可以通过 WearWearablePrefix 跳过检查（客户端）实现兼容

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[TKFastMt] Registered '{ItemKey}' as wearable helmet (hat slot).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[TKFastMt] Failed: {ex}");
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
        Plugin.Log.LogInfo($"[TKFastMt] CUCoreLib: Icon={customInfo.Icon != null}, WornSprite={customInfo.WornSprite != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "tkfastmt.png");
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
                    _cachedIcon.name = "tkfastmt-icon";
                }
            }
            else Plugin.Log.LogWarning($"[TKFastMt] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[TKFastMt] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsTkFastMtRequest(MedicalGrantRequest request) =>
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
