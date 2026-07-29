using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 煮熟的方便面 - 泡好了的方便面。色香味俱全，这下不用当作干脆面吃了。
/// 2小时腐坏，2次吃完，+13 饱食、+7 水分、+0.14 体重、+1.5 心情。
/// 仅通过合成获取，不在世界生成。
/// </summary>
public static class CookedNoodlesItemSystem
{
    public const string ItemKey = "cookednoodles";
    public const string DisplayName = "Cooked Noodles";

    private const float Weight = 0.7f;
    private const int Value = 0;
    private const int RecognitionMin = 4;
    private const float DecayMinutes = 120f; // 2 hours
    private const float ConditionCostPerUse = 0.5f; // 2 uses

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
        Plugin.Log.LogInfo($"[CookedNoodles] Configured spawned item.");
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
                decayMinutes = DecayMinutes,
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();

            info.useAction = (body, item) =>
            {
                if (item.condition <= 0f) return;
                body.Eat(13f, 0.14f);
                body.thirst += 7f;
                body.happiness += 1.5f;
                Sound.Play("eatFlesh", body.transform.position);
                item.condition -= ConditionCostPerUse;
            };

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[CookedNoodles] Registered '{ItemKey}' as food ({DecayMinutes}min decay, 2 uses, craft-only).");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[CookedNoodles] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[CookedNoodles] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "foods", "cookednoodles.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedIcon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
                    _cachedIcon.name = "cookednoodles-icon";
                }
            }
            else Plugin.Log.LogWarning($"[CookedNoodles] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[CookedNoodles] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsCookedNoodlesRequest(MedicalGrantRequest request) =>
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
