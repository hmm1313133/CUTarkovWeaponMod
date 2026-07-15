using System;
using HarmonyLib;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 确保 vanilla_on / vanilla_off 可通过控制台执行。
/// spawn 命令的自动补全由医疗模组的 ConsoleRegisterSpawnEntitiesPatch 统一注入
/// （CustomItemPrefabs 包含全部武器物品 ID，类型已修正为 Dictionary<int, List<string>>）。
/// CUCoreLib 模式下由 CUCoreLib ConsolePatch.RefreshSpawnAutofill 通过 ItemRegistry 注入。
/// </summary>
[HarmonyPatch(typeof(ConsoleScript), nameof(ConsoleScript.RegisterSpawnEntities))]
public static class ConsoleAutofillPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        try
        {
            // vanilla_on / vanilla_off 已由 VanillaBlockPatch 的 TryExecuteCommand Prefix 处理，
            // 支持 spawn vanilla_on 和直接 vanilla_on 两种格式。
            // 此处仅记录日志，不做额外操作。
            Plugin.Log.LogInfo($"[ConsoleAutofill] Ready. Use 'spawn vanilla_on' or 'spawn vanilla_off' to toggle vanilla items.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ConsoleAutofill] Postfix failed: {ex.Message}");
        }
    }
}
