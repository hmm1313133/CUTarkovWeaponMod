using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

public static class FastMtItemSystem
{
    public const string ItemKey = "fastmt";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "hat";

    public static string DisplayName => I18n.Tr("fastmt.name");
    public static string Description => I18n.Tr("fastmt.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsFastMtRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // 减伤40%: 1/(1+a) = 0.60, a = 0.67
    public static float WearableArmor = 0.67f;
    public static float Weight = 0.5f;
    public static float WearableHitDurabilityLossMultiplier = 0.39f;
    public static float WearableIsolation = 0.1f;
    public static int Value = 44;
    public static int RecognitionMin = 7;
    public static int WearableVisualOffset = 8;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsFastMtRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null) { sr.sprite = icon; Plugin.Log.LogInfo($"[FASTMT] Set sprite ({icon.texture.width}x{icon.texture.height})."); }
        else { Plugin.Log.LogWarning("[FASTMT] Icon load failed."); }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[FASTMT] Configured spawned item '{ItemKey}'.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey)) return false;
        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName, description = Description, category = "custom",
                slotRotation = 0f, usable = false, usableOnLimb = false,
                destroyAtZeroCondition = true, wearable = true,
                desiredWearLimb = "Head", wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset, weight = Weight, value = Value,
                tags = "cangetwet", rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[FASTMT] Registered '{ItemKey}' as wearable helmet.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[FASTMT] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();
        if (wornIcon != null) { customInfo.WornSprite = wornIcon; customInfo.WornSpriteOffset = new Vector2(0f, 0f); }
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[FASTMT] CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "fastmt.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 8.5f);
                _cachedIcon.name = "fastmt-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[FASTMT] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "fastmt.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedWornIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 8.5f);
                _cachedWornIcon.name = "fastmt-worn";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[FASTMT] Worn: {ex.Message}"); }
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

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class FastMtHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase)) return;
            if (!item.Stats.rec.recognizable) return;
            __result.Item1 = DisplayName;
        }
    }
}
