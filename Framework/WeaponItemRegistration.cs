using System;
using System.Collections.Generic;
using System.Linq;
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
    /// 所有武器物品 ID 集合（16 枪械/近战 + 31 护甲/插板 + 10 弹匣 + 9 弹药 = 66 个）。
    /// 用于 CUCoreLib 模式下将武器物品注册到 ItemRegistry。
    /// </summary>
    public static readonly HashSet<string> WeaponItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // 枪械/近战
        MP133ItemSystem.ItemKey, MP153ItemSystem.ItemKey, SKSItemSystem.ItemKey,
        AXMCItemSystem.ItemKey, DVL10ItemSystem.ItemKey, AKMItemSystem.ItemKey,
        DeagleItemSystem.ItemKey, Glock17ItemSystem.ItemKey, M4A1ItemSystem.ItemKey,
        P90ItemSystem.ItemKey, UMP45ItemSystem.ItemKey, RPDItemSystem.ItemKey,
        RedRebelItemSystem.ItemKey, M2SwordItemSystem.ItemKey, USPItemSystem.ItemKey,
        VSSItemSystem.ItemKey,
        // 护甲/胸挂
        MBSSItemSystem.ItemKey,
        TV115ItemSystem.ItemKey,
        TV110ItemSystem.ItemKey,
        SPPCV2ItemSystem.ItemKey,
        MK4AItemSystem.ItemKey,
        SiegeRItemSystem.ItemKey,
        SixB516ItemSystem.ItemKey,
        TTSKItemSystem.ItemKey,
        AVSTEItemSystem.ItemKey,
        LV119ItemSystem.ItemKey,
        SixB45ItemSystem.ItemKey,
        IDEAItemSystem.ItemKey,
        BankRobberItemSystem.ItemKey,
        Type56ItemSystem.ItemKey,
        WTChestRigItemSystem.ItemKey,
        LBCRItemSystem.ItemKey,
        CommandoItemSystem.ItemKey,
        UmkaItemSystem.ItemKey,
        BlackRockItemSystem.ItemKey,
        PACAItemSystem.ItemKey,
        MFUNItemSystem.ItemKey,
        DRDItemSystem.ItemKey,
        ThorItemSystem.ItemKey,
        TrooperItemSystem.ItemKey,
        SixB13ItemSystem.ItemKey,
        HPCItemSystem.ItemKey,
        GzhelKItemSystem.ItemKey,
        RedutT5ItemSystem.ItemKey,
        SlickItemSystem.ItemKey,
        HGridItemSystem.ItemKey,
        SixB43ItemSystem.ItemKey,
        ArmorPlateItemSystem.CheapPlateKey,
        ArmorPlateItemSystem.AdvancedPlateKey,
        RysTItemSystem.ItemKey,
        ExfilItemSystem.ItemKey,
        UlachItemSystem.ItemKey,
        B47ItemSystem.ItemKey,
        Ssh68ItemSystem.ItemKey,
        CalmanItemSystem.ItemKey,
        LK3FItemSystem.ItemKey,
        FastMtItemSystem.ItemKey,
        Pvs14ItemSystem.ItemKey,
        Gpnvg18ItemSystem.ItemKey,
        Pvs31aItemSystem.ItemKey,
        ReadyPackItemSystem.ItemKey,
        PartizanItemSystem.ItemKey,
        DayPackItemSystem.ItemKey,
        BerkutItemSystem.ItemKey,
        ScavPackItemSystem.ItemKey,
        MysteryRanch2DayItemSystem.ItemKey,
        PilgrimItemSystem.ItemKey,
        SsoAttack2ItemSystem.ItemKey,
        SH118ItemSystem.ItemKey,
        LBT2670ItemSystem.ItemKey,
        // 弹匣
        AXMCMagItemSystem.ItemKey, DVL10MagItemSystem.ItemKey, AKMMagItemSystem.ItemKey,
        DeagleMagItemSystem.ItemKey, Glock17MagItemSystem.ItemKey, M4A1MagItemSystem.ItemKey,
        P90MagItemSystem.ItemKey, UMP45MagItemSystem.ItemKey, RPDMagItemSystem.ItemKey,
        USPMagItemSystem.ItemKey,
        VSSMagItemSystem.ItemKey,
        Ammo76251BPZItemSystem.ItemKey, Ammo76239SPItemSystem.ItemKey, Ammo12g85ItemSystem.ItemKey,
        Ammo338UCWItemSystem.ItemKey, Ammo50CopperItemSystem.ItemKey, Ammo45FMJItemSystem.ItemKey,
        Ammo919PSOItemSystem.ItemKey, Ammo55645FMJItemSystem.ItemKey, Ammo5728SB193ItemSystem.ItemKey,
        Ammo939SP5ItemSystem.ItemKey,
        WeaponRepairKitItemSystem.ItemKey,
    };

    /// <summary>判断是否为武器模组自定义物品 ID</summary>
    public static bool IsWeaponItem(string id) => WeaponItemIds.Contains(id);

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
        prefabs[USPItemSystem.ItemKey] = "pistol";
        prefabs[VSSItemSystem.ItemKey] = "rifle";

        // 护甲/胸挂
        prefabs[MBSSItemSystem.ItemKey] = "bruisekit";
        prefabs[TV115ItemSystem.ItemKey] = "bruisekit";
        prefabs[TV110ItemSystem.ItemKey] = "bruisekit";
        prefabs[SPPCV2ItemSystem.ItemKey] = "bruisekit";
        prefabs[MK4AItemSystem.ItemKey] = "bruisekit";
        prefabs[SiegeRItemSystem.ItemKey] = "bruisekit";
        prefabs[SixB516ItemSystem.ItemKey] = "bruisekit";
        prefabs[TTSKItemSystem.ItemKey] = "bruisekit";
        prefabs[AVSTEItemSystem.ItemKey] = "bruisekit";
        prefabs[LV119ItemSystem.ItemKey] = "bruisekit";
        prefabs[SixB45ItemSystem.ItemKey] = "bruisekit";
        prefabs[IDEAItemSystem.ItemKey] = "bruisekit";
        prefabs[BankRobberItemSystem.ItemKey] = "bruisekit";
        prefabs[Type56ItemSystem.ItemKey] = "bruisekit";
        prefabs[WTChestRigItemSystem.ItemKey] = "bruisekit";
        prefabs[LBCRItemSystem.ItemKey] = "bruisekit";
        prefabs[CommandoItemSystem.ItemKey] = "bruisekit";
        prefabs[UmkaItemSystem.ItemKey] = "bruisekit";
        prefabs[BlackRockItemSystem.ItemKey] = "bruisekit";
        prefabs[PACAItemSystem.ItemKey] = "bruisekit";
        prefabs[MFUNItemSystem.ItemKey] = "bruisekit";
        prefabs[DRDItemSystem.ItemKey] = "bruisekit";
        prefabs[ThorItemSystem.ItemKey] = "bruisekit";
        prefabs[TrooperItemSystem.ItemKey] = "bruisekit";
        prefabs[SixB13ItemSystem.ItemKey] = "bruisekit";
        prefabs[HPCItemSystem.ItemKey] = "bruisekit";
        prefabs[GzhelKItemSystem.ItemKey] = "bruisekit";
        prefabs[RedutT5ItemSystem.ItemKey] = "bruisekit";
        prefabs[SlickItemSystem.ItemKey] = "bruisekit";
        prefabs[HGridItemSystem.ItemKey] = "bruisekit";
        prefabs[SixB43ItemSystem.ItemKey] = "bruisekit";
        prefabs[ArmorPlateItemSystem.CheapPlateKey] = "bruisekit";
        prefabs[ArmorPlateItemSystem.AdvancedPlateKey] = "bruisekit";
        prefabs[RysTItemSystem.ItemKey] = "bruisekit";
        prefabs[ExfilItemSystem.ItemKey] = "bruisekit";
        prefabs[UlachItemSystem.ItemKey] = "bruisekit";
        prefabs[B47ItemSystem.ItemKey] = "bruisekit";
        prefabs[Ssh68ItemSystem.ItemKey] = "bruisekit";
        prefabs[CalmanItemSystem.ItemKey] = "bruisekit";
        prefabs[LK3FItemSystem.ItemKey] = "bruisekit";
        prefabs[FastMtItemSystem.ItemKey] = "bruisekit";
        prefabs[Pvs14ItemSystem.ItemKey] = "bruisekit";
        prefabs[Gpnvg18ItemSystem.ItemKey] = "bruisekit";
        prefabs[Pvs31aItemSystem.ItemKey] = "bruisekit";
        prefabs[ReadyPackItemSystem.ItemKey] = "bruisekit";
        prefabs[PartizanItemSystem.ItemKey] = "bruisekit";
        prefabs[DayPackItemSystem.ItemKey] = "bruisekit";
        prefabs[BerkutItemSystem.ItemKey] = "bruisekit";
        prefabs[ScavPackItemSystem.ItemKey] = "bruisekit";
        prefabs[MysteryRanch2DayItemSystem.ItemKey] = "bruisekit";
        prefabs[PilgrimItemSystem.ItemKey] = "bruisekit";
        prefabs[SsoAttack2ItemSystem.ItemKey] = "bruisekit";
        prefabs[SH118ItemSystem.ItemKey] = "bruisekit";
        prefabs[LBT2670ItemSystem.ItemKey] = "bruisekit";

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
        prefabs[USPMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[VSSMagItemSystem.ItemKey] = "riflemagazine";

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
        prefabs[Ammo939SP5ItemSystem.ItemKey] = "556round";
        prefabs[WeaponRepairKitItemSystem.ItemKey] = "bruisekit";

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
        else if (USPItemSystem.IsUSPRequest(request))
            USPItemSystem.ConfigureSpawnedItem(item, request);
        else if (VSSItemSystem.IsVSSRequest(request))
            VSSItemSystem.ConfigureSpawnedItem(item, request);
        // 护甲/胸挂
        else if (MBSSItemSystem.IsMBSSRequest(request))
            MBSSItemSystem.ConfigureSpawnedItem(item, request);
        else if (TV115ItemSystem.IsTV115Request(request))
            TV115ItemSystem.ConfigureSpawnedItem(item, request);
        else if (TV110ItemSystem.IsTV110Request(request))
            TV110ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SPPCV2ItemSystem.IsSPPCV2Request(request))
            SPPCV2ItemSystem.ConfigureSpawnedItem(item, request);
        else if (MK4AItemSystem.IsMK4ARequest(request))
            MK4AItemSystem.ConfigureSpawnedItem(item, request);
        else if (SiegeRItemSystem.IsSiegeRRequest(request))
            SiegeRItemSystem.ConfigureSpawnedItem(item, request);
        else if (SixB516ItemSystem.Is6B516Request(request))
            SixB516ItemSystem.ConfigureSpawnedItem(item, request);
        else if (TTSKItemSystem.IsTTSKRequest(request))
            TTSKItemSystem.ConfigureSpawnedItem(item, request);
        else if (AVSTEItemSystem.IsAVSTERequest(request))
            AVSTEItemSystem.ConfigureSpawnedItem(item, request);
        else if (LV119ItemSystem.IsLV119Request(request))
            LV119ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SixB45ItemSystem.Is6B45Request(request))
            SixB45ItemSystem.ConfigureSpawnedItem(item, request);
        else if (IDEAItemSystem.IsIDEARequest(request))
            IDEAItemSystem.ConfigureSpawnedItem(item, request);
        else if (BankRobberItemSystem.IsBankRobberRequest(request))
            BankRobberItemSystem.ConfigureSpawnedItem(item, request);
        else if (Type56ItemSystem.IsType56Request(request))
            Type56ItemSystem.ConfigureSpawnedItem(item, request);
        else if (WTChestRigItemSystem.IsWTChestRigRequest(request))
            WTChestRigItemSystem.ConfigureSpawnedItem(item, request);
        else if (LBCRItemSystem.IsLBCRRequest(request))
            LBCRItemSystem.ConfigureSpawnedItem(item, request);
        else if (CommandoItemSystem.IsCommandoRequest(request))
            CommandoItemSystem.ConfigureSpawnedItem(item, request);
        else if (UmkaItemSystem.IsUmkaRequest(request))
            UmkaItemSystem.ConfigureSpawnedItem(item, request);
        else if (BlackRockItemSystem.IsBlackRockRequest(request))
            BlackRockItemSystem.ConfigureSpawnedItem(item, request);
        else if (PACAItemSystem.IsPACARequest(request))
            PACAItemSystem.ConfigureSpawnedItem(item, request);
        else if (MFUNItemSystem.IsMFUNRequest(request))
            MFUNItemSystem.ConfigureSpawnedItem(item, request);
        else if (DRDItemSystem.IsDRDRequest(request))
            DRDItemSystem.ConfigureSpawnedItem(item, request);
        else if (ThorItemSystem.IsThorRequest(request))
            ThorItemSystem.ConfigureSpawnedItem(item, request);
        else if (TrooperItemSystem.IsTrooperRequest(request))
            TrooperItemSystem.ConfigureSpawnedItem(item, request);
        else if (SixB13ItemSystem.Is6B13Request(request))
            SixB13ItemSystem.ConfigureSpawnedItem(item, request);
        else if (HPCItemSystem.IsHPCRequest(request))
            HPCItemSystem.ConfigureSpawnedItem(item, request);
        else if (GzhelKItemSystem.IsGzhelKRequest(request))
            GzhelKItemSystem.ConfigureSpawnedItem(item, request);
        else if (RedutT5ItemSystem.IsRedutT5Request(request))
            RedutT5ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SlickItemSystem.IsSlickRequest(request))
            SlickItemSystem.ConfigureSpawnedItem(item, request);
        else if (HGridItemSystem.IsHGridRequest(request))
            HGridItemSystem.ConfigureSpawnedItem(item, request);
        else if (SixB43ItemSystem.Is6B43Request(request))
            SixB43ItemSystem.ConfigureSpawnedItem(item, request);
        else if (ArmorPlateItemSystem.IsCheapPlateRequest(request))
            ArmorPlateItemSystem.ConfigureSpawnedItem(item, request);
        else if (ArmorPlateItemSystem.IsAdvancedPlateRequest(request))
            ArmorPlateItemSystem.ConfigureSpawnedItem(item, request);
        else if (RysTItemSystem.IsRysTRequest(request))
            RysTItemSystem.ConfigureSpawnedItem(item, request);
        else if (ExfilItemSystem.IsExfilRequest(request))
            ExfilItemSystem.ConfigureSpawnedItem(item, request);
        else if (UlachItemSystem.IsUlachRequest(request))
            UlachItemSystem.ConfigureSpawnedItem(item, request);
        else if (B47ItemSystem.IsB47Request(request))
            B47ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ssh68ItemSystem.IsSsh68Request(request))
            Ssh68ItemSystem.ConfigureSpawnedItem(item, request);
        else if (CalmanItemSystem.IsCalmanRequest(request))
            CalmanItemSystem.ConfigureSpawnedItem(item, request);
        else if (LK3FItemSystem.IsLK3FRequest(request))
            LK3FItemSystem.ConfigureSpawnedItem(item, request);
        else if (FastMtItemSystem.IsFastMtRequest(request))
            FastMtItemSystem.ConfigureSpawnedItem(item, request);
        else if (Pvs14ItemSystem.IsPvs14Request(request))
            Pvs14ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Gpnvg18ItemSystem.IsGpnvg18Request(request))
            Gpnvg18ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Pvs31aItemSystem.IsPvs31aRequest(request))
            Pvs31aItemSystem.ConfigureSpawnedItem(item, request);
        else if (ReadyPackItemSystem.IsReadyPackRequest(request))
            ReadyPackItemSystem.ConfigureSpawnedItem(item, request);
        else if (PartizanItemSystem.IsPartizanRequest(request))
            PartizanItemSystem.ConfigureSpawnedItem(item, request);
        else if (DayPackItemSystem.IsDayPackRequest(request))
            DayPackItemSystem.ConfigureSpawnedItem(item, request);
        else if (BerkutItemSystem.IsBerkutRequest(request))
            BerkutItemSystem.ConfigureSpawnedItem(item, request);
        else if (ScavPackItemSystem.IsScavPackRequest(request))
            ScavPackItemSystem.ConfigureSpawnedItem(item, request);
        else if (MysteryRanch2DayItemSystem.IsMysteryRanch2DayRequest(request))
            MysteryRanch2DayItemSystem.ConfigureSpawnedItem(item, request);
        else if (PilgrimItemSystem.IsPilgrimRequest(request))
            PilgrimItemSystem.ConfigureSpawnedItem(item, request);
        else if (SsoAttack2ItemSystem.IsSsoAttack2Request(request))
            SsoAttack2ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SH118ItemSystem.IsSH118Request(request))
            SH118ItemSystem.ConfigureSpawnedItem(item, request);
        else if (LBT2670ItemSystem.IsLBT2670Request(request))
            LBT2670ItemSystem.ConfigureSpawnedItem(item, request);
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
        else if (USPMagItemSystem.IsUSPMagRequest(request))
            USPMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (VSSMagItemSystem.IsVSSMagRequest(request))
            VSSMagItemSystem.ConfigureSpawnedItem(item, request);
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
        else if (Ammo939SP5ItemSystem.Is939SP5Request(request))
            Ammo939SP5ItemSystem.ConfigureSpawnedItem(item, request);
        else if (WeaponRepairKitItemSystem.IsRepairKitRequest(request))
            WeaponRepairKitItemSystem.ConfigureSpawnedItem(item, request);
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
        USPItemSystem.EnsureRegisteredInItemTable();
        VSSItemSystem.EnsureRegisteredInItemTable();
        MBSSItemSystem.EnsureRegisteredInItemTable();
        TV115ItemSystem.EnsureRegisteredInItemTable();
        TV110ItemSystem.EnsureRegisteredInItemTable();
        SPPCV2ItemSystem.EnsureRegisteredInItemTable();
        MK4AItemSystem.EnsureRegisteredInItemTable();
        SiegeRItemSystem.EnsureRegisteredInItemTable();
        SixB516ItemSystem.EnsureRegisteredInItemTable();
        TTSKItemSystem.EnsureRegisteredInItemTable();
        AVSTEItemSystem.EnsureRegisteredInItemTable();
        LV119ItemSystem.EnsureRegisteredInItemTable();
        IDEAItemSystem.EnsureRegisteredInItemTable();
        BankRobberItemSystem.EnsureRegisteredInItemTable();
        Type56ItemSystem.EnsureRegisteredInItemTable();
        WTChestRigItemSystem.EnsureRegisteredInItemTable();
        LBCRItemSystem.EnsureRegisteredInItemTable();
        CommandoItemSystem.EnsureRegisteredInItemTable();
        UmkaItemSystem.EnsureRegisteredInItemTable();
        BlackRockItemSystem.EnsureRegisteredInItemTable();
        PACAItemSystem.EnsureRegisteredInItemTable();
        MFUNItemSystem.EnsureRegisteredInItemTable();
        DRDItemSystem.EnsureRegisteredInItemTable();
        ThorItemSystem.EnsureRegisteredInItemTable();
        TrooperItemSystem.EnsureRegisteredInItemTable();
        SixB13ItemSystem.EnsureRegisteredInItemTable();
        HPCItemSystem.EnsureRegisteredInItemTable();
        GzhelKItemSystem.EnsureRegisteredInItemTable();
        RedutT5ItemSystem.EnsureRegisteredInItemTable();
        SlickItemSystem.EnsureRegisteredInItemTable();
        HGridItemSystem.EnsureRegisteredInItemTable();
        SixB43ItemSystem.EnsureRegisteredInItemTable();
        ArmorPlateItemSystem.EnsureCheapPlateRegistered();
        ArmorPlateItemSystem.EnsureAdvancedPlateRegistered();
        RysTItemSystem.EnsureRegisteredInItemTable();
        ExfilItemSystem.EnsureRegisteredInItemTable();
        UlachItemSystem.EnsureRegisteredInItemTable();
        B47ItemSystem.EnsureRegisteredInItemTable();
        Ssh68ItemSystem.EnsureRegisteredInItemTable();
        CalmanItemSystem.EnsureRegisteredInItemTable();
        LK3FItemSystem.EnsureRegisteredInItemTable();
        FastMtItemSystem.EnsureRegisteredInItemTable();
        Pvs14ItemSystem.EnsureRegisteredInItemTable();
        Gpnvg18ItemSystem.EnsureRegisteredInItemTable();
        Pvs31aItemSystem.EnsureRegisteredInItemTable();
        ReadyPackItemSystem.EnsureRegisteredInItemTable();
        PartizanItemSystem.EnsureRegisteredInItemTable();
        DayPackItemSystem.EnsureRegisteredInItemTable();
        BerkutItemSystem.EnsureRegisteredInItemTable();
        ScavPackItemSystem.EnsureRegisteredInItemTable();
        MysteryRanch2DayItemSystem.EnsureRegisteredInItemTable();
        PilgrimItemSystem.EnsureRegisteredInItemTable();
        SsoAttack2ItemSystem.EnsureRegisteredInItemTable();
        SH118ItemSystem.EnsureRegisteredInItemTable();
        LBT2670ItemSystem.EnsureRegisteredInItemTable();
        SixB45ItemSystem.EnsureRegisteredInItemTable();
        USPMagItemSystem.EnsureRegisteredInItemTable();
        VSSMagItemSystem.EnsureRegisteredInItemTable();
        Ammo939SP5ItemSystem.EnsureRegisteredInItemTable();
        WeaponRepairKitItemSystem.EnsureRegisteredInItemTable();

        // 通知集成模式武器物品已注册到 GlobalItems
        Plugin.IntegrationMode?.OnItemsSetup();
    }
}
