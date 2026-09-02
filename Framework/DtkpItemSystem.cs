using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Hexagon DTKP MK.2 7.62x39 消音器【DTKP】。
/// Hexagon 制造的 DTKP 消音器，7.62x39 毫米口径。为枪口螺纹规格为 24x1.5 的现代化 AK 平台而设计。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 后坐力 -4%（knockBack × 0.96）
/// - 听力损伤 -50%（loudness × 0.5）
/// - 精准度 -4.5%（散布 × 1.045）
/// - 瞄准速度 +0.32s（AimSystem.AttachmentAimTimeDelta +0.32，减慢）
/// - 无枪口火光
///
/// AKM 专属枪口槽；SKS 可通过 WT0032-1 转接器安装。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class DtkpItemSystem
{
    public const string ItemKey = "dtkp";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("dtkp.name");
    public static string Description => I18n.Tr("dtkp.desc");

    public const float KnockBackMult = 0.96f;   // 后坐力 -4%
    public const float LoudnessMult = 0.5f;     // 听力损伤 -50%
    public const float SpreadMult = 1f;           // 取消精准度减益
    public const float AimTimeDelta = 0.32f;    // 瞄准速度 +0.32s（减慢）

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedVisualIcon;

    public static bool IsDtkpRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsDtkpRequest(request)) return;
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
        Plugin.Log.LogInfo($"[DTKP] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[DTKP] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[DTKP] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[DTKP] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        _cachedIcon = LoadSprite("dtkp.png", "dtkp-icon");
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
        _cachedVisualIcon = LoadSprite("dtkp.png", "dtkp-visual");
        return _cachedVisualIcon;
    }

    private static Sprite? LoadSprite(string file, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "akm", file);
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
                        new Vector2(0.5f, 0.5f), 14f);
                    spr.name = name;
                    return spr;
                }
            }
            else Plugin.Log.LogWarning($"[DTKP] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[DTKP] Icon: {ex.Message}"); }
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
