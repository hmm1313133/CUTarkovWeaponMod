using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// LAS/TAC 2 战术手电【LAS/TAC 2】。
///
/// 规格：
/// - 照射亮度/范围 = 游戏原版一次性手电筒（flashlight）的 70%
/// - 使用小型电池供电；满电强光 20 分钟，弱光 40 分钟
/// - 可装在装有 MOE AKM 护木（moeakm）的 AKM 上 → 安装前提 = 护木
/// - 安装无需 Leatherman 工具钳
/// - 按 I 键循环三档：关 → 弱光（2x 时长） → 强光（1x 时长） → 关
/// - 贴图位置 = 护木（AKM 中间往右 22px、往上 2px）
///
/// 电池模型：
/// - 物品挂 BatteryItem(Small preset, SpawnWithBattery=true) → 自带 smallbattery 满电
/// - item.condition 表示电池电量比例（0~1）
/// - 安装到枪时把 condition 保存到 GunAttachmentHolder.lasTacCharge
/// - 控制器（枪上）按档位消耗 holder.lasTacCharge
/// - 卸下时生成新手电并恢复 condition = 剩余 charge
///
/// 基类物品：flashlight（自带 LightItem/Light2D/BatteryItem 预制体，ConfigureSpawnedItem 中保留/调整）。
/// </summary>
public static class LasTac2ItemSystem
{
    public const string ItemKey = "lastac2";
    public const string BaseGameItemId = "flashlight";

    public static string DisplayName => I18n.Tr("lastac2.name");
    public static string Description => I18n.Tr("lastac2.desc");

    // 贴图路径：common/LASTAC 2.png（13x7，PPI 14，与其他配件贴图一致）
    // 叠加贴图同路径（用于枪械贴图合成）
    private const string IconSubPath = "guns/common/lastac2.png";

    private const float Weight = 0.35f;   // 战术手电很轻
    private const int Value = 0;
    private const int RecognitionMin = 5;

    // ===== 电池 / 时长参数 =====
    // 小电池充满可用 20 分钟（强光），弱光 2 倍时间 = 40 分钟
    public const float HighLightDrainPerSecond = 1f / 1200f; // 强光 20 分钟 (1200s) 满电→0
    public const float LowLightDrainPerSecond  = 1f / 2400f; // 弱光 40 分钟 (2400s) 满电→0

    // ===== Light2D 参数（运行时从原版 flashlight prefab 读取）=====
    // 强光 = 原版 50%；弱光 = 原版 35%
    public static float HighLightIntensity { get; private set; } = 0.7f;
    public static float HighLightRadius    { get; private set; } = 2.0f;
    public static float LowLightIntensity  { get; private set; } = 0.49f;
    public static float LowLightRadius     { get; private set; } = 1.4f;
    public static float LightOuterAngle    { get; private set; } = 60f;
    private static bool _lightParamsResolved;

    private static Sprite? _cachedIcon;

    public static bool IsLasTac2Request(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsLasTac2Request(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);

        // 配件属性（无需 useAction，按 i 切换由控制器处理）
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
        item.Stats.destroyAtZeroCondition = false; // 没电不销毁，可以换电池

        // 移除 LightItem（避免原版 shouldEnable 自动开启逻辑）
        var lightItem = item.GetComponent<LightItem>();
        if (lightItem != null) UnityEngine.Object.Destroy(lightItem);

        // 禁用基类 flashlight 自带的 Light2D（原版朝 +Y 垂直向上常亮），
        // 装枪后由 LasTac2Controller 从 prefab 克隆并接管光照。
        var baseLight = item.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (baseLight != null) baseLight.enabled = false;

        // 保留 BatteryItem（基类 flashlight 自带 Small preset）；按需确保 Small 电池
        EnsureSmallBattery(item);

        // sprite
        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;

        ResizeColliderToSprite(item);

        Plugin.Log.LogInfo($"[LAS/TAC 2] Configured spawned item '{ItemKey}' (condition={item.condition}, battery={item.battery?.batteryType}).");
    }

    /// <summary>确保物品挂 BatteryItem(Small preset)，自带满电 smallbattery。</summary>
    public static void EnsureSmallBattery(Item item) => TacticalLightHelper.EnsureSmallBattery(item);

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
            Plugin.Log.LogInfo($"[LAS/TAC 2] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[LAS/TAC 2] Failed: {ex}"); return false; }
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
        // CUCoreLib 直接生成带 BatteryItem(Small) 的预制体
        customInfo.Battery = new BatteryProperties
        {
            Preset = BatteryItem.BatteryPreset.Small,
            SpawnWithBattery = true,
        };
        Plugin.Log.LogInfo($"[LAS/TAC 2] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Small.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", IconSubPath);
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
                        new Vector2(0.5f, 0.5f), 7f); // PPI 7：世界物品更大（13px→约1.9单位）
                    _cachedIcon.name = "lastac2-icon";
                }
            }
            else Plugin.Log.LogWarning($"[LAS/TAC 2] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[LAS/TAC 2] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>供纹理合成器使用的可读手电贴图（无贴图返回 null）。</summary>
    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadIcon();
        return spr != null ? spr.texture : null;
    }

    /// <summary>
    /// 在首次创建手电 Light2D 时，从原版 flashlight prefab 读取 Light2D 基准参数并应用系数：
    /// 强光 = 原版 50%，弱光 = 原版 25%。
    /// 如果 prefab 读取失败，保留默认值。
    /// </summary>
    public static void ResolveLightParamsFromVanilla()
    {
        if (_lightParamsResolved) return;
        _lightParamsResolved = true;
        TacticalLightHelper.EnsureVanillaParamsResolved();

        HighLightIntensity = TacticalLightHelper.VanillaIntensity * 0.5f;
        HighLightRadius    = TacticalLightHelper.VanillaRadius * 0.5f;
        LowLightIntensity  = TacticalLightHelper.VanillaIntensity * 0.35f;
        LowLightRadius     = TacticalLightHelper.VanillaRadius * 0.35f;
        LightOuterAngle    = TacticalLightHelper.VanillaAngle;

        Plugin.Log.LogInfo($"[LAS/TAC 2] Vanilla → high(50%)={HighLightIntensity:F2}/{HighLightRadius:F2}, low(35%)={LowLightIntensity:F2}/{LowLightRadius:F2}.");
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