using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 防弹插板物品系统（普通 + 高级）
/// 用于在配方界面修复防弹衣。
/// </summary>
public static class ArmorPlateItemSystem
{
    public const string CheapPlateKey = "cheapplate";
    public const string AdvancedPlateKey = "advancedplate";
    public const string BaseGameItemId = "bruisekit";

    public static string CheapPlateName => I18n.Tr("cheapplate.name");
    public static string CheapPlateDesc => I18n.Tr("cheapplate.desc");
    public static string AdvancedPlateName => I18n.Tr("advancedplate.name");
    public static string AdvancedPlateDesc => I18n.Tr("advancedplate.desc");

    private static Sprite? _cachedCheapIcon;
    private static Sprite? _cachedAdvancedIcon;

    public static bool IsCheapPlateRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(CheapPlateKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsAdvancedPlateRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(AdvancedPlateKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    public static float CheapWeight = 0.5f;
    public static int CheapValue = 10;
    public static float AdvancedWeight = 0.3f;
    public static int AdvancedValue = 22;
    public static int RecognitionMin = 2;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (IsCheapPlateRequest(request))
        {
            item.id = CheapPlateKey;
            item.SetCondition(1f);
            ApplyIcon(item, "cheapplate");
            ResizeColliderToSprite(item);
            Plugin.Log.LogInfo($"[ArmorPlate] Configured spawned item '{CheapPlateKey}'.");
        }
        else if (IsAdvancedPlateRequest(request))
        {
            item.id = AdvancedPlateKey;
            item.SetCondition(1f);
            ApplyIcon(item, "advancedplate");
            ResizeColliderToSprite(item);
            Plugin.Log.LogInfo($"[ArmorPlate] Configured spawned item '{AdvancedPlateKey}'.");
        }
    }

    private static void ApplyIcon(Item item, string iconName)
    {
        var icon = TryLoadIcon(iconName);
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null) sr.sprite = icon;
    }

    public static void EnsureCheapPlateRegistered()
    {
        EnsureRegistered(CheapPlateKey, CheapPlateName, CheapPlateDesc, CheapWeight, CheapValue);
    }

    public static void EnsureAdvancedPlateRegistered()
    {
        EnsureRegistered(AdvancedPlateKey, AdvancedPlateName, AdvancedPlateDesc, AdvancedWeight, AdvancedValue);
    }

    private static void EnsureRegistered(string key, string name, string desc, float weight, int value)
    {
        if (Item.GlobalItems.ContainsKey(key)) return;
        try
        {
            var info = new ItemInfo
            {
                fullName = name,
                description = desc,
                category = "utility",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                weight = weight,
                value = value,
                tags = "",
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();
            Item.GlobalItems[key] = info;
            Plugin.Log.LogInfo($"[ArmorPlate] Registered '{key}'.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ArmorPlate] Failed to register '{key}': {ex}");
        }
    }

    public static void RegisterCheapPlateWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon("cheapplate");
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[ArmorPlate] CUCoreLib cheapplate: Icon={customInfo.Icon != null}.");
    }

    public static void RegisterAdvancedPlateWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon("advancedplate");
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[ArmorPlate] CUCoreLib advancedplate: Icon={customInfo.Icon != null}.");
    }

    public static Sprite? TryLoadCheapIconPublic() => TryLoadIcon("cheapplate");
    public static Sprite? TryLoadAdvancedIconPublic() => TryLoadIcon("advancedplate");

    private static Sprite? TryLoadIcon(string iconName)
    {
        var cache = iconName == "cheapplate" ? ref _cachedCheapIcon : ref _cachedAdvancedIcon;
        if (cache != null) return cache;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", $"{iconName}.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                cache = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
                cache.name = $"{iconName}-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ArmorPlate] Failed to load icon '{iconName}': {ex.Message}");
        }
        return cache;
    }

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
    public static class ArmorPlateHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
            if (item == null) return;
            if (item.id.Equals(CheapPlateKey, StringComparison.OrdinalIgnoreCase) && item.Stats.rec.recognizable)
                __result.Item1 = CheapPlateName;
            else if (item.id.Equals(AdvancedPlateKey, StringComparison.OrdinalIgnoreCase) && item.Stats.rec.recognizable)
                __result.Item1 = AdvancedPlateName;
        }
    }
}
