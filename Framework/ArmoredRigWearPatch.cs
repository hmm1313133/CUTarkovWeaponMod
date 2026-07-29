using System;
using System.Collections.Generic;
using CUTarkovMedicalMod.Framework;
using HarmonyLib;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 反向双槽位锁定：当 bandolier 已穿戴弹挂时，阻止穿戴弹挂甲（outertorso）。
/// 正向锁定由各弹挂甲的 DualSlotPatch（GetWearableBySlotID Postfix）处理：
///   弹挂甲穿在 outertorso → GetWearableBySlotID("bandolier") 返回弹挂甲 → 阻止穿弹挂。
/// 此类处理反向：弹挂穿在 bandolier → WearWearable(弹挂甲) 被阻止。
/// 弹挂甲与纯护甲都用 outertorso，游戏原版同槽位互斥，无需额外处理。
/// </summary>
public static class ArmoredRigWearPatch
{
    /// <summary>所有弹挂甲的 ItemKey 集合。</summary>
    public static readonly HashSet<string> ArmoredRigIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "6b45", "6b516", "avste", "lv119", "mbss",
        "mk4a", "sieger", "sppcv2", "ttsk", "tv110", "tv115",
    };

    /// <summary>
    /// Body.WearWearable Prefix：穿弹挂甲时，如果 bandolier 已被弹挂占用，阻止穿戴。
    /// </summary>
    public static bool Prefix(Body __instance, Item item)
    {
        if (item == null) return true;

        // 多人模式：客户端跳过弹挂锁定检查（KrokMP 同步 WearWearable）
        if (KrokMpHelper.IsMultiplayer && !KrokMpHelper.IsHost) return true;

        // 只拦截弹挂甲
        if (!ArmoredRigIds.Contains(item.id))
            return true;

        // 检查 bandolier 是否已被占用
        var bandolierItem = __instance.GetWearableBySlotID("bandolier");
        if (bandolierItem == null)
            return true; // bandolier 空闲，允许穿戴

        // 如果 bandolier 上的物品本身就是弹挂甲（正向 DualSlotPatch 从 outertorso 投射过来的），
        // 说明是在替换弹挂甲，允许操作。
        if (ArmoredRigIds.Contains(bandolierItem.id))
            return true;

        // bandolier 上有弹挂，阻止穿戴弹挂甲
        Plugin.Log.LogInfo(
            $"[ArmoredRig] Cannot wear '{item.id}': bandolier occupied by '{bandolierItem.id}'.");
        return false;
    }
}
