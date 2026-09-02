using System;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Hexagon AK 管状护木（Anodized Red）【Hexagon AK】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 后坐力 -0.5%（knockBack × 0.995）
/// - 每发耐久损耗 -5%（conditionLossPerShot × 0.95）
///
/// 安装要求：
/// - 需要 Leatherman 工具钳（ToolSystem.AttachmentRequiresLeatherman）
/// - 与其他护木（MOE AKM）互斥：护木槽位同一把枪只能装一个
/// - 无其他安装前提
///
/// 视觉：方案 A 纹理合成。该护木右侧更长（有延伸），左侧位置与原护木（MOE AKM）不变，
/// 所以叠加中心比 moeakm 左移，让左缘对齐原位置、右侧延伸出去。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class HexagonAkHandguardItemSystem
{
    public const string ItemKey = "hexagonak_hg";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("hexagonak_hg.name");
    public static string Description => I18n.Tr("hexagonak_hg.desc");

    // 效果参数（用户指定）
    public const float KnockBackMult = 0.995f;   // 后坐力 -0.5%
    public const float ConditionLossMult = 0.95f; // 每发耐久损耗 -5%

    // 未指定数值的默认设定
    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsHexagonAkHandguardRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsHexagonAkHandguardRequest(request)) return;
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
        Plugin.Log.LogInfo($"[Hexagon AK] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[Hexagon AK] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Hexagon AK] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[Hexagon AK] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        var tex = TacticalLightHelper.LoadPointIconTexture("guns/akm/hexagonak_hg.png");
        if (tex != null)
        {
            _cachedIcon = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 14f); // PPI 14 与枪械贴图一致
            _cachedIcon.name = "hexagonak_hg-icon";
        }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>供纹理合成器使用的可读护木贴图（无贴图返回 null）。</summary>
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