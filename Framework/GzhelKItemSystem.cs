using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

public static class GzhelKItemSystem
{
    public const string ItemKey = "gzhel_k";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "outertorso";

    public static string DisplayName => I18n.Tr("gzhel_k.name");
    public static string Description => I18n.Tr("gzhel_k.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsGzhelKRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // 减伤58.6%: 1/(1+a) = 0.414, a = 1/0.414 - 1 = 1.4155
    public static float WearableArmor = 1.4155f;
    public static float Weight = 4.7f;
    public static float WearableHitDurabilityLossMultiplier = 0.2f;
    public static float WearableIsolation = 0.08f;
    public static int Value = 53;
    public static int RecognitionMin = 5;
    public static int WearableVisualOffset = 5;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsGzhelKRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null) { sr.sprite = icon; Plugin.Log.LogInfo($"[GZHEL-K] Set sprite ({icon.texture.width}x{icon.texture.height})."); }
        else { Plugin.Log.LogWarning("[GZHEL-K] Icon load failed."); }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[GZHEL-K] Configured spawned item '{ItemKey}'.");
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
                tags = "cangetwet", rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[GZHEL-K] Registered '{ItemKey}' as wearable armor.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[GZHEL-K] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();
        if (wornIcon != null) { customInfo.WornSprite = wornIcon; customInfo.WornSpriteOffset = new Vector2(0f, 0f); }
        if (icon != null) customInfo.Icon = icon;

        // 多部位防护：DownTorso（腹部）用透明占位确保 secondaryLimbs 生效，但不显示贴图
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, new Color(0, 0, 0, 0));
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        var downPlaceholder = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        downPlaceholder.name = "gzhel_k-down-empty";
        customInfo.MultiWornSprites["DownTorso"] = downPlaceholder;

        Plugin.Log.LogInfo($"[GZHEL-K] CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, MultiWornSprites=DownTorso.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "GZHEL_K.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 6f);
                _cachedIcon.name = "gzhel_k-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[GZHEL-K] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "GZHEL_K.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedWornIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "gzhel_k-worn";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[GZHEL-K] Worn: {ex.Message}"); }
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
    public static class GzhelKHoverPatch
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
