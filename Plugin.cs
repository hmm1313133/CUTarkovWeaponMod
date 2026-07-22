using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using CUTarkovMedicalMod.Framework;
using CUTarkovWeaponMod.Framework;
using CUTarkovWeaponMod.Integration;

namespace CUTarkovWeaponMod;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("com.yourname.cu.tarkovmedicalmod", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string ModGuid = "com.yourname.cu.tarkovweaponmod";
    public const string ModName = "Casualties: Unknown - Tarkov-Style Weapon Mod";
    public const string ModVersion = "1.1.1.0";

    internal static ManualLogSource Log = null!;
    internal static WeaponCUCoreLibMode IntegrationMode = null!;

    private WeaponUpdateNotifier _updateNotifier = null!;

    private void Awake()
    {
        Log = Logger;

        // Register weapon translations with the medical mod's I18n system
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                          ?? Paths.PluginPath;
        var langDir = Path.Combine(assemblyDir, "Lang");
        I18n.RegisterExternalLangDir(langDir);

        // Diagnostic: verify file deployment
        var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "knife", "redrebel.png");
        Log.LogInfo($"[WeaponMod] Assembly dir: {assemblyDir}");
        Log.LogInfo($"[WeaponMod] Lang dir: {langDir}, exists={Directory.Exists(langDir)}");
        if (Directory.Exists(langDir))
        {
            var langFiles = Directory.GetFiles(langDir, "*.json");
            Log.LogInfo($"[WeaponMod] Lang files: {string.Join(", ", langFiles)}");
        }
        Log.LogInfo($"[WeaponMod] redrebel.png: {iconPath}, exists={File.Exists(iconPath)}");

        // Verify I18n loaded redrebel keys
        var testTr = I18n.Tr("redrebel.name");
        Log.LogInfo($"[WeaponMod] I18n.Tr(\"redrebel.name\") = \"{testTr}\" (raw key means translations not loaded)");

        // Register weapon items in the medical mod's console spawn system
        WeaponItemRegistration.Register();

        Harmony harmony = new Harmony(ModGuid);
        try
        {
            harmony.PatchAll();

            // 手动注册 ScopeZoom patch (HandleVariables is private)
            try
            {
                var hvMethod = AccessTools.Method(typeof(PlayerCamera), "HandleVariables");
                if (hvMethod != null)
                {
                    var postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.ScopeZoomPatch),
                        nameof(CUTarkovWeaponMod.Framework.ScopeZoomPatch.PostfixHandleVariables));
                    harmony.Patch(hvMethod, postfix: postfix);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ScopeZoom] Manual patch failed: {ex}");
            }

            // 手动注册双槽位 patch (GetWearableBySlotID may not be public)
            try
            {
                var gwsMethod = AccessTools.Method(typeof(Body), "GetWearableBySlotID");
                if (gwsMethod != null)
                {
                    // MBSS
                    var mbssPostfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.MBSSItemSystem.MBSSDualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.MBSSItemSystem.MBSSDualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: mbssPostfix);

                    // TV-115
                    var tv115Postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.TV115ItemSystem.TV115DualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.TV115ItemSystem.TV115DualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: tv115Postfix);

                    // TV-110
                    var tv110Postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.TV110ItemSystem.TV110DualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.TV110ItemSystem.TV110DualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: tv110Postfix);

                    // SP PC V2
                    var sppcv2Postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.SPPCV2ItemSystem.SPPCV2DualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.SPPCV2ItemSystem.SPPCV2DualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: sppcv2Postfix);

                    // MK4A 突击型
                    var mk4aPostfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.MK4AItemSystem.MK4ADualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.MK4AItemSystem.MK4ADualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: mk4aPostfix);

                    // Siege-R
                    var siegerPostfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.SiegeRItemSystem.SiegeRDualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.SiegeRItemSystem.SiegeRDualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: siegerPostfix);

                    // 6B5-16
                    var sixB516Postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.SixB516ItemSystem.SixB516DualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.SixB516ItemSystem.SixB516DualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: sixB516Postfix);

                    // TT SK
                    var ttskPostfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.TTSKItemSystem.TTSKDualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.TTSKItemSystem.TTSKDualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: ttskPostfix);

                    // AVS TE
                    var avstePostfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.AVSTEItemSystem.AVSTEDualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.AVSTEItemSystem.AVSTEDualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: avstePostfix);

                    // LV-119
                    var lv119Postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.LV119ItemSystem.LV119DualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.LV119ItemSystem.LV119DualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: lv119Postfix);

                    // 6B45
                    var sixB45Postfix = new HarmonyMethod(typeof(CUTarkovWeaponMod.Framework.SixB45ItemSystem.SixB45DualSlotPatch),
                        nameof(CUTarkovWeaponMod.Framework.SixB45ItemSystem.SixB45DualSlotPatch.Postfix));
                    harmony.Patch(gwsMethod, postfix: sixB45Postfix);

                    Log.LogInfo("[Armor] Patched Body.GetWearableBySlotID for dual-slot lock.");
                }
                else
                {
                    Log.LogWarning("[Armor] GetWearableBySlotID method not found, dual-slot lock disabled.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Armor] Dual-slot patch failed: {ex}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"PatchAll() threw: {ex}");
        }

        // 手动注册 WearWearable patch (放在 PatchAll try-catch 之后，确保即使 PatchAll 失败也能执行)
        try
        {
            var wearMethod = AccessTools.Method(typeof(Body), "WearWearable");
            if (wearMethod != null)
            {
                var prefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.NightVisionController), "WearWearablePrefix");
                harmony.Patch(wearMethod, prefix: prefix);
                Log.LogInfo("[NVG] Manually patched Body.WearWearable for helmet check.");
            }
            else
            {
                Log.LogWarning("[NVG] WearWearable method not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[NVG] WearWearable patch failed: {ex}");
        }

        // Initialize CUCoreLib integration mode.
        // CUCoreLib is a hard dependency, medical mod loads first (also hard dep on CUCoreLib).
        try
        {
            IntegrationMode = new WeaponCUCoreLibMode();
            IntegrationMode.Initialize(new Harmony(ModGuid));
        }
        catch (Exception ex)
        {
            Log.LogError($"WeaponCUCoreLibMode.Initialize() threw: {ex}");
        }

        Log.LogInfo($"{ModName} loaded.");

        // Initialize Night Vision Controller (static, driven by Plugin.Update)
        try
        {
            NightVisionController.Init();
            Log.LogInfo("[NVG] NightVisionController initialized.");
        }
        catch (Exception ex)
        {
            Log.LogError($"[NVG] Controller init failed: {ex}");
        }

        // 创建更新提醒实例（由 Plugin 的 Update/OnGUI 驱动）
        _updateNotifier = new WeaponUpdateNotifier();
    }

    private void Update()
    {
        NightVisionController.Tick();
        _updateNotifier?.Tick();
        IDEAItemSystem.TickDecay();
        BankRobberItemSystem.TickDecay();
        Type56ItemSystem.TickDecay();
        WTChestRigItemSystem.TickDecay();
        LBCRItemSystem.TickDecay();
        CommandoItemSystem.TickDecay();
        UmkaItemSystem.TickDecay();
        BlackRockItemSystem.TickDecay();
        PACAItemSystem.TickDecay();
        MFUNItemSystem.TickDecay();
        DRDItemSystem.TickDecay();
        LK3FItemSystem.TickDecay();
        ReadyPackItemSystem.TickDecay();
        PartizanItemSystem.TickDecay();
        DayPackItemSystem.TickDecay();
        BerkutItemSystem.TickDecay();
        ScavPackItemSystem.TickDecay();
        MysteryRanch2DayItemSystem.TickDecay();
        PilgrimItemSystem.TickDecay();
        SsoAttack2ItemSystem.TickDecay();
        SH118ItemSystem.TickDecay();
        LBT2670ItemSystem.TickDecay();
    }

    private void OnGUI()
    {
        _updateNotifier?.OnGUI();
        NightVisionController.OnGUI();
    }
}
