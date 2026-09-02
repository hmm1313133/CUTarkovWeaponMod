using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 替换原版 GunScript.JamChance() 的卡壳概率公式。
///
/// 三段式分段线性映射（用户指定）：
///
///   条件 100% → 80%：0%   → 0.5%   (高耐久区，几乎不卡壳)
///   条件  80% → 60%：0.5% → 2%     (较高耐久区，缓升)
///   条件  60% → 50%：2%   → 10%    (中耐久区，陡升)
///   条件  50% → 20%：10%  → 30%    (低耐久区)
///   条件  20% →  0%：30%  → 60%    (极低耐久区，假设值，可按需调整)
///
/// 湿身不影响卡壳率。
/// </summary>
[HarmonyPatch(typeof(GunScript), nameof(GunScript.JamChance))]
public static class JamChancePatch
{
    /// <summary>缓存 it 字段访问器</summary>
    private static System.Reflection.FieldInfo? _itField;
    private static int _logCounter;

    [HarmonyPrefix]
    public static bool Prefix(GunScript __instance, ref float __result)
    {
        try
        {
            // 关键：只在 Fire() 开火时机判定卡壳。
            // 原版 JamChance() 被 3 个时机调用（Fire 开火 / Update 抛壳 / Update 上膛），
            // 每发子弹触发 3 次独立判定，导致实际卡壳率 ≈ 1-(1-p)^3，远高于单次概率。
            // 抛壳/上膛时机（InFire=false）返回 0，使每发子弹恰好判定一次。
            if (!SuppressorSystem.FireEffectsPatch.InFire)
            {
                __result = 0f;
                return false;
            }

            _itField ??= AccessTools.Field(typeof(GunScript), "it");
            var item = _itField?.GetValue(__instance) as Item;
            if (item == null)
            {
                // 诊断：反射失败或 it 未初始化（每 60 次打印一次）
                if (++_logCounter % 60 == 0)
                    Plugin.Log.LogWarning($"[JamChancePatch] it field FAILED (_itField={_itField != null}), returning 0 (no jam).");
                __result = 0f;
                return false;
            }

            float condition = item.condition;
            // 鲁棒归一化：兼容 condition 为 0~1 或 0~100 两种情况。
            // 若 condition > 1，说明是 0~100（百分制），除以 100 归一化为 0~1。
            // （实测 60 耐久几乎全卡，怀疑 condition 实为 0~100，阈值 0.8 被 60 恒真触发导致 100% 卡壳）
            if (condition > 1f)
                condition *= 0.01f;
            // 诊断日志：确认补丁生效及 condition 范围（每 120 次打印一次，避免刷屏）
            if (++_logCounter % 120 == 0)
                Plugin.Log.LogInfo($"[JamChancePatch] it field OK, rawCondition={item.condition}, normalized={condition}, firingMode={__instance.firingMode}");
            float jamChance;

            // 分段线性映射
            if (condition > 0.8f)
            {
                // 100% → 80%: jam 0% → 0.005 (0.5%)
                jamChance = Body.Remap(condition, 0.8f, 1f, 0f, 0.005f);
            }
            else if (condition > 0.6f)
            {
                // 80% → 60%: jam 0.005 (0.5%) → 0.02 (2%)
                jamChance = Body.Remap(condition, 0.6f, 0.8f, 0.005f, 0.02f);
            }
            else if (condition > 0.5f)
            {
                // 60% → 50%: jam 0.02 (2%) → 0.1 (10%)
                jamChance = Body.Remap(condition, 0.5f, 0.6f, 0.02f, 0.1f);
            }
            else if (condition > 0.2f)
            {
                // 50% → 20%: jam 0.1 (10%) → 0.3 (30%)
                jamChance = Body.Remap(condition, 0.2f, 0.5f, 0.1f, 0.3f);
            }
            else
            {
                // 20% → 0%: jam 0.3 (30%) → 0.6 (60%)（用户未指定，假设延续上升趋势）
                jamChance = Body.Remap(condition, 0f, 0.2f, 0.3f, 0.6f);
            }

            // 上限截断
            if (jamChance > 1f)
            {
                jamChance = 1f;
            }

            __result = jamChance;
            return false; // 跳过原版方法
        }
        catch
        {
            return true; // 出错时回退到原版逻辑
        }
    }
}
