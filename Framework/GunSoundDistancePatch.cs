using System;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 将自定义枪械音效（开火/拉栓/闭栓）从 2D 改为 3D，
/// 使其在多人游戏中有距离衰减，同时不影响原版炮台/世界音效。
///
/// 原版 GunScript 对 fireSound/customRack/customUnrack 使用 twoDimensional=true
/// （spatialBlend=0，无距离衰减），多人游戏中远程玩家的枪声无论距离都是满音量。
///
/// 这里通过记录“当前正在执行 GunScript.Update / Fire”的枪，
/// 只把这些枪自己的 AudioClip 改为 3D；不会误伤字符串加载的炮台等世界音效。
/// </summary>
public static class GunSoundDistancePatch
{
    private static GunScript _currentGun;

    [HarmonyPatch(typeof(GunScript), nameof(GunScript.Update))]
    [HarmonyPrefix]
    private static void GunUpdatePrefix(GunScript __instance)
    {
        _currentGun = __instance;
    }

    [HarmonyPatch(typeof(GunScript), nameof(GunScript.Update))]
    [HarmonyPostfix]
    private static void GunUpdatePostfix()
    {
        _currentGun = null;
    }

    [HarmonyPatch(typeof(GunScript), nameof(GunScript.Fire))]
    [HarmonyPrefix]
    private static void GunFirePrefix(GunScript __instance)
    {
        _currentGun = __instance;
    }

    [HarmonyPatch(typeof(GunScript), nameof(GunScript.Fire))]
    [HarmonyPostfix]
    private static void GunFirePostfix()
    {
        _currentGun = null;
    }

    [HarmonyPatch(typeof(Sound), nameof(Sound.Play), new[] {
        typeof(AudioClip), typeof(Vector2), typeof(bool), typeof(bool),
        typeof(Transform), typeof(float), typeof(float), typeof(bool), typeof(bool)
    })]
    [HarmonyPrefix]
    private static void SoundPlayPrefix(AudioClip clip, Vector2 pos, ref bool twoDimensional)
    {
        if (clip == null || _currentGun == null) return;

        bool isGunSound =
            clip == _currentGun.fireSound ||
            clip == _currentGun.customRack ||
            clip == _currentGun.customUnrack;

        if (isGunSound && pos != Vector2.zero)
            twoDimensional = false; // 3D，有距离衰减
    }
}
