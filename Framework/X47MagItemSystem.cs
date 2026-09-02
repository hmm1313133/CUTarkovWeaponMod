using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

// ===== X-47 弹鼓 (7.62x39, 50发) =====

public static class X47MagItemSystem
{
    public const string ItemKey = "x47mag";
    public const string BaseGameItemId = "riflemagazine";
    public const int MaxRounds = 50;

    public static string DisplayName => I18n.Tr("x47mag.name");
    public static string Description => I18n.Tr("x47mag.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedDrumVisual;

    public static bool IsX47MagRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsX47MagRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var ammo = item.GetComponent<AmmoScript>();
        if (ammo != null)
        {
            ammo.itemType = AmmoScript.AmmoItemType.Magazine;
            ammo.ammoType = GunScript.AmmoType.Rifle;
            ammo.maxRounds = MaxRounds;
            ammo.rounds = 0; // 空弹鼓（世界生成时随机装弹）

            Plugin.Log.LogInfo($"[X47] Configured AmmoScript: maxRounds={MaxRounds}, rounds={ammo.rounds}");
        }

        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<X47MagItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<X47MagItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[X47] Configured spawned item '{ItemKey}'.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            if (Item.GlobalItems.TryGetValue(BaseGameItemId, out var source))
            {
                Item.GlobalItems[ItemKey] = CloneItemInfo(source);
                Plugin.Log.LogInfo($"[X47] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[X47] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[X47] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    private static ItemInfo CloneItemInfo(ItemInfo source)
    {
        var clone = new ItemInfo
        {
            fullName = DisplayName,
            description = Description,
            category = source.category,
            slotRotation = source.slotRotation,
            usable = true,
            usableOnLimb = false,
            destroyAtZeroCondition = true,
            weight = 0.65f,
            scaleWeightWithCondition = false,
            combineable = true,
            value = 55,
            tags = "belttool",
            rec = new Recognition(5),
        };

        var useMethod = typeof(X47MagItemSystem).GetMethod(
            nameof(MagUseAction),
            BindingFlags.Static | BindingFlags.NonPublic);
        if (useMethod != null)
        {
            clone.useAction = (ItemInfo.Use)Delegate.CreateDelegate(
                typeof(ItemInfo.Use), useMethod);
        }

        clone.SetTags();
        return clone;
    }

    private static ItemInfo CreateFallbackItemInfo()
    {
        var info = new ItemInfo
        {
            fullName = DisplayName,
            description = Description,
            category = "custom",
            slotRotation = -90f,
            usable = true,
            usableOnLimb = false,
            destroyAtZeroCondition = true,
            combineable = true,
            weight = 0.65f,
            scaleWeightWithCondition = false,
            value = 55,
            tags = "belttool",
            rec = new Recognition(5),
        };

        var useMethod = typeof(X47MagItemSystem).GetMethod(
            nameof(MagUseAction), BindingFlags.Static | BindingFlags.NonPublic);
        if (useMethod != null)
        {
            info.useAction = (ItemInfo.Use)Delegate.CreateDelegate(
                typeof(ItemInfo.Use), useMethod);
        }

        info.SetTags();
        return info;
    }

    private static void MagUseAction(Body body, Item item)
    {
        var ammo = item.GetComponent<AmmoScript>();
        if (ammo != null)
            ammo.UnloadRound();
    }

    // ===== 弹鼓视觉（装在枪上时）=====

    /// <summary>弹鼓装到枪上时显示的 sprite（PPI 14，与枪身贴图一致）。</summary>
    public static Sprite? TryLoadDrumVisualPublic() => TryLoadDrumVisual();

    private static Sprite? TryLoadDrumVisual()
    {
        if (_cachedDrumVisual != null) return _cachedDrumVisual;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "akm", "x47.png");
            if (!File.Exists(iconPath)) return null;

            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            _cachedDrumVisual = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 14f);
            _cachedDrumVisual.name = "x47-drum-visual";
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[X47] Drum visual: {ex.Message}"); }
        return _cachedDrumVisual;
    }

    // ===== Icon =====

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "akm", "x47.png");

            if (!File.Exists(iconPath))
                return null;

            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            _cachedIcon = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 20f);
            _cachedIcon.name = "x47-mag-icon";
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[X47] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static void ResizeColliderToSprite(Item item)
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

public sealed class X47MagItemMarker : MonoBehaviour
{
    public string displayName = X47MagItemSystem.DisplayName;
    public string description = X47MagItemSystem.Description;
}
