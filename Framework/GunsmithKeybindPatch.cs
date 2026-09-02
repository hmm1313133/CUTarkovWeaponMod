using System;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 将改枪面板开关键位注册到游戏设置界面的 Input 分类。
/// 使用 CUCoreLib 的 ModOptionsRegistry（自动处理设置菜单注册、本地化、存档/网络同步）。
/// 复用 NvgKeybindPatch 的延迟注册模式。
/// </summary>
public static class GunsmithKeybindPatch
{
    public const string SettingName = "cutarkovweapon.gunsmithkey";

    /// <summary>当前改枪面板键位，每次访问实时从游戏设置读取</summary>
    public static KeyCode CurrentKey
    {
        get
        {
            if (Settings.settings == null) return KeyCode.G;
            foreach (var s in Settings.settings)
            {
                if (s is SettingKeybind kb && kb.name == SettingName)
                    return kb.value;
            }
            return KeyCode.G; // 设置项尚未注册时回退到默认 G
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
                "Gunsmith Panel",
                "Open/close the gunsmith modification panel.",
                KeyCode.G,
                value => { /* 值由 CUCoreLib 写入 Settings，CurrentKey 自动读取 */ });

            _registered = true;
            ModOptionLocaleInjector.Inject(); // 注入设置选项中英文翻译
            Plugin.Log.LogInfo($"[GunsmithKeybind] Registered gunsmith panel key (name={SettingName}, default=G).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[GunsmithKeybind] Registration failed: {ex.Message}");
        }
    }
}
