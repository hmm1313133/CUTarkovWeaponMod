using System;
using System.Linq;
using CUCoreLib.Data;
using CUCoreLib.Registries;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 设置键位注册辅助（参考医疗模组 NightmareSettings 的"方式2"）。
///
/// 问题：仅 ModOptionsRegistry.Register 可能不把 Setting 加入 Settings.settings，
/// 导致游戏 SaveSettings() 不序列化它，重启后改键丢失。
///
/// 方式2：注册后直接手动 Add SettingKeybind 到 Settings.settings，
/// 确保游戏 SaveSettings() 会序列化它（与医疗模组噩梦开关同样的成功机制）。
/// </summary>
public static class ModOptionKeybindHelper
{
    /// <summary>
    /// 注册键位选项（方式1：ModOptionsRegistry）并确保加入 Settings.settings（方式2）。
    /// </summary>
    public static void RegisterKeybind(string id, string label, string desc, KeyCode defaultKey, Action<KeyCode> onChanged)
    {
        // 方式1：ModOptionsRegistry.Register（负责进 RegisteredOptions，用于设置菜单显示）
        var option = ModOptionDefinition.Keybind(id, label, desc, Setting.SettingCategory.Input, defaultKey, v => onChanged?.Invoke(v));
        ModOptionsRegistry.Register(option);

        // Settings 未就绪时等待（调用方在 EnsureLoaded 后调用，通常已就绪）
        if (Settings.settings == null)
        {
            Plugin.Log.LogInfo($"[KeybindHelper] {id}: Settings.settings null, skip manual add.");
            return;
        }

        // 方式2：直接手动 Add SettingKeybind 到 Settings.settings（绕开 MergeIntoLoadedSettings 时机问题）
        bool exists = Settings.settings.Any(s => s != null && s.name == id);
        if (!exists)
        {
            var setting = new SettingKeybind
            {
                name = id,
                value = defaultKey,
                category = Setting.SettingCategory.Input,
            };
            setting.apply = () => onChanged?.Invoke(setting.value);
            Settings.settings.Add(setting);
            setting.Apply();
            Plugin.Log.LogInfo($"[KeybindHelper] {id}: manually added SettingKeybind (default={defaultKey}).");
        }
        else
        {
            Plugin.Log.LogInfo($"[KeybindHelper] {id}: already exists in Settings.settings.");
        }
    }
}
