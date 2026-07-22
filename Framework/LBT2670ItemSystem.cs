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

public static class LBT2670ItemSystem
{
    public const string ItemKey = "6lbt2670";
    public const string BaseGameItemId = "bruisekit";
    public const string WearSlotId = "back";

    public static string DisplayName => I18n.Tr("6lbt2670.name");
    public static string Description => I18n.Tr("6lbt2670.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;

    public static bool IsLBT2670Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    public static float Weight = 2.5f;                        // 重量 2.5u
    public static float WearableIsolation = 0.02f;            // 保温值
    public static int Value = 45;                             // 价值
    public static int RecognitionMin = 3;                    // 识别所需智力
    public static float ContainerCapacity = 10.0f;            // 容器容量 10u
    public static float ContainerMaxWeightPerItem = 3.0f;    // 单物品最大重量 3u
    public static float ContainerEncumbranceReduction = 0.60f; // 重量减免 60%
    public static float WearableHitDurabilityLossMultiplier = 6f; // 可撕裂属性 6点
    public static int WearableVisualOffset = 4;               // 穿戴时 sortingOrder 偏移

    // === 时间衰减 ===
    // 约7小时损坏：7 * 3600 = 25200 秒，每秒衰减 1/25200
    public static float DecayRatePerSecond = 1.0f / 25200.0f;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsLBT2670Request(request)) return;

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
            Plugin.Log.LogInfo($"[LBT2670] Set sprite to 6lbt2670 icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[LBT2670] Icon load failed - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        // 从原版 medkit 预制体复制 tagRestriction，使 SFMP 与原版医疗箱使用相同的物品过滤逻辑
        CopyTagRestrictionFromMedkit(item);

        Plugin.Log.LogInfo($"[LBT2670] Configured spawned item '{ItemKey}'.");
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
                tags = "cangetwet,rippable",
                rec = new Recognition(RecognitionMin),
            };

            info.wearableIsolation = WearableIsolation;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;

            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[LBT2670] Registered '{ItemKey}' as wearable backpack (no armor, decays over time, tearable).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[LBT2670] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[LBT2670] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

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

        Plugin.Log.LogInfo($"[LBT2670] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, Container={customInfo.Container != null}.");
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
            Plugin.Log.LogInfo($"[LBT2670] Backpack deteriorated to zero condition, destroying.");
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "6lbt2670.png");

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
                _cachedIcon.name = "6lbt2670-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[LBT2670] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "6lbt2670.png");

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
                _cachedWornIcon.name = "6lbt2670-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[LBT2670] Failed to load worn icon: {ex.Message}");
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

    /// <summary>
    /// 从原版 medkit 预制体复制 tagRestriction 到 SFMP，使其与原版医疗箱使用相同的物品过滤逻辑。
    /// </summary>
    private static void CopyTagRestrictionFromMedkit(Item item)
    {
        try
        {
            var container = item.GetComponent<Container>();
            if (container == null) return;

            var prefab = Resources.Load<GameObject>("medkit");
            if (prefab == null)
            {
                Plugin.Log.LogWarning("[LBT2670] medkit prefab not found, tagRestriction not copied.");
                return;
            }

            var prefabContainer = prefab.GetComponent<Container>();
            if (prefabContainer == null) return;

            if (prefabContainer.tagRestriction != null && prefabContainer.tagRestriction.Length > 0)
            {
                container.tagRestriction = (string[])prefabContainer.tagRestriction.Clone();
                Plugin.Log.LogInfo($"[LBT2670] Copied tagRestriction from medkit ({prefabContainer.tagRestriction.Length} tags).");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[LBT2670] CopyTagRestrictionFromMedkit failed: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
    public static class LBT2670HoverPatch
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
