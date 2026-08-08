using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Ops-Core FAST 护目罩头部保护效果：
/// - 30% 免疫眼部失明 (Body.RemoveEye)
/// （不免疫下颚缺失/毁容）
/// </summary>
public static class FastVisorHeadProtectionPatch
{
    // ===== 眼部失明免疫 =====

    [HarmonyPatch(typeof(Body), nameof(Body.RemoveEye))]
    [HarmonyPrefix]
    public static bool RemoveEyePrefix(Body __instance)
    {
        try
        {
            if (!__instance.HasWearable(FastVisorItemSystem.ItemKey)) return true;

            if (HeadInjuryProtectionHelper.TryBlockEyeRemoval(__instance, 0.3f))
            {
                Plugin.Log.LogInfo("[FastVisor] Blocked eye removal (30% immunity).");
                return false;
            }
        }
        catch { }
        return true;
    }
}
