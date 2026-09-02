using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// NcSTAR Tactical LAM 模块 蓝色激光【TBL】。
///
/// 规格：
/// - 使用小型电池
/// - 单模式：蓝色激光（开/关），续航 80 分钟
/// - 可装在装有 MOE AKM 护木（moeakm）的 AKM 上 → 前提 = 护木
/// - 安装无需 Leatherman 工具钳；占用战术设备槽（与 lastac2/klesch2u/baldrpro 互斥）
/// - 按战术设备键（默认 I，可改键）开/关
/// - 贴图位置 = 护木（AKM 中间往右 22px、往上 2px）
///
/// 激光 = LineRenderer 蓝色光柱（几何图元，任意角度平滑连续），
/// 从手电安装位置发射，射线检测在障碍物处截断。
/// 电池模型与其它战术设备相同：condition 表示电量比例，
/// 装枪时存 GunAttachmentHolder.tblCharge，卸下时写回新物品。
/// 基类物品：flashlight。
/// </summary>
public static class TblItemSystem
{
    public const string ItemKey = "tbl";
    public const string BaseGameItemId = "flashlight";

    public static string DisplayName => I18n.Tr("tbl.name");
    public static string Description => I18n.Tr("tbl.desc");

    private const string IconSubPath = "guns/common/TBL.png";

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    // ===== 电池 / 时长参数 =====
    // 单模式 80 分钟 = 4800 秒满电→空
    public const float DrainPerSecond = 1f / 4800f;

    // ===== 激光参数 =====
    public const float LaserRange = 14f;      // 激光最长距离（单位）
    public const float LaserWidth = 0.09f;    // 激光粗细（世界单位）

    private static Sprite? _cachedIcon;

    public static bool IsTblRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsTblRequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!);

        Plugin.Log.LogInfo($"[TBL] Configured spawned item '{ItemKey}' (condition={item.condition}, battery={item.battery?.batteryType}).");
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
            Plugin.Log.LogInfo($"[TBL] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[TBL] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[TBL] CUCoreLib: Icon={customInfo.Icon != null}, Battery=Small.");
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
            _cachedIcon.name = "tbl-icon";
        }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>供纹理合成器使用的可读激光器贴图（无贴图返回 null）。</summary>
    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadIcon();
        return spr != null ? spr.texture : null;
    }
}

/// <summary>
/// TBL 蓝色激光控制器（挂在枪上）。单模式：关/开。
/// 电量存于 GunAttachmentHolder.tblCharge；枪丢地/放背仍发射耗电。
/// </summary>
public sealed class TblController : MonoBehaviour
{
    private const string LaserObjectName = "TblLaser";

    private Item? _gunItem;
    private GameObject? _laserObj;
    private LineRenderer? _laserLine;
    private bool _on;

    public bool IsOn => _laserLine != null && _laserLine.gameObject.activeSelf;

    public static TblController Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<TblController>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<TblController>();
        ctrl._gunItem = gunItem;
        ctrl._on = false;
        return ctrl;
    }

    private void Awake()
    {
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void OnDestroy() => DestroyLaser();

    private void Update()
    {
        if (_gunItem == null) return;

        bool held = IsHeldByPlayer();
        // 战术设备键：仅当玩家手持此枪时切换档位
        if (held && Input.GetKeyDown(TacticalDeviceKeybindPatch.CurrentKey))
        {
            _on = !_on;
            Plugin.Log.LogInfo($"[TBL] {( _on ? "ON" : "OFF" )}.");
        }

        UpdateLaser();
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }

    private void UpdateLaser()
    {
        var holder = _gunItem != null ? _gunItem.GetComponent<GunAttachmentHolder>() : null;
        if (holder == null) { ApplyLaser(false); return; }

        bool wantLit = _on && holder.tblCharge > 0f;
        if (wantLit)
        {
            holder.tblCharge -= TblItemSystem.DrainPerSecond * Time.deltaTime;
            if (holder.tblCharge <= 0f)
            {
                holder.tblCharge = 0f;
                _on = false;
                wantLit = false;
                Plugin.Log.LogInfo("[TBL] Battery depleted, turned off.");
            }
            ApplyLaser(true);
        }
        else
        {
            ApplyLaser(false);
        }
    }

    private void ApplyLaser(bool lit)
    {
        if (!lit)
        {
            if (_laserObj != null) _laserObj.SetActive(false);
            return;
        }
        EnsureLaser();
        if (_laserObj == null || _laserLine == null || _gunItem == null) return;

        // 发射位置：与其它战术设备一致（AKM 中间往右 22px、往上 2px，PPI14）
        Vector2 start = _gunItem.transform.TransformPoint(GetLightLocalPos());

        // 方向：手持时游戏用 body.isRight 决定朝向（GunScript.Fire 中 num = body.isRight ? 1 : -1，
        // 枪械 transform.right 始终朝右，转身朝左靠 body.isRight=false 反向）。
        // 丢下/放背时枪是独立物体，激光应朝枪械自身朝向（transform.right），不受玩家朝向影响。
        bool held = IsHeldByPlayer();
        Vector2 dir;
        if (held)
        {
            var body = PlayerCamera.main?.body;
            bool isRight = body != null ? body.isRight : true;
            dir = (Vector2)_gunItem.transform.right * (isRight ? 1f : -1f);
        }
        else
        {
            dir = (Vector2)_gunItem.transform.right;
        }

        float len = TblItemSystem.LaserRange;

        // 射线截断在障碍物：仅手持时执行。
        // 丢下时若仍做截断，激光起点（枪身）会立即命中地面 → hit.distance 极小 → 缩成"一个点"。
        if (IsHeldByPlayer())
        {
            var hit = Physics2D.Raycast(start, dir, len);
            if (hit.collider != null) len = Mathf.Max(0.5f, hit.distance);
        }

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
        line.startWidth = TblItemSystem.LaserWidth;
        line.endWidth = TblItemSystem.LaserWidth;
        // 蓝色激光：枪口端亮蓝，远端渐隐
        line.startColor = new Color(0.12f, 0.5f, 1f, 1f);
        line.endColor = new Color(0.12f, 0.5f, 1f, 0f);
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 4;
        line.numCornerVertices = 0;
        line.loop = false;

        // 材质：优先克隆游戏 Special/PickupLine prefab 的 LineRenderer 材质（URP 已验证），失败回退 Sprites/Default
        Material? mat = null;
        try
        {
            var pickup = Resources.Load<GameObject>("Special/PickupLine");
            var pickupLine = pickup != null ? pickup.GetComponent<LineRenderer>() : null;
            if (pickupLine != null && pickupLine.sharedMaterial != null)
                mat = UnityEngine.Object.Instantiate(pickupLine.sharedMaterial);
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[TBL] PickupLine material: {ex.Message}"); }

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

    /// <summary>
    /// 激光起点（相对枪 transform 的 world 单位，PPI 14）。
    /// 基准 = AKM 护木位置（往右 22px、往上 2px）。
    /// SKS（贴图宽 158）时手电贴图往右移，激光起点也往右 13px。
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

    /// <summary>关闭并销毁激光（卸下时调用）。</summary>
    public void Shutdown()
    {
        _on = false;
        DestroyLaser();
    }
}