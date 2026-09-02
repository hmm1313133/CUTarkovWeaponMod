using System;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 将夜视仪开关键位注册到游戏设置界面的 Input 分类。
/// 使用 CUCoreLib 的 ModOptionsRegistry（自动处理设置菜单注册、本地化、存档/网络同步）。
/// 由于 Settings.EnsureLoaded 可能在 mod 加载前就被调用，
/// 采用延迟注册：在 EnsureLoaded Postfix + Update 轮询双重保障。
/// </summary>
public static class NvgKeybindPatch
{
    public const string SettingName = "cutarkovweapon.nvgkey";

    /// <summary>当前 NVG 切换键位，每次访问实时从游戏设置读取</summary>
    public static KeyCode CurrentKey
    {
        get
        {
            if (Settings.settings == null) return KeyCode.N;
            foreach (var s in Settings.settings)
            {
                if (s is SettingKeybind kb && kb.name == SettingName)
                    return kb.value;
            }
            return KeyCode.N; // 设置项尚未注册时回退到默认 N
        }
    }

    private static bool _registered;

    /// <summary>
    /// Harmony Postfix on Settings.EnsureLoaded。
    /// 如果 EnsureLoaded 在我们之后被调用（比如首次打开设置菜单），这里捕获。
    /// </summary>
    [HarmonyPatch(typeof(Settings), nameof(Settings.EnsureLoaded))]
    [HarmonyPostfix]
    public static void EnsureLoaded_Postfix()
    {
        TryRegister();
        // 注册后恢复保存的选项值（确保延迟注册的 keybind 也能持久化）
        ModOptionSaveRestorePatch.RestoreAfterLoad();
    }

    /// <summary>
    /// Plugin.Update 每帧调用，一旦注册成功则停止。
    /// </summary>
    public static void Tick()
    {
        if (!_registered)
            TryRegister();
    }

    private static void TryRegister()
    {
        if (_registered) return;

        try
        {
            // 方式1（ModOptionsRegistry 设置菜单显示）+ 方式2（直接加入 Settings.settings 确保持久化）
            ModOptionKeybindHelper.RegisterKeybind(
                SettingName,
                "NVG Toggle",
                "Toggle night vision goggles.",
                KeyCode.N,
                value => { /* 值由 CUCoreLib 写入 Settings，CurrentKey 自动读取 */ });

            _registered = true;
            ModOptionLocaleInjector.Inject(); // 注入设置选项中英文翻译
            Plugin.Log.LogInfo($"[NvgKeybind] Registered NVG toggle key (name={SettingName}, default=N).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[NvgKeybind] Registration failed: {ex.Message}");
        }
    }
}
