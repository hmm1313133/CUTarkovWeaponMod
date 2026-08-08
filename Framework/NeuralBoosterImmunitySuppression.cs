using System;
using HarmonyLib;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 药物副作用导致的失明不应享受头盔/面罩免疫。
///
/// 原版 neuralbooster（神经增强剂）在第二次使用时触发严重副作用，
/// 其 useAction 委托内连续调用两次 body.RemoveEye()（一次摘掉双眼）。
/// useAction 是匿名委托无法直接 Patch，因此改为 Patch useAction 的调用入口：
/// - Body.UseItem(Item)
/// - Body.UseItemInHand()
///
/// 当使用中的物品是 neuralbooster 且 body.usedNeuralBooster 已为 true（第二次使用）
/// 时，在副作用执行期间挂起眼部失明免疫（Prefix 设置，Postfix 清除）。
/// </summary>
public static class NeuralBoosterImmunitySuppression
{
    private const string NeuralBoosterId = "neuralbooster";

    // ===== Body.UseItem =====

    [HarmonyPatch(typeof(Body), nameof(Body.UseItem))]
    [HarmonyPrefix]
    public static void UseItemPrefix(Body __instance, Item item)
    {
        TrySuppressForNeuralBooster(__instance, item);
    }

    [HarmonyPatch(typeof(Body), nameof(Body.UseItem))]
    [HarmonyPostfix]
    public static void UseItemPostfix()
    {
        HeadInjuryProtectionHelper.SuppressEyeImmunity = false;
    }

    // ===== Body.UseItemInHand =====

    [HarmonyPatch(typeof(Body), nameof(Body.UseItemInHand))]
    [HarmonyPrefix]
    public static void UseItemInHandPrefix(Body __instance)
    {
        try
        {
            var item = __instance.GetItem(__instance.handSlot);
            TrySuppressForNeuralBooster(__instance, item);
        }
        catch { }
    }

    [HarmonyPatch(typeof(Body), nameof(Body.UseItemInHand))]
    [HarmonyPostfix]
    public static void UseItemInHandPostfix()
    {
        HeadInjuryProtectionHelper.SuppressEyeImmunity = false;
    }

    /// <summary>
    /// 仅当第二次使用 neuralbooster（usedNeuralBooster 已为 true）时挂起免疫。
    /// </summary>
    private static void TrySuppressForNeuralBooster(Body body, Item item)
    {
        if (body == null || item == null) return;
        if (!item.id.Equals(NeuralBoosterId, StringComparison.OrdinalIgnoreCase)) return;
        if (!body.usedNeuralBooster) return;

        HeadInjuryProtectionHelper.SuppressEyeImmunity = true;
    }
}
