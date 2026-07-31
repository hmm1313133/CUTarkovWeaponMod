using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 将夜视仪开关键位注册到游戏设置界面的 Input 分类。
/// 由于 Settings.EnsureLoaded 可能在 mod 加载前就被调用，
/// 采用延迟注册：在 EnsureLoaded Postfix + Update 轮询双重保障，
/// 一旦 Settings.settings 就绪且尚未注册则立即注入。
/// </summary>
public static class NvgKeybindPatch
{
    public const string SettingName = "nvgkey";
    public static KeyCode CurrentKey = KeyCode.N;

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
    }

    /// <summary>
    /// Plugin.Update 每帧调用，一旦 Settings 就绪且未注册则立即注入。
    /// 这是主要触发路径，因为 EnsureLoaded 通常在 mod 加载前就已执行。
    /// </summary>
    public static void Tick()
    {
        if (!_registered)
            TryRegister();
    }

    private static void TryRegister()
    {
        if (_registered) return;
        if (Settings.settings == null) return;

        // 避免重复添加
        foreach (var s in Settings.settings)
        {
            if (s.name == SettingName) return;
        }

        // Step 1: 创建 SettingKeybind 并注入到设置列表
        var keybind = new SettingKeybind
        {
            name = SettingName,
            category = Setting.SettingCategory.Input,
            value = CurrentKey,
        };

        Settings.settings.Add(keybind);

        // Step 2: 注册 Locale 标签（关键！没有这个设置界面无法显示文本）
        // LocaleRegistry.Register(int type, string key, string text)
        // type: 0=Item, 1=Building, 2=Moodle, 3=Other, 4=Log, 5=Command, 6=Option
        try
        {
            var localeType = System.Type.GetType("CUCoreLib.Registries.LocaleRegistry, CUCoreLib");
            if (localeType != null)
            {
                var registerMethod = localeType.GetMethod("Register",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(int), typeof(string), typeof(string) }, null);
                if (registerMethod != null)
                {
                    registerMethod.Invoke(null, new object[] { 6, SettingName, "NVG Toggle" });
                    Plugin.Log.LogInfo($"[NvgKeybind] Registered locale Option/{SettingName} = \"NVG Toggle\".");
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[NvgKeybind] Locale registration failed: {ex.Message}");
        }

        // Step 3: 刷新设置 UI
        try
        {
            var extenderType = System.Type.GetType("CUCoreLib.Helpers.SettingsMenuCategoryExtender, CUCoreLib");
            if (extenderType != null)
            {
                var refreshMethod = extenderType.GetMethod("RefreshLiveMenu",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                refreshMethod?.Invoke(null, null);
                Plugin.Log.LogInfo($"[NvgKeybind] Refreshed settings menu UI.");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[NvgKeybind] Settings menu refresh failed: {ex.Message}");
        }

        _registered = true;
        Plugin.Log.LogInfo($"[NvgKeybind] Registered NVG toggle key in settings (name={SettingName}, default={CurrentKey}).");
    }

    /// <summary>从设置中读取当前键位</summary>
    public static void RefreshKey()
    {
        foreach (var s in Settings.settings)
        {
            if (s is SettingKeybind kb && kb.name == SettingName)
            {
                CurrentKey = kb.value;
                return;
            }
        }
    }
}
