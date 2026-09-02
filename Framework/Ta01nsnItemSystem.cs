using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Trijicon ACOG TA01NSN 4x32 瞄准镜（黄褐色）【TA01NSN】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 精准度 +28%（verticalSpread × 0.72，永久生效，无供电机制）
/// - 瞄准速度 +0.3 秒（变慢，见 AimSystem.AttachmentAimTimeDelta）
/// - 单 4 倍缩放（视野变远幅度 ×6，见 AimZoomFovPatch）
///
/// 安装要求：
/// - 前提：先安装 PDC 导轨防尘盖（瞄准镜槽）
/// - 无需 Leatherman 工具钳
/// - 占用瞄准镜槽：一把枪只能装一个瞄准镜
///
/// 视觉：方案 A 纹理合成，瞄具绘制在 PDC 导轨上方（机匣顶部）。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class Ta01nsnItemSystem
{
    public const string ItemKey = "ta01nsn";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("ta01nsn.name");
    public static string Description => I18n.Tr("ta01nsn.desc");

    private const string IconSubPath = "guns/common/ta01nsn.png";

    // 效果参数（用户指定）
    public const float SpreadMult = 0.72f;      // 精准度 +28%（散布 -28%）

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsTa01nsnRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsTa01nsnRequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!, withBattery: false);

        Plugin.Log.LogInfo($"[TA01NSN] Configured spawned item '{ItemKey}' (no battery).");
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
            Plugin.Log.LogInfo($"[TA01NSN] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[TA01NSN] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[TA01NSN] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        var tex = TacticalLightHelper.LoadPointIconTexture(IconSubPath);
        if (tex != null)
        {
            _cachedIcon = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 14f);
            _cachedIcon.name = "ta01nsn-icon";
        }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>供纹理合成器使用的可读瞄具贴图（无贴图返回 null）。</summary>
    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadIcon();
        return spr != null ? spr.texture : null;
    }
}

/// <summary>
/// TA01NSN 瞄具控制器（挂在枪上）。单 4 倍缩放：按 O 键开/关。
/// </summary>
public sealed class Ta01nsnController : MonoBehaviour
{
    private const float ZoomTimeValue = 0.2f;   // >0 触发倍镜视野

    private Item? _gunItem;
    private bool _zoomed;

    public bool IsZoomed => _zoomed;

    public static Ta01nsnController Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<Ta01nsnController>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<Ta01nsnController>();
        ctrl._gunItem = gunItem;
        ctrl._zoomed = false;
        return ctrl;
    }

    private void Awake()
    {
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void Update()
    {
        if (_gunItem == null) return;

        // 单模式缩放（固定 4x）：始终放大，无 off 状态，不可切换
        bool held = IsHeldByPlayer();
        _zoomed = true;

        var cam = PlayerCamera.main;
        if (cam != null)
        {
            if (held && _zoomed) cam.zoomTime = ZoomTimeValue;
            else cam.zoomTime = 0f;
        }
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }
}