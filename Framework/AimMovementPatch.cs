using System.Collections.Generic;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 瞄准时降低移动/跳跃速度 30%。
/// 通过 Harmony Postfix 在 Body.FixedUpdate 结束后直接缩放 rb.velocity。
/// 为什么不 patch legSpeedMult getter：该 getter 被 JIT 内联进 FixedUpdate 的热路径
/// （移动限速/跳跃/climb 至少 8 处直接内联调用），Postfix 永远无法拦截，故失效。
/// FixedUpdate 由 Unity 每物理帧调用，不会被内联，是可靠的注入点。
/// </summary>
public static class AimMovementPatch
{
    private const float AimSpeedMult = 0.7f;   // 瞄准时速度 ×0.7（减少 30%）

    // 记录每 body 上一帧速度 y，用于判断"本帧刚起跳"
    private static readonly Dictionary<Body, float> LastVelY = new();

    // 缓存瞄准键位（KeyBinds.GetBind 是字典查找，每物理帧对每个 body 调用开销大）
    private static KeyCode _cachedAimBind;
    private static bool _cachedAimBindSet;
    private static int _lastVelYPruneCounter;

    private static bool IsAiming(Body body)
    {
        if (body == null) return false;
        var handItem = body.GetItem(body.handSlot);
        if (handItem == null || handItem.GetComponent<GunScript>() == null) return false;
        if (!_cachedAimBindSet) { _cachedAimBind = KeyBinds.GetBind("iteminteract"); _cachedAimBindSet = true; }
        return Input.GetKey(_cachedAimBind);
    }

    // 手动注册（见 Plugin.cs）
    public static void FixedUpdatePostfix(Body __instance)
    {
        if (__instance == null) return;
        // 仅本地玩家需要瞄准移动减速；NPC/其他玩家直接跳过，避免每物理帧无谓开销
        if (PlayerCamera.main?.body != __instance) return;
        if (!IsAiming(__instance)) return;

        var rb = __instance.rb;
        if (rb == null) return;

        // 水平移动减速
        rb.velocity = new Vector2(rb.velocity.x * AimSpeedMult, rb.velocity.y);

        // 跳跃减速：本帧刚起跳（上一帧 velocity.y 接近 0 / 仍 grounded，本帧变为正）
        if (LastVelY.TryGetValue(__instance, out float prevY))
        {
            bool wasGroundedOrSlow = prevY < 0.5f;
            if (wasGroundedOrSlow && rb.velocity.y > 1f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * AimSpeedMult);
            }
        }

        LastVelY[__instance] = rb.velocity.y;

        // 定期清理已销毁 Body 的旧条目（避免长期游戏积累）
        if ((++_lastVelYPruneCounter % 300) == 0 && LastVelY.Count > 0)
        {
            var dead = new List<Body>();
            foreach (var kv in LastVelY)
                if (kv.Key == null) dead.Add(kv.Key);
            foreach (var key in dead) LastVelY.Remove(key);
        }
    }

    // 清理断开的 body 引用，避免内存泄漏
    public static void CleanupAbsent(Dictionary<Body, float> liveBodies)
    {
        // 可选：由外部调用，这里保留简单实现
    }
}
