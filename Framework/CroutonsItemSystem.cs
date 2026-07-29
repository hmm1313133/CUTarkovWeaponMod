using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 黑麦面包块 - 以俄罗斯传统的酸酵母黑麦面包为原料制作的烤面包块。
/// 不会腐坏，一次吃完，+6 饱食、-3 水分、+1 心情。
/// </summary>
public static class CroutonsItemSystem
{
    public const string ItemKey = "croutons";
    public const string DisplayName = "Rye Croutons";

    private const float Weight = 0.2f;
    private const int Value = 3;
    private const int RecognitionMin = 4;

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
        Plugin.Log.LogInfo($"[Croutons] Configured spawned item.");
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
                value = Value,
                decayMinutes = 0f,
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();

            info.useAction = (body, item) =>
            {
                body.Eat(6f, 0.08f);       // +6 饱食度，+0.08 体重
                body.thirst -= 3f;
                body.happiness += 1f;
                Sound.Play("eatCrunch", body.transform.position);

                UnityEngine.Object.Destroy(item.gameObject);
            };

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo(
                $"[Croutons] Registered '{ItemKey}' as food (no decay, single use).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Croutons] Failed: {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null) customInfo.Icon = icon;
        Plugin.Log.LogInfo($"[Croutons] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "foods", "croutons.png");
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
                    _cachedIcon.name = "croutons-icon";
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[Croutons] Icon not found: {iconPath}");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Croutons] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static bool IsCroutonsRequest(MedicalGrantRequest request) =>
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
