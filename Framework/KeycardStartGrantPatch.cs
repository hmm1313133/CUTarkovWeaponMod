using System;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 每局开局给玩家直接发放三张钥匙卡（武器室 / Blue Area / Red Area 各一张）。
///
/// 挂钩点：WorldGeneration.FinishWorldGeneration（WorldGeneration.GenerateWorld 协程的最后一步，
/// 此时玩家已被 WorldPlacePlayer 放置、世界已生成完毕）——医疗模组的世界掉落注入用的是同一个钩子。
///
/// 发放条件（与原版起始补给 WorldPlacePlayer 内的判定保持一致）：
/// - 仅本局第一层：world.totalTraveled == 0 且 biomeOverride == None 且 debugStartDepth == 0
///   （下潜换层会重新生成世界，此判定为假，不会重复发放；教程/覆盖场景也不发）
/// - 仅新开一局：SaveSystem.loadedRun == false（PreRunScript.StartRun 新开一局时置 false，
///   LoadRun 读取存档继续游戏时置 true，此时不补发）
/// - 同一局只发一次：_grantedThisRun 标记（WorldGeneration.Start 时重置）
///
/// 多人模式：每位客户端只给自己本地玩家（PlayerCamera.main.body）发放，因此"每位玩家"都会拿到。
/// 物品生成走 Utils.Create（CUCoreLib 已打补丁支持自定义物品）+ Body.AutoPickUpItem，
/// 与医疗模组 QolMpSaveCompat 的本地玩家物品创建方式一致。
/// </summary>
[HarmonyPatch]
public static class KeycardStartGrantPatch
{
    /// <summary>开局发放的钥匙卡（各一张）。</summary>
    private static readonly string[] StartingKeycards =
    {
        WeaponRoomKeycardItemSystem.ItemKey,   // 武器室房卡
        BlueAreaKeycardItemSystem.ItemKey,     // Blue Area 钥匙卡
        RedAreaKeycardItemSystem.ItemKey,      // Red Area 钥匙卡
    };

    /// <summary>本局是否已经发放过（WorldGeneration.Start 时重置）。</summary>
    private static bool _grantedThisRun;

    /// <summary>每次世界场景加载（= 开始/载入一局）重置发放标记。</summary>
    [HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.Start))]
    [HarmonyPrefix]
    private static void ResetGrantFlag()
    {
        _grantedThisRun = false;
    }

    [HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.FinishWorldGeneration))]
    [HarmonyPostfix]
    private static void Postfix()
    {
        try
        {
            if (_grantedThisRun) return;

            var world = WorldGeneration.world;
            if (world == null) return;

            // 仅本局第一层（与原版起始补给同一判定）
            if (world.totalTraveled > 0
                || world.biomeOverride != WorldGeneration.OverrideSceneType.None
                || world.debugStartDepth != 0) return;

            // 读取存档继续游戏时不补发，只有新开一局才发
            if (SaveSystem.loadedRun) return;

            // 只给本地玩家自己的身体（多人模式下每位玩家各自发放自己那份）
            var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            if (body == null) return;

            if (Grant(body, world) > 0)
                _grantedThisRun = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[KeycardGrant] Failed: {ex}");
        }
    }

    private static int Grant(Body body, WorldGeneration world)
    {
        int given = 0;
        foreach (var key in StartingKeycards)
        {
            try
            {
                var go = Utils.Create(key, body.transform.position, 0f);
                var item = go != null ? go.GetComponent<Item>() : null;
                if (item == null)
                {
                    if (go != null) UnityEngine.Object.Destroy(go);
                    Plugin.Log.LogWarning($"[KeycardGrant] Utils.Create returned no Item for '{key}'.");
                    continue;
                }

                body.AutoPickUpItem(item);
                given++;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[KeycardGrant] Failed to grant '{key}': {ex.Message}");
            }
        }

        Plugin.Log.LogInfo(
            $"[KeycardGrant] New run start: granted {given}/{StartingKeycards.Length} keycard(s) to the local player " +
            $"(totalTraveled={world.totalTraveled}, loadedRun={SaveSystem.loadedRun}).");
        return given;
    }
}
