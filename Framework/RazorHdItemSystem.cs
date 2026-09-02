using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Vortex Razor HD Gen.2 1-6x24 30 毫米步枪瞄准镜【Razor HD Gen.2】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 精准度 +35%（verticalSpread × 0.65，永久生效，无供电机制）
/// - 瞄准速度 +0.5 秒（变慢，见 AimSystem.AttachmentAimTimeDelta）
/// - 三模式倍率：1x / 3x / 6x（按 O 键循环，视野变远幅度见 AimZoomFovPatch）
///
/// 安装要求：
/// - 前提：先安装 PDC 导轨防尘盖（瞄准镜槽）
/// - 无需 Leatherman 工具钳
/// - 占用瞄准镜槽：一把枪只能装一个瞄准镜
///
/// 视觉：方案 A 纹理合成，瞄具绘制在 PDC 导轨上方（机匣顶部）。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class RazorHdItemSystem
{
    public const string ItemKey = "razorhd";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("razorhd.name");
    public static string Description => I18n.Tr("razorhd.desc");

    private const string IconSubPath = "guns/common/razorhd.png";

    // 效果参数（用户指定）
    public const float SpreadMult = 0.65f;      // 精准度 +35%（散布 -35%）

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsRazorHdRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsRazorHdRequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!, withBattery: false);

        Plugin.Log.LogInfo($"[Razor HD] Configured spawned item '{ItemKey}' (no battery).");
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
            Plugin.Log.LogInfo($"[Razor HD] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Razor HD] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[Razor HD] CUCoreLib: Icon={customInfo.Icon != null}.");
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
            _cachedIcon.name = "razorhd-icon";
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
/// Razor HD 瞄具控制器（挂在枪上）。三模式倍率：1x / 3x / 6x（按 O 键循环）。
/// </summary>
public sealed class RazorHdController : MonoBehaviour
{
    private const float ZoomTimeValue = 0.2f;   // >0 触发倍镜视野

    private Item? _gunItem;
    private int _mode;   // 0=1x, 1=3x, 2=6x

    public int Mode => _mode;
    public bool IsZoomed => _mode > 0;

    public static RazorHdController Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<RazorHdController>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<RazorHdController>();
        ctrl._gunItem = gunItem;
        ctrl._mode = 0;
        return ctrl;
    }

    private void Awake()
    {
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void Update()
    {
        if (_gunItem == null) return;

        bool held = IsHeldByPlayer();
        if (held && Input.GetKeyDown(ScopeZoomKeybindPatch.CurrentKey))
        {
            _mode = (_mode + 1) % 3;   // 0→1→2→0
            Plugin.Log.LogInfo($"[Razor HD] Mode → {GetModeLabel()}.");
        }

        var cam = PlayerCamera.main;
        if (cam != null)
        {
            if (held && _mode > 0) cam.zoomTime = ZoomTimeValue;
            else cam.zoomTime = 0f;
        }
    }

    public string GetModeLabel()
    {
        return _mode switch { 1 => "3x", 2 => "6x", _ => "1x" };
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }
}