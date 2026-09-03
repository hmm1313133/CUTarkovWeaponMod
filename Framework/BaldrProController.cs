using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Baldr Pro 战术手电激光组合控制器（挂在枪上）。
///
/// 四档循环（战术设备键，默认 I，可改键）：
///   Off → Laser（激光）→ Light（单照明）→ Both（激光+照明）→ Off
///
/// 电量（GunAttachmentHolder.baldrCharge）按档位不同速率消耗：
///   Laser 60 分钟 / Light 35 分钟 / Both 30 分钟。
/// 枪丢地上/放背上仍发光耗电。
///
/// 照明 = 从原版 flashlight prefab 克隆 Light2D（70% 亮度/范围）。
/// 激光 = SpriteRenderer 红色光束贴图，从枪口射出，射线检测在障碍物处截断。
/// </summary>
public sealed class BaldrProController : MonoBehaviour
{
    public enum Mode { Off = 0, Laser = 1, Light = 2, Both = 3 }

    private const string LightObjectName = "BaldrProLight";
    private const string LaserObjectName = "BaldrProLaser";

    private Item? _gunItem;
    private GameObject? _lightObj;
    private Light2D? _light;
    private GameObject? _laserObj;
    private LineRenderer? _laserLine;

    private Mode _mode = Mode.Off;

    public Mode CurrentMode => _mode;

    public static BaldrProController Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<BaldrProController>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<BaldrProController>();
        ctrl._gunItem = gunItem;
        ctrl._mode = Mode.Off;
        return ctrl;
    }

    /// <summary>从多人同步消息设置档位。</summary>
    public void SetNetworkMode(int mode)
    {
        _mode = (Mode)Mathf.Clamp(mode, 0, 3);
    }

    private void Awake()
    {
        BaldrProItemSystem.ResolveLightParamsFromVanilla();
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void OnDestroy()
    {
        DestroyLight();
        DestroyLaser();
    }

    private void Update()
    {
        if (_gunItem == null) return;

        bool held = IsHeldByPlayer();
        // 战术设备键：仅当玩家手持此枪时切换档位
        if (held && Input.GetKeyDown(TacticalDeviceKeybindPatch.CurrentKey))
        {
            _mode = (Mode)(((int)_mode + 1) % 4);
            Plugin.Log.LogInfo($"[Baldr Pro] Mode → {_mode}.");
            if (_gunItem != null)
                WeaponMpSync.SyncTacticalState(_gunItem, BaldrProItemSystem.ItemKey, (int)_mode);
        }

        UpdateEffects();
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }

    // ===== 效果更新 =====

    private void UpdateEffects()
    {
        var holder = _gunItem != null ? _gunItem.GetComponent<GunAttachmentHolder>() : null;
        if (holder == null) { ApplyLight(false); UpdateLaser(false); return; }

        bool wantLight = (_mode == Mode.Light || _mode == Mode.Both) && holder.baldrCharge > 0f;
        bool wantLaser = (_mode == Mode.Laser || _mode == Mode.Both) && holder.baldrCharge > 0f;

        if (wantLight || wantLaser)
        {
            // 按档位消耗电量（激光+照明档同时开两种）
            float drain = _mode switch
            {
                Mode.Laser => BaldrProItemSystem.LaserDrainPerSecond,
                Mode.Light => BaldrProItemSystem.LightDrainPerSecond,
                Mode.Both => BaldrProItemSystem.BothDrainPerSecond,
                _ => 0f,
            };
            holder.baldrCharge -= drain * Time.deltaTime;
            if (holder.baldrCharge <= 0f)
            {
                holder.baldrCharge = 0f;
                _mode = Mode.Off;
                wantLight = false;
                wantLaser = false;
                Plugin.Log.LogInfo("[Baldr Pro] Battery depleted, turned off.");
            }
        }

        ApplyLight(wantLight);
        UpdateLaser(wantLaser);
    }

    // ===== 照明 =====

    private void ApplyLight(bool lit)
    {
        if (lit)
        {
            EnsureLight();
            if (_light != null)
            {
                _light.enabled = true;
                _light.intensity = BaldrProItemSystem.Intensity;
                _light.pointLightOuterRadius = BaldrProItemSystem.Radius;
                _light.pointLightOuterAngle = BaldrProItemSystem.LightOuterAngle;
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

        Light2D? newLight = null;
        try
        {
            var prefab = Resources.Load<GameObject>(BaldrProItemSystem.BaseGameItemId);
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
        catch (Exception ex) { Plugin.Log.LogWarning($"[Baldr Pro] Clone light failed: {ex.Message}"); }

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

    // ===== 激光 =====

    private void UpdateLaser(bool lit)
    {
        if (!lit)
        {
            if (_laserObj != null) _laserObj.SetActive(false);
            return;
        }
        EnsureLaser();
        if (_laserObj == null || _laserLine == null || _gunItem == null) return;

        // 发射位置：手电安装在枪上的位置（AKM 中间往右 22px、往上 2px，PPI14），
        // 转世界坐标 → 激光从手电发射器发出，而非枪口 barrel。
        Vector2 start = _gunItem.transform.TransformPoint(GetLightLocalPos());

        // 方向：手持时游戏用 body.isRight 决定朝向（GunScript.Fire 中 num = body.isRight ? 1 : -1，
        // 枪械 transform.right 始终朝右，转身朝左靠 body.isRight=false 反向）。
        // 丢下/放背时枪是独立物体，激光应朝枪械自身朝向（transform.right），不受玩家朝向影响。
        // 多人下不能只判断“本地玩家手持”：别人手上的枪要用枪所属的 Body 判断朝向，
        // 否则会按本地玩家面向决定方向，导致激光反向。
        var ownerBody = _gunItem != null ? _gunItem.GetComponentInParent<Body>() : null;
        Vector2 dir;
        if (ownerBody != null)
        {
            bool isRight = ownerBody.isRight;
            dir = (Vector2)_gunItem.transform.right * (isRight ? 1f : -1f);
        }
        else
        {
            dir = (Vector2)_gunItem.transform.right;
        }

        float len = BaldrProItemSystem.LaserRange;

        // 射线截断在障碍物：仅手持时执行。
        // 丢下时若仍做截断，激光起点（枪身）会立即命中地面 → hit.distance 极小 → 缩成"一个点"。
        if (IsHeldByPlayer())
        {
            var hit = Physics2D.Raycast(start, dir, len);
            if (hit.collider != null) len = Mathf.Max(0.5f, hit.distance);
        }

        // LineRenderer：几何图元，任意旋转角度都平滑连续（不会像细长 Sprite 那样
        // 在 PixelPerfectCamera 下像素化断裂/消失）。useWorldSpace + 两端点位置。
        _laserObj.SetActive(true);
        _laserLine.SetPosition(0, start);
        _laserLine.SetPosition(1, start + dir * len);
        var sr = _gunItem.GetComponent<SpriteRenderer>();
        _laserLine.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 2;
    }

    private void EnsureLaser()
    {
        if (_laserObj != null && _laserLine != null) return;
        if (_gunItem == null) return;

        var go = new GameObject(LaserObjectName);
        go.transform.SetParent(_gunItem.transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.09f;   // 激光粗细（世界单位，约 3px @ PPI32）
        line.endWidth = 0.09f;
        line.startColor = new Color(1f, 0.12f, 0.08f, 1f);   // 枪口端：亮红
        line.endColor = new Color(1f, 0.12f, 0.08f, 0f);     // 远端：渐隐
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 4; // 圆滑端头，避免细线端点闪烁
        line.numCornerVertices = 0;
        line.loop = false;

        // 材质：优先克隆游戏 Special/PickupLine prefab 的 LineRenderer 材质
        //（该材质在游戏 URP 渲染管线中已验证可用），失败则回退 Sprites/Default。
        Material? mat = null;
        try
        {
            var pickup = Resources.Load<GameObject>("Special/PickupLine");
            var pickupLine = pickup != null ? pickup.GetComponent<LineRenderer>() : null;
            if (pickupLine != null && pickupLine.sharedMaterial != null)
                mat = UnityEngine.Object.Instantiate(pickupLine.sharedMaterial);
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Baldr Pro] PickupLine material: {ex.Message}"); }

        if (mat == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) mat = new Material(shader);
        }
        if (mat != null) line.sharedMaterial = mat;

        _laserObj = go;
        _laserLine = line;
    }

    private void DestroyLaser()
    {
        if (_laserObj != null)
        {
            UnityEngine.Object.Destroy(_laserObj);
            _laserObj = null;
            _laserLine = null;
        }
    }

    /// <summary>关闭全部效果并销毁（卸下时调用）。</summary>
    public void Shutdown()
    {
        _mode = Mode.Off;
        DestroyLight();
        DestroyLaser();
    }
}