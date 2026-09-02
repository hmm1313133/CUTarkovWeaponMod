using System;
using System.Collections.Generic;
using System.Linq;
using CUCoreLib.Registries;
using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 独立控制台命令 "vanillablock"：管理原版物品拦截状态（不依附于 spawn 命令）。
///
/// 用法：
/// - vanillablock list                显示全局开关与每个物品的当前拦截状态
/// - vanillablock on / vanillablock off    全局开启/关闭拦截（清空所有单独覆盖）
/// - vanillablock [物品名] true/false 单独强制拦截/放行该物品（优先于全局开关）
///
/// 注册方式：CUCoreLib ConsoleCommandRegistry。
/// 若 ConsoleScript.Commands 已初始化则立即注入，
/// 否则由 CUCoreLib 在 ConsoleScript.RegisterAllCommands 后自动注入。
///
/// 自动补全：
/// - 参数1：list / on / off / 所有可拦截物品ID
/// - 参数2：true / false（由 Command 构造函数的 "bool" shortDesc 自动添加）
/// </summary>
public static class VanillaCommand
{
    private static ConsoleScript _cachedConsole;
    private static System.Reflection.MethodInfo _logMethod;

    /// <summary>注册命令（在插件初始化时调用一次）</summary>
    public static void Register()
    {
        var autofill = new Dictionary<int, List<string>>
        {
            {
                0,
                new List<string> { "list", "on", "off" }
                    .Concat(VanillaBlockPatch.BlockedVanillaIds
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
                    .ToList()
            }
        };

        ConsoleCommandRegistry.Register(
            "vanillablock",
            "Manage vanilla item blocking: list / on / off / [item] true|false",
            Execute,
            autofill,
            ("action", "list, on, off, or a blocked item id"),
            ("bool", "true to force block, false to allow"));

        Plugin.Log.LogInfo("[VanillaCommand] Registered 'vanillablock' console command.");
    }

    private static void Execute(string[] args)
    {
        if (args == null || args.Length < 2)
            throw new Exception("Usage: vanillablock [list|on|off] or vanillablock [item] [true|false]");

        var sub = args[1].Trim().ToLowerInvariant();

        switch (sub)
        {
            case "list":
                PrintList();
                return;

            case "on":
                VanillaBlockPatch.BlockEnabled = true;
                VanillaBlockPatch.IndividualOverrides.Clear();
                Log("Vanilla item blocking ENABLED (all item overrides cleared).");
                return;

            case "off":
                VanillaBlockPatch.BlockEnabled = false;
                VanillaBlockPatch.IndividualOverrides.Clear();
                Log("Vanilla item blocking DISABLED (all item overrides cleared).");
                return;
        }

        // vanillablock [物品名] true/false
        if (args.Length < 3)
            throw new Exception($"Usage: vanilla {sub} [true|false]");

        if (!bool.TryParse(args[2].Trim(), out var enable))
            throw new Exception($"Invalid value '{args[2]}'. Use true or false.");

        if (!VanillaBlockPatch.BlockedVanillaIds.Contains(sub))
            throw new Exception(
                $"'{sub}' is not a blockable vanilla item. Use 'vanillablock list' to see all items.");

        VanillaBlockPatch.IndividualOverrides[sub] = enable;
        Log($"Item '{sub}' is now {(enable ? "BLOCKED" : "ALLOWED")} (override).");
    }

    private static void PrintList()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Vanilla item blocking: {(VanillaBlockPatch.BlockEnabled ? "ON" : "OFF")}");
        foreach (var id in VanillaBlockPatch.BlockedVanillaIds
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            string state;
            if (VanillaBlockPatch.IndividualOverrides.TryGetValue(id, out var forced))
                state = forced ? "BLOCKED (override)" : "allowed (override)";
            else
                state = VanillaBlockPatch.BlockEnabled ? "blocked" : "allowed";

            sb.AppendLine($"  {id,-18} {state}");
        }

        if (VanillaBlockPatch.IndividualOverrides.Count > 0)
            sb.AppendLine("'vanillablock on/off' clears all item overrides.");
        sb.Append("Note: recipe changes apply on next world generation.");
        Log(sb.ToString());
    }

    /// <summary>输出到游戏内控制台（逐行）与 BepInEx 日志</summary>
    private static void Log(string message)
    {
        foreach (var line in message.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            Plugin.Log.LogInfo($"[VanillaCommand] {trimmed}");
            LogToConsole(trimmed);
        }
    }

    private static void LogToConsole(string line)
    {
        try
        {
            if (_cachedConsole == null)
                _cachedConsole = UnityEngine.Object.FindObjectOfType<ConsoleScript>();
            if (_cachedConsole == null) return;

            _logMethod ??= AccessTools.Method(typeof(ConsoleScript), "LogToConsole");
            _logMethod?.Invoke(_cachedConsole, new object[] { line });
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[VanillaCommand] LogToConsole failed: {ex.Message}");
        }
    }
}
