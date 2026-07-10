using System;
using System.Collections.Generic;
using HarmonyLib;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 枪械物品注册系统。
/// 在启动时将枪械/弹匣/弹药注册到医疗mod的 ConsoleSpawnPatch 系统中。
/// </summary>
public static class WeaponItemRegistration
{
    /// <summary>
    /// 注册所有枪械物品到控制台生成系统和物品配置系统。
    /// </summary>
    public static void Register()
    {
        // 1. 注册枪械/弹匣/弹药的预制体映射到 ConsoleSpawnPatch
        var prefabs = ConsoleSpawnPatch.CustomItemPrefabs;

        // 枪械类
        prefabs[MP133ItemSystem.ItemKey] = "shotgun";
        prefabs[MP153ItemSystem.ItemKey] = "shotgun";
        prefabs[SKSItemSystem.ItemKey] = "rifle";
        prefabs[AXMCItemSystem.ItemKey] = "rifle";
        prefabs[DVL10ItemSystem.ItemKey] = "rifle";
        prefabs[AKMItemSystem.ItemKey] = "rifle";
        prefabs[DeagleItemSystem.ItemKey] = "pistol";
        prefabs[Glock17ItemSystem.ItemKey] = "pistol";
        prefabs[M4A1ItemSystem.ItemKey] = "rifle";
        prefabs[P90ItemSystem.ItemKey] = "rifle";
        prefabs[UMP45ItemSystem.ItemKey] = "rifle";
        prefabs[RPDItemSystem.ItemKey] = "rifle";
        prefabs[RedRebelItemSystem.ItemKey] = "bruisekit";
        prefabs[M2SwordItemSystem.ItemKey] = "bruisekit";

        // 弹匣类
        prefabs[AXMCMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[DVL10MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[AKMMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[DeagleMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[Glock17MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[M4A1MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[P90MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[UMP45MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[RPDMagItemSystem.ItemKey] = "riflemagazine";

        // 弹药类
        prefabs[Ammo76251BPZItemSystem.ItemKey] = "556round";
        prefabs[Ammo76239SPItemSystem.ItemKey] = "556round";
        prefabs[Ammo12g85ItemSystem.ItemKey] = "12gauge";
        prefabs[Ammo338UCWItemSystem.ItemKey] = "556round";
        prefabs[Ammo50CopperItemSystem.ItemKey] = "556round";
        prefabs[Ammo45FMJItemSystem.ItemKey] = "556round";
        prefabs[Ammo919PSOItemSystem.ItemKey] = "556round";
        prefabs[Ammo55645FMJItemSystem.ItemKey] = "556round";
        prefabs[Ammo5728SB193ItemSystem.ItemKey] = "556round";

        // 2. 设置外部物品配置器
        ConsoleSpawnPatch.ExternalItemConfigurer = ConfigureWeaponItem;

        Plugin.Log.LogInfo("[WeaponRegistration] Registered weapon items in console spawn system.");
    }

    /// <summary>
    /// 配置枪械/弹匣/弹药物品实例。
    /// 返回 true 表示已处理，false 表示不是枪械物品（交给其他处理器）。
    /// </summary>
    private static bool ConfigureWeaponItem(Item item, MedicalGrantRequest request)
    {
        // 枪械
        if (MP133ItemSystem.IsMP133Request(request))
            MP133ItemSystem.ConfigureSpawnedItem(item, request);
        else if (MP153ItemSystem.IsMP153Request(request))
            MP153ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SKSItemSystem.IsSKSRequest(request))
            SKSItemSystem.ConfigureSpawnedItem(item, request);
        else if (AXMCItemSystem.IsAXMCRequest(request))
            AXMCItemSystem.ConfigureSpawnedItem(item, request);
        else if (DVL10ItemSystem.IsDVL10Request(request))
            DVL10ItemSystem.ConfigureSpawnedItem(item, request);
        else if (AKMItemSystem.IsAKMRequest(request))
            AKMItemSystem.ConfigureSpawnedItem(item, request);
        else if (DeagleItemSystem.IsDeagleRequest(request))
            DeagleItemSystem.ConfigureSpawnedItem(item, request);
        else if (Glock17ItemSystem.IsGlock17Request(request))
            Glock17ItemSystem.ConfigureSpawnedItem(item, request);
        else if (M4A1ItemSystem.IsM4A1Request(request))
            M4A1ItemSystem.ConfigureSpawnedItem(item, request);
        else if (P90ItemSystem.IsP90Request(request))
            P90ItemSystem.ConfigureSpawnedItem(item, request);
        else if (UMP45ItemSystem.IsUMP45Request(request))
            UMP45ItemSystem.ConfigureSpawnedItem(item, request);
        else if (RPDItemSystem.IsRPDRequest(request))
            RPDItemSystem.ConfigureSpawnedItem(item, request);
        else if (RedRebelItemSystem.IsRedRebelRequest(request))
            RedRebelItemSystem.ConfigureSpawnedItem(item, request);
        else if (M2SwordItemSystem.IsM2SwordRequest(request))
            M2SwordItemSystem.ConfigureSpawnedItem(item, request);
        // 弹匣
        else if (AXMCMagItemSystem.IsAXMCMagRequest(request))
            AXMCMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (DVL10MagItemSystem.IsDVL10MagRequest(request))
            DVL10MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (AKMMagItemSystem.IsAKMMagRequest(request))
            AKMMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (DeagleMagItemSystem.IsDeagleMagRequest(request))
            DeagleMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (Glock17MagItemSystem.IsGlock17MagRequest(request))
            Glock17MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (M4A1MagItemSystem.IsM4A1MagRequest(request))
            M4A1MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (P90MagItemSystem.IsP90MagRequest(request))
            P90MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (UMP45MagItemSystem.IsUMP45MagRequest(request))
            UMP45MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (RPDMagItemSystem.IsRPDMagRequest(request))
            RPDMagItemSystem.ConfigureSpawnedItem(item, request);
        // 弹药
        else if (Ammo76251BPZItemSystem.Is76251BPZRequest(request))
            Ammo76251BPZItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo76239SPItemSystem.Is76239SPRequest(request))
            Ammo76239SPItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo12g85ItemSystem.Is12g85Request(request))
            Ammo12g85ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo338UCWItemSystem.Is338UCWRequest(request))
            Ammo338UCWItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo50CopperItemSystem.Is50CopperRequest(request))
            Ammo50CopperItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo45FMJItemSystem.Is45FMJRequest(request))
            Ammo45FMJItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo919PSOItemSystem.Is919PSORequest(request))
            Ammo919PSOItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo55645FMJItemSystem.Is55645FMJRequest(request))
            Ammo55645FMJItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ammo5728SB193ItemSystem.Is5728SB193Request(request))
            Ammo5728SB193ItemSystem.ConfigureSpawnedItem(item, request);
        else
            return false; // 不是枪械物品

        return true;
    }
}

/// <summary>
/// Item.SetupItems postfix - 重新注册所有枪械物品到 GlobalItems。
/// 与医疗mod的 EtgStimRegistryPatch 并行运行（Harmony 支持多个 Postfix）。
/// </summary>
[HarmonyPatch(typeof(Item), nameof(Item.SetupItems))]
public static class WeaponItemRegistryPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        MP133ItemSystem.EnsureRegisteredInItemTable();
        MP153ItemSystem.EnsureRegisteredInItemTable();
        SKSItemSystem.EnsureRegisteredInItemTable();
        AXMCItemSystem.EnsureRegisteredInItemTable();
        DVL10ItemSystem.EnsureRegisteredInItemTable();
        AKMItemSystem.EnsureRegisteredInItemTable();
        AXMCMagItemSystem.EnsureRegisteredInItemTable();
        DVL10MagItemSystem.EnsureRegisteredInItemTable();
        AKMMagItemSystem.EnsureRegisteredInItemTable();
        Ammo76251BPZItemSystem.EnsureRegisteredInItemTable();
        Ammo76239SPItemSystem.EnsureRegisteredInItemTable();
        Ammo12g85ItemSystem.EnsureRegisteredInItemTable();
        Ammo338UCWItemSystem.EnsureRegisteredInItemTable();
        Ammo50CopperItemSystem.EnsureRegisteredInItemTable();
        Ammo45FMJItemSystem.EnsureRegisteredInItemTable();
        DeagleItemSystem.EnsureRegisteredInItemTable();
        DeagleMagItemSystem.EnsureRegisteredInItemTable();
        Glock17ItemSystem.EnsureRegisteredInItemTable();
        Glock17MagItemSystem.EnsureRegisteredInItemTable();
        Ammo919PSOItemSystem.EnsureRegisteredInItemTable();
        M4A1ItemSystem.EnsureRegisteredInItemTable();
        M4A1MagItemSystem.EnsureRegisteredInItemTable();
        Ammo55645FMJItemSystem.EnsureRegisteredInItemTable();
        P90ItemSystem.EnsureRegisteredInItemTable();
        P90MagItemSystem.EnsureRegisteredInItemTable();
        Ammo5728SB193ItemSystem.EnsureRegisteredInItemTable();
        UMP45ItemSystem.EnsureRegisteredInItemTable();
        UMP45MagItemSystem.EnsureRegisteredInItemTable();
        RPDItemSystem.EnsureRegisteredInItemTable();
        RPDMagItemSystem.EnsureRegisteredInItemTable();
        RedRebelItemSystem.EnsureRegisteredInItemTable();
        M2SwordItemSystem.EnsureRegisteredInItemTable();
    }
}
