using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// SSO Attack 2 突击背包（卡其色）【Attack 2】
///
/// Ataka 2 型突击背包容量 60 升，占用 back 槽位，不提供防护。
/// 穿戴时持续消耗耐久，约7.8小时后损坏（destroyAtZeroCondition = true）。
/// 拥有8点可撕裂属性（wearableHitDurabilityLossMultiplier = 8），受击时快速损耗。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite。
/// </summary>
public static class SsoAttack2ItemSystem
{
    public const string ItemKey = "ssoattack2";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "back";

    public static string DisplayName => I18n.Tr("ssoattack2.name");
    public static string Description => I18n.Tr("ssoattack2.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsSsoAttack2Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    public static float Weight = 1.5f;                        // 重量 1.5u
    public static float WearableIsolation = 0.02f;            // 保温值
    public static int Value = 42;                              // 价值
    public static int RecognitionMin = 3;                      // 识别所需智力
    public static float ContainerCapacity = 7.2f;              // 容器容量 7.2u
    public static float ContainerMaxWeightPerItem = 5.5f;      // 单物品最大重量 5.5u
    public static float ContainerEncumbranceReduction = 0.66f; // 重量减免 66%
    public static float WearableHitDurabilityLossMultiplier = 8f; // 可撕裂属性 8点
    public static int WearableVisualOffset = 4;               // 穿戴时 sortingOrder 偏移

    // === 时间衰减 ===
    // 约7.8小时损坏：7.8 * 3600 = 28080 秒，每秒衰减 1/28080
    public static float DecayRatePerSecond = 1.0f / 28080.0f;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsSsoAttack2Request(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[SsoAttack2] Set sprite to ssoattack2 icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[SsoAttack2] Icon load failed - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[SsoAttack2] Configured spawned item '{ItemKey}'.");
    }

    public static void ForceApplyIcon(Item item)
    {
        if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase)) return;
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
            sr.sprite = icon;
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "container",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = true,
                wearable = true,
                desiredWearLimb = "UpTorso",
                wearSlotId = WearSlotId,
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                tags = "cangetwet",
                rec = new Recognition(RecognitionMin),
            };

            info.wearableIsolation = WearableIsolation;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;

            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[SsoAttack2] Registered '{ItemKey}' as wearable backpack (no armor, decays over time, tearable).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SsoAttack2] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[SsoAttack2] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

        if (wornIcon != null)
        {
            customInfo.WornSprite = wornIcon;
            customInfo.WornSpriteOffset = new Vector2(0f, 0f);
        }

        if (icon != null)
        {
            customInfo.Icon = icon;
        }

        if (ContainerCapacity > 0)
        {
            customInfo.Container = new ContainerProperties
            {
                Capacity = ContainerCapacity,
                MaxWeightPerItem = ContainerMaxWeightPerItem > 0 ? ContainerMaxWeightPerItem : 3f,
                EncumbranceReduction = ContainerEncumbranceReduction,
            };
        }

        Plugin.Log.LogInfo($"[SsoAttack2] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, Container={customInfo.Container != null}.");
    }

    // === 时间衰减 ===

    public static void TickDecay()
    {
        var cam = PlayerCamera.main;
        if (cam == null) return;
        var body = cam.body;
        if (body == null) return;

        var item = body.GetWearableBySlotID(WearSlotId);
        if (item == null) return;
        if (!item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase)) return;

        var newCondition = item.condition - DecayRatePerSecond * Time.deltaTime;

        if (newCondition <= 0f)
        {
            Plugin.Log.LogInfo($"[SsoAttack2] Backpack deteriorated to zero condition, destroying.");
            item.condition = 0f;
            UnityEngine.Object.Destroy(item.gameObject);
        }
        else
        {
            item.condition = newCondition;
        }
    }

    // === Icon ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "ssoattack2.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 6f);
                _cachedIcon.name = "ssoattack2-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SsoAttack2] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "ssoattack2.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedWornIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 7f);
                _cachedWornIcon.name = "ssoattack2-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SsoAttack2] Failed to load worn icon: {ex.Message}");
        }

        return _cachedWornIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

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

    // === 悬停描述 ===

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class SsoAttack2HoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                return;
            if (!item.Stats.rec.recognizable) return;
            __result.Item1 = DisplayName;
        }
    }
}
