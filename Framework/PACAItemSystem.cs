using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

public static class PACAItemSystem
{
    public const string ItemKey = "paca";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "outertorso";

    public static string DisplayName => I18n.Tr("paca.name");
    public static string Description => I18n.Tr("paca.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsPACARequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static float WearableArmor = 0.4286f;
    public static float Weight = 1.3f;
    public static float WearableHitDurabilityLossMultiplier = 0.3f;
    public static float WearableIsolation = 0.07f;
    public static int Value = 17;
    public static int RecognitionMin = 4;
    public static int WearableVisualOffset = 5;
    public static float DecayRatePerSecond = 1.0f / 18000.0f;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsPACARequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null) { sr.sprite = icon; Plugin.Log.LogInfo($"[PACA] Set sprite ({icon.texture.width}x{icon.texture.height})."); }
        else { Plugin.Log.LogWarning("[PACA] Icon load failed."); }
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[PACA] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[PACA] Registered '{ItemKey}' as wearable armor.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[PACA] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();
        if (wornIcon != null) { customInfo.WornSprite = wornIcon; customInfo.WornSpriteOffset = new Vector2(0f, 0f); }
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[PACA] CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}.");
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "paca.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 6f);
                _cachedIcon.name = "paca-icon";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[PACA] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "paca.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                _cachedWornIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "paca-worn";
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[PACA] Worn: {ex.Message}"); }
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
    public static class PACAHoverPatch
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