using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.Networking;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 近战武器共享音效缓存 — 在插件启动时预加载 knife_swing*.wav，
/// 两把近战武器（冰镐 + M-2）共用，避免攻击时实时加载导致的卡顿。
/// </summary>
public static class MeleeSoundCache
{
    private static AudioClip?[]? _cachedClips;
    private static readonly List<AudioClip> _validClips = new();

    public static void Preload()
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var knifeDir = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "knife");

            for (int i = 1; i <= 2; i++)
            {
                var path = Path.Combine(knifeDir, $"knife_swing{i}.wav");
                if (!File.Exists(path)) continue;

                using var uwr = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.WAV);
                uwr.SendWebRequest();
                while (!uwr.isDone) { }
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(uwr);
                    clip.name = $"knife_swing{i}";
                    _validClips.Add(clip);
                    Plugin.Log.LogInfo($"[MeleeSoundCache] Preloaded '{clip.name}'");
                }
            }

            _cachedClips = _validClips.Count > 0 ? _validClips.ToArray() : null;
            if (_cachedClips != null)
                Plugin.Log.LogInfo($"[MeleeSoundCache] {_cachedClips.Length} swing sound(s) ready.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[MeleeSoundCache] Preload failed: {ex.Message}");
        }
    }

    /// <summary>随机获取一个挥砍音效，直接从内存中取，无 I/O。</summary>
    public static AudioClip? GetRandomSwing()
    {
        if (_cachedClips == null || _cachedClips.Length == 0) return null;
        return _cachedClips[UnityEngine.Random.Range(0, _cachedClips.Length)];
    }
}
