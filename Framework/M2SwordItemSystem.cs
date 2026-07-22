using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Miller Bros. Blades M-2 战术剑 - 近战武器。
/// 生物伤害87，结构伤害31，攻击距离7，击退320，穿刺。
/// 每次命中消耗 0.002 耐久。
/// </summary>
public static class M2SwordItemSystem
{
    public const string ItemKey = "m2sword";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("m2sword.name");
    public static string Description => I18n.Tr("m2sword.desc");

    private static Sprite? _cachedIcon;

    public static bool IsM2SwordRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsM2SwordRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<M2SwordItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<M2SwordItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[M2Sword] Configured spawned item '{ItemKey}' (condition={item.condition}).");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "utility",
                slotRotation = -90f,
                usable = true,
                usableOnLimb = false,
                usableWithLMB = true,
                autoAttack = true,
                destroyAtZeroCondition = true,
                wearable = false,
                weight = 0.8f,
                scaleWeightWithCondition = false,
                combineable = true,
                value = 30,
                tags = "cangetwet,tool,cutting,hammering,backflip",
                qualities = new List<CraftingQuality>
                {
                    new CraftingQuality("cutting", 30f),
                    new CraftingQuality("hammering", 12f),
                },
                rec = new Recognition(5),
            };
            info.SetTags();

            var useMethod = typeof(M2SwordItemSystem).GetMethod(
                nameof(M2SwordUseAction),
                BindingFlags.Static | BindingFlags.NonPublic);
            if (useMethod != null)
            {
                info.useAction = (ItemInfo.Use)Delegate.CreateDelegate(
                    typeof(ItemInfo.Use), useMethod);
            }

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[M2Sword] Registered '{ItemKey}' as melee weapon.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[M2Sword] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    // ===== Icon =====

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "knife", "m2sword.png");

            Plugin.Log.LogInfo($"[M2Sword] Looking for icon at: {iconPath}");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                Plugin.Log.LogInfo($"[M2Sword] Icon file found, size={bytes.Length} bytes.");
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false))
                {
                    Plugin.Log.LogWarning($"[M2Sword] ImageConversion.LoadImage failed for {iconPath}.");
                    return null;
                }
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.25f), 22.5f);
                _cachedIcon.name = "m2sword-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[M2Sword] Failed to load icon: {ex.Message}");
        }

        if (_cachedIcon == null)
            Plugin.Log.LogWarning($"[M2Sword] Icon not loaded - item will use bruisekit default sprite.");

        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static void ResizeColliderToSprite(Item item)
    {
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        var col = item.GetComponent<BoxCollider2D>();
        if (col == null) col = item.gameObject.AddComponent<BoxCollider2D>();
        var bounds = sr.sprite.bounds;
        col.size = new Vector2(bounds.size.x, bounds.size.y);
        col.offset = Vector2.zero;
    }

    // ===== Attack =====

    private const float ConditionLossPerAttack = 0.001f;

    private static void M2SwordUseAction(Body body, Item item)
    {
        var atk = new AttackInfo
        {
            damage = 87f,
            structuralDamage = 31f,
            attackCooldownMult = 0.7f,
            distance = 7f,
            knockBack = 320f,
            cooldown = 0.3f,
            attackAnim = Resources.Load<GameObject>("SwingAnim"),
            staminaUse = 0.85f,
            piercing = false,
            swingSounds = new string[] { "" },
            volume = 20f,
            rotateAmount = 15.5f,
        };

        if (body.Attack(atk, body.handSlot))
        {
            item.condition -= ConditionLossPerAttack;
        }
    }
}

/// <summary>
/// M-2 战术剑物品标记组件。
/// 在 Start() 中强制设置自定义图标。
/// </summary>
public sealed class M2SwordItemMarker : MonoBehaviour
{
    public string displayName = M2SwordItemSystem.DisplayName;
    public string description = M2SwordItemSystem.Description;

    private void Start()
    {
        var icon = M2SwordItemSystem.TryLoadIconPublic();
        if (icon != null)
        {
            var srs = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                if (sr != null)
                    sr.sprite = icon;
            }
        }

        var item = GetComponent<Item>();
        if (item != null)
            M2SwordItemSystem.ResizeColliderToSprite(item);
    }
}

/// <summary>
/// M-2 战术剑悬停描述补丁。
/// 仅覆盖名称（Item1），不覆盖描述（Item2）--游戏已从 ItemInfo.description/weight/value/tags
/// 构建完整详细页面。
/// 功能性 tags（cangetwet, tool, cutting, hammering）在 ItemInfo.tags 中声明。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class M2SwordHoverPatch
{
    [HarmonyPostfix]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        var marker = item.GetComponent<M2SwordItemMarker>();
        if (marker == null) return;
        if (!item.Stats.rec.recognizable) return;
        // Name updated by I18nRefreshPatch Prefix
        // 不覆盖 Item2：保留游戏构建的完整描述页（含重量/价值/tags/描述等）
    }
}
