using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CUTarkovMedicalMod.Framework;
using CUTarkovWeaponMod.Framework;

namespace CUTarkovWeaponMod;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("com.yourname.cu.tarkovmedicalmod", BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string ModGuid = "com.yourname.cu.tarkovweaponmod";
    public const string ModName = "Casualties: Unknown - Tarkov-Style Weapon Mod";
    public const string ModVersion = "1.0.0.0";

    internal static ManualLogSource Log = null!;

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
        var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "redrebel.png");
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

        try
        {
            var harmony = new Harmony(ModGuid);
            harmony.PatchAll();

            // PatchAll() cannot discover PlayerCamera.HandleVariables (private method),
            // so manually register the ScopeZoom patch
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
        }
        catch (Exception ex)
        {
            Log.LogError($"PatchAll() threw: {ex}");
        }

        Log.LogInfo($"{ModName} loaded.");

        // 创建更新提醒实例（由 Plugin 的 Update/OnGUI 驱动）
        _updateNotifier = new WeaponUpdateNotifier();
    }

    private void Update()
    {
        _updateNotifier?.Tick();
    }

    private void OnGUI()
    {
        _updateNotifier?.OnGUI();
    }
}
