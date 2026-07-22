using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 武器维修套件。
/// 可使用4次，每次将手中枪械耐久回满。
/// 重量8.5u，随耐久消耗减少。价值52。
/// </summary>
public static class WeaponRepairKitItemSystem
{
    public const string ItemKey = "weaponrepairkit";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("weaponrepairkit.name");
    public static string Description => I18n.Tr("weaponrepairkit.desc");

    private const float Weight = 8.5f;
    private const int Value = 52;
    private const float DurabilityPerUse = 0.25f; // 4 uses

    private static Sprite? _cachedIcon;

    public static bool IsRepairKitRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsRepairKitRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<WeaponRepairKitItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<WeaponRepairKitItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[RepairKit] Configured spawned item '{ItemKey}' (condition={item.condition}).");
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
                Plugin.Log.LogInfo($"[RepairKit] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[RepairKit] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[RepairKit] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    private static ItemInfo CloneItemInfo(ItemInfo source)
    {
        var clone = new ItemInfo
        {
            fullName = DisplayName,
            description = Description,
            category = "custom",
            slotRotation = source.slotRotation,
            usable = true,
            usableOnLimb = false,
            usableWithLMB = false,
            autoAttack = false,
            rotSpeed = source.rotSpeed,
            useAction = null,
            useLimbAction = null,
            destroyAtZeroCondition = true,
            weight = Weight,
            scaleWeightWithCondition = true,
            combineable = true,
            value = Value,
            tags = "cangetwet,combine",
            rec = new Recognition(8),
        };

        var useMethod = typeof(WeaponRepairKitItemSystem).GetMethod(
            nameof(RepairKitUseAction),
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
            usableWithLMB = false,
            autoAttack = false,
            destroyAtZeroCondition = true,
            combineable = true,
            weight = Weight,
            scaleWeightWithCondition = true,
            value = Value,
            tags = "cangetwet,combine",
            rec = new Recognition(8),
        };

        var useMethod = typeof(WeaponRepairKitItemSystem).GetMethod(
            nameof(RepairKitUseAction), BindingFlags.Static | BindingFlags.NonPublic);
        if (useMethod != null)
        {
            info.useAction = (ItemInfo.Use)Delegate.CreateDelegate(
                typeof(ItemInfo.Use), useMethod);
        }

        info.SetTags();
        return info;
    }

    /// <summary>
    /// 维修套件使用动作：将手中枪械耐久回满，消耗1/4套件耐久。
    /// 直接修改 item.condition，不影响枪上的弹匣和子弹。
    /// </summary>
    private static void RepairKitUseAction(Body body, Item item)
    {
        // 多人模式守卫：仅本地玩家执行
        if (KrokMpHelper.IsMultiplayer && body != PlayerCamera.main?.body) return;

        // 套件已耗尽
        if (item.condition <= 0f) return;

        // 检查手中是否持有物品
        if (!body.HoldingItem(body.handSlot)) return;

        var heldItem = body.GetItem(body.handSlot);
        if (heldItem == null) return;

        // 检查是否为枪械
        var gun = heldItem.GetComponent<GunScript>();
        if (gun == null) return;

        // 枪械已满耐久，不消耗套件
        if (heldItem.condition >= 1f) return;

        // 修理枪械：仅修改 condition，不影响 GunScript 的弹匣/子弹状态
        heldItem.SetCondition(1f);

        // 消耗 1/4 套件耐久
        item.condition -= DurabilityPerUse;
        if (item.condition <= 0f)
        {
            item.condition = 0f;
            UnityEngine.Object.Destroy(item.gameObject);
        }

        Plugin.Log.LogInfo(
            $"[RepairKit] Repaired '{heldItem.id}' to full condition. " +
            $"Kit remaining: {item.condition} (uses left: {Math.Ceiling(item.condition / DurabilityPerUse)})");
    }

    // ===== Icon =====

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "weaponrepairkit", "weaponrepairkit.png");

            if (!File.Exists(iconPath)) return null;

            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            _cachedIcon = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 20f);
            _cachedIcon.name = "weaponrepairkit-icon";
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RepairKit] Failed to load icon: {ex.Message}");
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

/// <summary>
/// 物品标记组件。
/// </summary>
public sealed class WeaponRepairKitItemMarker : MonoBehaviour
{
    public string displayName = WeaponRepairKitItemSystem.DisplayName;
    public string description = WeaponRepairKitItemSystem.Description;
}

/// <summary>
/// 悬停描述补丁。
/// </summary>
// [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class WeaponRepairKitHoverPatch
{
    [HarmonyPostfix]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        return; // Disabled: replaced by UnifiedHoverPatch
        var marker = item.GetComponent<WeaponRepairKitItemMarker>();
        if (marker == null) return;

        if (!item.Stats.rec.recognizable) return;

        // Name updated by I18nRefreshPatch Prefix
        HoverDescriptionHelper.StripEffectsWhenNotExpanded(ref __result);
    }
}
