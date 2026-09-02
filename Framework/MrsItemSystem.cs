using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Walther MRS 反射式瞄具【MRS】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 有电量时：精准度 +10%（verticalSpread × 0.90）
/// - 无电量时：无效果
///
/// 使用小型电池供电，续航 2 小时（7200 秒）。
/// 电量存于 GunAttachmentHolder.mrsCharge（0~1），安装时从物品 condition 读取。
///
/// 安装要求：
/// - 需要 Leatherman 工具钳（ToolSystem.AttachmentRequiresLeatherman）
/// - 前提：先安装 PDC 导轨防尘盖（瞄准镜槽）
/// - 占用瞄准镜槽：一把枪只能装一个瞄准镜
///
/// 视觉：方案 A 纹理合成，瞄具绘制在 PDC 导轨上方（机匣顶部）。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class MrsItemSystem
{
    public const string ItemKey = "mrs";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("mrs.name");
    public static string Description => I18n.Tr("mrs.desc");

    private const string IconSubPath = "guns/common/mrs.png";

    // 效果参数（用户指定）
    public const float SpreadMult = 0.90f;      // 精准度 +10%（散布 -10%）——仅在有电时生效

    // 电池 / 时长参数：2 小时 = 7200 秒满电→空
    public const float DrainPerSecond = 1f / 7200f;

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsMrsRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsMrsRequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!, withBattery: false);

        Plugin.Log.LogInfo($"[MRS] Configured spawned item '{ItemKey}' (no battery).");
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
            Plugin.Log.LogInfo($"[MRS] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[MRS] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[MRS] CUCoreLib: Icon={customInfo.Icon != null}.");
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
            _cachedIcon.name = "mrs-icon";
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
/// MRS 瞄具电量控制器（挂在枪上）。
/// 电量存于 GunAttachmentHolder.mrsCharge，随时间消耗（2 小时满电→空）。
/// 电量耗尽后精准度加成消失（FireEffectsPatch 读取电量判断是否生效）。
/// </summary>
public sealed class MrsController : MonoBehaviour
{
    private Item? _gunItem;

    public static MrsController Attach(Item gunItem)
    {
        if (gunItem == null) return null!;
        var ctrl = gunItem.gameObject.GetComponent<MrsController>();
        if (ctrl == null) ctrl = gunItem.gameObject.AddComponent<MrsController>();
        ctrl._gunItem = gunItem;
        return ctrl;
    }

    private void Awake()
    {
        if (_gunItem == null) _gunItem = GetComponent<Item>();
    }

    private void Update()
    {
        if (_gunItem == null) return;
        var holder = _gunItem.GetComponent<GunAttachmentHolder>();
        if (holder == null) return;
        if (holder.mrsCharge > 0f)
        {
            holder.mrsCharge -= MrsItemSystem.DrainPerSecond * Time.deltaTime;
            if (holder.mrsCharge < 0f) holder.mrsCharge = 0f;
        }
    }
}