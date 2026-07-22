using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// WARTECH 金雕 BB-102 背包（A-TACSFG迷彩）【Berkut】
///
/// 正面可完全开启的战术背包，占用 back 槽位，不提供防护。
/// 穿戴时持续消耗耐久，约6小时后损坏（destroyAtZeroCondition = true）。
/// 拥有6点可撕裂属性（wearableHitDurabilityLossMultiplier = 6），受击时快速损耗。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite。
/// </summary>
public static class BerkutItemSystem
{
    public const string ItemKey = "berkut";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "back";

    public static string DisplayName => I18n.Tr("berkut.name");
    public static string Description => I18n.Tr("berkut.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsBerkutRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    public static float Weight = 0.6f;                        // 重量 0.6u
    public static float WearableIsolation = 0.02f;            // 保温值
    public static int Value = 30;                              // 价值
    public static int RecognitionMin = 3;                      // 识别所需智力
    public static float ContainerCapacity = 5f;                // 容器容量 5u
    public static float ContainerMaxWeightPerItem = 4f;        // 单物品最大重量 4u
    public static float ContainerEncumbranceReduction = 0.55f; // 重量减免 55%
    public static float WearableHitDurabilityLossMultiplier = 6f; // 可撕裂属性 6点
    public static int WearableVisualOffset = 4;               // 穿戴时 sortingOrder 偏移

    // === 时间衰减 ===
    // 约6小时损坏：6 * 3600 = 21600 秒，每秒衰减 1/21600
    public static float DecayRatePerSecond = 1.0f / 21600.0f;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsBerkutRequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        // CUCoreLib 会覆盖 ItemInfo，需在 ConfigureSpawnedItem 中重新设置
        item.Stats.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
        item.Stats.rotSpeed = DecayRatePerSecond * 100f;
        item.Stats.decayMinutes = (1f / DecayRatePerSecond) / 60f;
        item.Stats.decayInfo = (byte)ItemInfo.DecayType.NoDecayWhenNotWorn;
        item.Stats.tags = "cangetwet,rippable";
        item.Stats.SetTags();
        if (item.Stats.qualities == null) item.Stats.qualities = new List<CraftingQuality>();
        item.Stats.qualities.RemoveAll(q => q.id == "rippable");
        item.Stats.qualities.Add(new CraftingQuality("rippable", WearableHitDurabilityLossMultiplier));

        var container = item.GetComponent<Container>();
        if (container == null) container = item.gameObject.AddComponent<Container>();
        container.maxWeight = ContainerCapacity;
        container.maxWeightPerItem = ContainerMaxWeightPerItem > 0 ? ContainerMaxWeightPerItem : 3f;
        container.encumberanceMult = ContainerEncumbranceReduction;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[Berkut] Set sprite to berkut icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[Berkut] Icon load failed - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[Berkut] Configured spawned item '{ItemKey}'.");
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
                tags = "cangetwet,rippable",
                rec = new Recognition(RecognitionMin),
            };

            info.wearableIsolation = WearableIsolation;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.rotSpeed = DecayRatePerSecond * 100f;
            info.decayInfo = (byte)ItemInfo.DecayType.NoDecayWhenNotWorn;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[Berkut] Registered '{ItemKey}' as wearable backpack (no armor, decays over time, tearable).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Berkut] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[Berkut] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

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

        Plugin.Log.LogInfo($"[Berkut] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, Container={customInfo.Container != null}.");
    }

    // === 时间衰减 ===

    public static void TickDecay()
    {
        // 衰减现在由游戏原生 Item.HandleDecay 通过 rotSpeed + decayInfo(NoDecayWhenNotWorn) 处理
        return;
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
            Plugin.Log.LogInfo($"[Berkut] Backpack deteriorated to zero condition, destroying.");
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "berkut.png");

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
                _cachedIcon.name = "berkut-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Berkut] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "berkut.png");

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
                _cachedWornIcon.name = "berkut-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Berkut] Failed to load worn icon: {ex.Message}");
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
    public static class BerkutHoverPatch
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
