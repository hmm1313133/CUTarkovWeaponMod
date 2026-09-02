using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CUTarkovMedicalMod.Framework;
using Newtonsoft.Json.Linq;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 武器模组本地化助手。
/// 优先使用本模组 Lang 目录内的语言文件（EN.json / zh_CN.json），
/// 避免 I18n 在英文语言码未命中时回退到 zh_CN 导致英文界面出现中文。
/// 若语言文件缺失，再回退 I18n.Tr 与中文兜底。
/// </summary>
public static class WModLoc
{
    private static bool _fallbackLoaded;
    private static bool _inTr;
    private static readonly Dictionary<string, string> _en = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> _zh = new(StringComparer.Ordinal);

    public static string Tr(string key, string zhFallback)
    {
        if (string.IsNullOrEmpty(key)) return zhFallback;
        // 防止 I18n.Tr -> RefreshAll -> WModLoc.Tr 的重入递归
        if (_inTr) return zhFallback;
        _inTr = true;
        try
        {
            // 先触发 I18n 语言检测
            string i18nResult = I18n.Tr(key);

            EnsureFallbackLoaded();

            bool chinese = ModOptionLocaleInjector.IsChineseLanguage(I18n.CurrentLanguage);
            if (!chinese && _en.TryGetValue(key, out var en))
                return en;
            if (chinese && _zh.TryGetValue(key, out var zh))
                return zh;

            // 本模组语言文件未覆盖时，才信任 I18n 的结果
            if (!string.IsNullOrEmpty(i18nResult) && i18nResult != key)
                return i18nResult;

            // 最后回退：英文语言优先用 EN 兜底（若 I18n.CurrentLanguage 检测失败）
            if (!chinese && _en.TryGetValue(key, out en))
                return en;
            if (_zh.TryGetValue(key, out zh))
                return zh;
        }
        catch { }
        finally
        {
            _inTr = false;
        }
        return zhFallback;
    }

    private static void EnsureFallbackLoaded()
    {
        if (_fallbackLoaded) return;
        _fallbackLoaded = true;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyDir)) return;
            var langDir = Path.Combine(assemblyDir, "Lang");
            LoadJsonInto(Path.Combine(langDir, "EN.json"), _en);
            LoadJsonInto(Path.Combine(langDir, "zh_CN.json"), _zh);
        }
        catch { }
    }

    private static void LoadJsonInto(string path, Dictionary<string, string> output)
    {
        if (!File.Exists(path)) return;
        var obj = JObject.Parse(File.ReadAllText(path));
        foreach (var prop in obj.Properties())
        {
            if (prop.Value.Type == JTokenType.String)
                output[prop.Name] = (string)prop.Value;
        }
    }
}
