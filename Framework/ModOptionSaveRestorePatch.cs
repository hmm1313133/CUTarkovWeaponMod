using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 设置选项持久化修复。
///
/// 根因：CUCoreLib 只 patch 了 Settings.DefaultSettings（Postfix 追加选项），
/// 没有 patch Settings.LoadSettings。我们的 keybind 是延迟注册（Update 轮询），
/// 启动时 Settings.LoadSettings() 调用 DefaultSettings() 时选项还没注册，
/// settings.json 里保存的选项值在加载时找不到对应 setting，被丢弃。
/// 之后注册追加选项（用默认值），导致每次重启改键重置。
///
/// 修复：patch Settings.EnsureLoaded Postfix（此时 LoadSettings 已完成），
/// 先触发所有 keybind 注册（确保选项在 Settings.settings 中），
/// 再从 settings.json 恢复保存的选项值。
/// </summary>
public static class ModOptionSaveRestorePatch
{
    // 需要持久化的选项 ID 集合
    private static readonly HashSet<string> PersistedOptionIds = new HashSet<string>(System.StringComparer.Ordinal)
    {
        "cutarkovweapon.nvgkey",
        "cutarkovweapon.tacticaldevicekey",
        "cutarkovweapon.gunsmithkey",
        "cutarkovweapon.gunhotkey_rack",
        "cutarkovweapon.gunhotkey_unloadmag",
        "cutarkovweapon.gunhotkey_safety",
        "cutarkovweapon.gunhotkey_checkammo",
    };

    private static bool _restored;

    /// <summary>
    /// 在 Settings.EnsureLoaded 之后调用（由 NvgKeybindPatch.EnsureLoaded_Postfix 触发，
    /// 该 Postfix 确定执行）。注册所有 keybind 选项后从 settings.json 恢复保存的值。
    /// </summary>
    public static void RestoreAfterLoad()
    {
        if (_restored) return;
        _restored = true;

        try
        {
            Plugin.Log.LogInfo("[OptionSave] RestoreAfterLoad fired.");

            // 确保所有 keybind 选项已注册（Settings.settings 中包含它们）
            NvgKeybindPatch.Tick();
            TacticalDeviceKeybindPatch.Tick();
            GunsmithKeybindPatch.Tick();
            GunHotkeysKeybindPatch.Tick();

            if (Settings.settings == null) return;

            string path = Application.persistentDataPath + "/settings.json";
            if (!File.Exists(path)) return;

            // 读取 settings.json 中保存的选项值
            var saved = JsonConvert.DeserializeObject<List<SettingSaveData>>(File.ReadAllText(path));
            if (saved == null) return;

            foreach (var data in saved)
            {
                if (data == null || !PersistedOptionIds.Contains(data.name)) continue;

                // 在 Settings.settings 中找到对应 setting（已注册）
                var setting = Settings.settings.Find(s => s != null && s.name == data.name);
                if (setting == null) continue;

                // 恢复保存的值
                setting.SetValue(data.value);
                setting.Apply();
            }

            Plugin.Log.LogInfo("[OptionSave] Restored saved option values from settings.json.");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[OptionSave] Restore failed: {ex.Message}");
        }
    }
}
