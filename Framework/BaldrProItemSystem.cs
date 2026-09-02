using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Olight Baldr Pro 战术手电激光组合【BaldrPro】。
///
/// 规格：
/// - 使用小型电池
/// - 三模式：激光、单照明、激光+照明（按战术设备键循环）
///   - 激光模式：续航 60 分钟
///   - 单照明模式：续航 35 分钟
///   - 激光+照明模式：续航 30 分钟
/// - 照明亮度/范围 = 原版一次性手电筒（flashlight）的 70%
/// - 激光 = 红色光束，从枪口射出、延伸较长距离（原版手枪激光的加长版），遇障碍物截断
/// - 可装在装有 MOE AKM 护木（moeakm）的 AKM 上 → 前提 = 护木
/// - 安装无需 Leatherman 工具钳
/// - 贴图位置 = 护木（AKM 中间往右 22px、往上 2px）
///
/// 电池模型与 LAS/TAC 2 / Klesch-2U 相同：condition 表示电量比例，
/// 装枪时存 GunAttachmentHolder.baldrCharge，卸下时写回新物品。
/// 基类物品：flashlight。
/// </summary>
public static class BaldrProItemSystem
{
    public const string ItemKey = "baldrpro";
    public const string BaseGameItemId = "flashlight";

    public static string DisplayName => I18n.Tr("baldrpro.name");
    public static string Description => I18n.Tr("baldrpro.desc");

    private const string IconSubPath = "guns/common/baldrpro.png";

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    // ===== 电池 / 时长参数（每模式满电→空秒数）=====
    public const float LaserDrainPerSecond = 1f / 3600f;   // 60 分钟
    public const float LightDrainPerSecond  = 1f / 2100f;   // 35 分钟
    public const float BothDrainPerSecond   = 1f / 1800f;   // 30 分钟

    // ===== Light2D 参数（运行时从原版 flashlight prefab 读取 = 70%）=====
    public static float Intensity { get; private set; } = 0.98f;
    public static float Radius { get; private set; } = 2.8f;
    public static float LightOuterAngle { get; private set; } = 60f;
    private static bool _lightParamsResolved;

    // ===== 激光参数 =====
    public const float LaserRange = 14f;      // 激光最长距离（单位）
    public const float LaserWidth = 0.035f;   // 激光粗细（世界单位）

    private static Sprite? _cachedIcon;

    public static bool IsBaldrProRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsBaldrProRequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!);

        Plugin.Log.LogInfo($"[Baldr Pro] Configured spawned item '{ItemKey}' (condition={item.condition}, battery={item.battery?.batteryType}).");
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
                destroyAtZeroCondition = false,
                weight = Weight,
                value = Value,
                tags = "attachment",
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[Baldr Pro] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Baldr Pro] Failed: {ex}"); return false; }
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
        customInfo.Battery = new BatteryProperties
        {
            Preset = BatteryItem.BatteryPreset.Small,
            SpawnWithBattery = true,
        };
        Plugin.Log.LogInfo($"[Baldr Pro] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Small.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        var tex = TacticalLightHelper.LoadPointIconTexture(IconSubPath);
        if (tex != null)
        {
            _cachedIcon = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 7f); // PPI 7：世界物品更大
            _cachedIcon.name = "baldrpro-icon";
        }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>供纹理合成器使用的可读手电贴图（无贴图返回 null）。</summary>
    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadIcon();
        return spr != null ? spr.texture : null;
    }

    /// <summary>首次创建手电 Light2D 时，从原版 flashlight prefab 读取基准参数（70%）。</summary>
    public static void ResolveLightParamsFromVanilla()
    {
        if (_lightParamsResolved) return;
        _lightParamsResolved = true;
        TacticalLightHelper.EnsureVanillaParamsResolved();

        Intensity = TacticalLightHelper.VanillaIntensity * 0.7f;
        Radius = TacticalLightHelper.VanillaRadius * 0.7f;
        LightOuterAngle = TacticalLightHelper.VanillaAngle;

        Plugin.Log.LogInfo($"[Baldr Pro] Vanilla (70%) → int={Intensity:F2} r={Radius:F2} angle={LightOuterAngle:F1}.");
    }
}