using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Zenit Klesch-2U 战术手电【Klesch-2U】。
///
/// 规格：
/// - 单照明模式（开/关），照射亮度/范围 = 原版一次性手电筒（flashlight）100%
/// - 使用小型电池；满电单档照明 11 分钟（660 秒）
/// - 可装在装有 MOE AKM 护木（moeakm）的 AKM 上 → 安装前提 = 护木
/// - 安装无需 Leatherman 工具钳
/// - 按战术设备键（默认 I，可改键）开/关
/// - 贴图位置 = 护木（AKM 中间往右 22px、往上 2px）
///
/// 电池模型与 LAS/TAC 2 相同：condition 表示电量比例，装枪时存 GunAttachmentHolder.kleschCharge，
/// 卸下时写回新物品（TacticalLightDetachedCharge 延迟写回）。
///
/// 基类物品：flashlight（自带 LightItem/Light2D/BatteryItem 预制体）。
/// </summary>
public static class Klesch2UItemSystem
{
    public const string ItemKey = "klesch2u";
    public const string BaseGameItemId = "flashlight";

    public static string DisplayName => I18n.Tr("klesch2u.name");
    public static string Description => I18n.Tr("klesch2u.desc");

    // 贴图路径：common/2u.png（13x7，PPI 14；世界显示 PPI 7）
    private const string IconSubPath = "guns/common/2u.png";

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    // ===== 电池 / 时长参数 =====
    // 单照明模式 11 分钟 = 660 秒满电→0
    public const float DrainPerSecond = 1f / 660f;

    // ===== Light2D 参数（运行时从原版 flashlight prefab 读取 = 100%）=====
    public static float Intensity { get; private set; } = 1.4f;
    public static float Radius { get; private set; } = 4.0f;
    public static float LightOuterAngle { get; private set; } = 60f;
    private static bool _lightParamsResolved;

    private static Sprite? _cachedIcon;

    public static bool IsKlesch2URequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsKlesch2URequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!);

        Plugin.Log.LogInfo($"[Klesch-2U] Configured spawned item '{ItemKey}' (condition={item.condition}, battery={item.battery?.batteryType}).");
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
            Plugin.Log.LogInfo($"[Klesch-2U] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Klesch-2U] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[Klesch-2U] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Small.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        var tex = TacticalLightHelper.LoadPointIconTexture(IconSubPath);
        if (tex != null)
        {
            _cachedIcon = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 7f); // PPI 7：世界物品更大（13px→约1.9单位）
            _cachedIcon.name = "klesch2u-icon";
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

    /// <summary>
    /// 首次创建手电 Light2D 时，从原版 flashlight prefab 读取基准参数（100%）。
    /// </summary>
    public static void ResolveLightParamsFromVanilla()
    {
        if (_lightParamsResolved) return;
        _lightParamsResolved = true;
        TacticalLightHelper.EnsureVanillaParamsResolved();

        Intensity = TacticalLightHelper.VanillaIntensity;
        Radius = TacticalLightHelper.VanillaRadius;
        LightOuterAngle = TacticalLightHelper.VanillaAngle;

        Plugin.Log.LogInfo($"[Klesch-2U] Vanilla (100%) → int={Intensity:F2} r={Radius:F2} angle={LightOuterAngle:F1}.");
    }
}

/// <summary>
/// Klesch-2U 战术手电控制器（挂在枪上）。单照明模式：关/开。
/// 电量存于 GunAttachmentHolder.kleschCharge；枪丢地/放背仍发光耗电。
/// </summary>
public sealed class Klesch2UController : MonoBehaviour
{
    private const string LightObjectName = "Klesch2ULight";

    private Item? _gunItem;
    private GameObject? _lightObj;
    private Light2D? _light;
    private bool _on;

    public bool IsOn => _light != null && _light.enabled;

    public static Klesch2UController Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<Klesch2UController>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<Klesch2UController>();
        ctrl._gunItem = gunItem;
        ctrl._on = false;
        return ctrl;
    }

    /// <summary>从多人同步消息设置开关状态。</summary>
    public void SetNetworkOn(bool on)
    {
        _on = on;
    }

    private void Awake()
    {
        Klesch2UItemSystem.ResolveLightParamsFromVanilla();
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void OnDestroy() => DestroyLight();

    private void Update()
    {
        if (_gunItem == null) return;

        bool held = IsHeldByPlayer();
        // 战术设备键：仅当玩家手持此枪时切换（避免多把带灯的枪同时切换）
        if (held && Input.GetKeyDown(TacticalDeviceKeybindPatch.CurrentKey))
        {
            _on = !_on;
            Plugin.Log.LogInfo($"[Klesch-2U] {( _on ? "ON" : "OFF" )}.");
            if (_gunItem != null)
                WeaponMpSync.SyncTacticalState(_gunItem, Klesch2UItemSystem.ItemKey, _on ? 1 : 0);
        }

        UpdateLight();
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }

    private void UpdateLight()
    {
        var holder = _gunItem != null ? _gunItem.GetComponent<GunAttachmentHolder>() : null;
        if (holder == null) { ApplyLight(false); return; }

        bool wantLit = _on && holder.kleschCharge > 0f;
        if (wantLit)
        {
            holder.kleschCharge -= Klesch2UItemSystem.DrainPerSecond * Time.deltaTime;
            if (holder.kleschCharge <= 0f)
            {
                holder.kleschCharge = 0f;
                _on = false;
                wantLit = false;
                Plugin.Log.LogInfo("[Klesch-2U] Battery depleted, turned off.");
            }
            ApplyLight(true);
        }
        else
        {
            ApplyLight(false);
        }
    }

    private void ApplyLight(bool lit)
    {
        if (lit)
        {
            EnsureLight();
            if (_light != null)
            {
                _light.enabled = true;
                _light.intensity = Klesch2UItemSystem.Intensity;
                _light.pointLightOuterRadius = Klesch2UItemSystem.Radius;
                _light.pointLightOuterAngle = Klesch2UItemSystem.LightOuterAngle;
            }
        }
        else if (_light != null)
        {
            _light.enabled = false;
        }
    }

    private void EnsureLight()
    {
        if (_lightObj != null && _light != null) return;
        if (_gunItem == null) return;

        // 克隆原版 flashlight prefab 的 Light2D，保证 URP 渲染器配置一致
        Light2D? newLight = null;
        try
        {
            var prefab = Resources.Load<GameObject>(Klesch2UItemSystem.BaseGameItemId);
            var prefabLight = prefab != null ? prefab.GetComponentInChildren<Light2D>() : null;
            if (prefabLight != null)
            {
                var go = UnityEngine.Object.Instantiate(prefabLight.gameObject, _gunItem.transform);
                go.name = LightObjectName;
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp is Transform || comp is Light2D) continue;
                    UnityEngine.Object.Destroy(comp);
                }
                go.transform.localPosition = GetLightLocalPos();
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                newLight = go.GetComponent<Light2D>();
                newLight.color = new Color(1f, 0.97f, 0.85f, 1f);
                newLight.enabled = false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Klesch-2U] Clone light failed: {ex.Message}");
        }

        if (newLight == null)
        {
            var go = new GameObject(LightObjectName);
            go.transform.SetParent(_gunItem.transform, false);
            go.transform.localPosition = GetLightLocalPos();
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            newLight = go.AddComponent<Light2D>();
            newLight.lightType = Light2D.LightType.Point;
            newLight.color = new Color(1f, 0.97f, 0.85f, 1f);
            newLight.pointLightOuterAngle = 60f;
            newLight.falloffIntensity = 0.5f;
            newLight.enabled = false;
        }

        _lightObj = newLight.gameObject;
        _light = newLight;
    }

    private void DestroyLight()
    {
        if (_lightObj != null)
        {
            UnityEngine.Object.Destroy(_lightObj);
            _lightObj = null;
            _light = null;
        }
    }

    /// <summary>
    /// 光照起点（相对枪 transform 的 world 单位，PPI 14）。
    /// 基准 = AKM 护木位置（往右 22px、往上 2px）。
    /// SKS（贴图宽 158）时手电贴图往右移，光照起点也往右 13px。
    /// </summary>
    private Vector3 GetLightLocalPos()
    {
        float x = 22f / 14f;
        float y = 2f / 14f;
        if (_gunItem != null)
        {
            if (string.Equals(_gunItem.id, SKSItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                x += 13f / 14f;
            else if (string.Equals(_gunItem.id, AXMCItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // AXMC：灯光往右 15px、往下 4px（PPI 13.2）
                x += 15f / 13.2f;
                y -= 4f / 13.2f;
            }
            else if (string.Equals(_gunItem.id, DVL10ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // DVL：灯光往右 15px、往下 6px（PPI 13.2）
                x += 15f / 13.2f;
                y -= 6f / 13.2f;
            }
            else if (string.Equals(_gunItem.id, DeagleItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // 沙鹰等手枪：光源向左 1px、向下 4px（PPI 14）
                x -= 1f / 14f;
                y -= 4f / 14f;
            }
        }
        return new Vector3(x, y, 0f);
    }

    /// <summary>关闭并销毁灯（卸下时调用）。</summary>
    public void Shutdown()
    {
        _on = false;
        DestroyLight();
    }
}