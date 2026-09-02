using System;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 战术手电公共逻辑（LAS/TAC 2 与 Klesch-2U 共用）：
/// - 小型电池确保（BatteryItem Small preset + smallbattery）
/// - 原版一次性手电筒（flashlight）Light2D 基准参数读取（缓存，读取一次）
/// - 卸下时电量写回延迟器（通用组件）
/// </summary>
public static class TacticalLightHelper
{
    // ===== 原版 flashlight 基准参数（缓存）=====
    private static bool _vanillaResolved;
    private static float _vIntensity = 1.4f;
    private static float _vRadius = 4.0f;
    private static float _vAngle = 60f;

    public static float VanillaIntensity => _vIntensity;
    public static float VanillaRadius => _vRadius;
    public static float VanillaAngle => _vAngle;

    /// <summary>
    /// 从原版 flashlight prefab 读取 Light2D 基准参数（intensity / outerRadius / outerAngle）。
    /// 失败时保留默认值。线程无关，缓存一次。
    /// </summary>
    public static void EnsureVanillaParamsResolved()
    {
        if (_vanillaResolved) return;
        _vanillaResolved = true;
        try
        {
            var prefab = Resources.Load<GameObject>("flashlight");
            if (prefab == null) return;
            var vanilla = prefab.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
            if (vanilla == null) return;

            _vIntensity = vanilla.intensity;
            _vRadius = vanilla.pointLightOuterRadius;
            if (vanilla.pointLightOuterAngle > 0) _vAngle = vanilla.pointLightOuterAngle;

            Plugin.Log.LogInfo($"[TacticalLight] Vanilla flashlight → int={_vIntensity:F2} r={_vRadius:F2} angle={_vAngle:F1}.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[TacticalLight] Failed to read vanilla params: {ex.Message}");
        }
    }

    /// <summary>确保物品挂 BatteryItem(Small preset)，自带满电 smallbattery。</summary>
    public static void EnsureSmallBattery(Item item)
    {
        if (item == null) return;
        var bat = item.GetComponent<BatteryItem>();
        if (bat == null) bat = item.gameObject.AddComponent<BatteryItem>();
        item.battery = bat;
        bat.preset = BatteryItem.BatteryPreset.Small;
        bat.maxAllowedCharge = 50f;
        bat.batteryType = "smallbattery";
        bat.maxCharge = 50f;
        if (item.condition <= 0f) item.SetCondition(1f);
    }

    /// <summary>
    /// 战术灯通用的基类配置：移除 LightItem、禁用基类 flashlight 的 Light2D（原版朝 +Y 垂直向上常亮）、
    /// 可选确保 Small 电池、设置图标。
    /// </summary>
    public static void ConfigureFlashlightBase(Item item, string itemId, string iconSubPath, float weight, int value, int recognitionMin, Sprite icon, bool withBattery = true)
    {
        item.id = itemId;
        item.SetCondition(1f);

        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "utility";
        item.Stats.tags = "attachment";
        item.Stats.SetTags();
        item.Stats.weight = weight;
        item.Stats.value = value;
        item.Stats.destroyAtZeroCondition = false; // 没电不销毁，可以换电池

        // 移除 LightItem（避免原版 shouldEnable 自动开启逻辑）
        var lightItem = item.GetComponent<LightItem>();
        if (lightItem != null) UnityEngine.Object.Destroy(lightItem);

        // 禁用基类 flashlight 自带的 Light2D（原版朝 +Y 垂直向上常亮），
        // 装枪后由控制器从 prefab 克隆并接管光照。
        var baseLight = item.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (baseLight != null) baseLight.enabled = false;

        // 可选：确保 Small 电池（瞄具等无供电机制配件传 false，不设电池）
        if (withBattery) EnsureSmallBattery(item);

        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;

        ResizeColliderToSprite(item);
    }

    /// <summary>按图标路径加载 PPI=7 的纹理（世界物品显示更大）。</summary>
    public static Texture2D? LoadPointIconTexture(string iconSubPath)
    {
        try
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? BepInEx.Paths.PluginPath,
                "Framework", "Assets", iconSubPath);
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(tex, bytes, false))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return tex;
                }
            }
        }
        catch { }
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

/// <summary>
/// 卸下战术灯时电量写回延迟器（LAS/TAC 2 与 Klesch-2U 通用）。
/// Utils.Create 内部流程（ConfigureSpawnedItem SetCondition(1f) / Item.Start 的 CUCoreLib 应用）
/// 会在创建后覆盖 condition，因此把剩余电量延迟到第一帧 Update（所有 Start 之后）再写入。
/// </summary>
public sealed class TacticalLightDetachedCharge : MonoBehaviour
{
    public string lightId = "lastac2";
    public float charge = 1f;
    public bool hadBattery = true;   // 安装时原配件是否带电池；卸下时据此决定是否补电池
    private bool _done;

    private void Update()
    {
        if (_done) return;
        _done = true;
        try
        {
            var item = GetComponent<Item>();
            if (item != null)
            {
                if (charge <= 0.01f) charge = 0.01f;
                item.SetCondition(charge);
                // 仅在原配件带电池时补电池；无电池的配件卸下后保持无电池（不凭空生成）
                if (hadBattery)
                    TacticalLightHelper.EnsureSmallBattery(item);
                Plugin.Log.LogInfo($"[{lightId}] Detached item charge restored to {charge:F2} (hadBattery={hadBattery}).");
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[{lightId}] Charge restore failed: {ex.Message}"); }
        Destroy(this);
    }
}