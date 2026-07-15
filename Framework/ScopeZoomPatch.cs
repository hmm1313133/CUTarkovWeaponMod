using System;
using HarmonyLib;
using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// AXMC 瞄准镜视野扩展补丁。
/// 补丁挂载在 HandleVariables 之后（zoomTime 递减之后、HandleCameraPosition 之前），
/// 这样同一帧内 HandleCameraPosition 即可使用更新后的 zoomTime。
///
/// 复用原版 autozoomgoggles 的 zoomTime 机制：
/// HandleCameraPosition 中 zoomTime > 0 时鼠标偏移倍率为 5x（扩展视野范围）。
/// 与 autozoomgoggles 互斥：已装备护目镜时不叠加 AXMC 视野扩展。
/// </summary>
public static class ScopeZoomPatch
{
    /// <summary>zoom 时的视野扩展时间（与 autozoomgoggles 一致）。</summary>
    private const float ScopeZoomTimeValue = 0.5f;

    private static bool _scopeZoomActive;

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.HandleVariables))]
    [HarmonyPostfix]
    public static void PostfixHandleVariables(PlayerCamera __instance)
    {
        UpdateScopeZoom(__instance);
    }

    /// <summary>
    /// 每帧检查并应用瞄准镜视野扩展。
    /// 条件：玩家有意识、主手持有 AXMC、未装备自动变焦护目镜。
    /// </summary>
    private static void UpdateScopeZoom(PlayerCamera? cam)
    {
        if (cam == null || cam.body == null) return;

        var body = cam.body;
        if (!body.conscious) return;

        try
        {
            // 已装备自动变焦护目镜时不叠加
            if (body.HasWearable("autozoomgoggles"))
            {
                if (_scopeZoomActive)
                {
                    _scopeZoomActive = false;
                    Plugin.Log.LogInfo("[ScopeZoom] Scope zoom deactivated (autozoomgoggles equipped).");
                }
                return;
            }

            var handItem = body.GetItem(body.handSlot);
            bool shouldZoom = handItem != null && handItem.id == AXMCItemSystem.ItemKey;

            if (shouldZoom)
            {
                cam.zoomTime = ScopeZoomTimeValue;
                if (!_scopeZoomActive)
                {
                    _scopeZoomActive = true;
                    Plugin.Log.LogInfo("[ScopeZoom] Scope zoom activated (AXMC).");
                }
            }
            else if (_scopeZoomActive)
            {
                _scopeZoomActive = false;
                Plugin.Log.LogInfo("[ScopeZoom] Scope zoom deactivated (AXMC unequipped).");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ScopeZoom] UpdateScopeZoom failed: {ex.Message}");
        }
    }
}
