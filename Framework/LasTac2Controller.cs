using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// LAS/TAC 2 战术手电控制器（挂在枪上）。
///
/// 三档光照：
/// - Off (0): 关闭，灯光禁用
/// - Low (1): 弱光，约 0.55x 强度，2x 使用时间（40 分钟满电）
/// - High (2): 强光，原版一次性手电筒 70% 强度，1x 使用时间（20 分钟满电）
///
/// I 键循环：Off → Low → High → Off
/// 电量耗尽自动关闭（电量存于 GunAttachmentHolder.lasTacCharge）。
/// 手持枪时才生效；枪不在手上自动关闭灯（保留档位状态）。
///
/// 光照实现：在枪上创建子物体 "LasTac2Light"，挂 Light2D（聚光模式，
/// 方向朝枪口 +X）。位置与护木贴图位置一致（AKM 中间往右 22px、往上 2px）。
/// </summary>
public sealed class LasTac2Controller : MonoBehaviour
{
    public enum Mode { Off = 0, Low = 1, High = 2 }

    private const string LightObjectName = "LasTac2Light";

    private Item? _gunItem;
    private GameObject? _lightObj;
    private Light2D? _light;

    private Mode _mode = Mode.Off;

    // 上一帧灯光状态（用于在开启/切换模式时播放手电音效）
    private bool _wasLit;
    private Mode _wasMode = Mode.Off;

    /// <summary>当前档位（持久于 holder，关灯后保留）。</summary>
    public Mode CurrentMode
    {
        get => _mode;
        set => _mode = value;
    }

    /// <summary>当前是否在点亮状态（Mode ≠ Off 且有电）。</summary>
    public bool IsLit => _light != null && _light.enabled;

    /// <summary>初始化并附加到枪上（由 SuppressorSystem.AttachToGun 调用）。</summary>
    public static LasTac2Controller Attach(Item gunItem, float initialCharge)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<LasTac2Controller>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<LasTac2Controller>();
        ctrl._gunItem = gunItem;
        ctrl._mode = Mode.Off;
        return ctrl;
    }

    /// <summary>从多人同步消息设置档位。</summary>
    public void SetNetworkMode(int mode)
    {
        _mode = (Mode)Mathf.Clamp(mode, 0, 2);
    }

    private void Awake()
    {
        LasTac2ItemSystem.ResolveLightParamsFromVanilla();
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void OnDestroy()
    {
        DestroyLight();
    }

    private void Update()
    {
        if (_gunItem == null) return;

        bool held = IsHeldByPlayer();

        // 战术设备键：仅当玩家手持此枪时切换档位（避免多把带手电的枪同时切换）。
        // 键位从设置读取（TacticalDeviceKeybindPatch，默认 I，可改键）。
        if (held && Input.GetKeyDown(TacticalDeviceKeybindPatch.CurrentKey))
        {
            CycleMode();
        }

        // 灯光只要开着且还有电就持续生效——枪丢在地上 / 放到背上同样发光、耗电
        //（手电真实装在枪上，枪在哪光就在哪）。
        UpdateLight();
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        var held = body.GetItem(body.handSlot);
        return held == _gunItem;
    }

    private void CycleMode()
    {
        _mode = (Mode)(((int)_mode + 1) % 3);
        Plugin.Log.LogInfo($"[LAS/TAC 2] Mode → {_mode}.");
        if (_gunItem != null)
            WeaponMpSync.SyncTacticalState(_gunItem, LasTac2ItemSystem.ItemKey, (int)_mode);
    }

    private void UpdateLight()
    {
        var holder = _gunItem != null ? _gunItem.GetComponent<GunAttachmentHolder>() : null;
        if (holder == null) { ApplyLight(false); return; }

        bool wantLit = _mode != Mode.Off && holder.lasTacCharge > 0f;
        if (wantLit)
        {
            // 按档位消耗电量
            float drain = _mode == Mode.High
                ? LasTac2ItemSystem.HighLightDrainPerSecond
                : LasTac2ItemSystem.LowLightDrainPerSecond;
            holder.lasTacCharge -= drain * Time.deltaTime;
            if (holder.lasTacCharge <= 0f)
            {
                holder.lasTacCharge = 0f;
                _mode = Mode.Off;
                wantLit = false;
                Plugin.Log.LogInfo("[LAS/TAC 2] Battery depleted, turned off.");
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
                if (_mode == Mode.High)
                {
                    _light.intensity = LasTac2ItemSystem.HighLightIntensity;
                    _light.pointLightOuterRadius = LasTac2ItemSystem.HighLightRadius;
                }
                else
                {
                    _light.intensity = LasTac2ItemSystem.LowLightIntensity;
                    _light.pointLightOuterRadius = LasTac2ItemSystem.LowLightRadius;
                }
                _light.pointLightOuterAngle = LasTac2ItemSystem.LightOuterAngle;
            }
            // 开启/切换模式时播放原版手电开启音效（状态从关到开或模式变化）
            if (!_wasLit || _wasMode != _mode)
                Sound.Play("flashlight", _gunItem.transform.position);
        }
        else if (_light != null)
        {
            _light.enabled = false;
        }
        _wasLit = lit;
        _wasMode = _mode;
    }

    private void EnsureLight()
    {
        if (_lightObj != null && _light != null) return;

        if (_gunItem == null) return;

        // 优先从原版 flashlight prefab 克隆 Light2D：
        // 运行时 AddComponent<Light2D> 无法正确接入 URP 2D Renderer 的渲染配置
        //（就是"按 I 有日志但看不到光"的根因）。克隆 prefab 组件保证渲染器配置一致。
        Light2D? newLight = null;
        try
        {
            var prefab = Resources.Load<GameObject>(LasTac2ItemSystem.BaseGameItemId);
            var prefabLight = prefab != null ? prefab.GetComponentInChildren<Light2D>() : null;
            if (prefabLight != null)
            {
                var go = UnityEngine.Object.Instantiate(prefabLight.gameObject, _gunItem.transform);
                go.name = LightObjectName;

                // 移除多余组件（保留 Transform + Light2D），避免残留 Item/LightItem 等逻辑
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp is Transform || comp is Light2D) continue;
                    UnityEngine.Object.Destroy(comp);
                }

                go.transform.localPosition = GetLightLocalPos();
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                newLight = go.GetComponent<Light2D>();
                newLight.color = new Color(1f, 0.97f, 0.85f, 1f); // 暖白
                newLight.enabled = false; // 由 ApplyLight 控制
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[LAS/TAC 2] Clone light failed: {ex.Message}");
        }

        // 兜底：AddComponent
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
            else if (string.Equals(_gunItem.id, Glock17ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            {
                // 格洛克：光源向左 17px、向下 4px（在 15px/1px 基础上再往左 2px、往下 3px，PPI 14）
                x -= 17f / 14f;
                y -= 4f / 14f;
            }
        }
        return new Vector3(x, y, 0f);
    }

    /// <summary>关闭并销毁灯（卸下手电时调用）。</summary>
    public void Shutdown()
    {
        _mode = Mode.Off;
        DestroyLight();
    }
}