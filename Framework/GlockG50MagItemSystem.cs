using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// SGMT Glock 9x19 50发弹鼓【G 50发】。
/// 容量 50 发，瞄准速度+1s。
/// 世界图标：g 50.png（24x48）；枪械贴图：glock_g50.png（90x82）。
/// </summary>
public static class GlockG50MagItemSystem
{
    public const string ItemKey = "g50_mag";
    public const string BaseGameItemId = "riflemagazine";
    public const int MaxRounds = 50;

    public static string DisplayName => I18n.Tr("glock_g50_mag.name");
    public static string Description => I18n.Tr("glock_g50_mag.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedGunIcon;

    public static bool IsG50MagRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsG50MagRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var ammo = item.GetComponent<AmmoScript>();
        if (ammo != null)
        {
            ammo.itemType = AmmoScript.AmmoItemType.Magazine;
            ammo.ammoType = GunScript.AmmoType.Pistol;
            ammo.maxRounds = MaxRounds;
            ammo.rounds = 0; // 空弹匣（世界生成时由 SpawnCustomItemAt 随机装弹）

            Plugin.Log.LogInfo($"[Glock G50_MAG] Configured AmmoScript: maxRounds={MaxRounds}, rounds={ammo.rounds}");
        }

        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<GlockG50MagItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<GlockG50MagItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[Glock G50_MAG] Configured spawned item '{ItemKey}'.");
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
                Plugin.Log.LogInfo($"[Glock G50_MAG] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[Glock G50_MAG] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Glock G50_MAG] Failed to register '{ItemKey}': {ex}");
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
            weight = 0.5f,
            scaleWeightWithCondition = false,
            combineable = true,
            value = 50,
            tags = "belttool",
            rec = new Recognition(5),
        };

        var useMethod = typeof(GlockG50MagItemSystem).GetMethod(
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
            weight = 0.5f,
            scaleWeightWithCondition = false,
            value = 50,
            tags = "belttool",
            rec = new Recognition(5),
        };

        var useMethod = typeof(GlockG50MagItemSystem).GetMethod(
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

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "g 50.png");

            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "g 50.webp");
                if (!File.Exists(iconPath)) return null;
            }

            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            _cachedIcon = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 26.7f);
            _cachedIcon.name = "glock-g50-mag-icon";
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Glock G50_MAG] Failed to load icon: {ex.Message}");
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

    /// <summary>公开：加载 G 50发 弹鼓的枪械贴图（glock_g50.png，90x82，与格洛克同尺寸）。</summary>
    public static Sprite? TryLoadGunIconPublic() => TryLoadGunIcon();

    private static Sprite? TryLoadGunIcon()
    {
        if (_cachedGunIcon != null) return _cachedGunIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "glock_g50.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedGunIcon = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.75f, 0.75f), 27f);
                    _cachedGunIcon.name = "glock-g50-gun-icon";
                }
            }
            else Plugin.Log.LogWarning($"[Glock G50_MAG] Gun icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Glock G50_MAG] Gun icon: {ex.Message}"); }
        return _cachedGunIcon;
    }
}

public sealed class GlockG50MagItemMarker : MonoBehaviour
{
    public string displayName = GlockG50MagItemSystem.DisplayName;
    public string description = GlockG50MagItemSystem.Description;
}
