using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// "Big Stick" Glock 9x19 加长弹匣【Big Stick】。
/// 容量 33 发，瞄准速度+0.2s。
/// 世界图标：big stick.png（48x46）；枪械贴图：glock_bigstick.png（90x82）。
/// </summary>
public static class GlockBigStickMagItemSystem
{
    public const string ItemKey = "bigstick_mag";
    public const string BaseGameItemId = "riflemagazine";
    public const int MaxRounds = 33;

    public static string DisplayName => I18n.Tr("glock_bigstick_mag.name");
    public static string Description => I18n.Tr("glock_bigstick_mag.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedGunIcon;

    public static bool IsBigStickMagRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsBigStickMagRequest(request)) return;

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

            Plugin.Log.LogInfo($"[Glock BigStick_MAG] Configured AmmoScript: maxRounds={MaxRounds}, rounds={ammo.rounds}");
        }

        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<GlockBigStickMagItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<GlockBigStickMagItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[Glock BigStick_MAG] Configured spawned item '{ItemKey}'.");
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
                Plugin.Log.LogInfo($"[Glock BigStick_MAG] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[Glock BigStick_MAG] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Glock BigStick_MAG] Failed to register '{ItemKey}': {ex}");
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
            weight = 0.3f,
            scaleWeightWithCondition = false,
            combineable = true,
            value = 33,
            tags = "belttool",
            rec = new Recognition(5),
        };

        var useMethod = typeof(GlockBigStickMagItemSystem).GetMethod(
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
            weight = 0.3f,
            scaleWeightWithCondition = false,
            value = 33,
            tags = "belttool",
            rec = new Recognition(5),
        };

        var useMethod = typeof(GlockBigStickMagItemSystem).GetMethod(
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "big stick.png");

            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "big stick.webp");
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
            _cachedIcon.name = "glock-bigstick-mag-icon";
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Glock BigStick_MAG] Failed to load icon: {ex.Message}");
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

    /// <summary>公开：加载 Big Stick 弹匣的枪械贴图（glock_bigstick.png，90x82，与格洛克同尺寸）。</summary>
    public static Sprite? TryLoadGunIconPublic() => TryLoadGunIcon();

    private static Sprite? TryLoadGunIcon()
    {
        if (_cachedGunIcon != null) return _cachedGunIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "glock", "glock_bigstick.png");
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
                    _cachedGunIcon.name = "glock-bigstick-gun-icon";
                }
            }
            else Plugin.Log.LogWarning($"[Glock BigStick_MAG] Gun icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Glock BigStick_MAG] Gun icon: {ex.Message}"); }
        return _cachedGunIcon;
    }
}

public sealed class GlockBigStickMagItemMarker : MonoBehaviour
{
    public string displayName = GlockBigStickMagItemSystem.DisplayName;
    public string description = GlockBigStickMagItemSystem.Description;
}
