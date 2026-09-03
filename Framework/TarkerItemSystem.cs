using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 塔克肉干 - 真空包装的牛肉干。超好吃。
/// 不腐坏，三次吃完，+6 饱食、-2 水分、+1.3 心情、+0.12 体重。
/// </summary>
public static class TarkerItemSystem
{
    public const string ItemKey = "tarker";
    public const string DisplayName = "Tarker Beef Jerky";

    private const float Weight = 0.15f;
    private const int Value = 8;
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
        Plugin.Log.LogInfo($"[Tarker] Configured spawned item.");
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
                decayMinutes = 0f, // 不腐坏
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();

            info.useAction = (body, item) =>
            {
                if (!KrokMpHelper.ShouldApplyUseEffect(body, item)) return;
                body.Eat(6f, 0.12f);          // +6 饱食，+0.12 体重
                body.thirst -= 2f;            // -2 水分
                body.happiness += 1.3f;      // +1.3 心情
                Sound.Play("eatFlesh", body.transform.position); // 吃肉音效

                item.condition -= ConditionCostPerUse;
                if (item.condition <= 0f)
                {
                    item.condition = 0f;
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            };

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo(
                $"[Tarker] Registered '{ItemKey}' as food (no decay, 3 uses).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Tarker] Failed: {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[Tarker] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "foods", "tarker.png");
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
                        new Vector2(0.5f, 0.5f), 16f);
                    _cachedIcon.name = "tarker-icon";
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[Tarker] Icon not found: {iconPath}");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Tarker] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsTarkerRequest(MedicalGrantRequest request) =>
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
