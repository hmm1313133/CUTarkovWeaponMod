using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

public static class RedutT5ItemSystem
{
    public const string ItemKey = "redut_t5";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "outertorso";

    public static string DisplayName => I18n.Tr("redut_t5.name");
    public static string Description => I18n.Tr("redut_t5.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;
    private static Sprite? _cachedDownIcon;
    private static Sprite? _cachedArmIcon;
    private static Sprite? _cachedThighIcon;

    public static bool IsRedutT5Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // 减伤56.2%: 1/(1+a) = 0.438, a = 1/0.438 - 1 = 1.2831
    public static float WearableArmor = 1.2831f;
    public static float Weight = 5f;
    public static float WearableHitDurabilityLossMultiplier = 0.15f;
    public static float WearableIsolation = 0.17f;
    public static int Value = 67;
    public static int RecognitionMin = 5;
    public static int WearableVisualOffset = 5;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsRedutT5Request(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null) { sr.sprite = icon; Plugin.Log.LogInfo($"[Redut-T5] Set sprite ({icon.texture.width}x{icon.texture.height})."); }
        else { Plugin.Log.LogWarning("[Redut-T5] Icon load failed."); }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[Redut-T5] Configured spawned item '{ItemKey}'.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey)) return false;
        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName, description = Description, category = "utility",
                slotRotation = 0f, usable = false, usableOnLimb = false,
                destroyAtZeroCondition = false, wearable = true,
                desiredWearLimb = "UpTorso", wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset, weight = Weight, value = Value,
                tags = "", rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[Redut-T5] Registered '{ItemKey}' as wearable armor.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Redut-T5] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();
        if (wornIcon != null) { customInfo.WornSprite = wornIcon; customInfo.WornSpriteOffset = new Vector2(0f, 0f); }
        if (icon != null) customInfo.Icon = icon;

        // 多部位防护：DownTorso + UpArmF/B + ThighF/B
        var downSprite = TryLoadDownIcon();
        if (downSprite != null) customInfo.MultiWornSprites["DownTorso"] = downSprite;

        var armSprite = TryLoadArmIcon();
        if (armSprite != null)
        {
            customInfo.MultiWornSprites["UpArmF"] = armSprite;
            customInfo.MultiWornSprites["UpArmB"] = armSprite;
        }

        var thighSprite = TryLoadThighIcon();
        if (thighSprite != null)
        {
            customInfo.MultiWornSprites["ThighF"] = thighSprite;
            customInfo.MultiWornSprites["ThighB"] = thighSprite;
        }

        Plugin.Log.LogInfo($"[Redut-T5] CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, MultiWornSprites={customInfo.MultiWornSprites?.Count ?? 0}.");
    }

    // === Icons ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "RedutT5.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 6f);
                _cachedIcon.name = "redut_t5-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Redut-T5] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "RedutT5_up.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedWornIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "redut_t5-worn";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Redut-T5] Worn: {ex.Message}"); }
        return _cachedWornIcon;
    }

    private static Sprite? TryLoadDownIcon()
    {
        if (_cachedDownIcon != null) return _cachedDownIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "RedutT5_down.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedDownIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedDownIcon.name = "redut_t5-down";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Redut-T5] Down: {ex.Message}"); }
        return _cachedDownIcon;
    }

    private static Sprite? TryLoadArmIcon()
    {
        if (_cachedArmIcon != null) return _cachedArmIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "RedutT5_arm.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedArmIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedArmIcon.name = "redut_t5-arm";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Redut-T5] Arm: {ex.Message}"); }
        return _cachedArmIcon;
    }

    private static Sprite? TryLoadThighIcon()
    {
        if (_cachedThighIcon != null) return _cachedThighIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "RedutT5_thigh.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedThighIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedThighIcon.name = "redut_t5-thigh";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Redut-T5] Thigh: {ex.Message}"); }
        return _cachedThighIcon;
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

    // [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class RedutT5HoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
        return; // Disabled: replaced by UnifiedHoverPatch
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase)) return;
            if (!item.Stats.rec.recognizable) return;
            __result.Item1 = DisplayName;
        }
    }
}
