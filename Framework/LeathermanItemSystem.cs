using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Leatherman 多功能工具钳。
///
/// 是一个独立的工具物品（非枪械配件、不消耗），
/// 仅用于「需要工具钳安装的配件」判定——见 <see cref="ToolSystem"/>。
///
/// 规格参考：weight 0.5u（与消音器同级工具），value 35，源图标 32x20 PPI 16，
/// 类别 custom，耐久用尽销毁，但不调用任何 useAction（右键不消耗也不触发效果）。
///
/// 配方（RecipePatch）：2废料管 + 1废料板 + 2细绳 + 2钉子性质 + 3切割性质 + 2捶打性质，INT=12。
/// </summary>
public static class LeathermanItemSystem
{
    public const string ItemKey = "leatherman";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("leatherman.name");
    public static string Description => I18n.Tr("leatherman.desc");

    private const float Weight = 0.5f;
    private const int Value = 20;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsLeathermanRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsLeathermanRequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        // 工具钳：不可右键触发 useAction（不做任何事）。
        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "custom";
        // 用 "tool" tag 标识，但**不**调用 SetTags()，避免把 "tool" 写入 actualTags
        // 干扰游戏其他逻辑（参考 tkfastmt 的 bikehelmet 方案）。
        item.Stats.tags = "tool";
        item.Stats.weight = Weight;
        item.Stats.value = Value;
        item.Stats.destroyAtZeroCondition = true;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<LeathermanItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<LeathermanItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[Leatherman] Configured spawned item '{ItemKey}' (condition={item.condition}).");
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
                category = "custom",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                usableWithLMB = false,
                autoAttack = false,
                destroyAtZeroCondition = true,
                weight = Weight,
                value = Value,
                tags = "tool",
                rec = new Recognition(RecognitionMin),
            };
            // 工具钳不调用 SetTags()，参考 tkfastmt 仿制做法：
            // 把 "tool" 保留在 tags 字段但不让其进入 actualTags，
            // 避免被其他游戏逻辑（如某些 patch 检查 tool tag）误判。
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[Leatherman] Registered '{ItemKey}' as tool (tag=tool, no SetTags).");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[Leatherman] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[Leatherman] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "leatherman", "leatherman.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedIcon = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 16f);
                    _cachedIcon.name = "leatherman-icon";
                }
            }
            else Plugin.Log.LogWarning($"[Leatherman] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[Leatherman] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

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

/// <summary>
/// 工具钳物品标记组件（用于悬停描述补丁识别自定义物品）。
/// </summary>
public sealed class LeathermanItemMarker : MonoBehaviour
{
    public string displayName = LeathermanItemSystem.DisplayName;
    public string description = LeathermanItemSystem.Description;
}