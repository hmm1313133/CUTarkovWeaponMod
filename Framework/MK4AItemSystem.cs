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
/// CQC 鱼鹰 MK4A 防弹胸挂（突击型，多地形迷彩）
///
/// 防弹胸挂，占用胸部（outertorso）和弹挂（bandolier）槽位。
/// 通过 CUCoreLib CustomItemInfo 注册，设置 wearable + Container + WornSprite + MultiWornSprites。
/// 主肢体 UpTorso（胸部57%减伤），额外肢体 DownTorso（腹部45%）+ UpperArmL/R（上臂30%）。
/// 不同部位减伤通过 Mk4aArmorPatch 在 Limb.GetArmorReduction 上实现。
/// 弹挂功能由 Container 组件提供（可装物品）。
/// </summary>
public static class MK4AItemSystem
{
    public const string ItemKey = "mk4a";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("mk4a.name");
    public static string Description => I18n.Tr("mk4a.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedWornIcon;
    private static Sprite? _cachedArmIcon;

    public static bool IsMK4ARequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    // === 数值 ===
    // 减伤44.0%: 1/(1+a) = 0.560, a = 1/0.560 - 1 = 0.7857
    public static float WearableArmor = 0.7857f;        // 所有部位统一减伤44.0%
    public static float Weight = 3.7f;                  // 重量 3.7u
    public static float WearableHitDurabilityLossMultiplier = 0.15f; // 被击中耐久损失倍率
    public static float WearableIsolation = 0.13f;      // 保温值
    public static int Value = 55;                       // 价值
    public static int RecognitionMin = 6;               // 识别所需智力
    public static float ContainerCapacity = 3.5f;       // 容器容量 3.5u
    public static float ContainerMaxWeightPerItem = 2f; // 单物品最大重量 2u
    public static float ContainerEncumbranceReduction = 0.35f; // 重量减免 65%
    public static int WearableVisualOffset = 5;         // 穿戴时 sortingOrder 偏移

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsMK4ARequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (icon != null && sr != null)
        {
            sr.sprite = icon;
            Plugin.Log.LogInfo($"[MK4A] Set sprite to mk4a icon ({icon.texture.width}x{icon.texture.height}).");
        }
        else
        {
            Plugin.Log.LogWarning($"[MK4A] Icon load failed - will keep base prefab sprite.");
        }

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[MK4A] Configured spawned item '{ItemKey}'.");
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
                category = "utility",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                wearable = true,
                desiredWearLimb = "UpTorso",
                wearSlotId = "outertorso",
                wearableVisualOffset = WearableVisualOffset,
                weight = Weight,
                value = Value,
                tags = "cangetwet",
                rec = new Recognition(RecognitionMin),
            };

            info.wearableArmor = WearableArmor;
            info.wearableHitDurabilityLossMultiplier = WearableHitDurabilityLossMultiplier;
            info.wearableIsolation = WearableIsolation;

            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[MK4A] Registered '{ItemKey}' as wearable vest.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[MK4A] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        var wornIcon = TryLoadWornIcon();

        Plugin.Log.LogInfo($"[MK4A] RegisterWithCUCoreLib: icon={icon != null}, wornIcon={wornIcon != null}");

        if (wornIcon != null)
        {
            customInfo.WornSprite = wornIcon;
            customInfo.WornSpriteOffset = new Vector2(0f, 0f);
        }

        if (icon != null)
        {
            customInfo.Icon = icon;
        }

        // 多部位防护：腹部（透明占位）+ 大臂（UpArmF/B）
        // DownTorso 用透明 sprite 占位，确保 secondaryLimbs 包含腹部减伤
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, new Color(0, 0, 0, 0));
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        var downPlaceholder = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        downPlaceholder.name = "mk4a-down-empty";
        customInfo.MultiWornSprites["DownTorso"] = downPlaceholder;

        var armSprite = TryLoadArmIcon();
        if (armSprite != null)
        {
            customInfo.MultiWornSprites["UpArmF"] = armSprite;
            customInfo.MultiWornSprites["UpArmB"] = armSprite;
            Plugin.Log.LogInfo($"[MK4A] Set MultiWornSprites: DownTorso + UpArmF/B loaded.");
        }
        else
        {
            Plugin.Log.LogWarning($"[MK4A] mk4a_arm.png not found, arm protection will have no custom sprite.");
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

        Plugin.Log.LogInfo($"[MK4A] Configured CUCoreLib: WornSprite={customInfo.WornSprite != null}, Icon={customInfo.Icon != null}, MultiWornSprites={customInfo.MultiWornSprites?.Count ?? 0}, Container={customInfo.Container != null}.");
    }

    // === Icon ===

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "mk4a.png");

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
                _cachedIcon.name = "mk4a-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[MK4A] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadWornIcon()
    {
        if (_cachedWornIcon != null) return _cachedWornIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "mk4a_up.png");

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
                _cachedWornIcon.name = "mk4a-worn";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[MK4A] Failed to load worn icon: {ex.Message}");
        }

        return _cachedWornIcon;
    }

    private static Sprite? TryLoadArmIcon()
    {
        if (_cachedArmIcon != null) return _cachedArmIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "mk4a_arm.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedArmIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 7f);
                _cachedArmIcon.name = "mk4a-arm";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[MK4A] Failed to load arm icon: {ex.Message}");
        }

        return _cachedArmIcon;
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
    public static class MK4AHoverPatch
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

    // === 多部位防护（UpTorso + DownTorso + UpArmF/B） ===
    //
    // 通过 CUCoreLib CustomItemInfo.MultiWornSprites 实现 secondaryLimbs。
    // 所有部位统一使用 wearableArmor 减伤值（54%）。
    // 无需额外的 Harmony 补丁。

    // === 双槽位锁定 ===

    public static class MK4ADualSlotPatch
    {
        private static bool _inPatch;

        public static void Postfix(Body __instance, string id, ref Item __result)
        {
            if (_inPatch) return;

            if (id == "bandolier" && __result == null)
            {
                _inPatch = true;
                try
                {
                    var upTorsoItem = __instance.GetWearableBySlotID("outertorso");
                    if (upTorsoItem != null &&
                        upTorsoItem.id.Equals(ItemKey, StringComparison.OrdinalIgnoreCase))
                    {
                        __result = upTorsoItem;
                    }
                }
                finally
                {
                    _inPatch = false;
                }
            }
        }
    }
}
