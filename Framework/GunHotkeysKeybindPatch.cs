using System;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 枪械快捷键（拉栓/卸弹匣/切保险/检查弹药）注册到游戏设置界面的 Input 分类。
/// 使用 CUCoreLib 的 ModOptionsRegistry（自动处理设置菜单注册、本地化、存档/网络同步）。
/// 复用 NvgKeybindPatch 的延迟注册模式。
/// </summary>
public static class GunHotkeysKeybindPatch
{
    public const string RackSettingName = "cutarkovweapon.gunhotkey_rack";
    public const string UnloadMagSettingName = "cutarkovweapon.gunhotkey_unloadmag";
    public const string SafetySettingName = "cutarkovweapon.gunhotkey_safety";
    public const string CheckAmmoSettingName = "cutarkovweapon.gunhotkey_checkammo";

    /// <summary>当前拉栓键位，每次访问实时从游戏设置读取（默认 R）。</summary>
    public static KeyCode RackKey => GetKey(RackSettingName, KeyCode.R);

    /// <summary>当前卸弹匣键位（默认 X）。</summary>
    public static KeyCode UnloadMagKey => GetKey(UnloadMagSettingName, KeyCode.X);

    /// <summary>当前切保险键位（默认 F）。</summary>
    public static KeyCode SafetyKey => GetKey(SafetySettingName, KeyCode.F);

    /// <summary>当前检查弹药键位（默认 C）。</summary>
    public static KeyCode CheckAmmoKey => GetKey(CheckAmmoSettingName, KeyCode.C);

    private static KeyCode GetKey(string name, KeyCode fallback)
    {
        if (Settings.settings == null) return fallback;
        foreach (var s in Settings.settings)
        {
            if (s is SettingKeybind kb && kb.name == name)
                return kb.value;
        }
        return fallback; // 设置项尚未注册时回退到默认
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
                RackSettingName,
                "Rack Gun",
                "Rack/charge the held gun.",
                KeyCode.R,
                value => { });
            ModOptionKeybindHelper.RegisterKeybind(
                UnloadMagSettingName,
                "Unload Magazine",
                "Unload the magazine from the held gun.",
                KeyCode.X,
                value => { });
            ModOptionKeybindHelper.RegisterKeybind(
                SafetySettingName,
                "Toggle Safety",
                "Toggle the safety of the held gun.",
                KeyCode.F,
                value => { });
            ModOptionKeybindHelper.RegisterKeybind(
                CheckAmmoSettingName,
                "Check Ammo",
                "Check the ammo count of the held gun.",
                KeyCode.C,
                value => { });

            _registered = true;
            ModOptionLocaleInjector.Inject(); // 注入设置选项中英文翻译
            Plugin.Log.LogInfo("[GunHotkeys] Registered gun hotkeys via CUCoreLib (R=rack, X=unload, F=safety, C=check).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[GunHotkeys] Registration failed: {ex.Message}");
        }
    }
}
