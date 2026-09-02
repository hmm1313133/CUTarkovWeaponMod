using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// SKS 7.62x39 卡宾枪 Tapco INTRAFUSE 套件组【Tapco intrafuse】。
/// 由 Tapco 制造的 SKS 枪托、缓冲管等一系列部件，可以直接安装使用。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 有后托时：后坐力 -5%（knockBack × 0.95）、瞄准速度 -0.44s、每发耐久损耗 -2%（× 0.98）
/// - 无后托时：后坐力 +26%（knockBack × 1.26）、瞄准速度 +0.6s、每发耐久损耗 -2%（× 0.98）
///
/// 安装 Tapco 后：
/// - 可安装前握把（SKS 原厂不可装）
/// - 可安装 M4 系列后托（Viper Mod.1/CTR/DS150/ACS/MOE）
/// - 不可安装战术手电和瞄准镜
///
/// 视觉：方案 A 局部叠加贴图（Tapco intrafuse.png，158x41 与 SKS 同尺寸），
/// 覆盖 SKS 枪托/缓冲管区域。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class TapcoIntrafuseItemSystem
{
    public const string ItemKey = "tapco_intrafuse";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("tapco_intrafuse.name");
    public static string Description => I18n.Tr("tapco_intrafuse.desc");

    // 效果参数（用户指定）
    public const float KnockBackMultWithStock = 0.95f;   // 有后托：后坐力 -5%
    public const float KnockBackMultNoStock = 1.26f;     // 无后托：后坐力 +26%
    public const float ConditionLossMult = 0.98f;        // 每发耐久损耗 -2%
    public const float AimTimeDeltaWithStock = -0.44f;   // 有后托：瞄准速度 -0.44s（加快）
    public const float AimTimeDeltaNoStock = 0.6f;       // 无后托：瞄准速度 +0.6s（减慢）

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedVisualIcon;

    public static bool IsTapcoIntrafuseRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsTapcoIntrafuseRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "utility";
        item.Stats.tags = "attachment,backflip";
        item.Stats.SetTags();
        item.Stats.weight = Weight;
        item.Stats.value = Value;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[Tapco INTRAFUSE] Configured spawned item '{ItemKey}'.");
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
                tags = "attachment,backflip",
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[Tapco INTRAFUSE] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Tapco INTRAFUSE] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[Tapco INTRAFUSE] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        _cachedIcon = LoadSprite("Tapco intrafuse.png", "tapco-intrafuse-icon");
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static Sprite? TryLoadVisualIconPublic() => TryLoadVisualIcon();

    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadVisualIcon();
        return spr != null ? spr.texture : null;
    }

    private static Sprite? TryLoadVisualIcon()
    {
        if (_cachedVisualIcon != null) return _cachedVisualIcon;
        _cachedVisualIcon = LoadSprite("Tapco intrafuse.png", "tapco-intrafuse-visual");
        return _cachedVisualIcon;
    }

    private static Sprite? LoadSprite(string file, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", file);
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    var spr = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.30f, 0.5f), 22.5f);
                    spr.name = name;
                    return spr;
                }
            }
            else Plugin.Log.LogWarning($"[Tapco INTRAFUSE] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Tapco INTRAFUSE] Icon: {ex.Message}"); }
        return null;
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
