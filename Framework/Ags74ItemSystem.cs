using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// AK Custom Arms AGS-74 PRO + Sniper Kit 手枪式握把【AGS-74】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 后坐力 -3%（knockBack × 0.97）
/// - 精准度 +3%（verticalSpread × 0.97，越小越准）
///
/// 安装要求：
/// - 需要 Leatherman 工具钳（ToolSystem.AttachmentRequiresLeatherman）
/// - 占用后握把槽：与一体式枪托（CQR47）、RK-3、MG-47 互斥，同一把枪只能装一个
/// - 无其他安装前提
///
/// 视觉：方案 A 纹理合成。位置与 RK-3 相同（覆盖原 AK 后握把区域），
/// 但贴图向左和下各延伸 1px（8x10 vs 7x9），对齐上/右侧。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class Ags74ItemSystem
{
    public const string ItemKey = "ags74";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("ags74.name");
    public static string Description => I18n.Tr("ags74.desc");

    // 效果参数（用户指定）
    public const float KnockBackMult = 0.97f;   // 后坐力 -3%
    public const float SpreadMult = 0.97f;      // 精准度 +3%（散布 -3%）

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsAgs74Request(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsAgs74Request(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "utility";
        item.Stats.tags = "attachment,backflip";
        item.Stats.SetTags();
        item.Stats.weight = Weight;
        item.Stats.value = Value;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[AGS-74] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[AGS-74] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[AGS-74] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[AGS-74] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        var tex = TacticalLightHelper.LoadPointIconTexture("guns/akm/ags74.png");
        if (tex != null)
        {
            _cachedIcon = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 14f); // PPI 14 与枪械贴图一致
            _cachedIcon.name = "ags74-icon";
        }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>供纹理合成器使用的可读握把贴图（无贴图返回 null）。</summary>
    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadIcon();
        return spr != null ? spr.texture : null;
    }

    private static void ResizeColliderToSprite(Item item)
    {
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        var col = item.GetComponent<BoxCollider2D>();
        if (col == null) col = item.gameObject.AddComponent<BoxCollider2D>();
        var bounds = sr.sprite.bounds;
        col.size = new Vector2(bounds.size.x, bounds.size.y);
        col.offset = Vector2.zero;
    }
}