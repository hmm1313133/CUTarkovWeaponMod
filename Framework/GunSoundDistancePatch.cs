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
/// 此 Prefix 拦截 Sound.Play(AudioClip,...)，只把“带世界坐标”的枪械/世界音效改为 3D。
/// UI 音效通常使用 Vector2.zero，强制 3D 会变很小，因此保留 twoDimensional=true。
/// UnityWebRequest 加载的 AudioClip 不保证有文件名，因此不能靠 clip.name 判断；
/// 判断依据是 pos != Vector2.zero（枪械开火/拉栓等使用 transform.position）。
/// </summary>
[HarmonyPatch(typeof(Sound), nameof(Sound.Play), new[] {
    typeof(AudioClip), typeof(Vector2), typeof(bool), typeof(bool),
    typeof(Transform), typeof(float), typeof(float), typeof(bool), typeof(bool)
})]
public static class GunSoundDistancePatch
{
    [HarmonyPrefix]
    public static void Prefix(AudioClip clip, Vector2 pos, ref bool twoDimensional)
    {
        if (clip == null) return;
        // 只处理带世界坐标的音效；UI/医疗面板/配方界面使用 Vector2.zero，保持 2D。
        if (pos != Vector2.zero)
            twoDimensional = false;
    }
}
