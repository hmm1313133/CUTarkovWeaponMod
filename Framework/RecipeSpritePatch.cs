using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 拦截 Recipe.resultSprite getter —— 为自定义弹药提供正确的图标。
///
/// 原版逻辑（非液体结果）：
///   Resources.Load&lt;GameObject&gt;(result.id).GetComponent&lt;SpriteRenderer&gt;().sprite
/// 自定义弹药 ID 不在游戏 Resources 中，Resources.Load 返回 null，
/// 导致 NullRefException 或显示错误图标（如植物纤维袋）。
///
/// 此 Prefix 对自定义弹药 ID 跳过原版逻辑，直接返回我们的自定义 Sprite。
/// </summary>
[HarmonyPatch(typeof(Recipe), "resultSprite", MethodType.Getter)]
public static class RecipeSpritePatch
{
    // 自定义弹药 ID → 缓存 Sprite 的映射
    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    // 所有自定义弹药 ItemKey 列表
    private static readonly HashSet<string> CustomAmmoIds = new()
    {
        Ammo338UCWItemSystem.ItemKey,
        Ammo76251BPZItemSystem.ItemKey,
        Ammo76239SPItemSystem.ItemKey,
        Ammo12g85ItemSystem.ItemKey,
        Ammo50CopperItemSystem.ItemKey,
        Ammo45FMJItemSystem.ItemKey,
        Ammo919PSOItemSystem.ItemKey,
        Ammo55645FMJItemSystem.ItemKey,
        Ammo5728SB193ItemSystem.ItemKey,
    };

    // 所有自定义弹匣 ItemKey 列表
    private static readonly HashSet<string> CustomMagIds = new()
    {
        AXMCMagItemSystem.ItemKey,
        DVL10MagItemSystem.ItemKey,
        AKMMagItemSystem.ItemKey,
        DeagleMagItemSystem.ItemKey,
        Glock17MagItemSystem.ItemKey,
        M4A1MagItemSystem.ItemKey,
        P90MagItemSystem.ItemKey,
        UMP45MagItemSystem.ItemKey,
        RPDMagItemSystem.ItemKey,
        USPMagItemSystem.ItemKey,
    };

    // 所有自定义护甲 ItemKey 列表
    private static readonly HashSet<string> CustomArmorIds = new()
    {
        MBSSItemSystem.ItemKey,
        TV115ItemSystem.ItemKey,
        TV110ItemSystem.ItemKey,
        SPPCV2ItemSystem.ItemKey,
        MK4AItemSystem.ItemKey,
        SiegeRItemSystem.ItemKey,
        SixB516ItemSystem.ItemKey,
        // 新增护甲和插板
        SixB45ItemSystem.ItemKey,
        LV119ItemSystem.ItemKey,
        IDEAItemSystem.ItemKey,
        BankRobberItemSystem.ItemKey,
        Type56ItemSystem.ItemKey,
        WTChestRigItemSystem.ItemKey,
        LBCRItemSystem.ItemKey,
        CommandoItemSystem.ItemKey,
        UmkaItemSystem.ItemKey,
        BlackRockItemSystem.ItemKey,
        PACAItemSystem.ItemKey,
        MFUNItemSystem.ItemKey,
        DRDItemSystem.ItemKey,
        ThorItemSystem.ItemKey,
        TrooperItemSystem.ItemKey,
        SixB13ItemSystem.ItemKey,
        HPCItemSystem.ItemKey,
        GzhelKItemSystem.ItemKey,
        RedutT5ItemSystem.ItemKey,
        SlickItemSystem.ItemKey,
        HGridItemSystem.ItemKey,
        SixB43ItemSystem.ItemKey,
        ArmorPlateItemSystem.CheapPlateKey,
        ArmorPlateItemSystem.AdvancedPlateKey,
        RysTItemSystem.ItemKey,
        // 背包
        LK3FItemSystem.ItemKey,
        ReadyPackItemSystem.ItemKey,
        PartizanItemSystem.ItemKey,
        DayPackItemSystem.ItemKey,
        BerkutItemSystem.ItemKey,
        ScavPackItemSystem.ItemKey,
        MysteryRanch2DayItemSystem.ItemKey,
        PilgrimItemSystem.ItemKey,
        SsoAttack2ItemSystem.ItemKey,
        SH118ItemSystem.ItemKey,
        LBT2670ItemSystem.ItemKey,
    };

    // 弹匣 ItemKey → 图标文件名的映射（文件名不含扩展名）
    private static readonly Dictionary<string, string> MagIconFileMap = new()
    {
        { AXMCMagItemSystem.ItemKey, "axmc_magazine" },
        { DVL10MagItemSystem.ItemKey, "dvl10_magazine" },
        { AKMMagItemSystem.ItemKey, "akm_magazine" },
        { DeagleMagItemSystem.ItemKey, "deagle_magazine" },
        { Glock17MagItemSystem.ItemKey, "glock_magazine" },
        { M4A1MagItemSystem.ItemKey, "m4a1_magazine" },
        { P90MagItemSystem.ItemKey, "p90_magazine" },
        { UMP45MagItemSystem.ItemKey, "ump45_magazine" },
        { RPDMagItemSystem.ItemKey, "rpd_magazine" },
        { USPMagItemSystem.ItemKey, "usp_magazine" },
    };

    // 护甲 ItemKey -> 图标文件名的映射（仅 ItemKey 与文件名不一致的条目）
    private static readonly Dictionary<string, string> ArmorIconFileMap = new()
    {
        { RedutT5ItemSystem.ItemKey, "RedutT5" },
        { GzhelKItemSystem.ItemKey, "GZHEL_K" },
    };

    // 弹匣图标文件名 -> 所在枪械文件夹名的映射
    private static readonly Dictionary<string, string> MagIconDirMap = new()
    {
        { "axmc_magazine", "ax" },
        { "dvl10_magazine", "dvl10" },
        { "akm_magazine", "akm" },
        { "deagle_magazine", "deagle" },
        { "glock_magazine", "glock" },
        { "m4a1_magazine", "m4" },
        { "p90_magazine", "p90" },
        { "ump45_magazine", "ump45" },
        { "rpd_magazine", "rpd" },
        { "usp_magazine", "usp" },
    };

    [HarmonyPrefix]
    public static bool Prefix(Recipe __instance, ref ValueTuple<Sprite, Color> __result)
    {
        var resultId = __instance.result?.id;
        if (resultId == null)
            return true; // 无结果ID，走原版逻辑

        bool isCustomAmmo = CustomAmmoIds.Contains(resultId);
        bool isCustomMag = CustomMagIds.Contains(resultId);
        bool isCustomArmor = CustomArmorIds.Contains(resultId);

        if (!isCustomAmmo && !isCustomMag && !isCustomArmor)
            return true; // 不是自定义弹药/弹匣/护甲，走原版逻辑

        // 图标文件名可能与 ItemKey 不同，需映射
        string iconFileName;
        if (isCustomMag && MagIconFileMap.TryGetValue(resultId, out var magIcon))
            iconFileName = magIcon;
        else if (isCustomArmor && ArmorIconFileMap.TryGetValue(resultId, out var armorIcon))
            iconFileName = armorIcon;
        else
            iconFileName = resultId;

        // 确定图标所在子目录
        string iconSubDir;
        if (isCustomArmor)
            iconSubDir = "equipment";
        else if (isCustomMag && MagIconDirMap.TryGetValue(iconFileName, out var gunFolder))
            iconSubDir = Path.Combine("guns", gunFolder);
        else
            iconSubDir = "ammo";

        var sprite = GetOrLoadSprite(iconFileName, iconSubDir);
        if (sprite != null)
        {
            __result = new ValueTuple<Sprite, Color>(sprite, Color.white);
            return false; // 跳过原版逻辑，返回我们的自定义图标
        }

        // 自定义图标加载失败，也跳过原版（避免 NullRefException）
        // 使用原版同类物品的图标作为 fallback
        var fallbackSprite = GetFallbackSprite(resultId) ?? GetBaseGameSprite(resultId);
        if (fallbackSprite == null)
            return true; // 完全找不到图标，放行原版
        __result = new ValueTuple<Sprite, Color>(fallbackSprite, Color.white);
        return false;
    }

    /// <summary>
    /// 从插件 Assets 目录加载自定义物品图标（PNG/WebP）
    /// </summary>
    /// <param name="iconId">图标文件名（不含扩展名）</param>
    /// <param name="subDir">Assets 下的子目录路径（如 "ammo", "equipment", "guns/akm"）</param>
    private static Sprite? GetOrLoadSprite(string iconId, string subDir)
    {
        if (_spriteCache.TryGetValue(iconId, out var cached))
            return cached;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? BepInEx.Paths.PluginPath;

            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets");
            iconPath = Path.Combine(iconPath, subDir);
            iconPath = Path.Combine(iconPath, $"{iconId}.png");

            if (!File.Exists(iconPath))
            {
                iconPath = Path.ChangeExtension(iconPath, ".webp");
                if (!File.Exists(iconPath))
                {
                    Plugin.Log.LogWarning($"[RecipeSprite] No icon file found for '{iconId}'");
                    return null;
                }
            }

            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                Plugin.Log.LogWarning($"[RecipeSprite] Failed to load image for '{iconId}'");
                return null;
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 32f);
            sprite.name = $"{iconId}-recipe-icon";

            _spriteCache[iconId] = sprite;
            Plugin.Log.LogInfo($"[RecipeSprite] Loaded custom icon for '{iconId}' ({texture.width}x{texture.height})");
            return sprite;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[RecipeSprite] Failed to load icon for '{iconId}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取 fallback Sprite（自定义图标加载失败时使用）
    /// </summary>
    private static Sprite? GetFallbackSprite(string ammoId)
    {
        _ = ammoId; // 保留参数签名供调用方统一
        // 尝试从缓存的其他弹药图标中选一个
        foreach (var kvp in _spriteCache)
        {
            if (kvp.Value != null)
                return kvp.Value;
        }
        return null;
    }

    /// <summary>
    /// 获取原版同类弹药的 Sprite 作为最后 fallback
    /// 使用 Resources.Load 加载原版弹药预制体（这些在游戏 Resources 中存在）
    /// </summary>
    private static Sprite? GetBaseGameSprite(string ammoId)
    {
        try
        {
            // 根据弹药 ID 确定原版弹药预制体 ID
            var baseId = GetBaseGameItemId(ammoId);
            if (baseId == null) return null;

            var prefab = Resources.Load<GameObject>(baseId);
            if (prefab == null) return null;

            var sr = prefab.GetComponent<SpriteRenderer>();
            return sr?.sprite;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 根据自定义物品 ID 返回对应的原版预制体 ID（用于 fallback 图标）
    /// </summary>
    private static string GetBaseGameItemId(string itemId)
    {
        // 弹匣 → "riflemagazine" 或 "smallmagazine"
        if (CustomMagIds.Contains(itemId))
        {
            // 手枪弹匣（glock17, deagle）用 smallmagazine，步枪弹匣用 riflemagazine
            switch (itemId)
            {
                case Glock17MagItemSystem.ItemKey: return "smallmagazine";
                case DeagleMagItemSystem.ItemKey: return "smallmagazine";
                case UMP45MagItemSystem.ItemKey: return "smallmagazine";
                case USPMagItemSystem.ItemKey: return "smallmagazine";
                default: return "riflemagazine";
            }
        }

        // 所有步枪弹 → "556round", 霰弹 → "12gauge", 手枪弹 → "9mmround"
        switch (itemId)
        {
            case Ammo12g85ItemSystem.ItemKey: return "12gauge";
            // 以下全部基于 Rifle (556round) 预制体
            case Ammo338UCWItemSystem.ItemKey: return "556round";
            case Ammo76251BPZItemSystem.ItemKey: return "556round";
            case Ammo76239SPItemSystem.ItemKey: return "556round";
            case Ammo50CopperItemSystem.ItemKey: return "556round";
            case Ammo45FMJItemSystem.ItemKey: return "556round";
            case Ammo919PSOItemSystem.ItemKey: return "556round";
            case Ammo55645FMJItemSystem.ItemKey: return "556round";
            case Ammo5728SB193ItemSystem.ItemKey: return "556round";
            default: return "riflemagazine"; // 通用 fallback
        }
    }
}
