using System;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 战术耳机控制器：电池耗电 + 听力保护 + 音频调整 + 声波炮效果减轻。
/// 支持 TEP-300 和 ProFlex DX5 两种耳机，参数不同。
///
/// 听力保护采用全局帧监控：每帧对比 hearingLoss 变化，减少正向增量。
/// 音频调整在 PlayerCamera.LateUpdate Postfix 中执行，确保在 HandleAmbientSound 之后。
/// </summary>
public static class Tep300Controller
{
    private const float DrainRate = 1f / 1200f; // 20 minutes

    private static Item? _cached;
    private static int _cacheFrame = -1;
    private static float _lastHearingLoss;
    private static float _lastGlobalMuteTime;

    // ============ 耳机参数 ============

    /// <summary>耳机参数配置</summary>
    private readonly struct HeadsetConfig
    {
        public readonly float DmgReductionPowered;
        public readonly float DmgReductionUnpowered;
        public readonly float VolOffsetPowered;   // dB
        public readonly float VolOffsetUnpowered;  // dB
        public readonly float MaxDistPowered;      // multiplier
        public readonly float MaxDistUnpowered;    // multiplier
        public readonly float CutoffPowered;       // multiplier (0-1, lower = more noise reduction)
        public readonly float BonusAbberCapLand;
        public readonly float BonusAbberCapWater;
        public readonly float MuteCapLand;
        public readonly float MuteCapWater;
        public readonly float MuteCapBlocked;

        public HeadsetConfig(
            float dmgP, float dmgU,
            float volP, float volU,
            float distP, float distU,
            float cutoffP,
            float abberLand, float abberWater,
            float muteLand, float muteWater, float muteBlocked)
        {
            DmgReductionPowered = dmgP;
            DmgReductionUnpowered = dmgU;
            VolOffsetPowered = volP;
            VolOffsetUnpowered = volU;
            MaxDistPowered = distP;
            MaxDistUnpowered = distU;
            CutoffPowered = cutoffP;
            BonusAbberCapLand = abberLand;
            BonusAbberCapWater = abberWater;
            MuteCapLand = muteLand;
            MuteCapWater = muteWater;
            MuteCapBlocked = muteBlocked;
        }
    }

    // TEP-300 参数
    private static readonly HeadsetConfig Tep300Config = new(
        dmgP: 0.60f, dmgU: 0.50f,
        volP: 5f, volU: -13f,
        distP: 1.2f, distU: 0.4f,
        cutoffP: 0.4f,
        abberLand: -10f, abberWater: -40f,
        muteLand: 3f, muteWater: 30f, muteBlocked: 0f);

    // ProFlex DX5 参数
    private static readonly HeadsetConfig ProFlexConfig = new(
        dmgP: 0.70f, dmgU: 0.60f,
        volP: 5.5f, volU: -14f,
        distP: 1.4f, distU: 0.25f,
        cutoffP: 0.3f,
        abberLand: -7f, abberWater: -40f,
        muteLand: 2f, muteWater: 25f, muteBlocked: 0f);

    /// <summary>获取当前装备耳机的参数，未装备返回 null</summary>
    private static HeadsetConfig? GetConfig()
    {
        var item = GetEquipped();
        if (item == null) return null;
        if (item.id == ProFlexItemSystem.ItemKey) return ProFlexConfig;
        return Tep300Config; // TEP-300 默认
    }

    /// <summary>
    /// 获取本地玩家当前装备的耳机（带帧缓存）。
    /// </summary>
    public static Item? GetEquipped()
    {
        if (_cacheFrame == Time.frameCount) return _cached;

        _cacheFrame = Time.frameCount;
        _cached = null;

        try
        {
            var body = PlayerCamera.main?.body;
            if (body == null) return null;

            var item = body.GetWearableBySlotID("ear");
            if (item != null &&
                (item.id == Tep300ItemSystem.ItemKey || item.id == ProFlexItemSystem.ItemKey))
                _cached = item;
        }
        catch { }

        return _cached;
    }

    /// <summary>
    /// 每帧更新（从 Plugin.Update 调用）：
    /// 1. 全局监控 hearingLoss 变化，减少所有来源的听力损伤增量
    /// 2. 有电时消耗电池
    /// 3. 监控 globalMuteTime 突增，限制声波炮静音时间
    /// </summary>
    public static void Tick()
    {
        var body = PlayerCamera.main?.body;
        if (body == null) return;

        var headset = GetEquipped();
        var config = GetConfig();

        if (headset == null || config == null)
        {
            _lastHearingLoss = body.hearingLoss;
            _lastGlobalMuteTime = PlayerCamera.main?.globalMuteTime ?? 0f;
            return;
        }

        // --- 全局听力损伤减少 ---
        bool powered = headset.condition > 0f;
        float reduction = powered ? config.Value.DmgReductionPowered : config.Value.DmgReductionUnpowered;

        float current = body.hearingLoss;
        float diff = current - _lastHearingLoss;
        if (diff > 0f)
        {
            body.hearingLoss = _lastHearingLoss + diff * (1f - reduction);
        }

        // --- 电池耗电 ---
        if (powered)
        {
            headset.condition -= DrainRate * Time.deltaTime;
            if (headset.condition < 0f) headset.condition = 0f;
        }

        // --- globalMuteTime 上限（声波炮静音持续时间）---
        var pc = PlayerCamera.main;
        if (pc != null)
        {
            float muteTime = pc.globalMuteTime;
            float muteIncrease = muteTime - _lastGlobalMuteTime;
            if (muteIncrease > 1f)
            {
                if (body.inWater)
                    pc.globalMuteTime = config.Value.MuteCapWater;
                else if (muteIncrease > 10f)
                    pc.globalMuteTime = config.Value.MuteCapLand;
                else
                    pc.globalMuteTime = config.Value.MuteCapBlocked;
            }
            _lastGlobalMuteTime = pc.globalMuteTime;
        }

        _lastHearingLoss = body.hearingLoss;
    }

    // ============ Harmony Patches ============

    /// <summary>
    /// PlayerCamera.LateUpdate Postfix：在 HandleAmbientSound 之后调整音频。
    /// 有电：Volume 偏移 +dB，SoundCutoff 降噪
    /// 无电：Volume 偏移 -dB
    /// 不管有无电量：bonusAbber 上限
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.LateUpdate))]
    public static class AudioAdjustPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            var headset = GetEquipped();
            var config = GetConfig();
            if (headset == null || config == null) return;

            var mixer = WorldGeneration.world?.soundMixerGroup?.audioMixer;
            if (mixer == null) return;

            bool powered = headset.condition > 0f;

            // 调整音量（dB 偏移）
            if (mixer.GetFloat("Volume", out float vol))
            {
                float offset = powered ? config.Value.VolOffsetPowered : config.Value.VolOffsetUnpowered;
                mixer.SetFloat("Volume", vol + offset);
            }

            // 有电时降低 SoundCutoff 模拟降噪
            if (powered && mixer.GetFloat("SoundCutoff", out float cutoff))
            {
                float t = Mathf.InverseLerp(50f, 22000f, cutoff);
                t *= config.Value.CutoffPowered;
                mixer.SetFloat("SoundCutoff", Mathf.Lerp(50f, 22000f, t));
            }

            // 鱼眼效应减轻（不管有无电量）
            var pc = PlayerCamera.main;
            if (pc != null)
            {
                var body = pc.body;
                if (body != null)
                {
                    float cap = body.inWater ? config.Value.BonusAbberCapWater : config.Value.BonusAbberCapLand;
                    if (pc.bonusAbber < cap)
                        pc.bonusAbber = cap;
                }
            }
        }
    }

    /// <summary>
    /// Sound.Play Postfix：调整 AudioSource.maxDistance（听距）。
    /// </summary>
    [HarmonyPatch(typeof(Sound), nameof(Sound.Play), typeof(AudioClip), typeof(Vector2),
        typeof(bool), typeof(bool), typeof(Transform), typeof(float), typeof(float),
        typeof(bool), typeof(bool))]
    public static class SoundPlayPatch
    {
        [HarmonyPostfix]
        public static void Postfix(AudioSource __result)
        {
            if (__result == null) return;
            var headset = GetEquipped();
            var config = GetConfig();
            if (headset == null || config == null) return;

            if (headset.condition > 0f)
                __result.maxDistance *= config.Value.MaxDistPowered;
            else
                __result.maxDistance *= config.Value.MaxDistUnpowered;
        }
    }
}
