using System;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 枪械快捷键控制器（类似 GunHotkeys.dll）：
/// - 拉栓（TryRack）
/// - 卸下弹匣（UnloadMag）
/// - 切换保险（ToggleSafety）
/// - 检查弹匣弹药余量（显示提示）
///
/// 键位从游戏设置界面读取（GunHotkeysKeybindPatch 注册）。
/// 由 Plugin.Update 每帧驱动。
/// </summary>
public static class GunHotkeysController
{
 
    /// <summary>每帧检测快捷键输入。</summary>
    public static void Tick()
    {
        var body = PlayerCamera.main?.body;
        if (body == null) return;

        // 改枪面板打开时不响应枪械快捷键
        if (GunsmithPanel.IsOpen) return;

        var gunItem = body.GetItem(body.handSlot);
        if (gunItem == null) return;
        var gun = gunItem.GetComponent<GunScript>();
        if (gun == null) return;

        // 检查弹匣进行中：抑制拉栓/卸弹匣/保险（检查弹药本身仍响应）
        bool checkingMag = Hhs1ZoomUiPatch.IsCheckingMag;

        // 拉栓
        if (!checkingMag && Input.GetKeyDown(GunHotkeysKeybindPatch.RackKey))
        {
            // 使用 PlayerCamera.GunRack 而不是 GunScript.TryRack：
            // KrokMP 对 PlayerCamera.GunRack 打了网络补丁，客户端拉栓会通知服务器，
            // 并设置 round-trip 忽略窗口，避免本地预测状态被服务器的旧状态立刻覆盖。
            PlayerCamera.main.GunRack();
            Plugin.Log.LogInfo($"[GunHotkeys] Rack toggled (racked={gun.racked}).");
        }
        // 卸下弹匣
        else if (!checkingMag && Input.GetKeyDown(GunHotkeysKeybindPatch.UnloadMagKey))
        {
            // 同样经过 PlayerCamera 入口，KrokMP 的 GunEjectMag 多人补丁才能向服务器上报。
            PlayerCamera.main.GunEjectMag();
            Plugin.Log.LogInfo($"[GunHotkeys] UnloadMag triggered (hasMag={gun.hasMag}).");
        }
        // 切换保险
        else if (!checkingMag && Input.GetKeyDown(GunHotkeysKeybindPatch.SafetyKey))
        {
            // KrokMP 的 GunToggleSafety 多人补丁挂在 PlayerCamera.GunToggleSafety。
            PlayerCamera.main.GunToggleSafety();
            Plugin.Log.LogInfo($"[GunHotkeys] Safety toggled (safe={gun.safe}).");
        }
        // 检查弹匣弹药余量
        else if (Input.GetKeyDown(GunHotkeysKeybindPatch.CheckAmmoKey))
        {
            // 复用 Hhs1ZoomUiPatch 的检查弹匣逻辑（绿色像素字体，4 秒渐隐，音效，快照）
            Hhs1ZoomUiPatch.OnCheckMagClicked(PlayerCamera.main);
        }
    }

}
