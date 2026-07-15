using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Red Rebel 冰镐 - 手持攀爬工具。
/// 拿在主手时允许在墙上连续跳跃（firstWallJump=true），每次消耗 0.002 耐久。
/// 左键可以攻击（usable=true, usableWithLMB=true）。
/// </summary>
public static class RedRebelItemSystem
{
    public const string ItemKey = "redrebel";
    public const string BaseGameItemId = "bruisekit"; // 有 Resources prefab 的基础物品

    public static string DisplayName => I18n.Tr("redrebel.name");
    public static string Description => I18n.Tr("redrebel.desc");

    private static Sprite? _cachedIcon;

    public static bool IsRedRebelRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsRedRebelRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        // 设置自定义图标
        var icon = TryLoadIcon();
        if (icon != null)
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = icon;
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<RedRebelItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<RedRebelItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[RedRebel] Configured spawned item '{ItemKey}' (condition={item.condition}).");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            // 创建手持工具 ItemInfo
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "utility",
                slotRotation = -90f,
                usable = true,           // 可使用（左键攻击）
                usableOnLimb = false,
                usableWithLMB = true,    // 左键触发
                autoAttack = true,
                destroyAtZeroCondition = true,
                wearable = false,        // 不是可穿戴物品，是手持工具
                weight = 1.1f,
                scaleWeightWithCondition = false,
                combineable = true,
                value = 30,
                tags = "cangetwet,tool,cutting,hammering,backflip",
                qualities = new List<CraftingQuality>
                {
                    new CraftingQuality("cutting", 12f),
                    new CraftingQuality("hammering", 18f),
                },
                rec = new Recognition(5),
            };
            info.SetTags();

            // 设置 useAction 攻击委托
            var useMethod = typeof(RedRebelItemSystem).GetMethod(
                nameof(RedRebelUseAction),
                BindingFlags.Static | BindingFlags.NonPublic);
            if (useMethod != null)
            {
                info.useAction = (ItemInfo.Use)Delegate.CreateDelegate(
                    typeof(ItemInfo.Use), useMethod);
            }

            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[RedRebel] Registered '{ItemKey}' as hand-held tool.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[RedRebel] Failed to register '{ItemKey}': {ex}");
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "redrebel.png");

            Plugin.Log.LogInfo($"[RedRebel] Looking for icon at: {iconPath}");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                Plugin.Log.LogInfo($"[RedRebel] Icon file found, size={bytes.Length} bytes.");
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false))
                {
                    Plugin.Log.LogWarning($"[RedRebel] ImageConversion.LoadImage failed for {iconPath}.");
                    return null;
                }
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.30f, 0.5f), 22.5f);
                _cachedIcon.name = "redrebel-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedRebel] Failed to load icon: {ex.Message}");
        }

        if (_cachedIcon == null)
            Plugin.Log.LogWarning($"[RedRebel] Icon not loaded - item will use bruisekit default sprite.");

        return _cachedIcon;
    }

    /// <summary>
    /// 公开方法供 RedRebelItemMarker.Start() 调用。
    /// </summary>
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

    private const float ConditionLossPerAttack = 0.0010157895f;

    /// <summary>
    /// 冰镐攻击委托 - 创建 AttackInfo 并调用 Body.Attack。
    /// 生物伤害37，结构伤害35，攻击冷却倍率0.7，攻击距离4，
    /// 基础冷却时间0.3，体力消耗0.8。
    /// </summary>
    private static void RedRebelUseAction(Body body, Item item)
    {
        var atk = new AttackInfo
        {
            damage = 37f,
            structuralDamage = 35f,
            attackCooldownMult = 0.7f,
            distance = 4f,
            knockBack = 270f,
            cooldown = 0.3f,
            attackAnim = Resources.Load<GameObject>("SwingAnim"),
            staminaUse = 0.95f,
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
/// Red Rebel 物品标记组件。
/// 在 Start() 中强制设置自定义图标，确保在预制体初始化之后执行。
/// </summary>
public sealed class RedRebelItemMarker : MonoBehaviour
{
    public string displayName = RedRebelItemSystem.DisplayName;
    public string description = RedRebelItemSystem.Description;

    private void Start()
    {
        // 在预制体初始化完成后强制设置自定义图标
        // （解决 bruisekit 预制体 Start() 可能重置精灵的问题）
        var icon = RedRebelItemSystem.TryLoadIconPublic();
        if (icon != null)
        {
            // 替换所有 SpriteRenderer（包括子对象）
            var srs = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                if (sr != null)
                    sr.sprite = icon;
            }
        }

        // 更新碰撞体以匹配新精灵
        var item = GetComponent<Item>();
        if (item != null)
            RedRebelItemSystem.ResizeColliderToSprite(item);
    }
}

/// <summary>
/// Red Rebel 悬停描述补丁。
/// 仅覆盖名称（Item1），不覆盖描述（Item2）——游戏已从 ItemInfo.description/weight/value/tags
/// 构建完整详细页面。攀爬功能由 RedRebelJumpPatch 提供，描述文本中已包含说明。
/// 功能性 tags（cangetwet, tool, cutting, hammering）在 ItemInfo.tags 中声明。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class RedRebelHoverPatch
{
    [HarmonyPostfix]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        var marker = item.GetComponent<RedRebelItemMarker>();
        if (marker == null) return;
        if (!item.Stats.rec.recognizable) return;
        __result.Item1 = marker.displayName;
        // 不覆盖 Item2：保留游戏构建的完整描述页（含重量/价值/tags/描述等）
    }
}

/// <summary>
/// Red Rebel 攀爬补丁 - 在 Body.Jump Postfix 中检测主手是否持有冰镐，
/// 如果持有则设置 firstWallJump=true（允许连续蹬墙跳），
/// 每次消耗 0.002 耐久 + 1.0 体力。
/// </summary>
[HarmonyPatch(typeof(Body), nameof(Body.Jump))]
public static class RedRebelJumpPatch
{
    private const float ConditionLossPerJump = 0.001f;
    private const float StaminaPerJump = 1.0f;

    [HarmonyPostfix]
    public static void Postfix(Body __instance)
    {
        try
        {
            // 如果原方法已经因为攀爬爪设置了 firstWallJump=true，则不需要重复处理
            if (__instance.firstWallJump) return;

            // 检查主手是否持有 Red Rebel 冰镐
            var handItem = __instance.GetItem(__instance.handSlot);
            if (handItem == null) return;
            if (!handItem.id.Equals(RedRebelItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                return;

            // 检查耐久和体力
            if (handItem.condition <= 0f) return;
            if (__instance.stamina < StaminaPerJump) return;

            // 检查是否在蹬墙跳场景（jumpCooldown 被设为 0.25 表示刚跳过）
            if (__instance.jumpCooldown > 0f && __instance.jumpCooldown <= 0.25f)
            {
                // 设置 firstWallJump=true，允许连续蹬墙跳
                __instance.firstWallJump = true;

                // 消耗耐久和体力
                handItem.condition -= ConditionLossPerJump;
                __instance.stamina -= StaminaPerJump;

                Plugin.Log.LogInfo($"[RedRebel] Wall jump enabled, condition -{ConditionLossPerJump}, stamina -{StaminaPerJump}.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedRebel] Jump postfix failed: {ex.Message}");
        }
    }
}
