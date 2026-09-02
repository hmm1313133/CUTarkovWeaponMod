using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// SKS Leapers UTG PRO MTU017 机匣基座【MTU017】。
/// Leapers Inc 为 SKS 卡宾枪设计的皮卡汀尼导轨基座，可安装在机匣盖上。
///
/// 效果（参考 AKM 的 PDC 防尘盖）：
/// - 安装后可使 SKS 安装所有瞄准镜
/// - 只能在 SKS 原厂、SKS MC、Tapco 上安装（UAS 不可安装）
///
/// 视觉：方案 A 局部叠加贴图（MTU017.png，158x41 与 SKS 同尺寸），
/// 位置参考 SKS MC 枪托参数。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class Mtu017ItemSystem
{
    public const string ItemKey = "mtu017";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("mtu017.name");
    public static string Description => I18n.Tr("mtu017.desc");

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedVisualIcon;

    public static bool IsMtu017Request(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsMtu017Request(request)) return;
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
        Plugin.Log.LogInfo($"[MTU017] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[MTU017] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[MTU017] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[MTU017] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        _cachedIcon = LoadSprite("MTU017.png", "mtu017-icon");
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
        _cachedVisualIcon = LoadSprite("MTU017.png", "mtu017-visual");
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
            else Plugin.Log.LogWarning($"[MTU017] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[MTU017] Icon: {ex.Message}"); }
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
