using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Monstrum 紧凑战术棱镜式瞄准镜 2x32【Monstr. 2x32】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 精准度 +15%（verticalSpread × 0.85，永久生效，无供电机制）
/// - 瞄准速度 -0.2 秒（加快瞄准，见 AimSystem.AttachmentAimTimeDelta）
/// - 单两倍缩放（2x，视野变远幅度 ×3，见 AimZoomFovPatch）
///
/// 安装要求：
/// - 前提：先安装 PDC 导轨防尘盖（瞄准镜槽）
/// - 无需 Leatherman 工具钳
/// - 占用瞄准镜槽：一把枪只能装一个瞄准镜
///
/// 视觉：方案 A 纹理合成，瞄具绘制在 PDC 导轨上方（机匣顶部）。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class Monstr2x32ItemSystem
{
    public const string ItemKey = "monstr2x32";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("monstr2x32.name");
    public static string Description => I18n.Tr("monstr2x32.desc");

    private const string IconSubPath = "guns/common/monstr2x32.png";

    // 效果参数（用户指定）
    public const float SpreadMult = 0.85f;      // 精准度 +15%（散布 -15%）

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsMonstr2x32Request(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsMonstr2x32Request(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!, withBattery: false);

        Plugin.Log.LogInfo($"[Monstr 2x32] Configured spawned item '{ItemKey}' (no battery).");
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
            Plugin.Log.LogInfo($"[Monstr 2x32] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Monstr 2x32] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[Monstr 2x32] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        var tex = TacticalLightHelper.LoadPointIconTexture(IconSubPath);
        if (tex != null)
        {
            _cachedIcon = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 14f); // PPI 14 与枪械贴图一致
            _cachedIcon.name = "monstr2x32-icon";
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
/// Monstr 2x32 瞄具控制器（挂在枪上）。
/// 单两倍缩放（2x）：按 O 键开/关，视野变远幅度 ×3（见 AimZoomFovPatch）。
/// </summary>
public sealed class Monstr2x32Controller : MonoBehaviour
{
    private const float ZoomTimeValue = 0.2f;   // >0 触发倍镜视野

    private Item? _gunItem;
    private bool _zoomed;

    public bool IsZoomed => _zoomed;

    public static Monstr2x32Controller Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<Monstr2x32Controller>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<Monstr2x32Controller>();
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

        // 单模式缩放（固定 2x）：始终放大，无 off 状态，不可切换
        bool held = IsHeldByPlayer();
        _zoomed = true;

        var cam = PlayerCamera.main;
        if (cam != null)
        {
            if (held && _zoomed)
                cam.zoomTime = ZoomTimeValue;
            else
                cam.zoomTime = 0f;
        }
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }
}