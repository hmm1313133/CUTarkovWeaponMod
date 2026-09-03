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
    public const string ModVersion = "2.0.0.0";

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
                var nvgPrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.NightVisionController), "WearWearablePrefix");
                harmony.Patch(wearMethod, prefix: nvgPrefix);
                Log.LogInfo("[NVG] Manually patched Body.WearWearable for helmet check.");

                // 反向双槽位锁定：穿弹挂后阻止穿弹挂甲
                var rigPrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.ArmoredRigWearPatch), "Prefix");
                harmony.Patch(wearMethod, prefix: rigPrefix);
                Log.LogInfo("[ArmoredRig] Manually patched Body.WearWearable for reverse dual-slot lock.");
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

        // 手动注册瞄准移动减速 patch（Body.FixedUpdate Postfix）
        // 注意：不能 patch legSpeedMult getter——它被 JIT 内联进 FixedUpdate 热路径，Postfix 永不执行。
        try
        {
            var fixedUpdate = AccessTools.Method(typeof(Body), "FixedUpdate");
            if (fixedUpdate != null)
            {
                var aimMovePostfix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.AimMovementPatch), "FixedUpdatePostfix");
                harmony.Patch(fixedUpdate, postfix: aimMovePostfix);
                Log.LogInfo("[AimMove] Manually patched Body.FixedUpdate postfix.");
            }
            else
            {
                Log.LogWarning("[AimMove] Body.FixedUpdate not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[AimMove] FixedUpdate patch failed: {ex}");
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

        // 注册 CUCoreLib 结构背景砖纯白补丁（大理石背景用于实验室风格地堡）
        try
        {
            var bgSpawn = AccessTools.Method(
                typeof(CUCoreLib.Registries.StructureRegistry), "SpawnBackgroundTile");
            if (bgSpawn != null)
            {
                var whiteBgPrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.LabWhiteBackgroundPatch), "Prefix");
                harmony.Patch(bgSpawn, prefix: whiteBgPrefix);
                Log.LogInfo("[WeaponCacheBunker] Patched StructureRegistry.SpawnBackgroundTile for lab-white backgrounds.");
            }
            else
            {
                Log.LogWarning("[WeaponCacheBunker] StructureRegistry.SpawnBackgroundTile not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[WeaponCacheBunker] Lab background patch failed: {ex}");
        }

        // 注册 CUCoreLib 地堡墙体替换补丁（H -> 防弹加强钢 tile 36）
        try
        {
            var placeStructure = AccessTools.Method(
                typeof(CUCoreLib.Registries.StructureRegistry), "PlaceStructure");
            if (placeStructure != null)
            {
                var wallTilePrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.BunkerWallTilePatch), "Prefix");
                harmony.Patch(placeStructure, prefix: wallTilePrefix);
                Log.LogInfo("[WeaponCacheBunker] Patched StructureRegistry.PlaceStructure for bulletproof-steel walls.");
            }
            else
            {
                Log.LogWarning("[WeaponCacheBunker] StructureRegistry.PlaceStructure not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[WeaponCacheBunker] Wall tile patch failed: {ex}");
        }
        // 注册防弹加强钢 tile 减伤补丁（爆炸/挖掘/近战伤害只承受 25%）
        try
        {
            var damageBlock = AccessTools.Method(
                typeof(WorldGeneration), "DamageBlock",
                new[] { typeof(Vector2Int), typeof(float), typeof(bool), typeof(bool), typeof(bool) });
            if (damageBlock != null)
            {
                var tileDamagePrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.BunkerTileDamagePatch), "Prefix");
                harmony.Patch(damageBlock, prefix: tileDamagePrefix);
                Log.LogInfo("[WeaponCacheBunker] Patched WorldGeneration.DamageBlock for bulletproof-steel tile damage reduction.");
            }
            else
            {
                Log.LogWarning("[WeaponCacheBunker] WorldGeneration.DamageBlock(Vector2Int,float,bool,bool,bool) not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[WeaponCacheBunker] Tile damage patch failed: {ex}");
        }
        // 注册防弹加强钢 tile 爆炸保护补丁（爆炸 GenerateBlockCircle 恢复 tile 36）
        try
        {
            var generateCircle = AccessTools.Method(
                typeof(WorldGeneration), "GenerateBlockCircle");
            if (generateCircle != null)
            {
                var explosionPrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.BunkerExplosionPatch), "Prefix");
                var explosionPostfix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.BunkerExplosionPatch), "Postfix");
                harmony.Patch(generateCircle, prefix: explosionPrefix, postfix: explosionPostfix);
                Log.LogInfo("[WeaponCacheBunker] Patched WorldGeneration.GenerateBlockCircle to protect bulletproof-steel walls from explosions.");
            }
            else
            {
                Log.LogWarning("[WeaponCacheBunker] WorldGeneration.GenerateBlockCircle not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[WeaponCacheBunker] Explosion protection patch failed: {ex}");
        }

        // 注册武器配件地堡 + 武器物资箱建筑（CUCoreLib Buildings/Structures）
        try
        {
            WeaponCacheBunker.Register();
        }
        catch (Exception ex)
        {
            Log.LogError($"[WeaponCacheBunker] Register failed: {ex}");
        }
        // 注册 Blue Area 地堡（医疗区）
        try
        {
            BlueAreaBunker.Register();
        }
        catch (Exception ex)
        {
            Log.LogError($"[BlueAreaBunker] Register failed: {ex}");
        }

        // 注册 Red Area 地堡（高级装备/弹药区）
        try
        {
            RedAreaBunker.Register();
        }
        catch (Exception ex)
        {
            Log.LogError($"[RedAreaBunker] Register failed: {ex}");
        }

        // 注册 Red Area 背景补丁（红底白字长条 + 干净/脏混合背景）
        try
        {
            var redBgSpawn = AccessTools.Method(
                typeof(CUCoreLib.Registries.StructureRegistry), "SpawnBackgroundTile");
            if (redBgSpawn != null)
            {
                var redBgPrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.RedAreaBackgroundPatch), "Prefix");
                harmony.Patch(redBgSpawn, prefix: redBgPrefix);
                Log.LogInfo("[RedAreaBunker] Patched StructureRegistry.SpawnBackgroundTile for red area backgrounds.");
            }
            else
            {
                Log.LogWarning("[RedAreaBunker] StructureRegistry.SpawnBackgroundTile not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[RedAreaBunker] Red area background patch failed: {ex}");
        }

        // 注册 Blue Area 背景补丁（蓝底白字长条 + 干净/脏混合背景）
        try
        {
            var blueBgSpawn = AccessTools.Method(
                typeof(CUCoreLib.Registries.StructureRegistry), "SpawnBackgroundTile");
            if (blueBgSpawn != null)
            {
                var blueBgPrefix = new HarmonyMethod(
                    typeof(CUTarkovWeaponMod.Framework.BlueAreaBackgroundPatch), "Prefix");
                harmony.Patch(blueBgSpawn, prefix: blueBgPrefix);
                Log.LogInfo("[BlueAreaBunker] Patched StructureRegistry.SpawnBackgroundTile for blue area backgrounds.");
            }
            else
            {
                Log.LogWarning("[BlueAreaBunker] StructureRegistry.SpawnBackgroundTile not found.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[BlueAreaBunker] Blue area background patch failed: {ex}");
        }

        // 立即注册所有 keybind 选项（确保在 Settings.LoadSettings 之前完成，
        // 这样 DefaultSettings() 包含它们，settings.json 里保存的值能恢复）
        try
        {
            NvgKeybindPatch.Tick();
            TacticalDeviceKeybindPatch.Tick();
            GunsmithKeybindPatch.Tick();
            GunHotkeysKeybindPatch.Tick();
            Log.LogInfo("[WeaponMod] Keybinds registered in Awake.");
        }
        catch (Exception ex)
        {
            Log.LogError($"[WeaponMod] Keybind registration in Awake failed: {ex}");
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
        NvgKeybindPatch.Tick();   // 延迟注册 NVG 键位（等待 Settings 就绪）
        GunsmithKeybindPatch.Tick(); // 延迟注册改枪面板键位
        TacticalDeviceKeybindPatch.Tick(); // 延迟注册战术设备键位
        GunHotkeysKeybindPatch.Tick(); // 延迟注册枪械快捷键键位
        ScopeZoomKeybindPatch.Tick(); // 延迟注册倍率切换键位
        ModOptionLocaleInjector.Inject(); // 持续注入设置选项中英文翻译（Locale 就绪后生效）
        HandleGunsmithPanelInput();
        NightVisionController.Tick();
        Tep300Controller.Tick();
        GunHotkeysController.Tick();
        WeaponMpSync.Tick();
        _updateNotifier?.Tick();
        RecipePatch.InjectLiquidTranslations();
    }

    /// <summary>改枪面板按键开关 + 面板自动关闭检查。</summary>
    private static void HandleGunsmithPanelInput()
    {
        if (Input.GetKeyDown(GunsmithKeybindPatch.CurrentKey))
            GunsmithPanel.Toggle();
        GunsmithPanel.Tick();
    }

    private void OnGUI()
    {
        _updateNotifier?.OnGUI();
        NightVisionController.OnGUI();
    }
}
