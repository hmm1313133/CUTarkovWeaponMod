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
/// Partizan的包【Partizan】
///
/// 饱经风霜的 Gorka 包，占用 back 槽位，不提供防护。
/// 穿戴时持续消耗耐久，约6小时后损坏（destroyAtZeroCondition = true）。
/// 拥有6点可撕裂属性（wearableHitDurabilityLossMultiplier = 6），受击时快速损耗。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite。
/// </summary>
public static class PartizanItemSystem
{
    public const string ItemKey = "partizan";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "back";

    public static string DisplayName => I18n.Tr("partizan.name");
    public static string Description => I18n.Tr("partizan.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsPartizanRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    public static float Weight = 0.4f;                        // 重量 0.4u
    public static float WearableIsolation = 0.02f;            // 保温值
    public static int Value = 35;                              // 价值
    public static int RecognitionMin = 3;                      // 识别所需智力
    public static float ContainerCapacity = 5.5f;              // 容器容量 5.5u
    public static float ContainerMaxWeightPerItem = 3f;        // 单物品最大重量 3u
    public static float ContainerEncumbranceReduction = 0.64f; // 重量减免 36%
    public static float WearableHitDurabilityLossMultiplier = 0f; // 背包不受击减耐久
    public static float RippableAmount = 6f; // 可撕裂属性 6点
    public static int WearableVisualOffset = 4;               // 穿戴时 sortingOrder 偏移

    // === 时间衰减 ===
    // 约6小时损坏：6 * 3600 = 21600 秒，每秒衰减 1/21600
    public static float DecayRatePerSecond = 1.0f / 21600.0f;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsPartizanRequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        item.Stats.tags = "cangetwet,rippable";
        item.Stats.SetTags();
        if (item.Stats.qualities == null) item.Stats.qualities = new List<CraftingQuality>();
        item.Stats.qualities.RemoveAll(q => q.id == "rippable");
        item.Stats.qualities.Add(new CraftingQuality("rippable", RippableAmount));

        item.Stats.rotSpeed = DecayRatePerSecond * 100f;
        item.Stats.decayMinutes = (1f / DecayRatePerSecond) / 60f;
        item.Stats.decayInfo = (byte)ItemInfo.DecayType.NoDecayWhenNotWorn;

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
            Plugin.Log.LogInfo($"[Partizan] Set sprite to partizan icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[Partizan] Icon load failed - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[Partizan] Configured spawned item '{ItemKey}'.");
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
            info.decayMinutes = (1f / DecayRatePerSecond) / 60f;
            info.decayInfo = (byte)ItemInfo.DecayType.NoDecayWhenNotWorn;
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[Partizan] Registered '{ItemKey}' as wearable backpack (no armor, decays over time, tearable).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Partizan] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[Partizan] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

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

        Plugin.Log.LogInfo($"[Partizan] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, Container={customInfo.Container != null}.");
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
            Plugin.Log.LogInfo($"[Partizan] Backpack deteriorated to zero condition, destroying.");
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "partizan.png");

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
                _cachedIcon.name = "partizan-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Partizan] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "partizan.png");

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
                _cachedWornIcon.name = "partizan-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Partizan] Failed to load worn icon: {ex.Message}");
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

    // [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class PartizanHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item item, ref (string, string) __result)
        {
        return; // Disabled: replaced by UnifiedHoverPatch
            if (item == null || !item.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                return;
            if (!item.Stats.rec.recognizable) return;
            __result.Item1 = DisplayName;
        }
    }
}
