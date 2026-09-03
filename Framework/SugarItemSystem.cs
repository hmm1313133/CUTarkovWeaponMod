using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 一包糖 - 一包方糖。在食物短缺的状况下是很珍贵的东西，它也可以被用作酿造。
/// 虽然你挺喜欢吃甜食，不过这样干吃真的齁的难受。
/// 10小时腐坏，8次吃完，+6 饱食、-4 水分、+0.12 体重、-0.2 心情、+2 患病。
/// </summary>
public static class SugarItemSystem
{
    public const string ItemKey = "sugar";
    public const string DisplayName = "Pack of Sugar";

    private const float Weight = 0.5f;
    private const int Value = 13;
    private const int RecognitionMin = 4;
    private const float DecayMinutes = 600f; // 10 hours
    private const float ConditionCostPerUse = 0.125f; // 8 uses

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
        Plugin.Log.LogInfo($"[Sugar] Configured spawned item.");
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
                if (!KrokMpHelper.ShouldApplyUseEffect(body, item)) return;
                body.Eat(6f, 0.12f);          // +6 饱食，+0.12 体重
                body.thirst -= 4f;             // -4 水分
                body.happiness -= 0.2f;        // -0.2 心情
                body.sicknessAmount += 2f;     // +2 患病
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
                $"[Sugar] Registered '{ItemKey}' as food ({DecayMinutes}min decay, 8 uses).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Sugar] Failed: {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[Sugar] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "foods", "sugar.png");
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
                    _cachedIcon.name = "sugar-icon";
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[Sugar] Icon not found: {iconPath}");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Sugar] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsSugarRequest(MedicalGrantRequest request) =>
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
