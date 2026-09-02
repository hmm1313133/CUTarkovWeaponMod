using System;
using System.Collections.Generic;
using CUCoreLib.Registries;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 设置选项中英文翻译注入器。
/// CUCoreLib 的 ApplyActiveLocaleOverlay 只在 Locale.LoadLanguage 时加载插件根目录语言文件，
/// 而我们的 keybind 是延迟注册（Update 轮询），注册时翻译可能未合并到 Locale.currentLang.other。
/// 此注入器在 keybind 注册后主动把中英文翻译写入 Locale.currentLang.other，
/// 确保设置界面显示正确语言。
/// </summary>
public static class ModOptionLocaleInjector
{
    // 所有设置选项的翻译：key -> (中文, 英文)
    private static readonly Dictionary<string, (string zh, string en)> Translations = new(StringComparer.Ordinal)
    {
        { "cutarkovweapon.nvgkey", ("夜视仪开关", "Night Vision Toggle") },
        { "cutarkovweapon.nvgkeydsc", ("切换夜视仪的开关状态。", "Toggle the night vision device on/off.") },
        { "cutarkovweapon.tacticaldevicekey", ("战术设备开关", "Tactical Device Toggle") },
        { "cutarkovweapon.tacticaldevicekeydsc", ("切换战术设备（手电/激光）的开关状态。", "Toggle the tactical device (light/laser) on/off.") },
        { "cutarkovweapon.gunsmithkey", ("改枪面板", "Gunsmith Panel") },
        { "cutarkovweapon.gunsmithkeydsc", ("打开/关闭改枪面板。", "Open/close the gunsmith panel.") },
        { "cutarkovweapon.gunhotkey_rack", ("拉栓", "Rack Gun") },
        { "cutarkovweapon.gunhotkey_rackdsc", ("拉栓/上膛当前手持枪械。", "Rack/charge the held gun.") },
        { "cutarkovweapon.gunhotkey_unloadmag", ("卸下弹匣", "Unload Magazine") },
        { "cutarkovweapon.gunhotkey_unloadmagdsc", ("从当前手持枪械卸下弹匣。", "Unload the magazine from the held gun.") },
        { "cutarkovweapon.gunhotkey_safety", ("切换保险", "Toggle Safety") },
        { "cutarkovweapon.gunhotkey_safetydsc", ("切换当前手持枪械的保险状态。", "Toggle the safety of the held gun.") },
        { "cutarkovweapon.gunhotkey_checkammo", ("检查弹药", "Check Ammo") },
        { "cutarkovweapon.gunhotkey_checkammodsc", ("检查当前手持枪械的弹药余量。", "Check the ammo count of the held gun.") },
    };

    private static bool _injected;

    /// <summary>
    /// 把中英文翻译注入 Locale.currentLang.other（幂等，只执行一次）。
    /// 在 keybind 注册成功后调用。
    /// </summary>
    public static void Inject()
    {
        if (_injected) return;

        try
        {
            // Locale 未就绪时不设置 _injected，让后续重试（注册提前到 Awake 时 Locale 可能未加载）
            if (Locale.currentLang == null) return;

            bool isChinese = IsChineseLanguage(Locale.currentLangName);
            foreach (var kv in Translations)
            {
                var text = isChinese ? kv.Value.zh : kv.Value.en;
                // 写入 Locale.currentLang.other，设置界面 Locale.GetOther("gameset"+id) 会命中
                Locale.currentLang.other[kv.Key] = text;
                // 同时注册到 CUCoreLib CustomLocales（Locale.GetString 兜底）
                LocaleRegistry.Register(LocaleRegistry.LocaleCategory.Option, kv.Key, text);
            }
            _injected = true;

            Plugin.Log.LogInfo($"[OptionLocale] Injected {Translations.Count} option translations (lang={Locale.currentLangName}, chinese={isChinese}).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[OptionLocale] Inject failed: {ex.Message}");
        }
    }

    /// <summary>判断当前语言是否为中文（含维基中文等中文变体）。</summary>
    public static bool IsChineseLanguage(string langName)
    {
        if (string.IsNullOrWhiteSpace(langName)) return false;
        var name = langName.Trim();
        if (name.Equals("EN", StringComparison.OrdinalIgnoreCase)) return false;
        // 中文语言名通常含"中文"/"Chinese"/"zh"，或非 EN 的常见中文代码
        return name.IndexOf("中文", StringComparison.Ordinal) >= 0
               || name.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0
               || name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
    }
}
