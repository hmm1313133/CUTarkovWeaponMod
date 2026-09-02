using System;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 瞄准镜倍率切换键位注册到游戏设置界面的 Input 分类。
/// 默认 O，可改键。
/// </summary>
public static class ScopeZoomKeybindPatch
{
    public const string SettingName = "cutarkovweapon.scopezoomkey";

    /// <summary>当前倍率切换键位，每次访问实时从游戏设置读取</summary>
    public static KeyCode CurrentKey
    {
        get
        {
            if (Settings.settings == null) return KeyCode.O;
            foreach (var s in Settings.settings)
            {
                if (s is SettingKeybind kb && kb.name == SettingName)
                    return kb.value;
            }
            return KeyCode.O; // 设置项尚未注册时回退到默认 O
        }
    }

    private static bool _registered;

    [HarmonyPatch(typeof(Settings), nameof(Settings.EnsureLoaded))]
    [HarmonyPostfix]
    public static void EnsureLoaded_Postfix()
    {
        TryRegister();
    }

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
                "Scope Zoom",
                "Switch scope magnification mode.",
                KeyCode.O,
                value => { /* 值由 CUCoreLib 写入 Settings，CurrentKey 自动读取 */ });

            _registered = true;
            ModOptionLocaleInjector.Inject(); // 注入设置选项中英文翻译
            Plugin.Log.LogInfo($"[ScopeZoomKeybind] Registered scope zoom key (name={SettingName}, default=O).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ScopeZoomKeybind] Registration failed: {ex.Message}");
        }
    }
}
