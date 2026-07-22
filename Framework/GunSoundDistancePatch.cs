using System;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 将自定义枪械音效（开火/拉栓/闭栓/装弹匣/卸弹匣）从 2D 改为 3D，
/// 使其在多人游戏中有距离衰减。
/// 
/// 原版 Sound.Play 对 fireSound/customRack/customUnrack 使用 twoDimensional=true
/// （spatialBlend=0，无距离衰减），多人游戏中远程玩家的枪声无论距离都是满音量。
/// 
/// 此 Prefix 拦截 Sound.Play(AudioClip,...)，对自定义音效设置 twoDimensional=false。
/// 判断依据：自定义音效文件名含下划线（如 akm_fire、vss_open），
/// 原版音效名为单词（如 guntrigger、gunrack、gunloadmag）。
/// </summary>
[HarmonyPatch(typeof(Sound), nameof(Sound.Play), new[] {
    typeof(AudioClip), typeof(Vector2), typeof(bool), typeof(bool),
    typeof(Transform), typeof(float), typeof(float), typeof(bool), typeof(bool)
})]
public static class GunSoundDistancePatch
{
    [HarmonyPrefix]
    public static void Prefix(ref AudioClip clip, ref bool twoDimensional)
    {
        if (clip == null || string.IsNullOrEmpty(clip.name)) return;
        // 自定义音效名含下划线（akm_fire, vss_open 等），原版不含
        if (clip.name.Contains("_"))
        {
            twoDimensional = false; // 3D，有距离衰减
        }
    }
}
