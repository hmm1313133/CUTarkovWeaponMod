using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// MRE 个人即食口粮 - 为美国陆军量身打造的单兵作战口粮。
/// 不腐坏，3次吃完，+20 饱食、+3 水分、+0.2 体重、+1.5 心情。
/// </summary>
public static class MreItemSystem
{
    public const string ItemKey = "mre";
    public const string DisplayName = "MRE Field Ration";

    private const float Weight = 1f;
    private const int Value = 23;
    private const int RecognitionMin = 4;
    private const float ConditionCostPerUse = 0.334f; // ~3 uses

    private static Sprite? _cachedIcon;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest? request = null)
    {
        if (item == null) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[MRE] Configured spawned item.");
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
                category = "food",
                slotRotation = 0f,
                usable = true,
                usableOnLimb = false,
                destroyAtZeroCondition = true,
                weight = Weight,
                scaleWeightWithCondition = true,
                value = Value,
                decayMinutes = 0f,
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();

            info.useAction = (body, item) =>
            {
                if (item.condition <= 0f) return;
                body.Eat(20f, 0.2f);
                body.thirst += 3f;
                body.happiness += 1.5f;
                Sound.Play("eatFlesh", body.transform.position);
                item.condition -= ConditionCostPerUse;
            };

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[MRE] Registered '{ItemKey}' as food (no decay, 3 uses).");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[MRE] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[MRE] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "foods", "mre.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 8f);
                    _cachedIcon.name = "mre-icon";
                }
            }
            else Plugin.Log.LogWarning($"[MRE] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[MRE] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsMreRequest(MedicalGrantRequest request) =>
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
