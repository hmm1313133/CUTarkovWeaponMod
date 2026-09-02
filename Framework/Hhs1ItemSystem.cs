using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// EOTech HHS-1 复合瞄具【HHS-1】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 有电量时：精准度 +25%（verticalSpread × 0.75）
/// - 无电量时：无效果，且不能放大
///
/// 使用小型电池供电，续航 3 小时（10800 秒）。
/// 电量存于 GunAttachmentHolder.hhs1Charge（0~1），安装时从物品 condition 读取。
///
/// 倍率：按 O 键切换 1x / 3x。3x 放大与 AXMC 相同（复用 zoomTime 机制）。
/// 当前倍率显示在枪械卸下弹匣、保险 UI 栏位的右侧（"1x/3x"）。
///
/// 安装要求：
/// - 前提：先安装 PDC 导轨防尘盖（瞄准镜槽）
/// - 无需 Leatherman 工具钳
/// - 占用瞄准镜槽：一把枪只能装一个瞄准镜
///
/// 视觉：方案 A 纹理合成，瞄具绘制在 PDC 导轨上方（机匣顶部）。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class Hhs1ItemSystem
{
    public const string ItemKey = "hhs1";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("hhs1.name");
    public static string Description => I18n.Tr("hhs1.desc");

    private const string IconSubPath = "guns/common/hhs1.png";

    // 效果参数（用户指定）
    public const float SpreadMult = 0.75f;      // 精准度 +25%（散布 -25%）——仅在有电时生效

    // 电池 / 时长参数：3 小时 = 10800 秒满电→空
    public const float DrainPerSecond = 1f / 10800f;

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsHhs1Request(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsHhs1Request(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!, withBattery: false);

        Plugin.Log.LogInfo($"[HHS-1] Configured spawned item '{ItemKey}' (no battery).");
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
            Plugin.Log.LogInfo($"[HHS-1] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[HHS-1] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[HHS-1] CUCoreLib: Icon={customInfo.Icon != null}.");
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
            _cachedIcon.name = "hhs1-icon";
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
/// HHS-1 瞄具控制器（挂在枪上）。
/// - 电量存于 GunAttachmentHolder.hhs1Charge，随时间消耗（3 小时满电→空）
/// - 按 O 键切换倍率（1x/3x）
/// - 3x 时复用 zoomTime 机制（与 AXMC 相同）
/// - 无电量时不能放大
/// </summary>
public sealed class Hhs1Controller : MonoBehaviour
{
    private const float ZoomTimeValue = 0.25f;  // 3x 放大（以 AXMC 0.5=6x 为基准线性推导：3x=0.25）

    private Item? _gunItem;
    private bool _zoomed;

    public bool IsZoomed => _zoomed;

    public static Hhs1Controller Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<Hhs1Controller>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<Hhs1Controller>();
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

        // O 键切换倍率（仅手持此枪时）
        bool held = IsHeldByPlayer();
        if (held && Input.GetKeyDown(ScopeZoomKeybindPatch.CurrentKey))
        {
            _zoomed = !_zoomed;
            Plugin.Log.LogInfo($"[HHS-1] Zoom → {(_zoomed ? "3x" : "1x")}.");
        }

        // 应用放大：3x 且手持（无供电机制，始终可用）
        var cam = PlayerCamera.main;
        if (cam != null)
        {
            if (held && _zoomed)
                cam.zoomTime = ZoomTimeValue;
            else
                cam.zoomTime = 0f; // 切回 1x 或离手时立即清零，避免衰减期间残留倍镜视野
        }
    }

    private bool IsHeldByPlayer()
    {
        var body = PlayerCamera.main?.body;
        if (body == null || _gunItem == null) return false;
        return body.GetItem(body.handSlot) == _gunItem;
    }
}