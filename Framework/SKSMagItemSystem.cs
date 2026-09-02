using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// ProMag SKS-A5 7.62x39 20发 SKS 弹匣【SKS-A5】。
/// 适用于 SKS 7.62x39 的 20 发聚合物可拆卸弹匣。
/// 当配件栏卸下 10 发弹仓时可正常安装此弹匣，此时枪械变为半自动弹匣模式。
/// </summary>
public static class SksA5MagItemSystem
{
    public const string ItemKey = "sks_a5_mag";
    public const string BaseGameItemId = "riflemagazine";
    public const int MaxRounds = 20;

    public static string DisplayName => I18n.Tr("sks_a5_mag.name");
    public static string Description => I18n.Tr("sks_a5_mag.desc");

    private static Sprite? _cachedIcon;

    public static bool IsSksA5MagRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsSksA5MagRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var ammo = item.GetComponent<AmmoScript>();
        if (ammo != null)
        {
            ammo.itemType = AmmoScript.AmmoItemType.Magazine;
            ammo.ammoType = GunScript.AmmoType.Rifle;
            ammo.maxRounds = MaxRounds;
            ammo.rounds = 0; // 空弹匣（世界生成时由 SpawnCustomItemAt 随机装弹）

            Plugin.Log.LogInfo($"[SKS_A5_MAG] Configured AmmoScript: maxRounds={MaxRounds}, rounds={ammo.rounds}");
        }

        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<SksA5MagItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<SksA5MagItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[SKS_A5_MAG] Configured spawned item '{ItemKey}'.");
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
                Plugin.Log.LogInfo($"[SKS_A5_MAG] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[SKS_A5_MAG] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SKS_A5_MAG] Failed to register '{ItemKey}': {ex}");
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
            value = 35,
            // 仅 belttool：作为普通弹匣使用，不出现在改枪面板（卸下弹仓后枪自动变 Mag 模式）
            tags = "belttool",
            rec = new Recognition(7),
        };

        var useMethod = typeof(SksA5MagItemSystem).GetMethod(
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
            value = 35,
            // 仅 belttool：作为普通弹匣使用，不出现在改枪面板（卸下弹仓后枪自动变 Mag 模式）
            tags = "belttool",
            rec = new Recognition(7),
        };

        var useMethod = typeof(SksA5MagItemSystem).GetMethod(
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

    // ===== Icon =====

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", "SKS-A5.png");
            if (!File.Exists(iconPath)) return null;

            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            _cachedIcon = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 20f);
            _cachedIcon.name = "sks-a5-mag-icon";
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SKS_A5_MAG] Failed to load icon: {ex.Message}");
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

public sealed class SksA5MagItemMarker : MonoBehaviour
{
    public string displayName = SksA5MagItemSystem.DisplayName;
    public string description = SksA5MagItemSystem.Description;
}

/// <summary>
/// SKS 10 发弹仓改件（默认自带）。
/// 作为 attachment 物品，默认安装在 SKS 上（Direct 弹仓模式）。
/// 卸下它后可安装 SKS-A5 弹匣（切换为 Mag 弹匣模式）。
/// </summary>
public static class SksIntegralMagItemSystem
{
    public const string ItemKey = "sks_integral_mag";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("sks_integral_mag.name");
    public static string Description => I18n.Tr("sks_integral_mag.desc");

    private const float Weight = 0.3f;
    private const int Value = 15;
    private const int RecognitionMin = 4;

    private static Sprite? _cachedIcon;

    public static bool IsSksIntegralMagRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsSksIntegralMagRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "utility";
        item.Stats.tags = "attachment";
        item.Stats.SetTags();
        item.Stats.weight = Weight;
        item.Stats.value = Value;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[SKS_INTEGRAL] Configured spawned item '{ItemKey}'.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey)) return false;
        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "utility",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                usableWithLMB = false,
                autoAttack = false,
                destroyAtZeroCondition = true,
                weight = Weight,
                value = Value,
                tags = "attachment",
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[SKS_INTEGRAL] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[SKS_INTEGRAL] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null)
        {
            customInfo.Icon = icon;
            customInfo.WornSprite = icon;
            customInfo.WornSpriteOffset = Vector2.zero;
        }
        Plugin.Log.LogInfo($"[SKS_INTEGRAL] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", "SKS10round.png");
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
                        new Vector2(0.5f, 0.5f), 14f);
                    _cachedIcon.name = "sks-integral-mag-icon";
                }
            }
            else Plugin.Log.LogWarning($"[SKS_INTEGRAL] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[SKS_INTEGRAL] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

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
