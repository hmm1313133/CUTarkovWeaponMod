using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// SKS 7.62x39 卡宾枪 UAS 套件组【UAS SKS】。
/// 由 Fab Defence 制造的 UAS 套件，一整套配件，可以直接安装使用。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 后坐力 -30%（knockBack × 0.70）
/// - 瞄准速度 -1s（AimSystem.AttachmentAimTimeDelta -1.0，加快）
/// - 每发耐久损耗 -7%（conditionLossPerShot × 0.93）
///
/// 安装 UAS 后：
/// - 可安装手电（战术设备）和瞄准镜（仅 553全息/MRS/微型速瞄，其他倍镜不允许）
/// - 玩家手持位置挪到握把位置
///
/// 视觉：方案 A 局部叠加贴图（uas sks.png），与 SKS 贴图位置完全相同直接覆盖，
/// 需擦除 SKS 贴图 x轴45往左的部分。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class UasSksItemSystem
{
    public const string ItemKey = "uas_sks";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("uas_sks.name");
    public static string Description => I18n.Tr("uas_sks.desc");

    // 效果参数（用户指定）
    public const float KnockBackMult = 0.70f;    // 后坐力 -30%
    public const float ConditionLossMult = 0.93f; // 每发耐久损耗 -7%
    public const float AimTimeDelta = -1.0f;     // 瞄准速度 -1s（加快）

    // UAS 允许安装的瞄准镜白名单（553全息 / MRS / 微型速瞄）
    // 注意：当前模组没有独立"微型速瞄"，此处白名单 = MRS + Eotech553 + DeltaPoint + ACRO P-1
    public static readonly string[] AllowedSights =
    {
        MrsItemSystem.ItemKey,
        Eotech553ItemSystem.ItemKey,
        DeltaPointItemSystem.ItemKey,
        AcroP1ItemSystem.ItemKey,
    };

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedVisualIcon;

    public static bool IsUasSksRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsUasSksRequest(request)) return;
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
        Plugin.Log.LogInfo($"[UAS SKS] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[UAS SKS] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[UAS SKS] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[UAS SKS] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        _cachedIcon = LoadSprite("uas sks.png", "uas-sks-icon");
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static Sprite? TryLoadVisualIconPublic() => TryLoadVisualIcon();

    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadVisualIcon();
        return spr != null ? spr.texture : null;
    }

    private static Sprite? TryLoadVisualIcon()
    {
        if (_cachedVisualIcon != null) return _cachedVisualIcon;
        _cachedVisualIcon = LoadSprite("uas sks.png", "uas-sks-visual");
        return _cachedVisualIcon;
    }

    private static Sprite? LoadSprite(string file, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", file);
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    var spr = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.30f, 0.5f), 22.5f);
                    spr.name = name;
                    return spr;
                }
            }
            else Plugin.Log.LogWarning($"[UAS SKS] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[UAS SKS] Icon: {ex.Message}"); }
        return null;
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
