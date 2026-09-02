using System;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 枪械 prefab 继承物清理。
///
/// 背景：游戏原版 pistol prefab 自带一个「激光」子物体（纯 SpriteRenderer 红色长条，
/// 挂在枪口偏下，无任何脚本逻辑）。模组手枪（Glock17/USP/Deagle）克隆自 pistol prefab，
/// 因此会把激光子物体一并继承——表现为"只要是手枪就有红色激光"。
///
/// 处理：移除主 SpriteRenderer 之外的所有子物体 SpriteRenderer（即激光装饰），
/// 同时保留 muzzleParticle 等 ParticleSystem 子物体（非 SpriteRenderer 不受影响）。
/// 每个被移除的子物体会记录日志，便于后续按需调整。
/// </summary>
public static class GunPrefabCleanup
{
    /// <summary>疑似激光/瞄准装饰的子物体名字特征词（不区分大小写）。</summary>
    private static readonly string[] LaserNameKeywords =
    {
        "laser", "beam", "aim", "sight", "red", "pointer", "led", "light",
    };

    /// <summary>
    /// 移除从 pistol 等 prefab 继承的激光子物体。
    /// 判定：子物体是 SpriteRenderer 且（名字含激光特征词 或 其 sprite 与主 sprite 不同）。
    /// 主 sprite 已被替换为模组手枪贴图，激光子物体用的是原版 pistol 贴图 → sprite 必然不同，
    /// 因此即使名字未知也能被识别。保留与主 sprite 相同的子物体（如有）以防误删。
    /// </summary>
    public static void RemoveInheritedLaser(Item item)
    {
        if (item == null) return;
        try
        {
            var mainSr = item.GetComponent<SpriteRenderer>();
            Sprite? mainSprite = mainSr != null ? mainSr.sprite : null;

            var children = item.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t == null || t == item.transform) continue;
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                string name = t.name.ToLowerInvariant();
                bool nameMatches = false;
                foreach (var kw in LaserNameKeywords)
                {
                    if (name.Contains(kw)) { nameMatches = true; break; }
                }

                // sprite 与主 sprite 不同 → 装饰（激光）
                bool spriteDiffers = mainSprite != null && sr.sprite != null && sr.sprite != mainSprite;

                if (nameMatches || spriteDiffers)
                {
                    Plugin.Log.LogInfo($"[GunCleanup] Removing inherited laser child '{t.name}' on '{item.id}' (sprite={(sr.sprite != null ? sr.sprite.name : "null")}).");
                    // 配置流程非 Unity 渲染回调，可安全立即销毁
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
                else
                {
                    Plugin.Log.LogInfo($"[GunCleanup] Keeping child '{t.name}' on '{item.id}' (sprite={(sr.sprite != null ? sr.sprite.name : "null")}).");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[GunCleanup] RemoveInheritedLaser failed on '{item.id}': {ex.Message}");
        }
    }
}