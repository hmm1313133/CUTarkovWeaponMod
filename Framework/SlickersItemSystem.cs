using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 士力架能量棒 - 可供快速食用的甜味能量棒。
/// 4小时腐坏，两次吃完，+7 饱食、-3 水分、+2 心情、+0.15 体重、+22 患病。
/// </summary>
public static class SlickersItemSystem
{
    public const string ItemKey = "slickers";
    public const string DisplayName = "Slickers Energy Bar";

    private const float Weight = 0.15f;
    private const int Value = 5;
    private const int RecognitionMin = 4;
    private const float DecayMinutes = 240f; // 4 hours
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
        Plugin.Log.LogInfo($"[Slickers] Configured spawned item.");
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
                body.Eat(7f, 0.15f);          // +7 饱食，+0.15 体重
                body.thirst -= 3f;             // -3 水分
                body.happiness += 2f;          // +2 心情
                body.sicknessAmount += 22f;    // +22 患病
                Sound.Play("eatCrunch", body.transform.position);

                item.condition -= ConditionCostPerUse;
                if (item.condition <= 0f)
                {
                    item.condition = 0f;
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            };

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo(
                $"[Slickers] Registered '{ItemKey}' as food ({DecayMinutes}min decay, 2 uses).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Slickers] Failed: {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[Slickers] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "foods", "slickers.png");
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
                    _cachedIcon.name = "slickers-icon";
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[Slickers] Icon not found: {iconPath}");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Slickers] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsSlickersRequest(MedicalGrantRequest request) =>
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
