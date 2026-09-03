using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

public static class BlueAreaKeycardItemSystem
{
    public const string ItemKey = "bluearea_keycard";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => WModLoc.Tr("bluearea_keycard.name", "Terragroup-Blue Area钥匙卡【Blue Area】");
    public static string Description => WModLoc.Tr("bluearea_keycard.desc", "Terragroup的Blue Area钥匙卡，使用此卡可解锁Labs的Blue Area。\n\n<color=#4fc3f7>每次使用损耗 2% 耐久</color>");
    private const float Weight = 0.05f;
    private const int Value = 100;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;

    public static bool IsKeycardRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsKeycardRequest(request)) return;

        item.id = ItemKey;
        item.SetCondition(1f);

        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "custom";
        item.Stats.tags = "keycard";
        item.Stats.weight = Weight;
        item.Stats.value = Value;
        item.Stats.destroyAtZeroCondition = true;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;

        var col = item.GetComponent<BoxCollider2D>();
        if (col == null) col = item.gameObject.AddComponent<BoxCollider2D>();
        if (sr != null && sr.sprite != null)
        {
            var bounds = sr.sprite.bounds;
            col.size = new Vector2(bounds.size.x, bounds.size.y);
            col.offset = Vector2.zero;
        }

        Plugin.Log.LogInfo($"[BlueAreaKeycard] Configured spawned item '{ItemKey}'.");
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
                tags = "keycard",
                rec = new Recognition(RecognitionMin),
            };
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[BlueAreaKeycard] Registered '{ItemKey}' in GlobalItems.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[BlueAreaKeycard] Register failed: {ex}");
            return false;
        }
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
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "blueareacard.png");
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
                        new Vector2(0.5f, 0.5f), 18f);
                    _cachedIcon.name = "blueareacard-icon";
                }
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[BlueAreaKeycard] Icon load failed: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();
}
