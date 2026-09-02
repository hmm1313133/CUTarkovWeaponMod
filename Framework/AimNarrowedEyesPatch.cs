using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 瞄准时眯眼（Narrowed eyes）补丁。
/// 游戏原生 eyeList[1] 是半闭眼（Narrowed eyes）。
/// 玩家长按右键瞄准时，强制眼睛显示为半闭眼表情。
/// </summary>
[HarmonyPatch(typeof(FacialExpression), nameof(FacialExpression.Update))]
public static class AimNarrowedEyesPatch
{
    [HarmonyPostfix]
    public static void Postfix(FacialExpression __instance)
    {
        try
        {
            var body = __instance.body;
            if (body == null) return;
            var handItem = body.GetItem(body.handSlot);
            if (handItem == null || handItem.GetComponent<GunScript>() == null) return;

            // 瞄准中（aimProgress > 0）才眯眼
            if (AimSystem.GetAimProgress(handItem) <= 0.01f) return;
            if (__instance.eyeList == null || __instance.eyeList.Count < 2) return;
            if (__instance.eyes == null) return;

            // eyeList[1] = 半闭眼（Narrowed eyes）
            var narrowed = __instance.eyeList[1];
            if (narrowed.front != null)
                __instance.eyes.sprite = narrowed.front;
        }
        catch { }
    }
}