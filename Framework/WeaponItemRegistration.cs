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
        SksA5MagItemSystem.ItemKey, SksIntegralMagItemSystem.ItemKey,
        UasSksItemSystem.ItemKey, TapcoIntrafuseItemSystem.ItemKey,
        HexagonSksItemSystem.ItemKey, Wt0032_1ItemSystem.ItemKey,
        SksMcItemSystem.ItemKey, Mtu017ItemSystem.ItemKey,
        SrvvAkmItemSystem.ItemKey, Dtk4mItemSystem.ItemKey, DtkpItemSystem.ItemKey,
        Ac858ItemSystem.ItemKey, HekateDt338ItemSystem.ItemKey, Tmb338lmItemSystem.ItemKey,
        Tsm338lmItemSystem.ItemKey, AxmcGripItemSystem.ItemKey,
        Dvl10SilencedItemSystem.ItemKey,
        AXMCItemSystem.ItemKey, DVL10ItemSystem.ItemKey, AKMItemSystem.ItemKey,
        DeagleItemSystem.ItemKey, Glock17ItemSystem.ItemKey, M4A1ItemSystem.ItemKey,
        P90ItemSystem.ItemKey, UMP45ItemSystem.ItemKey, RPDItemSystem.ItemKey,
        RedRebelItemSystem.ItemKey, M2SwordItemSystem.ItemKey, USPItemSystem.ItemKey,
        VSSItemSystem.ItemKey,
        AA12ItemSystem.ItemKey,
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
        M4A1Mag560ItemSystem.ItemKey,
        P90MagItemSystem.ItemKey, UMP45MagItemSystem.ItemKey, RPDMagItemSystem.ItemKey,
        USPMagItemSystem.ItemKey, X47MagItemSystem.ItemKey,
        VSSMagItemSystem.ItemKey,
        AA12MagItemSystem.ItemKey,
        GlockBigStickMagItemSystem.ItemKey, GlockG50MagItemSystem.ItemKey,
        Ammo76251BPZItemSystem.ItemKey, Ammo76239SPItemSystem.ItemKey, Ammo12g85ItemSystem.ItemKey,
        Ammo338UCWItemSystem.ItemKey, Ammo50CopperItemSystem.ItemKey, Ammo45FMJItemSystem.ItemKey,
        Ammo919PSOItemSystem.ItemKey, Ammo55645FMJItemSystem.ItemKey, Ammo5728SB193ItemSystem.ItemKey,
        Ammo939SP5ItemSystem.ItemKey,
        // 弹药盒（不在世界刷新，仅注册/控制台/配方）
        "box_338ucw", "box_76251bpz", "box_50copper", "box_12g85",
        "box_76239sp", "box_55645fmj", "box_939sp5", "box_45fmj",
        "box_919pso", "box_5728sb193",
        WeaponRepairKitItemSystem.ItemKey,
        Tep300ItemSystem.ItemKey,
        ProFlexItemSystem.ItemKey,
        CrackersItemSystem.ItemKey,
        CroutonsItemSystem.ItemKey,
        SlickersItemSystem.ItemKey,
        TarkerItemSystem.ItemKey,
        AlyonkaItemSystem.ItemKey,
        SugarItemSystem.ItemKey,
        IskraItemSystem.ItemKey,
        MreItemSystem.ItemKey,
        PeasItemSystem.ItemKey,
        NoodlesItemSystem.ItemKey,
        CookedNoodlesItemSystem.ItemKey,
        TkFastMtItemSystem.ItemKey,
        FastVisorItemSystem.ItemKey,
        FastVisor2ItemSystem.ItemKey,
        HexagonAKMSuppressorItemSystem.ItemKey,
        DynacompItemSystem.ItemKey,
        Dtk1ItemSystem.ItemKey,
        MoeAkmItemSystem.ItemKey,
        MoeSlItemSystem.ItemKey,
        ViperItemSystem.ItemKey,
        KacRisItemSystem.ItemKey,
        SmrMk16ItemSystem.ItemKey,
        AdarWoodItemSystem.ItemKey,
        LvoaItemSystem.ItemKey,
        M4LongBarrelItemSystem.ItemKey,
        Rotor43ItemSystem.ItemKey,
        Nt4ItemSystem.ItemKey,
        SakerItemSystem.ItemKey,
        Kx3ItemSystem.ItemKey,
        Vp09ItemSystem.ItemKey,
        Rotor43762ItemSystem.ItemKey,
        HexagonAkHandguardItemSystem.ItemKey,
        B10mB19ItemSystem.ItemKey,
        WasrItemSystem.ItemKey,
        AkmLItemSystem.ItemKey,
        Rk3ItemSystem.ItemKey,
        Mg47ItemSystem.ItemKey,
        Ags74ItemSystem.ItemKey,
        Td120001ItemSystem.ItemKey,
        StarkArrgItemSystem.ItemKey,
        MiadItemSystem.ItemKey,
        F1st2pcItemSystem.ItemKey,
        ErgoItemSystem.ItemKey,
        Vipermod1ItemSystem.ItemKey,
        CtrItemSystem.ItemKey,
        Ds150fdeItemSystem.ItemKey,
        AcsItemSystem.ItemKey,
        MoefgItemSystem.ItemKey,
        MoefdeItemSystem.ItemKey,
        MoesgItemSystem.ItemKey,
        PdcItemSystem.ItemKey,
        MrsItemSystem.ItemKey,
        Eotech553ItemSystem.ItemKey,
        Hhs1ItemSystem.ItemKey,
        SpecterDrItemSystem.ItemKey,
        Monstr2x32ItemSystem.ItemKey,
        Ta01nsnItemSystem.ItemKey,
        RazorHdItemSystem.ItemKey,
        Pm2ItemSystem.ItemKey,
        OpforAak7ItemSystem.ItemKey,
        KochergaItemSystem.ItemKey,
        ZhukovSItemSystem.ItemKey,
        Cqr47ItemSystem.ItemKey,
        LasTac2ItemSystem.ItemKey,
        Klesch2UItemSystem.ItemKey,
        BaldrProItemSystem.ItemKey,
        TblItemSystem.ItemKey,
        // 前握把（垂直握把）
        ShiftForegripItemSystem.ItemKey,
        Se5ForegripItemSystem.ItemKey,
        Rk0ForegripItemSystem.ItemKey,
        Rk2ForegripItemSystem.ItemKey,
        B25ur1ForegripItemSystem.ItemKey,
        CobraForegripItemSystem.ItemKey,
        P2ForegripItemSystem.ItemKey,
        AfgForegripItemSystem.ItemKey,
        LeathermanItemSystem.ItemKey,
        WeaponRoomKeycardItemSystem.ItemKey,
        BlueAreaKeycardItemSystem.ItemKey,
        RedAreaKeycardItemSystem.ItemKey,
        // 格洛克配件（套筒/基座/枪管/枪口）
        GlockViperCutItemSystem.ItemKey,
        GlockPs9ItemSystem.ItemKey,
        GlockUm3ItemSystem.ItemKey,
        GlockAwlwItemSystem.ItemKey,
        GlockG3PortItemSystem.ItemKey,
        GlockLw9ItemSystem.ItemKey,
        GlockOsprey9ItemSystem.ItemKey,
        GlockSrd9ItemSystem.ItemKey,
        // 战术速瞄
        DeltaPointItemSystem.ItemKey,
        AcroP1ItemSystem.ItemKey,
        // P90 枪口消音器
        P90AttenuatorItemSystem.ItemKey,
        // UMP 枪口消音器
        UmpOemItemSystem.ItemKey,
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
        prefabs[SksA5MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[SksIntegralMagItemSystem.ItemKey] = "bruisekit";
        prefabs[UasSksItemSystem.ItemKey] = "bruisekit";
        prefabs[TapcoIntrafuseItemSystem.ItemKey] = "bruisekit";
        prefabs[HexagonSksItemSystem.ItemKey] = "bruisekit";
        prefabs[Wt0032_1ItemSystem.ItemKey] = "bruisekit";
        prefabs[SksMcItemSystem.ItemKey] = "bruisekit";
        prefabs[Mtu017ItemSystem.ItemKey] = "bruisekit";
        prefabs[SrvvAkmItemSystem.ItemKey] = "bruisekit";
        prefabs[Dtk4mItemSystem.ItemKey] = "bruisekit";
        prefabs[DtkpItemSystem.ItemKey] = "bruisekit";
        prefabs[Ac858ItemSystem.ItemKey] = "bruisekit";
        prefabs[HekateDt338ItemSystem.ItemKey] = "bruisekit";
        prefabs[Tmb338lmItemSystem.ItemKey] = "bruisekit";
        prefabs[Tsm338lmItemSystem.ItemKey] = "bruisekit";
        prefabs[AxmcGripItemSystem.ItemKey] = "bruisekit";
        prefabs[Dvl10SilencedItemSystem.ItemKey] = "bruisekit";
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
        prefabs[AA12ItemSystem.ItemKey] = "rifle";

        // 配件
        prefabs[HexagonAKMSuppressorItemSystem.ItemKey] = "bruisekit";
        prefabs[DynacompItemSystem.ItemKey] = "bruisekit";
        prefabs[Dtk1ItemSystem.ItemKey] = "bruisekit";
        prefabs[MoeAkmItemSystem.ItemKey] = "bruisekit";
        prefabs[MoeSlItemSystem.ItemKey] = "bruisekit";
        prefabs[ViperItemSystem.ItemKey] = "bruisekit";
        prefabs[KacRisItemSystem.ItemKey] = "bruisekit";
        prefabs[SmrMk16ItemSystem.ItemKey] = "bruisekit";
        prefabs[AdarWoodItemSystem.ItemKey] = "bruisekit";
        prefabs[LvoaItemSystem.ItemKey] = "bruisekit";
        prefabs[M4LongBarrelItemSystem.ItemKey] = "bruisekit";
        prefabs[Rotor43ItemSystem.ItemKey] = "bruisekit";
        prefabs[Nt4ItemSystem.ItemKey] = "bruisekit";
        prefabs[SakerItemSystem.ItemKey] = "bruisekit";
        prefabs[Kx3ItemSystem.ItemKey] = "bruisekit";
        prefabs[Vp09ItemSystem.ItemKey] = "bruisekit";
        prefabs[Rotor43762ItemSystem.ItemKey] = "bruisekit";
        prefabs[HexagonAkHandguardItemSystem.ItemKey] = "bruisekit";
        prefabs[B10mB19ItemSystem.ItemKey] = "bruisekit";
        prefabs[WasrItemSystem.ItemKey] = "bruisekit";
        prefabs[AkmLItemSystem.ItemKey] = "bruisekit";
        prefabs[Rk3ItemSystem.ItemKey] = "bruisekit";
        prefabs[Mg47ItemSystem.ItemKey] = "bruisekit";
        prefabs[Ags74ItemSystem.ItemKey] = "bruisekit";
        prefabs[Td120001ItemSystem.ItemKey] = "bruisekit";
        prefabs[StarkArrgItemSystem.ItemKey] = "bruisekit";
        prefabs[MiadItemSystem.ItemKey] = "bruisekit";
        prefabs[F1st2pcItemSystem.ItemKey] = "bruisekit";
        prefabs[ErgoItemSystem.ItemKey] = "bruisekit";
        prefabs[Vipermod1ItemSystem.ItemKey] = "bruisekit";
        prefabs[CtrItemSystem.ItemKey] = "bruisekit";
        prefabs[Ds150fdeItemSystem.ItemKey] = "bruisekit";
        prefabs[AcsItemSystem.ItemKey] = "bruisekit";
        prefabs[MoefgItemSystem.ItemKey] = "bruisekit";
        prefabs[MoefdeItemSystem.ItemKey] = "bruisekit";
        prefabs[MoesgItemSystem.ItemKey] = "bruisekit";
        prefabs[PdcItemSystem.ItemKey] = "bruisekit";
        prefabs[MrsItemSystem.ItemKey] = MrsItemSystem.BaseGameItemId;
        prefabs[Eotech553ItemSystem.ItemKey] = Eotech553ItemSystem.BaseGameItemId;
        prefabs[Hhs1ItemSystem.ItemKey] = Hhs1ItemSystem.BaseGameItemId;
        prefabs[SpecterDrItemSystem.ItemKey] = SpecterDrItemSystem.BaseGameItemId;
        prefabs[Monstr2x32ItemSystem.ItemKey] = Monstr2x32ItemSystem.BaseGameItemId;
        prefabs[Ta01nsnItemSystem.ItemKey] = Ta01nsnItemSystem.BaseGameItemId;
        prefabs[RazorHdItemSystem.ItemKey] = RazorHdItemSystem.BaseGameItemId;
        prefabs[Pm2ItemSystem.ItemKey] = Pm2ItemSystem.BaseGameItemId;
        prefabs[OpforAak7ItemSystem.ItemKey] = "bruisekit";
        prefabs[KochergaItemSystem.ItemKey] = "bruisekit";
        prefabs[ZhukovSItemSystem.ItemKey] = "bruisekit";
        prefabs[Cqr47ItemSystem.ItemKey] = "bruisekit";
        prefabs[LasTac2ItemSystem.ItemKey] = LasTac2ItemSystem.BaseGameItemId;
        prefabs[Klesch2UItemSystem.ItemKey] = Klesch2UItemSystem.BaseGameItemId;
        prefabs[BaldrProItemSystem.ItemKey] = BaldrProItemSystem.BaseGameItemId;
        prefabs[TblItemSystem.ItemKey] = TblItemSystem.BaseGameItemId;
        prefabs[ShiftForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[Se5ForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[Rk0ForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[Rk2ForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[B25ur1ForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[CobraForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[P2ForegripItemSystem.ItemKey] = "bruisekit";
        prefabs[AfgForegripItemSystem.ItemKey] = "bruisekit";
        // 格洛克配件（套筒/基座/枪管/枪口）
        prefabs[GlockViperCutItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockPs9ItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockUm3ItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockAwlwItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockG3PortItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockLw9ItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockOsprey9ItemSystem.ItemKey] = "bruisekit";
        prefabs[GlockSrd9ItemSystem.ItemKey] = "bruisekit";
        // 战术速瞄
        prefabs[DeltaPointItemSystem.ItemKey] = "bruisekit";
        prefabs[AcroP1ItemSystem.ItemKey] = "bruisekit";
        // P90 枪口消音器
        prefabs[P90AttenuatorItemSystem.ItemKey] = "bruisekit";
        // UMP 枪口消音器
        prefabs[UmpOemItemSystem.ItemKey] = "bruisekit";

        // 工具
        prefabs[LeathermanItemSystem.ItemKey] = "bruisekit";
        prefabs[WeaponRoomKeycardItemSystem.ItemKey] = "bruisekit";
        prefabs[BlueAreaKeycardItemSystem.ItemKey] = "bruisekit";
        prefabs[RedAreaKeycardItemSystem.ItemKey] = "bruisekit";

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
        prefabs[X47MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[DeagleMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[Glock17MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[GlockBigStickMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[GlockG50MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[M4A1MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[M4A1Mag560ItemSystem.ItemKey] = "riflemagazine";
        prefabs[P90MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[UMP45MagItemSystem.ItemKey] = "riflemagazine";
        prefabs[RPDMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[USPMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[VSSMagItemSystem.ItemKey] = "riflemagazine";
        prefabs[AA12MagItemSystem.ItemKey] = "riflemagazine";

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
        // 弹药盒：使用弹匣预制体（可装退弹，但禁止插枪）
        prefabs["box_338ucw"] = "riflemagazine";
        prefabs["box_76251bpz"] = "riflemagazine";
        prefabs["box_50copper"] = "riflemagazine";
        prefabs["box_12g85"] = "riflemagazine";
        prefabs["box_76239sp"] = "riflemagazine";
        prefabs["box_55645fmj"] = "riflemagazine";
        prefabs["box_939sp5"] = "riflemagazine";
        prefabs["box_45fmj"] = "riflemagazine";
        prefabs["box_919pso"] = "riflemagazine";
        prefabs["box_5728sb193"] = "riflemagazine";
        prefabs[WeaponRepairKitItemSystem.ItemKey] = "bruisekit";
        prefabs[Tep300ItemSystem.ItemKey] = "bruisekit";
        prefabs[ProFlexItemSystem.ItemKey] = "bruisekit";
        prefabs[CrackersItemSystem.ItemKey] = "bread";
        prefabs[CroutonsItemSystem.ItemKey] = "bread";
        prefabs[SlickersItemSystem.ItemKey] = "chocolatebar";
        prefabs[TarkerItemSystem.ItemKey] = "burger";
        prefabs[AlyonkaItemSystem.ItemKey] = "chocolatebar";
        prefabs[SugarItemSystem.ItemKey] = "bread";
        prefabs[IskraItemSystem.ItemKey] = "burger";
        prefabs[MreItemSystem.ItemKey] = "burger";
        prefabs[PeasItemSystem.ItemKey] = "bread";
        prefabs[NoodlesItemSystem.ItemKey] = "bread";
        prefabs[CookedNoodlesItemSystem.ItemKey] = "bread";
        prefabs[TkFastMtItemSystem.ItemKey] = "bruisekit";
        prefabs[FastVisorItemSystem.ItemKey] = "bruisekit";
        prefabs[FastVisor2ItemSystem.ItemKey] = "bruisekit";

        // 2. 设置外部物品配置器
        ConsoleSpawnPatch.ExternalItemConfigurer = ConfigureWeaponItem;

        // 3. 配件安装条件注册
        // MOE AKM 护木需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(MoeAkmItemSystem.ItemKey);
        // Hexagon AK 管状护木需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(HexagonAkHandguardItemSystem.ItemKey);
        // B-10M+B-19 导轨护木需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(B10mB19ItemSystem.ItemKey);
        // WASR 木制握把护木需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(WasrItemSystem.ItemKey);
        // TDI AKM-L 护木需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(AkmLItemSystem.ItemKey);
        // MOE SL 护木（M4）需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(MoeSlItemSystem.ItemKey);
        // Viper 护木（M4）需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(ViperItemSystem.ItemKey);
        // KAC RIS 护木（M4）需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(KacRisItemSystem.ItemKey);
        // 长枪管专属护木（M4）需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(SmrMk16ItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(AdarWoodItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(LvoaItemSystem.ItemKey);
        // M4 加长枪管需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(M4LongBarrelItemSystem.ItemKey);
        // Rotor 43 消音器（M4）为枪口装置，无需工具钳
        // RK-3 手枪式握把需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(Rk3ItemSystem.ItemKey);
        // MG-47 手枪式握把需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(Mg47ItemSystem.ItemKey);
        // AGS-74 手枪式握把需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(Ags74ItemSystem.ItemKey);
        // M4 专属后握把（TD120001 / Stark AR RG / MIAD / F1 St2 PC / Ergo）需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(Td120001ItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(StarkArrgItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(MiadItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(F1st2pcItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(ErgoItemSystem.ItemKey);
        // M4 专属后托（Viper Mod.1 / CTR / DS150 FDE / ACS / MOE FG/FDE/SG）需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(Vipermod1ItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(CtrItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(Ds150fdeItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(AcsItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(MoefgItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(MoefdeItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(MoesgItemSystem.ItemKey);
        // PDC 导轨防尘盖需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(PdcItemSystem.ItemKey);
        // SKS 10发弹仓改件需要 Leatherman 工具钳才能安装/卸下
        ToolSystem.RegisterAttachmentRequiringLeatherman(SksIntegralMagItemSystem.ItemKey);
        // UAS SKS 套件需要 Leatherman 工具钳才能安装/卸下
        ToolSystem.RegisterAttachmentRequiringLeatherman(UasSksItemSystem.ItemKey);
        // Tapco INTRAFUSE 套件需要 Leatherman 工具钳才能安装/卸下
        ToolSystem.RegisterAttachmentRequiringLeatherman(TapcoIntrafuseItemSystem.ItemKey);
        // SKS ATI Monte Carlo 枪托需要 Leatherman 工具钳才能安装/卸下
        ToolSystem.RegisterAttachmentRequiringLeatherman(SksMcItemSystem.ItemKey);
        // SKS Leapers UTG PRO MTU017 机匣基座需要 Leatherman 工具钳才能安装/卸下
        ToolSystem.RegisterAttachmentRequiringLeatherman(Mtu017ItemSystem.ItemKey);
        // 枪口装置（Hexagon SKS / WT0032-1 / SRVV / DTK-4M / DTKP / AC-858 / Hekate DT / TMB / TSM）
        // 和 AMXC 橡胶握把垫均为枪口/握把配件，无需工具钳（用户指定）
        // DVL-10 消音枪管枪口组合需要 Leatherman 工具钳才能安装/卸下（用户指定）
        ToolSystem.RegisterAttachmentRequiringLeatherman(Dvl10SilencedItemSystem.ItemKey);
        // MRS 反射式瞄具：以 PDC 为前提（无需工具钳）
        ToolSystem.RegisterPrerequisite(MrsItemSystem.ItemKey, PdcItemSystem.ItemKey);
        // EOTech 553 全息瞄具：以 PDC 为前提（无需工具钳）
        ToolSystem.RegisterPrerequisite(Eotech553ItemSystem.ItemKey, PdcItemSystem.ItemKey);
        // EOTech HHS-1 复合瞄具：以 PDC 为前提（无需工具钳）
        ToolSystem.RegisterPrerequisite(Hhs1ItemSystem.ItemKey, PdcItemSystem.ItemKey);
        // ELCAN SpecterDR 变倍瞄具：以 PDC 为前提（无需工具钳）
        ToolSystem.RegisterPrerequisite(SpecterDrItemSystem.ItemKey, PdcItemSystem.ItemKey);
        // Monstrum 2x32 棱镜瞄具：以 PDC 为前提（无需工具钳）
        ToolSystem.RegisterPrerequisite(Monstr2x32ItemSystem.ItemKey, PdcItemSystem.ItemKey);
        // TA01NSN / Razor HD / PM II 瞄具：以 PDC 为前提（无需工具钳）
        ToolSystem.RegisterPrerequisite(Ta01nsnItemSystem.ItemKey, PdcItemSystem.ItemKey);
        ToolSystem.RegisterPrerequisite(RazorHdItemSystem.ItemKey, PdcItemSystem.ItemKey);
        ToolSystem.RegisterPrerequisite(Pm2ItemSystem.ItemKey, PdcItemSystem.ItemKey);
        // OPFOR AA47 枪托需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(OpforAak7ItemSystem.ItemKey);
        // Kocherga 枪托需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(KochergaItemSystem.ItemKey);
        // Zhukov-S 枪托需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(ZhukovSItemSystem.ItemKey);
        // CQR47 一体式枪托需要 Leatherman 工具钳才能安装
        ToolSystem.RegisterAttachmentRequiringLeatherman(Cqr47ItemSystem.ItemKey);
        // 战术设备（手电/激光）需要先安装 MOE AKM、Hexagon AKM 或 TDI AKM-L 改装护木（任一即可，无需工具钳）
        ToolSystem.RegisterOrPrerequisite(LasTac2ItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(Klesch2UItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(BaldrProItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(TblItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        // 前握把同样可安装在 MOE AKM、Hexagon AKM 或 TDI AKM-L 改装护木上（任一即可）
        ToolSystem.RegisterOrPrerequisite(ShiftForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(Se5ForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(Rk0ForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(Rk2ForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(B25ur1ForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(CobraForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(P2ForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        ToolSystem.RegisterOrPrerequisite(AfgForegripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey, HexagonAkHandguardItemSystem.ItemKey, AkmLItemSystem.ItemKey);
        // 格洛克枪管（AW螺纹）和滑套（Viper Cut/PS9）改装需要 Leatherman 工具钳才能安装/卸下（用户指定）
        ToolSystem.RegisterAttachmentRequiringLeatherman(GlockAwlwItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(GlockViperCutItemSystem.ItemKey);
        ToolSystem.RegisterAttachmentRequiringLeatherman(GlockPs9ItemSystem.ItemKey);
        // 格洛克枪口配件：需要先装 AW螺纹枪管（GlockAwlwItemSystem）
        ToolSystem.RegisterPrerequisite(GlockG3PortItemSystem.ItemKey, GlockAwlwItemSystem.ItemKey);
        ToolSystem.RegisterPrerequisite(GlockLw9ItemSystem.ItemKey, GlockAwlwItemSystem.ItemKey);
        ToolSystem.RegisterPrerequisite(GlockOsprey9ItemSystem.ItemKey, GlockAwlwItemSystem.ItemKey);
        ToolSystem.RegisterPrerequisite(GlockSrd9ItemSystem.ItemKey, GlockAwlwItemSystem.ItemKey);
        // 格洛克瞄准镜的 UM3 基座前提已由 IsAttachmentBlockedForGlock 动态处理，
        // 不能注册为全局 AND 前提（否则其他枪装瞄具也会被要求装 UM3）。
        // 说明：握把/激光制作完成后，在此注册前提关系，例如：
        // ToolSystem.RegisterPrerequisite(GripItemSystem.ItemKey, MoeAkmItemSystem.ItemKey);
        // ToolSystem.RegisterPrerequisite(LaserItemSystem.ItemKey, MoeAkmItemSystem.ItemKey);

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
        else if (SksA5MagItemSystem.IsSksA5MagRequest(request))
            SksA5MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (SksIntegralMagItemSystem.IsSksIntegralMagRequest(request))
            SksIntegralMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (UasSksItemSystem.IsUasSksRequest(request))
            UasSksItemSystem.ConfigureSpawnedItem(item, request);
        else if (TapcoIntrafuseItemSystem.IsTapcoIntrafuseRequest(request))
            TapcoIntrafuseItemSystem.ConfigureSpawnedItem(item, request);
        else if (HexagonSksItemSystem.IsHexagonSksRequest(request))
            HexagonSksItemSystem.ConfigureSpawnedItem(item, request);
        else if (Wt0032_1ItemSystem.IsWt0032_1Request(request))
            Wt0032_1ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SksMcItemSystem.IsSksMcRequest(request))
            SksMcItemSystem.ConfigureSpawnedItem(item, request);
        else if (Mtu017ItemSystem.IsMtu017Request(request))
            Mtu017ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SrvvAkmItemSystem.IsSrvvAkmRequest(request))
            SrvvAkmItemSystem.ConfigureSpawnedItem(item, request);
        else if (Dtk4mItemSystem.IsDtk4mRequest(request))
            Dtk4mItemSystem.ConfigureSpawnedItem(item, request);
        else if (DtkpItemSystem.IsDtkpRequest(request))
            DtkpItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ac858ItemSystem.IsAc858Request(request))
            Ac858ItemSystem.ConfigureSpawnedItem(item, request);
        else if (HekateDt338ItemSystem.IsHekateDt338Request(request))
            HekateDt338ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Tmb338lmItemSystem.IsTmb338lmRequest(request))
            Tmb338lmItemSystem.ConfigureSpawnedItem(item, request);
        else if (Tsm338lmItemSystem.IsTsm338lmRequest(request))
            Tsm338lmItemSystem.ConfigureSpawnedItem(item, request);
        else if (AxmcGripItemSystem.IsAxmcGripRequest(request))
            AxmcGripItemSystem.ConfigureSpawnedItem(item, request);
        else if (Dvl10SilencedItemSystem.IsDvl10SilencedRequest(request))
            Dvl10SilencedItemSystem.ConfigureSpawnedItem(item, request);
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
        else if (AA12ItemSystem.IsAA12Request(request))
            AA12ItemSystem.ConfigureSpawnedItem(item, request);
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
        else if (X47MagItemSystem.IsX47MagRequest(request))
            X47MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (DeagleMagItemSystem.IsDeagleMagRequest(request))
            DeagleMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (Glock17MagItemSystem.IsGlock17MagRequest(request))
            Glock17MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (M4A1MagItemSystem.IsM4A1MagRequest(request))
            M4A1MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (M4A1Mag560ItemSystem.IsMag560Request(request))
            M4A1Mag560ItemSystem.ConfigureSpawnedItem(item, request);
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
        else if (AA12MagItemSystem.IsAA12MagRequest(request))
            AA12MagItemSystem.ConfigureSpawnedItem(item, request);
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
        else if (AmmoBoxItemSystem.IsAmmoBoxRequest(request))
            AmmoBoxItemSystem.ConfigureSpawnedItem(item, request);
        else if (WeaponRepairKitItemSystem.IsRepairKitRequest(request))
            WeaponRepairKitItemSystem.ConfigureSpawnedItem(item, request);
        else if (Tep300ItemSystem.IsTep300Request(request))
            Tep300ItemSystem.ConfigureSpawnedItem(item, request);
        else if (ProFlexItemSystem.IsProFlexRequest(request))
            ProFlexItemSystem.ConfigureSpawnedItem(item, request);
        else if (CrackersItemSystem.IsCrackersRequest(request))
            CrackersItemSystem.ConfigureSpawnedItem(item, request);
        else if (CroutonsItemSystem.IsCroutonsRequest(request))
            CroutonsItemSystem.ConfigureSpawnedItem(item, request);
        else if (SlickersItemSystem.IsSlickersRequest(request))
            SlickersItemSystem.ConfigureSpawnedItem(item, request);
        else if (TarkerItemSystem.IsTarkerRequest(request))
            TarkerItemSystem.ConfigureSpawnedItem(item, request);
        else if (AlyonkaItemSystem.IsAlyonkaRequest(request))
            AlyonkaItemSystem.ConfigureSpawnedItem(item, request);
        else if (SugarItemSystem.IsSugarRequest(request))
            SugarItemSystem.ConfigureSpawnedItem(item, request);
        else if (IskraItemSystem.IsIskraRequest(request))
            IskraItemSystem.ConfigureSpawnedItem(item, request);
        else if (MreItemSystem.IsMreRequest(request))
            MreItemSystem.ConfigureSpawnedItem(item, request);
        else if (PeasItemSystem.IsPeasRequest(request))
            PeasItemSystem.ConfigureSpawnedItem(item, request);
        else if (NoodlesItemSystem.IsNoodlesRequest(request))
            NoodlesItemSystem.ConfigureSpawnedItem(item, request);
        else if (CookedNoodlesItemSystem.IsCookedNoodlesRequest(request))
            CookedNoodlesItemSystem.ConfigureSpawnedItem(item, request);
        else if (TkFastMtItemSystem.IsTkFastMtRequest(request))
            TkFastMtItemSystem.ConfigureSpawnedItem(item, request);
        else if (FastVisorItemSystem.IsFastVisorRequest(request))
            FastVisorItemSystem.ConfigureSpawnedItem(item, request);
        else if (FastVisor2ItemSystem.IsFastVisor2Request(request))
            FastVisor2ItemSystem.ConfigureSpawnedItem(item, request);
        else if (HexagonAKMSuppressorItemSystem.IsHexagonAKMRequest(request))
            HexagonAKMSuppressorItemSystem.ConfigureSpawnedItem(item, request);
        else if (DynacompItemSystem.IsRequest(request))
            DynacompItemSystem.ConfigureSpawnedItem(item, request);
        else if (Dtk1ItemSystem.IsRequest(request))
            Dtk1ItemSystem.ConfigureSpawnedItem(item, request);
        else if (MoeAkmItemSystem.IsMoeAkmRequest(request))
            MoeAkmItemSystem.ConfigureSpawnedItem(item, request);
        else if (MoeSlItemSystem.IsMoeSlRequest(request))
            MoeSlItemSystem.ConfigureSpawnedItem(item, request);
        else if (ViperItemSystem.IsViperRequest(request))
            ViperItemSystem.ConfigureSpawnedItem(item, request);
        else if (KacRisItemSystem.IsKacRisRequest(request))
            KacRisItemSystem.ConfigureSpawnedItem(item, request);
        else if (SmrMk16ItemSystem.IsSmrMk16Request(request))
            SmrMk16ItemSystem.ConfigureSpawnedItem(item, request);
        else if (AdarWoodItemSystem.IsAdarWoodRequest(request))
            AdarWoodItemSystem.ConfigureSpawnedItem(item, request);
        else if (LvoaItemSystem.IsLvoaRequest(request))
            LvoaItemSystem.ConfigureSpawnedItem(item, request);
        else if (M4LongBarrelItemSystem.IsLongBarrelRequest(request))
            M4LongBarrelItemSystem.ConfigureSpawnedItem(item, request);
        else if (Rotor43ItemSystem.IsRotor43Request(request))
            Rotor43ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Nt4ItemSystem.IsNt4Request(request))
            Nt4ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SakerItemSystem.IsSakerRequest(request))
            SakerItemSystem.ConfigureSpawnedItem(item, request);
        else if (Kx3ItemSystem.IsKx3Request(request))
            Kx3ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Vp09ItemSystem.IsVp09Request(request))
            Vp09ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Rotor43762ItemSystem.IsRotor43762Request(request))
            Rotor43762ItemSystem.ConfigureSpawnedItem(item, request);
        else if (B10mB19ItemSystem.IsRequest(request))
            B10mB19ItemSystem.ConfigureSpawnedItem(item, request);
        else if (WasrItemSystem.IsRequest(request))
            WasrItemSystem.ConfigureSpawnedItem(item, request);
        else if (AkmLItemSystem.IsRequest(request))
            AkmLItemSystem.ConfigureSpawnedItem(item, request);
        else if (HexagonAkHandguardItemSystem.IsHexagonAkHandguardRequest(request))
            HexagonAkHandguardItemSystem.ConfigureSpawnedItem(item, request);
        else if (Rk3ItemSystem.IsRk3Request(request))
            Rk3ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Mg47ItemSystem.IsMg47Request(request))
            Mg47ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ags74ItemSystem.IsAgs74Request(request))
            Ags74ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Td120001ItemSystem.IsTd120001Request(request))
            Td120001ItemSystem.ConfigureSpawnedItem(item, request);
        else if (StarkArrgItemSystem.IsStarkArrgRequest(request))
            StarkArrgItemSystem.ConfigureSpawnedItem(item, request);
        else if (MiadItemSystem.IsMiadRequest(request))
            MiadItemSystem.ConfigureSpawnedItem(item, request);
        else if (F1st2pcItemSystem.IsF1st2pcRequest(request))
            F1st2pcItemSystem.ConfigureSpawnedItem(item, request);
        else if (ErgoItemSystem.IsErgoRequest(request))
            ErgoItemSystem.ConfigureSpawnedItem(item, request);
        else if (Vipermod1ItemSystem.IsVipermod1Request(request))
            Vipermod1ItemSystem.ConfigureSpawnedItem(item, request);
        else if (CtrItemSystem.IsCtrRequest(request))
            CtrItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ds150fdeItemSystem.IsDs150fdeRequest(request))
            Ds150fdeItemSystem.ConfigureSpawnedItem(item, request);
        else if (AcsItemSystem.IsAcsRequest(request))
            AcsItemSystem.ConfigureSpawnedItem(item, request);
        else if (MoefgItemSystem.IsMoefgRequest(request))
            MoefgItemSystem.ConfigureSpawnedItem(item, request);
        else if (MoefdeItemSystem.IsMoefdeRequest(request))
            MoefdeItemSystem.ConfigureSpawnedItem(item, request);
        else if (MoesgItemSystem.IsMoesgRequest(request))
            MoesgItemSystem.ConfigureSpawnedItem(item, request);
        else if (PdcItemSystem.IsPdcRequest(request))
            PdcItemSystem.ConfigureSpawnedItem(item, request);
        else if (MrsItemSystem.IsMrsRequest(request))
            MrsItemSystem.ConfigureSpawnedItem(item, request);
        else if (Eotech553ItemSystem.IsEotech553Request(request))
            Eotech553ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Hhs1ItemSystem.IsHhs1Request(request))
            Hhs1ItemSystem.ConfigureSpawnedItem(item, request);
        else if (SpecterDrItemSystem.IsSpecterDrRequest(request))
            SpecterDrItemSystem.ConfigureSpawnedItem(item, request);
        else if (Monstr2x32ItemSystem.IsMonstr2x32Request(request))
            Monstr2x32ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Ta01nsnItemSystem.IsTa01nsnRequest(request))
            Ta01nsnItemSystem.ConfigureSpawnedItem(item, request);
        else if (RazorHdItemSystem.IsRazorHdRequest(request))
            RazorHdItemSystem.ConfigureSpawnedItem(item, request);
        else if (Pm2ItemSystem.IsPm2Request(request))
            Pm2ItemSystem.ConfigureSpawnedItem(item, request);
        else if (OpforAak7ItemSystem.IsOpforAak7Request(request))
            OpforAak7ItemSystem.ConfigureSpawnedItem(item, request);
        else if (KochergaItemSystem.IsKochergaRequest(request))
            KochergaItemSystem.ConfigureSpawnedItem(item, request);
        else if (ZhukovSItemSystem.IsZhukovSRequest(request))
            ZhukovSItemSystem.ConfigureSpawnedItem(item, request);
        else if (Cqr47ItemSystem.IsCqr47Request(request))
            Cqr47ItemSystem.ConfigureSpawnedItem(item, request);
        else if (LasTac2ItemSystem.IsLasTac2Request(request))
            LasTac2ItemSystem.ConfigureSpawnedItem(item, request);
        else if (Klesch2UItemSystem.IsKlesch2URequest(request))
            Klesch2UItemSystem.ConfigureSpawnedItem(item, request);
        else if (BaldrProItemSystem.IsBaldrProRequest(request))
            BaldrProItemSystem.ConfigureSpawnedItem(item, request);
        else if (TblItemSystem.IsTblRequest(request))
            TblItemSystem.ConfigureSpawnedItem(item, request);
        else if (ShiftForegripItemSystem.IsRequest(request))
            ShiftForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (Se5ForegripItemSystem.IsRequest(request))
            Se5ForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (Rk0ForegripItemSystem.IsRequest(request))
            Rk0ForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (Rk2ForegripItemSystem.IsRequest(request))
            Rk2ForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (B25ur1ForegripItemSystem.IsRequest(request))
            B25ur1ForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (CobraForegripItemSystem.IsRequest(request))
            CobraForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (P2ForegripItemSystem.IsRequest(request))
            P2ForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (AfgForegripItemSystem.IsRequest(request))
            AfgForegripItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockViperCutItemSystem.IsViperCutRequest(request))
            GlockViperCutItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockPs9ItemSystem.IsPs9Request(request))
            GlockPs9ItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockUm3ItemSystem.IsUm3Request(request))
            GlockUm3ItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockAwlwItemSystem.IsAwlwRequest(request))
            GlockAwlwItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockG3PortItemSystem.IsG3PortRequest(request))
            GlockG3PortItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockLw9ItemSystem.IsLw9Request(request))
            GlockLw9ItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockOsprey9ItemSystem.IsOsprey9Request(request))
            GlockOsprey9ItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockSrd9ItemSystem.IsSrd9Request(request))
            GlockSrd9ItemSystem.ConfigureSpawnedItem(item, request);
        else if (DeltaPointItemSystem.IsDpRequest(request))
            DeltaPointItemSystem.ConfigureSpawnedItem(item, request);
        else if (AcroP1ItemSystem.IsAcroP1Request(request))
            AcroP1ItemSystem.ConfigureSpawnedItem(item, request);
        else if (P90AttenuatorItemSystem.IsP90AttenuatorRequest(request))
            P90AttenuatorItemSystem.ConfigureSpawnedItem(item, request);
        else if (UmpOemItemSystem.IsUmpOemRequest(request))
            UmpOemItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockBigStickMagItemSystem.IsBigStickMagRequest(request))
            GlockBigStickMagItemSystem.ConfigureSpawnedItem(item, request);
        else if (GlockG50MagItemSystem.IsG50MagRequest(request))
            GlockG50MagItemSystem.ConfigureSpawnedItem(item, request);
        else if (BlueAreaKeycardItemSystem.IsKeycardRequest(request))
            BlueAreaKeycardItemSystem.ConfigureSpawnedItem(item, request);
        else if (RedAreaKeycardItemSystem.IsKeycardRequest(request))
            RedAreaKeycardItemSystem.ConfigureSpawnedItem(item, request);
        else if (WeaponRoomKeycardItemSystem.IsKeycardRequest(request))
            WeaponRoomKeycardItemSystem.ConfigureSpawnedItem(item, request);
        else if (LeathermanItemSystem.IsLeathermanRequest(request))
            LeathermanItemSystem.ConfigureSpawnedItem(item, request);
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
        SksA5MagItemSystem.EnsureRegisteredInItemTable();
        SksIntegralMagItemSystem.EnsureRegisteredInItemTable();
        UasSksItemSystem.EnsureRegisteredInItemTable();
        TapcoIntrafuseItemSystem.EnsureRegisteredInItemTable();
        HexagonSksItemSystem.EnsureRegisteredInItemTable();
        Wt0032_1ItemSystem.EnsureRegisteredInItemTable();
        SksMcItemSystem.EnsureRegisteredInItemTable();
        Mtu017ItemSystem.EnsureRegisteredInItemTable();
        SrvvAkmItemSystem.EnsureRegisteredInItemTable();
        Dtk4mItemSystem.EnsureRegisteredInItemTable();
        DtkpItemSystem.EnsureRegisteredInItemTable();
        Ac858ItemSystem.EnsureRegisteredInItemTable();
        HekateDt338ItemSystem.EnsureRegisteredInItemTable();
        Tmb338lmItemSystem.EnsureRegisteredInItemTable();
        Tsm338lmItemSystem.EnsureRegisteredInItemTable();
        AxmcGripItemSystem.EnsureRegisteredInItemTable();
        Dvl10SilencedItemSystem.EnsureRegisteredInItemTable();
        AXMCItemSystem.EnsureRegisteredInItemTable();
        DVL10ItemSystem.EnsureRegisteredInItemTable();
        AKMItemSystem.EnsureRegisteredInItemTable();
        AXMCMagItemSystem.EnsureRegisteredInItemTable();
        DVL10MagItemSystem.EnsureRegisteredInItemTable();
        AKMMagItemSystem.EnsureRegisteredInItemTable();
        X47MagItemSystem.EnsureRegisteredInItemTable();
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
        M4A1Mag560ItemSystem.EnsureRegisteredInItemTable();
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
        AA12ItemSystem.EnsureRegisteredInItemTable();
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
        AA12MagItemSystem.EnsureRegisteredInItemTable();
        Ammo939SP5ItemSystem.EnsureRegisteredInItemTable();
        AmmoBoxItemSystem.EnsureRegisteredInItemTable();
        WeaponRepairKitItemSystem.EnsureRegisteredInItemTable();
        Tep300ItemSystem.EnsureRegisteredInItemTable();
        ProFlexItemSystem.EnsureRegisteredInItemTable();
        CrackersItemSystem.EnsureRegisteredInItemTable();
        CroutonsItemSystem.EnsureRegisteredInItemTable();
        SlickersItemSystem.EnsureRegisteredInItemTable();
        TarkerItemSystem.EnsureRegisteredInItemTable();
        AlyonkaItemSystem.EnsureRegisteredInItemTable();
        SugarItemSystem.EnsureRegisteredInItemTable();
        IskraItemSystem.EnsureRegisteredInItemTable();
        MreItemSystem.EnsureRegisteredInItemTable();
        PeasItemSystem.EnsureRegisteredInItemTable();
        NoodlesItemSystem.EnsureRegisteredInItemTable();
        CookedNoodlesItemSystem.EnsureRegisteredInItemTable();
        TkFastMtItemSystem.EnsureRegisteredInItemTable();
        FastVisorItemSystem.EnsureRegisteredInItemTable();
        FastVisor2ItemSystem.EnsureRegisteredInItemTable();
        HexagonAKMSuppressorItemSystem.EnsureRegisteredInItemTable();
        DynacompItemSystem.EnsureRegisteredInItemTable();
        Dtk1ItemSystem.EnsureRegisteredInItemTable();
        MoeAkmItemSystem.EnsureRegisteredInItemTable();
        MoeSlItemSystem.EnsureRegisteredInItemTable();
        ViperItemSystem.EnsureRegisteredInItemTable();
        KacRisItemSystem.EnsureRegisteredInItemTable();
        SmrMk16ItemSystem.EnsureRegisteredInItemTable();
        AdarWoodItemSystem.EnsureRegisteredInItemTable();
        LvoaItemSystem.EnsureRegisteredInItemTable();
        M4LongBarrelItemSystem.EnsureRegisteredInItemTable();
        Rotor43ItemSystem.EnsureRegisteredInItemTable();
        Nt4ItemSystem.EnsureRegisteredInItemTable();
        SakerItemSystem.EnsureRegisteredInItemTable();
        Kx3ItemSystem.EnsureRegisteredInItemTable();
        Vp09ItemSystem.EnsureRegisteredInItemTable();
        Rotor43762ItemSystem.EnsureRegisteredInItemTable();
        HexagonAkHandguardItemSystem.EnsureRegisteredInItemTable();
        B10mB19ItemSystem.EnsureRegisteredInItemTable();
        WasrItemSystem.EnsureRegisteredInItemTable();
        AkmLItemSystem.EnsureRegisteredInItemTable();
        Rk3ItemSystem.EnsureRegisteredInItemTable();
        Mg47ItemSystem.EnsureRegisteredInItemTable();
        Ags74ItemSystem.EnsureRegisteredInItemTable();
        Td120001ItemSystem.EnsureRegisteredInItemTable();
        StarkArrgItemSystem.EnsureRegisteredInItemTable();
        MiadItemSystem.EnsureRegisteredInItemTable();
        F1st2pcItemSystem.EnsureRegisteredInItemTable();
        ErgoItemSystem.EnsureRegisteredInItemTable();
        Vipermod1ItemSystem.EnsureRegisteredInItemTable();
        CtrItemSystem.EnsureRegisteredInItemTable();
        Ds150fdeItemSystem.EnsureRegisteredInItemTable();
        AcsItemSystem.EnsureRegisteredInItemTable();
        MoefgItemSystem.EnsureRegisteredInItemTable();
        MoefdeItemSystem.EnsureRegisteredInItemTable();
        MoesgItemSystem.EnsureRegisteredInItemTable();
        PdcItemSystem.EnsureRegisteredInItemTable();
        MrsItemSystem.EnsureRegisteredInItemTable();
        Eotech553ItemSystem.EnsureRegisteredInItemTable();
        Hhs1ItemSystem.EnsureRegisteredInItemTable();
        SpecterDrItemSystem.EnsureRegisteredInItemTable();
        Monstr2x32ItemSystem.EnsureRegisteredInItemTable();
        Ta01nsnItemSystem.EnsureRegisteredInItemTable();
        RazorHdItemSystem.EnsureRegisteredInItemTable();
        Pm2ItemSystem.EnsureRegisteredInItemTable();
        OpforAak7ItemSystem.EnsureRegisteredInItemTable();
        KochergaItemSystem.EnsureRegisteredInItemTable();
        ZhukovSItemSystem.EnsureRegisteredInItemTable();
        Cqr47ItemSystem.EnsureRegisteredInItemTable();
        LasTac2ItemSystem.EnsureRegisteredInItemTable();
        Klesch2UItemSystem.EnsureRegisteredInItemTable();
        BaldrProItemSystem.EnsureRegisteredInItemTable();
        TblItemSystem.EnsureRegisteredInItemTable();
        ShiftForegripItemSystem.EnsureRegisteredInItemTable();
        Se5ForegripItemSystem.EnsureRegisteredInItemTable();
        Rk0ForegripItemSystem.EnsureRegisteredInItemTable();
        Rk2ForegripItemSystem.EnsureRegisteredInItemTable();
        B25ur1ForegripItemSystem.EnsureRegisteredInItemTable();
        CobraForegripItemSystem.EnsureRegisteredInItemTable();
        P2ForegripItemSystem.EnsureRegisteredInItemTable();
        AfgForegripItemSystem.EnsureRegisteredInItemTable();
        LeathermanItemSystem.EnsureRegisteredInItemTable();
        WeaponRoomKeycardItemSystem.EnsureRegisteredInItemTable();
        BlueAreaKeycardItemSystem.EnsureRegisteredInItemTable();
        RedAreaKeycardItemSystem.EnsureRegisteredInItemTable();
        GlockViperCutItemSystem.EnsureRegisteredInItemTable();
        GlockPs9ItemSystem.EnsureRegisteredInItemTable();
        GlockUm3ItemSystem.EnsureRegisteredInItemTable();
        GlockAwlwItemSystem.EnsureRegisteredInItemTable();
        GlockG3PortItemSystem.EnsureRegisteredInItemTable();
        GlockLw9ItemSystem.EnsureRegisteredInItemTable();
        GlockOsprey9ItemSystem.EnsureRegisteredInItemTable();
        GlockSrd9ItemSystem.EnsureRegisteredInItemTable();
        DeltaPointItemSystem.EnsureRegisteredInItemTable();
        AcroP1ItemSystem.EnsureRegisteredInItemTable();
        P90AttenuatorItemSystem.EnsureRegisteredInItemTable();
        UmpOemItemSystem.EnsureRegisteredInItemTable();
        GlockBigStickMagItemSystem.EnsureRegisteredInItemTable();
        GlockG50MagItemSystem.EnsureRegisteredInItemTable();

        // 通知集成模式武器物品已注册到 GlobalItems
        Plugin.IntegrationMode?.OnItemsSetup();
    }
}
