using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Leupold DeltaPoint 反射式瞄具【DP】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 精准度 +12%（verticalSpread × 0.88）
///
/// 安装要求：
/// - 占用瞄准镜槽：一把枪只能装一个瞄准镜
/// - 格洛克需先装 UM3 基座；SKS 需先装 UAS/MTU017；沙鹰可直接装
///
/// 视觉：方案 A 纹理合成，瞄具绘制在机匣顶部（AddSightLayer）。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class DeltaPointItemSystem
{
    public const string ItemKey = "dp";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("dp.name");
    public static string Description => I18n.Tr("dp.desc");

    private const string IconSubPath = "guns/common/DP.png";

    // 效果参数（用户指定）
    public const float SpreadMult = 0.88f;      // 精准度 +12%（散布 -12%）

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsDpRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsDpRequest(request)) return;

        var icon = TryLoadIcon();
        TacticalLightHelper.ConfigureFlashlightBase(
            item, ItemKey, IconSubPath, Weight, Value, RecognitionMin, icon!, withBattery: false);

        Plugin.Log.LogInfo($"[DeltaPoint] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[DeltaPoint] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[DeltaPoint] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[DeltaPoint] CUCoreLib: Icon={customInfo.Icon != null}.");
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
            _cachedIcon.name = "dp-icon";
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
