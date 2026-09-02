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
            gun.TryRack();
            Plugin.Log.LogInfo($"[GunHotkeys] Rack toggled (racked={gun.racked}).");
        }
        // 卸下弹匣
        else if (!checkingMag && Input.GetKeyDown(GunHotkeysKeybindPatch.UnloadMagKey))
        {
            gun.UnloadMag();
            Plugin.Log.LogInfo($"[GunHotkeys] UnloadMag triggered (hasMag={gun.hasMag}).");
        }
        // 切换保险
        else if (!checkingMag && Input.GetKeyDown(GunHotkeysKeybindPatch.SafetyKey))
        {
            gun.ToggleSafety();
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
