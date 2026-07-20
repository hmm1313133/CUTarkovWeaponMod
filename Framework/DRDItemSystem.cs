using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

public static class DRDItemSystem
{
    public const string ItemKey = "drd";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "outertorso";

    public static string DisplayName => I18n.Tr("drd.name");
    public static string Description => I18n.Tr("drd.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsDRDRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // 减伤35.8%: 1/(1+a) = 0.642, a = 1/0.642 - 1 = 0.5576
    public static float WearableArmor = 0.5576f;
    public static float Weight = 1.5f;
    public static float WearableHitDurabilityLossMultiplier = 0.27f;
    public static float WearableIsolation = 0.07f;
    public static int Value = 24;
    public static int RecognitionMin = 4;
    public static int WearableVisualOffset = 5;
    public static float DecayRatePerSecond = 1.0f / 19800.0f; // ~5.5h

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsDRDRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null) { sr.sprite = icon; Plugin.Log.LogInfo($"[DRD] Set sprite ({icon.texture.width}x{icon.texture.height})."); }
        else { Plugin.Log.LogWarning("[DRD] Icon load failed."); }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[DRD] Configured spawned item '{ItemKey}'.");
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
                destroyAtZeroCondition = true, wearable = true,
                desiredWearLimb = "UpTorso", wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset, weight = Weight, value = Value,
                tags = "cangetwet", rec = new Recognition(RecognitionMin),
            };
            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;
            info.rotSpeed = DecayRatePerSecond * 100f;
            info.decayInfo = (byte)ItemInfo.DecayType.NoDecayWhenNotWorn;

            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[DRD] Registered '{ItemKey}' as wearable armor.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[DRD] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();
        if (wornIcon != null) { customInfo.WornSprite = wornIcon; customInfo.WornSpriteOffset = new Vector2(0f, 0f); }
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[DRD] CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}.");
    }

    public static void TickDecay()
    {
        // 衰减现在由游戏原生 Item.HandleDecay 通过 rotSpeed + decayInfo(NoDecayWhenNotWorn) 处理
        return;
        var cam = PlayerCamera.main;
        if (cam == null) return;
        var body = cam.body;
        if (body == null) return;
        var item = body.GetWearableBySlotID(WearSlotId);
        if (item == null) return;
        if (!item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase)) return;
        var newCondition = item.condition - DecayRatePerSecond * Time.deltaTime;
        if (newCondition <= 0f) { item.condition = 0f; UnityEngine.Object.Destroy(item.gameObject); }
        else { item.condition = newCondition; }
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "drd.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 6f);
                _cachedIcon.name = "drd-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[DRD] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "drd.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedWornIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "drd-worn";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[DRD] Worn: {ex.Message}"); }
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
    public static class DRDHoverPatch
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
