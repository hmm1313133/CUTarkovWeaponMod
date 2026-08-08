using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 头部受伤免疫辅助工具。
///
/// 原版多个伤害路径会在同一事件内连续调用两次 RemoveEye()
/// （如断肢、NeuralBooster 副作用——一次摘掉双眼）。
/// 若每次调用独立掷骰，保住双眼需要 免疫² 的概率，
/// 导致体感上失明减免远低于介绍数值。
///
/// TryBlockEyeRemoval 将同一事件（0.5 秒内同一身体）的多次调用视为一次判定：
/// 第一次被免疫阻止后，紧随其后的第二次调用自动同样阻止。
///
/// 非战斗来源（如药物副作用导致的失明）不享受免疫：
/// 由 NeuralBoosterImmunitySuppression 在使用副作用药物时挂起 SuppressEyeImmunity。
/// </summary>
public static class HeadInjuryProtectionHelper
{
    /// <summary>药物副作用等非战斗场景下，挂起眼部失明免疫。</summary>
    public static bool SuppressEyeImmunity { get; set; }

    private static Body? _lastProtectedEyeBody;
    private static float _lastProtectedEyeTime;

    /// <summary>
    /// 判定是否阻止一次眼部失明。
    /// 若同一身体在 0.5 秒内再次调用（同事件双眼移除），沿用上一次的阻止结果。
    /// 非战斗来源（SuppressEyeImmunity 为 true）时不阻止。
    /// </summary>
    public static bool TryBlockEyeRemoval(Body body, float immunity)
    {
        if (body == null) return false;

        // 药物副作用等非战斗来源：不享受免疫
        if (SuppressEyeImmunity) return false;

        // 同事件内的第二次调用：沿用首次阻止结果
        if (_lastProtectedEyeBody == body && Time.time - _lastProtectedEyeTime < 0.5f)
            return true;

        if (Random.value < immunity)
        {
            _lastProtectedEyeBody = body;
            _lastProtectedEyeTime = Time.time;
            return true;
        }

        // 未阻止：清除记录，避免污染后续判定
        _lastProtectedEyeBody = null;
        return false;
    }
}
