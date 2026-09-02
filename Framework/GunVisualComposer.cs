using System;
using System.Collections.Generic;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 枪械配件视觉合成器（方案 A 的无抖动终极实现）。
///
/// 把配件像素在运行时直接 alpha 合成进主枪械贴图，生成一张**新的完整枪械贴图**，
/// 替换 GunScript.normalSprite 等 4 个状态引用。配件与枪械变成同一张图，
/// 任何来源的抖动（帧动画 / transform 旋转）都作用于整张图，物理上不可能不同步。
///
/// 合成时机（一次性，非每帧）：安装/卸下配件、装/卸弹鼓、存档恢复。
/// </summary>
public static class GunVisualComposer
{
    public const string SuppressorLayerName = "hexagonakm";
    public const string HandguardLayerName = "moeakm";

    // 护木相对枪械 transform（pivot 点）的像素偏移（PPI 14）：
    // 用户指定：AKM 中间往右 22px、往上 2px。
    public const float HandguardOffsetPxX = 22f;
    public const float HandguardOffsetPxY = 2f;

    // 枪托位置：枪械最尾部（AKM 左端）。X 负方向=尾部。
    private const float StockOffsetPxX = -13f;
    // CQR47 一体式枪托：相对普通枪托整体偏移。
    // 往右 4px（再往右 2px），向下 3px。
    private const float CqrStockOffsetPxX = 4f;
    private const float CqrStockOffsetPxY = -3f;
    private const float StockOffsetPxY = 0f;

    /// <summary>纹理叠加层（消音器、护木、手电、激光器、枪托等配件）。</summary>
    internal sealed class OverlayLayer
    {
        public Texture2D Texture = null!;
        public int CenterX;   // 配件中心在基础贴图纹理中的 X（像素，相对左下角）
        public int CenterY;   // 配件中心在基础贴图纹理中的 Y（像素，相对左下角，向上为正）
        public float Scale = 1f; // 配件绘制缩放（>1 放大，中心不变）。用于 SKS 等大尺寸枪械贴图上的配件
        // 绘制裁剪（可选）：只绘制贴图 x < CropRightX 的部分（相对贴图坐标，未缩放）。
        // 用于 Tapco 等覆盖范围过大的配件，避免盖住弹匣/弹仓区域。
        public float? CropRightX;
        public bool HasErase; // 是否在绘制前擦除（清透明）下方矩形区域
        public int EraseW;    // 擦除矩形宽（以 center 为中心）
        public int EraseH;    // 擦除矩形高（以 center 为中心）
        // 不对称擦除边界（可选）：若设置 EraseLeftX/RightX/TopY/BotY（绝对值），则优先用它们，
        // 否则回退到以 center 为中心的对称 EraseW/EraseH。
        public float? EraseLeftX;
        public float? EraseRightX;
        public float? EraseTopY;
        public float? EraseBotY;
        // 三角形切除（可选）：擦除区域右侧不再是垂直边界，而是从右上角斜切到右下角，
        // 形成三角形缺口——用于贴合 AKM 后握把轮廓，避免矩形右边界切掉握把。
        // 当 EraseTriangleRight = true 时，右侧斜边从 (EraseRightX, EraseTopY) 到
        // (EraseRightX - EraseTriangleWidth, EraseBotY) 线性收窄。
        public bool EraseTriangleRight;
        public float EraseTriangleWidth;
        // 右边界向下线性扩展（可选）：擦除区域右边界从顶部到底部线性向右扩展
        // EraseRightExpandBottom 像素（顶部不扩展，底部全量扩展）。
        // 用于一体式枪托：下部（握把处）裁切比上部更靠右。
        public float EraseRightExpandBottom;
    }

    /// <summary>
    /// 根据当前配件/弹鼓状态，重新合成枪械的 4 个状态贴图并刷新渲染。
    /// </summary>
    public static void Rebuild(Item gunItem)
    {
        if (gunItem == null) return;
        try
        {
            var gun = gunItem.GetComponent<GunScript>();
            if (gun == null) return;

            // 配件视觉合成适用于 AKM / SKS / AXMC / 沙鹰（护木/枪托/手电/枪口/瞄准镜等配件）。
            // 其他枪械装弹匣时不应替换为基础贴图。
            bool isAkm = string.Equals(gunItem.id, AKMItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isSks = string.Equals(gunItem.id, SKSItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isAxmc = string.Equals(gunItem.id, AXMCItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isDeagle = string.Equals(gunItem.id, DeagleItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isDvl10 = string.Equals(gunItem.id, DVL10ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isGlock = string.Equals(gunItem.id, Glock17ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isP90 = string.Equals(gunItem.id, P90ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool isUmp = string.Equals(gunItem.id, UMP45ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            if (!isAkm && !isSks && !isAxmc && !isDeagle && !isDvl10 && !isGlock && !isP90 && !isUmp)
                return;

            var holder = gunItem.GetComponent<GunAttachmentHolder>();

            bool HasAttachment(string id)
                => holder != null && holder.attachmentIds != null && holder.attachmentIds.Contains(id);

            bool isDrum = holder != null
                && holder.currentMagId.Equals(X47MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool hasSuppressor = HasAttachment(HexagonAKMSuppressorItemSystem.ItemKey)
                || HasAttachment(Rotor43762ItemSystem.ItemKey)
                || HasAttachment(HexagonSksItemSystem.ItemKey);
            bool hasWt0032 = HasAttachment(Wt0032_1ItemSystem.ItemKey);
            bool hasHandguard = HasAttachment(MoeAkmItemSystem.ItemKey);
            bool hasHexagonHg = HasAttachment(HexagonAkHandguardItemSystem.ItemKey);
            bool hasRk3 = HasAttachment(Rk3ItemSystem.ItemKey);
            bool hasMg47 = HasAttachment(Mg47ItemSystem.ItemKey);
            bool hasAgs74 = HasAttachment(Ags74ItemSystem.ItemKey);
            bool hasPdc = HasAttachment(PdcItemSystem.ItemKey);
            bool hasMtu017 = HasAttachment(Mtu017ItemSystem.ItemKey);
            bool hasMrs = HasAttachment(MrsItemSystem.ItemKey);
            bool hasEotech553 = HasAttachment(Eotech553ItemSystem.ItemKey);
            bool hasDp = HasAttachment(DeltaPointItemSystem.ItemKey);
            bool hasAcroP1 = HasAttachment(AcroP1ItemSystem.ItemKey);
            bool hasHhs1 = HasAttachment(Hhs1ItemSystem.ItemKey);
            bool hasSpecterDr = HasAttachment(SpecterDrItemSystem.ItemKey);
            bool hasMonstr = HasAttachment(Monstr2x32ItemSystem.ItemKey);
            bool hasTa01nsn = HasAttachment(Ta01nsnItemSystem.ItemKey);
            bool hasRazorHd = HasAttachment(RazorHdItemSystem.ItemKey);
            bool hasPm2 = HasAttachment(Pm2ItemSystem.ItemKey);
            bool hasLasTac2 = HasAttachment(LasTac2ItemSystem.ItemKey);
            bool hasKlesch2U = HasAttachment(Klesch2UItemSystem.ItemKey);
            bool hasBaldrPro = HasAttachment(BaldrProItemSystem.ItemKey);
            bool hasTbl = HasAttachment(TblItemSystem.ItemKey);
            bool hasOpfor = HasAttachment(OpforAak7ItemSystem.ItemKey);
            bool hasKocherga = HasAttachment(KochergaItemSystem.ItemKey);
            bool hasZhukovS = HasAttachment(ZhukovSItemSystem.ItemKey);
            bool hasCqr47 = HasAttachment(Cqr47ItemSystem.ItemKey);
            // M4 系列后托（SKS+Tapco 可装）
            bool hasVipermod1 = HasAttachment(Vipermod1ItemSystem.ItemKey);
            bool hasCtr = HasAttachment(CtrItemSystem.ItemKey);
            bool hasDs150fde = HasAttachment(Ds150fdeItemSystem.ItemKey);
            bool hasAcs = HasAttachment(AcsItemSystem.ItemKey);
            bool hasMoefg = HasAttachment(MoefgItemSystem.ItemKey);
            bool hasMoefde = HasAttachment(MoefdeItemSystem.ItemKey);
            bool hasMoesg = HasAttachment(MoesgItemSystem.ItemKey);
            bool hasShift = HasAttachment(ShiftForegripItemSystem.ItemKey);
            bool hasSe5 = HasAttachment(Se5ForegripItemSystem.ItemKey);
            bool hasRk0 = HasAttachment(Rk0ForegripItemSystem.ItemKey);
            bool hasRk2 = HasAttachment(Rk2ForegripItemSystem.ItemKey);
            bool hasB25ur1 = HasAttachment(B25ur1ForegripItemSystem.ItemKey);
            bool hasCobra = HasAttachment(CobraForegripItemSystem.ItemKey);
            bool hasP2 = HasAttachment(P2ForegripItemSystem.ItemKey);
            bool hasAfg = HasAttachment(AfgForegripItemSystem.ItemKey);
            bool hasB10mB19 = HasAttachment(B10mB19ItemSystem.ItemKey);
            bool hasWasr = HasAttachment(WasrItemSystem.ItemKey);
            bool hasAkmL = HasAttachment(AkmLItemSystem.ItemKey);
            bool hasDynacomp = HasAttachment(DynacompItemSystem.ItemKey);
            bool hasDtk1 = HasAttachment(Dtk1ItemSystem.ItemKey);
            bool hasSrvvAkm = HasAttachment(SrvvAkmItemSystem.ItemKey);
            bool hasDtk4m = HasAttachment(Dtk4mItemSystem.ItemKey);
            bool hasDtkp = HasAttachment(DtkpItemSystem.ItemKey);

            // 1. 基础贴图（有弹匣态 / 无弹匣态）
            Sprite? baseWithMag;
            Sprite? baseNoMag;
            if (isSks)
            {
                // SKS：基础贴图按当前供弹方式选择（sks_10 / sks_magout / sks_sksa5），
                // 与 UpdateSksVisual 的判定一致，保证卸弹仓/装弹匣后基础贴图正确。
                baseWithMag = SKSItemSystem.GetCurrentBaseSprite(gunItem);
                baseNoMag = baseWithMag;
                // 若已装 UAS，基础贴图用带 UAS 的合成（见下方 UAS 层）
            }
            else if (isAxmc)
            {
                baseWithMag = AXMCItemSystem.TryLoadIconPublic();
                baseNoMag = baseWithMag;
            }
            else if (isDeagle)
            {
                baseWithMag = DeagleItemSystem.TryLoadIconPublic();
                baseNoMag = baseWithMag;
            }
            else if (isGlock)
            {
                // 格洛克：基础贴图按当前弹匣选择（原版 glock.png / Big Stick / G 50发弹鼓）
                bool isBigStick = holder != null
                    && holder.currentMagId.Equals(GlockBigStickMagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
                bool isG50 = holder != null
                    && holder.currentMagId.Equals(GlockG50MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
                if (isBigStick)
                    baseWithMag = GlockBigStickMagItemSystem.TryLoadGunIconPublic() ?? Glock17ItemSystem.TryLoadIconPublic();
                else if (isG50)
                    baseWithMag = GlockG50MagItemSystem.TryLoadGunIconPublic() ?? Glock17ItemSystem.TryLoadIconPublic();
                else
                    baseWithMag = Glock17ItemSystem.TryLoadIconPublic();
                baseNoMag = Glock17ItemSystem.TryLoadNoMagIconPublic() ?? baseWithMag;
            }
            else if (isP90)
            {
                baseWithMag = P90ItemSystem.TryLoadIconPublic();
                baseNoMag = P90ItemSystem.TryLoadNoMagIconPublic() ?? baseWithMag;
            }
            else if (isUmp)
            {
                baseWithMag = UMP45ItemSystem.TryLoadIconPublic();
                baseNoMag = UMP45ItemSystem.TryLoadNoMagIconPublic() ?? baseWithMag;
            }
            else if (isDvl10)
            {
                // DVL-10：装消音套件时替换整枪贴图为消音版
                if (HasAttachment(Dvl10SilencedItemSystem.ItemKey))
                {
                    baseWithMag = Dvl10SilencedItemSystem.TryLoadSilencedGunIcon() ?? DVL10ItemSystem.TryLoadIconPublic();
                    baseNoMag = Dvl10SilencedItemSystem.TryLoadSilencedGunNoMagIcon() ?? baseWithMag;
                }
                else
                {
                    baseWithMag = DVL10ItemSystem.TryLoadIconPublic();
                    baseNoMag = baseWithMag;
                }
            }
            else if (isDrum)
            {
                baseWithMag = AKMItemSystem.TryLoadDrumIconPublic() ?? AKMItemSystem.TryLoadIconPublic();
                baseNoMag = baseWithMag;
            }
            else
            {
                baseWithMag = AKMItemSystem.TryLoadIconPublic();
                baseNoMag = AKMItemSystem.TryLoadNoMagIconPublic() ?? baseWithMag;
            }
            if (baseWithMag == null) return;

            // 2. 收集配件叠加层
            var overlays = new List<OverlayLayer>();
            // UMP：按用户给定贴图坐标合成前握把/手电/瞄准镜/UMP OEM
            if (isUmp)
            {
                if (HasAttachment(UmpOemItemSystem.ItemKey))
                    AddUmpOemLayer(overlays, gun, baseWithMag, UmpOemItemSystem.TryLoadOverlayTexturePublic());
                if (hasShift) AddUmpForegripLayer(overlays, baseWithMag, ShiftForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasSe5) AddUmpForegripLayer(overlays, baseWithMag, Se5ForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasRk0) AddUmpForegripLayer(overlays, baseWithMag, Rk0ForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasRk2) AddUmpForegripLayer(overlays, baseWithMag, Rk2ForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasB25ur1) AddUmpForegripLayer(overlays, baseWithMag, B25ur1ForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasCobra) AddUmpForegripLayer(overlays, baseWithMag, CobraForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasP2) AddUmpForegripLayer(overlays, baseWithMag, P2ForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasAfg) AddUmpForegripLayer(overlays, baseWithMag, AfgForegripItemSystem.TryLoadOverlayTexturePublic());
                if (hasLasTac2) AddUmpTacticalLayer(overlays, baseWithMag, LasTac2ItemSystem.TryLoadOverlayTexturePublic());
                if (hasKlesch2U) AddUmpTacticalLayer(overlays, baseWithMag, Klesch2UItemSystem.TryLoadOverlayTexturePublic());
                if (hasBaldrPro) AddUmpTacticalLayer(overlays, baseWithMag, BaldrProItemSystem.TryLoadOverlayTexturePublic());
                if (hasTbl) AddUmpTacticalLayer(overlays, baseWithMag, TblItemSystem.TryLoadOverlayTexturePublic());
                if (hasMrs) AddUmpSightLayer(overlays, baseWithMag, MrsItemSystem.TryLoadOverlayTexturePublic());
                if (hasEotech553) AddUmpSightLayer(overlays, baseWithMag, Eotech553ItemSystem.TryLoadOverlayTexturePublic());
                if (hasDp) AddUmpSightLayer(overlays, baseWithMag, DeltaPointItemSystem.TryLoadOverlayTexturePublic());
                if (hasAcroP1) AddUmpSightLayer(overlays, baseWithMag, AcroP1ItemSystem.TryLoadOverlayTexturePublic());
                if (hasHhs1) AddUmpSightLayer(overlays, baseWithMag, Hhs1ItemSystem.TryLoadOverlayTexturePublic());
                if (hasSpecterDr) AddUmpSightLayer(overlays, baseWithMag, SpecterDrItemSystem.TryLoadOverlayTexturePublic());
                if (hasMonstr) AddUmpSightLayer(overlays, baseWithMag, Monstr2x32ItemSystem.TryLoadOverlayTexturePublic());
                if (hasTa01nsn) AddUmpSightLayer(overlays, baseWithMag, Ta01nsnItemSystem.TryLoadOverlayTexturePublic());
            }

            if (!isUmp)
            {
            if (isSks && HasAttachment(UasSksItemSystem.ItemKey))
                AddUasSksLayer(overlays, baseWithMag);
            if (isSks && HasAttachment(TapcoIntrafuseItemSystem.ItemKey))
                AddTapcoIntrafuseLayer(overlays, baseWithMag);
            if (isSks && HasAttachment(SksMcItemSystem.ItemKey))
                AddSksMcLayer(overlays, baseWithMag);
            if (isSks && HasAttachment(Mtu017ItemSystem.ItemKey))
                AddMtu017Layer(overlays, baseWithMag);
            if (hasSuppressor) AddSuppressorLayer(overlays, gun, baseWithMag);
            // WT0032-1 螺纹转换器：装了其他膛口装置（DTK-1 等）时被替换，不叠加显示
            if (hasWt0032 && !HasMuzzleDeviceOnWt0032(gunItem))
                AddWt0032Layer(overlays, baseWithMag);
            if (hasDynacomp) AddMuzzleLayer(overlays, gun, baseWithMag, DynacompItemSystem.TryLoadOverlayTexturePublic());
            if (hasDtk1) AddMuzzleLayer(overlays, gun, baseWithMag, Dtk1ItemSystem.TryLoadOverlayTexturePublic());
            if (hasSrvvAkm) AddMuzzleLayer(overlays, gun, baseWithMag, SrvvAkmItemSystem.TryLoadOverlayTexturePublic());
            if (hasDtk4m) AddMuzzleLayer(overlays, gun, baseWithMag, Dtk4mItemSystem.TryLoadOverlayTexturePublic());
            if (hasDtkp) AddMuzzleLayer(overlays, gun, baseWithMag, DtkpItemSystem.TryLoadOverlayTexturePublic());
            // 格洛克套筒/基座（90x82 与格洛克同尺寸，直接覆盖叠加）
            if (isGlock && HasAttachment(GlockViperCutItemSystem.ItemKey))
                AddGlockOverlayLayer(overlays, baseWithMag, GlockViperCutItemSystem.TryLoadOverlayTexturePublic());
            if (isGlock && HasAttachment(GlockPs9ItemSystem.ItemKey))
                AddGlockOverlayLayer(overlays, baseWithMag, GlockPs9ItemSystem.TryLoadOverlayTexturePublic());
            if (isGlock && HasAttachment(GlockUm3ItemSystem.ItemKey))
                AddGlockUm3Layer(overlays, baseWithMag, GlockUm3ItemSystem.TryLoadOverlayTexturePublic());
            // 格洛克枪口配件（28x13 小贴图，叠加在枪口位置）
            if (isGlock && HasAttachment(GlockG3PortItemSystem.ItemKey))
                AddGlockMuzzleLayer(overlays, gun, baseWithMag, GlockG3PortItemSystem.TryLoadOverlayTexturePublic());
            if (isGlock && HasAttachment(GlockLw9ItemSystem.ItemKey))
                AddGlockMuzzleLayer(overlays, gun, baseWithMag, GlockLw9ItemSystem.TryLoadOverlayTexturePublic());
            if (isGlock && HasAttachment(GlockOsprey9ItemSystem.ItemKey))
                AddGlockMuzzleLayer(overlays, gun, baseWithMag, GlockOsprey9ItemSystem.TryLoadOverlayTexturePublic());
            if (isGlock && HasAttachment(GlockSrd9ItemSystem.ItemKey))
                AddGlockMuzzleLayer(overlays, gun, baseWithMag, GlockSrd9ItemSystem.TryLoadOverlayTexturePublic());
            // P90 枪口消音器（Attenuator）
            if (isP90 && HasAttachment(P90AttenuatorItemSystem.ItemKey))
                AddP90MuzzleLayer(overlays, gun, baseWithMag, P90AttenuatorItemSystem.TryLoadOverlayTexturePublic());
            // AXMC 枪口配件（AC-858 / Hekate DT / TMB 338LM / TSM 338LM）
            if (isAxmc && HasAttachment(Ac858ItemSystem.ItemKey))
                AddMuzzleLayer(overlays, gun, baseWithMag, Ac858ItemSystem.TryLoadOverlayTexturePublic());
            if (isAxmc && HasAttachment(HekateDt338ItemSystem.ItemKey))
                AddMuzzleLayer(overlays, gun, baseWithMag, HekateDt338ItemSystem.TryLoadOverlayTexturePublic());
            if (isAxmc && HasAttachment(Tmb338lmItemSystem.ItemKey))
                AddMuzzleLayer(overlays, gun, baseWithMag, Tmb338lmItemSystem.TryLoadOverlayTexturePublic());
            if (isAxmc && HasAttachment(Tsm338lmItemSystem.ItemKey))
                AddMuzzleLayer(overlays, gun, baseWithMag, Tsm338lmItemSystem.TryLoadOverlayTexturePublic());
            if (hasHandguard) AddHandguardLayer(overlays, baseWithMag);
            if (hasHexagonHg) AddHexagonAkHandguardLayer(overlays, baseWithMag);
            if (hasB10mB19) AddB10mB19Layer(overlays, baseWithMag);
            if (hasWasr) AddWasrLayer(overlays, baseWithMag);
            if (hasAkmL) AddAkmLLayer(overlays, baseWithMag);
            if (hasRk3) AddRk3Layer(overlays, baseWithMag);
            if (hasMg47) AddMg47Layer(overlays, baseWithMag);
            if (hasAgs74) AddAgs74Layer(overlays, baseWithMag);
            if (hasPdc) AddPdcLayer(overlays, baseWithMag);
            if (hasMrs) AddMrsLayer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasEotech553) AddEotech553Layer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasDp) AddDpLayer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasAcroP1) AddAcroP1Layer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasHhs1) AddHhs1Layer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasSpecterDr) AddSpecterDrLayer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasMonstr) AddMonstrLayer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasTa01nsn) AddTa01nsnLayer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasRazorHd) AddRazorHdLayer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasPm2) AddPm2Layer(overlays, baseWithMag, hasMtu017, isDvl10);
            if (hasLasTac2) AddLasTac2Layer(overlays, baseWithMag, isDvl10);
            if (hasKlesch2U) AddKlesch2ULayer(overlays, baseWithMag, isDvl10);
            if (hasBaldrPro) AddBaldrProLayer(overlays, baseWithMag, isDvl10);
            if (hasTbl) AddTblLayer(overlays, baseWithMag, isDvl10);
            if (hasOpfor) AddOpforLayer(overlays, baseWithMag);
            if (hasKocherga) AddKochergaLayer(overlays, baseWithMag);
            if (hasZhukovS) AddZhukovSLayer(overlays, baseWithMag);
            if (hasCqr47) AddCqr47Layer(overlays, baseWithMag);
            // M4 系列后托（SKS+Tapco 场景）
            if (hasVipermod1) AddM4StockLayer(overlays, baseWithMag, Vipermod1ItemSystem.TryLoadOverlayTexturePublic());
            if (hasCtr) AddM4StockLayer(overlays, baseWithMag, CtrItemSystem.TryLoadOverlayTexturePublic());
            if (hasDs150fde) AddM4StockLayer(overlays, baseWithMag, Ds150fdeItemSystem.TryLoadOverlayTexturePublic());
            if (hasAcs) AddM4StockLayer(overlays, baseWithMag, AcsItemSystem.TryLoadOverlayTexturePublic());
            if (hasMoefg) AddM4StockLayer(overlays, baseWithMag, MoefgItemSystem.TryLoadOverlayTexturePublic());
            if (hasMoefde) AddM4StockLayer(overlays, baseWithMag, MoefdeItemSystem.TryLoadOverlayTexturePublic());
            if (hasMoesg) AddM4StockLayer(overlays, baseWithMag, MoesgItemSystem.TryLoadOverlayTexturePublic());
            if (hasShift) AddForegripLayer(overlays, baseWithMag, ShiftForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasSe5) AddForegripLayer(overlays, baseWithMag, Se5ForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasRk0) AddForegripLayer(overlays, baseWithMag, Rk0ForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasRk2) AddForegripLayer(overlays, baseWithMag, Rk2ForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasB25ur1) AddForegripLayer(overlays, baseWithMag, B25ur1ForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasCobra) AddForegripLayer(overlays, baseWithMag, CobraForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasP2) AddForegripLayer(overlays, baseWithMag, P2ForegripItemSystem.TryLoadOverlayTexturePublic());
            if (hasAfg) AddForegripLayer(overlays, baseWithMag, AfgForegripItemSystem.TryLoadOverlayTexturePublic());
            }


            // 3. 合成两个基础态
            var spriteWithMag = Compose(baseWithMag, overlays);
            var spriteNoMag = baseNoMag != null ? Compose(baseNoMag, overlays) : spriteWithMag;
            if (spriteWithMag == null) return;

            // SKS 装 UAS 后：手持位置向左下挪（pivot 向右上移动，粗略调整，后期可细调）
            if (isSks && HasAttachment(UasSksItemSystem.ItemKey))
            {
                spriteWithMag = AdjustPivot(spriteWithMag, 0.05f, 0.03f);
                if (spriteNoMag != null)
                    spriteNoMag = AdjustPivot(spriteNoMag, 0.05f, 0.03f);
            }
            // SKS 装 Tapco 后：手持位置往下一些、稍稍往右（pivot 向左上移动）
            // 用户指定 dx=0.05（pivot 往右移 = 手持位置往左调）
            if (isSks && HasAttachment(TapcoIntrafuseItemSystem.ItemKey))
            {
                spriteWithMag = AdjustPivot(spriteWithMag, 0.05f, -0.02f);
                if (spriteNoMag != null)
                    spriteNoMag = AdjustPivot(spriteNoMag, 0.05f, -0.02f);
            }

            // 4. 替换 GunScript 状态贴图（先销毁旧合成贴图，避免 GPU/托管泄漏）
            var destroyedTextures = new HashSet<Texture2D>();
            var destroyedSprites = new HashSet<Sprite>();
            DestroyOwnedSprite(gun.normalSprite, destroyedTextures, destroyedSprites);
            DestroyOwnedSprite(gun.rackedSprite, destroyedTextures, destroyedSprites);
            DestroyOwnedSprite(gun.normalSpriteNoMag, destroyedTextures, destroyedSprites);
            DestroyOwnedSprite(gun.rackedSpriteNoMag, destroyedTextures, destroyedSprites);

            gun.normalSprite = spriteWithMag;
            gun.rackedSprite = spriteWithMag;
            gun.normalSpriteNoMag = spriteNoMag;
            gun.rackedSpriteNoMag = spriteNoMag;

            // 5. 立即刷新当前渲染贴图
            var sr = gunItem.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = !gun.racked
                    ? (gun.hasMag ? gun.normalSprite : gun.normalSpriteNoMag)
                    : (gun.hasMag ? gun.rackedSprite : gun.rackedSpriteNoMag);
            }

            // 6. 清理旧的子物体叠加视觉（不再使用）
            RemoveChild(gunItem.transform, "SuppressorVisual");
            RemoveChild(gunItem.transform, "HandguardVisual");

            Plugin.Log.LogInfo($"[GunVisual] Rebuilt visual for '{gunItem.id}' (drum={isDrum}, supp={hasSuppressor}, handguard={hasHandguard}, hexhg={hasHexagonHg}, rk3={hasRk3}, mg47={hasMg47}, ags74={hasAgs74}, pdc={hasPdc}, mrs={hasMrs}, eotech={hasEotech553}, hhs1={hasHhs1}, specterdr={hasSpecterDr}, monstr={hasMonstr}, ta01nsn={hasTa01nsn}, razorhd={hasRazorHd}, pm2={hasPm2}, lastac={hasLasTac2}, klesch={hasKlesch2U}, baldr={hasBaldrPro}, tbl={hasTbl}, opfor={hasOpfor}, kocherga={hasKocherga}, zhukov={hasZhukovS}, cqr={hasCqr47}).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[GunVisual] Rebuild failed: {ex.Message}");
        }
    }

    private static void RemoveChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) UnityEngine.Object.Destroy(child.gameObject);
    }

    /// <summary>销毁我们生成的合成 Sprite 及其独占贴图，避免反复改枪时贴图泄漏。</summary>
    private static void DestroyOwnedSprite(Sprite? oldSprite, HashSet<Texture2D> destroyedTextures, HashSet<Sprite> destroyedSprites)
    {
        if (oldSprite == null || destroyedSprites.Contains(oldSprite)) return;
        if (!oldSprite.name.EndsWith("-composed", StringComparison.Ordinal)
            && !oldSprite.name.EndsWith("-pivot", StringComparison.Ordinal))
            return;
        destroyedSprites.Add(oldSprite);
        if (oldSprite.texture != null && !destroyedTextures.Contains(oldSprite.texture))
        {
            destroyedTextures.Add(oldSprite.texture);
            UnityEngine.Object.Destroy(oldSprite.texture);
        }
        UnityEngine.Object.Destroy(oldSprite);
    }

    // ===== 配件层构造 =====

    /// <summary>
    /// UAS SKS 套件层：贴图与 SKS 贴图位置完全相同，直接覆盖。
    /// 需擦除 SKS 贴图 x轴45往左的部分（UAS 覆盖整个护木/机匣前段）。
    /// UAS 画布与 SKS 基础贴图（sks_10）同尺寸（158x41），中心固定用 UAS 自身
    /// 高度一半（= sks_10 画布中心）。
    /// 装 SKS-A5 弹匣时基础贴图是 sks_sksa5（高46，比 UAS 高 5px），
    /// 需把 UAS 上移 5px 对齐枪身（否则 UAS 偏下露出木托）。
    /// 注意：不能用 baseSprite.pivot（像素值）作中心，否则 UAS 会左移约 32px。
    /// </summary>
    private static void AddUasSksLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = UasSksItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        // UAS 中心固定 = UAS 画布中心（与 sks_10 同尺寸，即枪身基准位置）
        float centerX = tex.width * 0.5f;
        float centerY = tex.height * 0.5f;

        // 装 SKS-A5 弹匣（sks_sksa5 高46 > UAS 高41）：UAS 上移 5px 对齐枪身
        if (baseSprite.texture.height > tex.height)
            centerY += 5f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 擦除 SKS 贴图 x轴45往左的部分（从 x=0 到 x=45，y 覆盖整个 SKS 高度）
            HasErase = true,
            EraseLeftX = 0f,
            EraseRightX = 45f,
            EraseTopY = baseSprite.texture.height,
            EraseBotY = 0f,
        });
    }

    /// <summary>
    /// Tapco INTRAFUSE SKS 套件层：贴图与 SKS 贴图位置完全相同，直接覆盖。
    /// 需擦除 SKS 贴图枪托/缓冲管区域（x轴0~45，与 UAS 一致）。
    /// Tapco 画布与 SKS 基础贴图同尺寸（158x41），中心对齐基础贴图中心。
    /// 用户指定：贴图往右挪 6 像素（原 9 再往左 3）。
    /// 装 20 发弹匣时基础贴图是 sks_sksa5（高46，比 Tapco 高 5px），
    /// 需把 Tapco 上移 4px 对齐枪身。
    /// </summary>
    private static void AddTapcoIntrafuseLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = TapcoIntrafuseItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        // Tapco 中心对齐基础贴图中心（位置完全相同），贴图往右挪 6 像素
        float centerX = baseSprite.texture.width * 0.5f + 6f;
        float centerY = baseSprite.texture.height * 0.5f;

        // 装 SKS-A5 弹匣（sks_sksa5 高46 > Tapco 高41）：Tapco 上移 4px 对齐枪身
        if (baseSprite.texture.height > tex.height)
            centerY += 4f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 擦除范围与 UAS 完全一致（x=0~45，y 覆盖整个 SKS 高度），
            // 只擦除枪托/缓冲管区域，弹匣/弹仓区域保留基础贴图状态
            HasErase = true,
            EraseLeftX = 0f,
            EraseRightX = 45f,
            EraseTopY = baseSprite.texture.height,
            EraseBotY = 0f,
        });
    }

    /// <summary>
    /// SKS ATI Monte Carlo 枪托层：贴图位置与 Tapco 相同（直接覆盖枪托区域）。
    /// SKS MC 画布 160x41（比 SKS 158x41 宽 2px），中心对齐基础贴图中心。
    /// 装 20 发弹匣时基础贴图是 sks_sksa5（高46），需上移 4px 对齐枪身。
    /// </summary>
    private static void AddSksMcLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = SksMcItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        // SKS MC 中心对齐基础贴图中心（位置与 Tapco 相同），贴图往左 2px（6-2=4）
        float centerX = baseSprite.texture.width * 0.5f + 4f;
        float centerY = baseSprite.texture.height * 0.5f;

        // 装 SKS-A5 弹匣（sks_sksa5 高46 > SKS MC 高41）：上移 4px 对齐枪身
        if (baseSprite.texture.height > tex.height)
            centerY += 4f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 擦除范围与 Tapco/UAS 一致（x=0~45，y 覆盖整个 SKS 高度）
            HasErase = true,
            EraseLeftX = 0f,
            EraseRightX = 45f,
            EraseTopY = baseSprite.texture.height,
            EraseBotY = 0f,
        });
    }

    /// <summary>
    /// SKS Leapers UTG PRO MTU017 机匣基座层：贴图位置参考 SKS MC 枪托参数。
    /// MTU017 画布 158x41（与 SKS 同尺寸），中心对齐基础贴图中心。
    /// 装 20 发弹匣时基础贴图是 sks_sksa5（高46），需上移 4px 对齐枪身。
    /// </summary>
    private static void AddMtu017Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = Mtu017ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        // MTU017 中心对齐基础贴图中心（位置参考 SKS MC）
        float centerX = baseSprite.texture.width * 0.5f + 4f;
        float centerY = baseSprite.texture.height * 0.5f;

        // 装 SKS-A5 弹匣（sks_sksa5 高46 > MTU017 高41）：上移 3px 对齐枪身（用户指定向下 1px）
        if (baseSprite.texture.height > tex.height)
            centerY += 3f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 不擦除：MTU017 直接叠加在机匣盖上
            HasErase = false,
        });
    }

    private static void AddSuppressorLayer(List<OverlayLayer> list, GunScript gun, Sprite baseSprite)
    {
        var gunItem = gun != null ? gun.GetComponent<Item>() : null;
        Texture2D? tex = null;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, HexagonAKMSuppressorItemSystem.ItemKey))
            tex = HexagonAKMSuppressorItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Rotor43762ItemSystem.ItemKey))
            tex = Rotor43762ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, HexagonSksItemSystem.ItemKey))
            tex = HexagonSksItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float ppi = baseSprite.pixelsPerUnit;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        float ox = 0f, oy = 0f;
        if (gun != null && gun.barrel != null)
        {
            ox = gun.barrel.localPosition.x * ppi + tex.width * 0.5f - 0.15f * ppi;
            oy = gun.barrel.localPosition.y * ppi - 0.29f * ppi;
        }
        else
        {
            ox = tex.width * 0.5f - 0.15f * ppi;
            oy = -0.29f * ppi;
        }

        // SKS（贴图宽 158）：消音器放大 1.5 倍 + 右移 23px + 上移 1px 对齐膛口装置（用户指定）
        // 注意：oy 减 = 贴图向下（CenterY 减小），故向上 = 减少向下量
        float scale = 1f;
        if (baseSprite.texture.width >= 150f)
        {
            scale = 1.5f;
            ox += 23f;
            oy -= 1f;
            // 装 20 发弹匣（sks_sksa5 高46）：上移 4px 适配（与膛口装置一致）
            if (baseSprite.texture.height > 41f)
                oy += 4f;
        }

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + ox),
            CenterY = (int)(pivotY + oy),
            Scale = scale,
        });
    }

    /// <summary>
    /// 判断枪上是否已装需要 WT0032-1 的膛口装置（DTK-1 等）。
    /// 装了则 WT0032-1 贴图被目标枪口替换，不叠加显示。
    /// </summary>
    private static bool HasMuzzleDeviceOnWt0032(Item gunItem)
    {
        if (gunItem == null) return false;
        var holder = gunItem.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null) return false;
        foreach (var id in holder.attachmentIds)
            if (SuppressorSystem.IsMuzzleDeviceRequiresWt0032(id))
                return true;
        return false;
    }

    /// <summary>
    /// WT0032-1 螺纹转换器层（SKS 专属）。
    /// 装在枪口处（枪管末端），放大 1.5 倍适配 SKS 大分辨率。
    /// 位置比 DTK-1 膛口装置上移 3px 对齐（用户指定）。
    /// </summary>
    private static void AddWt0032Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = Wt0032_1ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        // 仅 SKS 场景（贴图宽 158）
        if (baseSprite.texture.width < 150f) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float scale = 1.5f;

        // SKS 枪口位置：pivot 右侧约 100px（SKS 枪口在 x≈148），再右移 23px 与膛口装置一致
        float centerX = pivotX + 100f + 23f;
        // 相比 DTK-1 偏下 3px，上移 1px 对齐（用户指定；centerY 减 = 向下，故向上 = 减少减量）
        float centerY = pivotY + 1f - 1f;
        // 装 20 发弹匣（sks_sksa5 高46）：上移 4px 适配（与膛口装置一致）
        if (baseSprite.texture.height > 41f)
            centerY += 4f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            Scale = scale,
        });
    }

    /// <summary>
    /// 膛口制退器层（Dynacomp / DTK-1 共用）。
    /// 位置与消音器相同：枪口（gun.barrel）处。
    /// </summary>
    private static void AddMuzzleLayer(List<OverlayLayer> list, GunScript gun, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;

        float ppi = baseSprite.pixelsPerUnit;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        float ox = 0f, oy = 0f;
        if (gun != null && gun.barrel != null)
        {
            ox = gun.barrel.localPosition.x * ppi + tex.width * 0.5f - 0.15f * ppi;
            oy = gun.barrel.localPosition.y * ppi - 0.29f * ppi;
        }
        else
        {
            ox = tex.width * 0.5f - 0.15f * ppi;
            oy = -0.29f * ppi;
        }

        // SKS（贴图宽 158）：膛口装置放大 1.5 倍 + 右移 23px 适配大分辨率
        float scale = 1f;
        if (baseSprite.texture.width >= 150f)
        {
            scale = 1.5f;
            ox += 23f;
            // 装 20 发弹匣（sks_sksa5 高46）：膛口装置上移 4px 适配（与 Tapco 一致）
            if (baseSprite.texture.height > 41f)
                oy += 4f;
        }
        // AXMC（贴图宽 95）：枪口装置往右 17px、往下 1px（用户指定）
        else if (baseSprite.texture.width >= 90f && baseSprite.texture.width < 150f)
        {
            ox += 17f;
            oy -= 1f; // 往下 1px（oy 减 = 向下）
        }

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + ox),
            CenterY = (int)(pivotY + oy),
            Scale = scale,
        });
    }

    /// <summary>
    /// 格洛克套筒/基座层（Viper Cut / PS9 / UM3）。
    /// 贴图 90x82 与格洛克同尺寸，位置没有偏差，直接覆盖到枪身上无需挪位和擦除。
    /// 中心对齐基础贴图中心，往上 7px、往右 2px（在 5px/1px 基础上再往上 2px、往右 1px）。
    /// </summary>
    private static void AddGlockOverlayLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + 2f),
            CenterY = (int)(pivotY + 7f),
            HasErase = false,
        });
    }

    /// <summary>
    /// 格洛克 UM3 基座层。
    /// 在 AddGlockOverlayLayer 基础上再往右 15px、往左 3px（用户指定）。
    /// </summary>
    private static void AddGlockUm3Layer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + 2f + 15f - 3f),
            CenterY = (int)(pivotY + 7f),
            HasErase = false,
        });
    }

    /// <summary>
    /// 格洛克枪口配件层（G 3 Port / LW 9 / Osprey 9 / SRD 9）。
    /// 28x13 小贴图，叠加在枪口位置（gun.barrel 处，与消音器/膛口装置同位置逻辑）。
    /// 往上 9px、往右 1px（在 5px/1px 基础上再往上 4px）。
    /// </summary>
    private static void AddGlockMuzzleLayer(List<OverlayLayer> list, GunScript gun, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        float ppi = baseSprite.pixelsPerUnit;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        float ox = 0f, oy = 0f;
        if (gun != null && gun.barrel != null)
        {
            ox = gun.barrel.localPosition.x * ppi + tex.width * 0.5f - 0.15f * ppi;
            oy = gun.barrel.localPosition.y * ppi - 0.29f * ppi;
        }
        else
        {
            ox = tex.width * 0.5f - 0.15f * ppi;
            oy = -0.29f * ppi;
        }

        ox += 1f; // 往右 1px
        oy += 9f; // 往上 9px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + ox),
            CenterY = (int)(pivotY + oy),
        });
    }

    /// <summary>
    /// P90 枪口消音器层（Attenuator）。
    /// 20x4 小贴图，叠加在 P90 枪口位置（gun.barrel 处，与消音器/膛口装置同位置逻辑）。
    /// 往下 6px（在 3px 基础上再往下 3px）。
    /// </summary>
    private static void AddP90MuzzleLayer(List<OverlayLayer> list, GunScript gun, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        float ppi = baseSprite.pixelsPerUnit;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        float ox = 0f, oy = 0f;
        if (gun != null && gun.barrel != null)
        {
            ox = gun.barrel.localPosition.x * ppi + tex.width * 0.5f - 0.15f * ppi;
            oy = gun.barrel.localPosition.y * ppi - 0.29f * ppi;
        }
        else
        {
            ox = tex.width * 0.5f - 0.15f * ppi;
            oy = -0.29f * ppi;
        }

        oy -= 6f; // 往下 6px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + ox),
            CenterY = (int)(pivotY + oy),
        });
    }

    /// <summary>UMP 瞄准镜层。用户给定 UMP 贴图位置：x 23~33, y 14（顶部坐标），中心 (28, 14top) → (28, 26bottom)。</summary>
    private static void AddUmpSightLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = 28,
            CenterY = 29,
        });
    }

    /// <summary>UMP 前握把层。用户给定 UMP 贴图位置：x 37~41, y 22（顶部坐标），中心 (39, 22top) → (39, 18bottom)，再往下 3px → (39, 15bottom)；缩放 0.7。</summary>
    private static void AddUmpForegripLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = 39,
            CenterY = 17,
            Scale = 0.7f,
        });
    }

    /// <summary>UMP 战术设备层（手电/激光）。用户给定 UMP 贴图位置：x 36~40, y 18（顶部坐标），中心 (38, 18top) → (38, 22bottom)，再往下 3px → (38, 19bottom)。</summary>
    private static void AddUmpTacticalLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = 38,
            CenterY = 21,
        });
    }

    /// <summary>
    /// UMP OEM 消音器层。11x4 小贴图，叠加在 UMP 枪口位置（gun.barrel 处）。
    /// 位置为近似值，后续可在游戏内微调。
    /// </summary>
    private static void AddUmpOemLayer(List<OverlayLayer> list, GunScript gun, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        float ppi = baseSprite.pixelsPerUnit;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        float ox = 0f, oy = 0f;
        if (gun != null && gun.barrel != null)
        {
            ox = gun.barrel.localPosition.x * ppi + tex.width * 0.5f - 0.15f * ppi;
            oy = gun.barrel.localPosition.y * ppi - 0.29f * ppi;
        }
        else
        {
            ox = tex.width * 0.5f - 0.15f * ppi;
            oy = -0.29f * ppi;
        }

        oy -= 3f; // 之前近似位置
        oy += 5f; // 用户指定：往上 5px
        ox -= 30f; // 用户指定：往左 30px
        ox += 6f;  // 用户指定：往右 6px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + ox),
            CenterY = (int)(pivotY + oy),
        });
    }

    private static void AddHandguardLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = MoeAkmItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX),
            CenterY = (int)(pivotY + HandguardOffsetPxY),
        });
    }

    /// <summary>
    /// Hexagon AK 管状护木层。
    /// 该护木右侧更长（有延伸），左侧位置与原护木（MOE AKM）不变。
    /// moeakm 14px 宽、中心 pivot+22 → 左缘 pivot+15；
    /// hexagonak_hg 29px 宽、左缘同 pivot+15 → 中心 pivot+29。
    /// 垂直位置与 moeakm 相同（pivot+2）。
    /// </summary>
    private static void AddHexagonAkHandguardLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = HexagonAkHandguardItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // 左缘对齐 moeakm（pivot+15），中心 = 左缘 + 半宽(14.5) ≈ pivot+29.5
        float centerX = pivotX + 15f + tex.width * 0.5f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)(pivotY + HandguardOffsetPxY),
        });
    }

    /// <summary>
    /// B-10M+B-19 护木层。
    /// 位置与 MOE AKM 护木相同（pivot+22, +2）。
    /// </summary>
    private static void AddB10mB19Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = B10mB19ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX),
            CenterY = (int)(pivotY + HandguardOffsetPxY),
        });
    }

    /// <summary>
    /// WASR 木制握把护木层。
    /// 贴图尺寸略大，上方对齐（贴图顶边对齐护木顶边，而非中心对齐）。
    /// </summary>
    private static void AddWasrLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = WasrItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // 上方对齐：护木顶边 = pivot + HandguardOffsetPxY + 半高（护木贴图约 8px 高）
        // 贴图中心 Y = 顶边 - 贴图半高
        float topY = pivotY + HandguardOffsetPxY + 4f;
        float centerY = topY - tex.height * 0.5f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX),
            CenterY = (int)centerY,
        });
    }

    /// <summary>
    /// TDI AKM-L 护木层。
    /// 位置与 MOE AKM 护木相同（pivot+22, +2）。
    /// </summary>
    private static void AddAkmLLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = AkmLItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX),
            CenterY = (int)(pivotY + HandguardOffsetPxY),
        });
    }

    /// <summary>
    /// 手枪式握把层（RK-3 / MG-47 共用）。
    /// 覆盖原 AK 后握把区域（枪身中段偏右下，pivot 附近下方）。
    /// 只擦除贴图自身画布范围（不放大，避免擦过头）。
    /// </summary>
    private static void AddRk3Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = Rk3ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddGripLayer(list, baseSprite, tex);
    }

    private static void AddMg47Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = Mg47ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddGripLayer(list, baseSprite, tex);
    }

    private static void AddAgs74Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = Ags74ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        // AGS-74 是 8x10，比 rk3(7x9) 左/下各延伸 1px。
        // 对齐 rk3 的右边界(pivot+1.5)和上边界(pivot-1.5)：
        // 中心 = (pivot+1.5-4, pivot-1.5-5) = (pivot-2.5, pivot-6.5)
        AddGripLayer(list, baseSprite, tex, centerOffsetX: -2.5f, centerOffsetY: -6.5f);
    }

    private static void AddGripLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D tex,
        float centerOffsetX = -2f, float centerOffsetY = -6f)
    {
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // 后握把位置：pivot 附近偏左下方（AKM 握把在机匣下方，略靠尾部）
        float centerX = pivotX + centerOffsetX;
        float centerY = pivotY + centerOffsetY;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 只擦除贴图自身画布范围（不放大，避免擦过头）
            HasErase = true,
            EraseLeftX = centerX - tex.width * 0.5f,
            EraseRightX = centerX + tex.width * 0.5f,
            EraseTopY = centerY + tex.height * 0.5f,
            EraseBotY = centerY - tex.height * 0.5f,
        });
    }

    /// <summary>
    /// 前握把（垂直握把）层。
    /// 垂直握把装在护木下方导轨上：位置在护木（pivot+22,+2）下方偏前。
    /// 只擦除贴图自身画布范围，避免擦过头。
    /// </summary>
    private static void AddForegripLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // 前握把位置：护木下方（pivot 偏右、偏下）
        // 原 (pivot+22, pivot-7)，整体上移 3px、左移 2px → (pivot+20, pivot-4)，再下移 1px → (pivot+20, pivot-3)
        float centerX = pivotX + 20f;
        float centerY = pivotY - 3f;
        float scale = 1f;

        // SKS（贴图宽 158，Tapco 场景）：前握把放大 1.5 倍，位置适配 Tapco 护木下方
        // 用户指定：往右 33px、往下 6px（相对原位置 pivot+20, pivot-3）
        if (baseSprite.texture.width >= 150f)
        {
            scale = 1.5f;
            centerX = pivotX + 20f + 33f;   // 往右 33px
            centerY = pivotY - 3f - 6f;     // 往下 6px
            // 装 20 发弹匣（sks_sksa5 高46）：前握把上移 4px 适配（与 Tapco 一致）
            if (baseSprite.texture.height > 41f)
                centerY += 4f;
        }

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            Scale = scale,
            // 不擦除护木：前握把直接叠加在护木上
            HasErase = false,
        });
    }

    /// <summary>
    /// PDC 导轨防尘盖层。
    /// 皮卡汀尼导轨条绘制在机匣顶部（覆盖原防尘盖上缘）。
    /// </summary>
    private static void AddPdcLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = PdcItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // 机匣顶部：pivot 附近偏右上方（AKM 机匣上缘）
        float centerX = pivotX + 7f; // 左移5px
        float centerY = pivotY + 4f;

        // 擦除区域：只切除原版照门（枪机上方的两个凸起）。
        // 照门在导轨条上方：向上延伸到凸起顶（+6），向下只到贴图底边（-1），
        // 不触碰下方护木。
        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            HasErase = true,
            EraseLeftX = centerX - 10f, // 往左少擦4px（原-14）
            EraseRightX = centerX + 8f,  // 右侧也少擦4px（原+12），保护右侧护木
            EraseTopY = centerY + 6f,
            EraseBotY = centerY - 1f,
        });
    }

    /// <summary>
    /// 瞄准镜层（MRS / EOTech 553 共用）。
    /// 瞄具安装在 PDC 导轨上方（机匣顶部，pivot 上方偏右）。
    /// </summary>
    private static void AddMrsLayer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = MrsItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddEotech553Layer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = Eotech553ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddDpLayer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = DeltaPointItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddAcroP1Layer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = AcroP1ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddHhs1Layer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = Hhs1ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddSpecterDrLayer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = SpecterDrItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddMonstrLayer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = Monstr2x32ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddTa01nsnLayer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = Ta01nsnItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddRazorHdLayer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = RazorHdItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddPm2Layer(List<OverlayLayer> list, Sprite baseSprite, bool isMtu017, bool isDvl10)
    {
        var tex = Pm2ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddSightLayer(list, baseSprite, tex, isMtu017, isDvl10);
    }

    private static void AddSightLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D tex, bool isMtu017, bool isDvl10)
    {
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // 瞄具位置：PDC 导轨（pivot+7, pivot+4）上方，浮在机匣顶部
        float centerX = pivotX + 7f;
        float centerY = pivotY + 8f;
        float scale = 1f;

        // SKS（贴图宽 158）：瞄具放大 1.5 倍
        if (baseSprite.texture.width >= 150f)
        {
            scale = 1.5f;
            if (isMtu017)
            {
                // MTU017 场景：瞄具位于 MTU017 机匣基座上方（位置参考 MTU017 贴图位置）
                // 所有情况往右 5px（4+5=9）
                centerX += 9f;
                // 原厂无弹匣/弹仓（sks_magout/sks_10 高41）时往下 2px；20发弹匣（高46）保持
                centerY += (baseSprite.texture.height > 41f) ? 8f : 6f;
            }
            else
            {
                // UAS 场景：瞄具往右 48px、往上 4px
                centerX += 48f;
                centerY += 4f;
            }
        }
        // AXMC（贴图宽 95）：瞄具往右 6px、往下 3px（用户指定）
        // DVL（贴图宽 95）：瞄具往下 1px、往左 2px（用户指定）
        // 格洛克（贴图宽 90）：瞄具往上 9px、往右 1px（在 10px/1px 基础上再往下 1px、往右 2px）
        else if (baseSprite.texture.width >= 90f && baseSprite.texture.width < 150f)
        {
            if (isDvl10)
            {
                centerX -= 2f; // 往左 2px
                centerY -= 1f; // 往下 1px
            }
            else if (baseSprite.texture.width < 95f)
            {
                // 格洛克（宽 90）：往上 9px、往右 1px
                centerX += 1f;
                centerY += 9f;
            }
            else
            {
                centerX += 6f;
                centerY -= 3f; // AXMC：往下 3px
            }
        }
        // 沙鹰等手枪（贴图宽 45~90）：瞄具放大 1.1 倍，往右 20px、向上 4px（用户指定：原向下1px再上5px）
        else if (baseSprite.texture.width >= 45f && baseSprite.texture.width < 90f)
        {
            // 仅手枪（沙鹰/USP）应用手枪偏移；AKM/UMP 等长枪保持在导轨上方
            bool isPistolSight = baseSprite.name.StartsWith("deagle-", StringComparison.Ordinal)
                              || baseSprite.name.StartsWith("usp-", StringComparison.Ordinal);
            if (isPistolSight)
            {
                scale = 1.1f;
                centerX += 20f; // 往右 20px
                centerY += 4f;  // 向上 4px（-1 + 5）
            }
        }

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            Scale = scale,
            // SKS 场景不擦除（瞄具直接叠加在枪身上，避免擦掉 UAS/枪身细节）
            HasErase = baseSprite.texture.width < 150f,
            EraseLeftX = centerX - tex.width * 0.5f * scale,
            EraseRightX = centerX + tex.width * 0.5f * scale,
            EraseTopY = centerY + tex.height * 0.5f * scale,
            EraseBotY = centerY - tex.height * 0.5f * scale,
        });
    }

    private static void AddLasTac2Layer(List<OverlayLayer> list, Sprite baseSprite, bool isDvl10)
    {
        var tex = LasTac2ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // SKS（贴图宽 158，UAS 场景）：战术设备放大 1.5 倍，往右 40px（10+30）
        // AXMC（贴图宽 95）：战术设备往下 4px、往右 15px（用户指定）
        // DVL（贴图宽 95）：战术设备往下 7px、往右 12px（用户指定）
        // 格洛克（贴图宽 90）：战术设备往左 15px（用户指定）
        float scale = 1f;
        float ox = 0f;
        float oy = 0f;
        if (baseSprite.texture.width >= 150f) { scale = 1.5f; ox = 40f; }
        else if (isDvl10) { oy = -7f; ox = 12f; }
        else if (baseSprite.texture.width >= 90f && baseSprite.texture.width < 95f) { ox = -15f; } // 格洛克：往左 15px
        else if (baseSprite.texture.width >= 95f && baseSprite.texture.width < 150f) { ox = 15f; oy = -4f; } // AXMC：往右 15px、往下 4px
        else if (baseSprite.texture.width >= 45f && baseSprite.texture.width < 90f) { scale = 1.1f; oy = -7f; } // 沙鹰等手枪：放大1.1倍，往下7px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX + ox),
            CenterY = (int)(pivotY + HandguardOffsetPxY + oy),
            Scale = scale,
        });
    }

    private static void AddKlesch2ULayer(List<OverlayLayer> list, Sprite baseSprite, bool isDvl10)
    {
        var tex = Klesch2UItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // SKS（贴图宽 158，UAS 场景）：战术设备放大 1.5 倍，往右 40px（10+30）
        // AXMC（贴图宽 95）：战术设备往下 4px、往右 15px（用户指定）
        // DVL（贴图宽 95）：战术设备往下 7px、往右 12px（用户指定）
        // 格洛克（贴图宽 90）：战术设备往左 10px、往下 4px（在 15px 基础上往右 5px、往下 4px）
        float scale = 1f;
        float ox = 0f;
        float oy = 0f;
        if (baseSprite.texture.width >= 150f) { scale = 1.5f; ox = 40f; }
        else if (isDvl10) { oy = -7f; ox = 12f; }
        else if (baseSprite.texture.width >= 90f && baseSprite.texture.width < 95f) { ox = -10f; oy = -4f; } // 格洛克：往左 10px、往下 4px
        else if (baseSprite.texture.width >= 95f && baseSprite.texture.width < 150f) { ox = 15f; oy = -4f; } // AXMC：往右 15px、往下 4px
        else if (baseSprite.texture.width >= 45f && baseSprite.texture.width < 90f) { scale = 1.1f; oy = -7f; } // 沙鹰等手枪：放大1.1倍，往下7px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX + ox),
            CenterY = (int)(pivotY + HandguardOffsetPxY + oy),
            Scale = scale,
        });
    }

    private static void AddBaldrProLayer(List<OverlayLayer> list, Sprite baseSprite, bool isDvl10)
    {
        var tex = BaldrProItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // SKS（贴图宽 158，UAS 场景）：战术设备放大 1.5 倍，往右 40px（10+30）
        // AXMC（贴图宽 95）：战术设备往下 4px、往右 15px（用户指定）
        // DVL（贴图宽 95）：战术设备往下 7px、往右 12px（用户指定）
        // 格洛克（贴图宽 90）：战术设备往左 10px、往下 4px（在 15px 基础上往右 5px、往下 4px）
        float scale = 1f;
        float ox = 0f;
        float oy = 0f;
        if (baseSprite.texture.width >= 150f) { scale = 1.5f; ox = 40f; }
        else if (isDvl10) { oy = -7f; ox = 12f; }
        else if (baseSprite.texture.width >= 90f && baseSprite.texture.width < 95f) { ox = -10f; oy = -4f; } // 格洛克：往左 10px、往下 4px
        else if (baseSprite.texture.width >= 95f && baseSprite.texture.width < 150f) { ox = 15f; oy = -4f; } // AXMC：往右 15px、往下 4px
        else if (baseSprite.texture.width >= 45f && baseSprite.texture.width < 90f) { scale = 1.1f; oy = -7f; } // 沙鹰等手枪：放大1.1倍，往下7px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX + ox),
            CenterY = (int)(pivotY + HandguardOffsetPxY + oy),
            Scale = scale,
        });
    }

    private static void AddTblLayer(List<OverlayLayer> list, Sprite baseSprite, bool isDvl10)
    {
        var tex = TblItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        // SKS（贴图宽 158，UAS 场景）：战术设备放大 1.5 倍，往右 40px（10+30）
        // AXMC（贴图宽 95）：战术设备往下 4px、往右 15px（用户指定）
        // DVL（贴图宽 95）：战术设备往下 7px、往右 12px（用户指定）
        // 格洛克（贴图宽 90）：战术设备往左 10px、往下 4px（在 15px 基础上往右 5px、往下 4px）
        float scale = 1f;
        float ox = 0f;
        float oy = 0f;
        if (baseSprite.texture.width >= 150f) { scale = 1.5f; ox = 40f; }
        else if (isDvl10) { oy = -7f; ox = 12f; }
        else if (baseSprite.texture.width >= 90f && baseSprite.texture.width < 95f) { ox = -10f; oy = -4f; } // 格洛克：往左 10px、往下 4px
        else if (baseSprite.texture.width >= 95f && baseSprite.texture.width < 150f) { ox = 15f; oy = -4f; } // AXMC：往右 15px、往下 4px
        else if (baseSprite.texture.width >= 45f && baseSprite.texture.width < 90f) { scale = 1.1f; oy = -7f; } // 沙鹰等手枪：放大1.1倍，往下7px

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX + ox),
            CenterY = (int)(pivotY + HandguardOffsetPxY + oy),
            Scale = scale,
        });
    }

    // ===== OPFOR AA47 枪托（镂空，擦除范围向左延伸）=====
    private static void AddOpforLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = OpforAak7ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddStockLayer(list, baseSprite, tex);
    }

    private static void AddKochergaLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = KochergaItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddStockLayer(list, baseSprite, tex);
    }

    private static void AddZhukovSLayer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = ZhukovSItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;
        AddStockLayer(list, baseSprite, tex);
    }

    /// <summary>
    /// CQR47 一体式枪托层：擦除范围覆盖【原木质枪托 + 原后握把】两个区域，
    /// 因为一体式设计同时替换两者。
    /// 位置：整体往右偏移（CqrStockOffsetPxX 相对普通枪托 +5px）。
    /// </summary>
    private static void AddCqr47Layer(List<OverlayLayer> list, Sprite baseSprite)
    {
        var tex = Cqr47ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + StockOffsetPxX + CqrStockOffsetPxX;
        float centerY = pivotY + StockOffsetPxY + CqrStockOffsetPxY;

        // 一体式枪托：擦除范围
        float leftX = -20f;                                          // 左边界
        float rightX = 21f;                                          // 上半右边界（右移5px）
        // 上方与贴图齐平（不超出贴图上缘，避免上半部分被切出露白）
        float topY = centerY + tex.height * 0.5f;
        // 下方延伸裁掉握把（再往下延伸，确保切到握把）
        float botY = centerY - tex.height * 0.5f - 3f - 15f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            HasErase = true,
            EraseLeftX = leftX,
            EraseRightX = rightX,
            EraseTopY = topY,
            EraseBotY = botY,
            // 右边界向下线性扩展：底部额外向右 9px（底部右边界 = 21+9 = 30 = 贴图右缘）
            EraseRightExpandBottom = 9f,
        });
    }

    /// <summary>
    /// 枪托通用叠加层（OPFOR AA47 / Kocherga 共用）：
    /// 位置与擦除区域相同（枪械最尾部，镂空擦除向左延伸覆盖原木质枪托）。
    /// </summary>
    private static void AddStockLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D tex)
    {
        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + StockOffsetPxX;
        float centerY = pivotY + StockOffsetPxY;

        // 擦除范围（不对称 + 右侧三角形切除）：
        // - 左边界延伸到贴图最左端（X=0），覆盖整段原木质枪托
        // - 右边界停在贴图右缘，但从右上角向左下方斜切（三角形缺口），
        //   贴合 AKM 后握把轮廓，避免矩形右边界切掉握把
        float leftX = 0f;                                          // 贴图最左端
        float rightX = centerX + tex.width * 0.5f + 1f;            // 贴图右缘 +1px 余量
        float topY = centerY + tex.height * 0.5f + 2f;
        float botY = centerY - tex.height * 0.5f - 2f;
        float triW = 8f;                                           // 三角形切除宽度（像素）

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 镂空枪托：绘制前擦除（清透明）下方区域，防止镂空处漏出原木质枪托
            HasErase = true,
            EraseLeftX = leftX,
            EraseRightX = rightX,
            EraseTopY = topY,
            EraseBotY = botY,
            // 右侧三角形切除：从 (rightX, topY) 斜切到 (rightX - triW, botY)
            EraseTriangleRight = true,
            EraseTriangleWidth = triW,
        });
    }

    /// <summary>
    /// M4 系列后托层（SKS+Tapco 场景）。
    /// M4 后托装在 Tapco 缓冲管末端（枪托位置，pivot 左侧）。
    /// SKS 贴图宽 158，后托放大 1.5 倍适配大分辨率。
    /// 位置：SKS 枪托区域（pivot 左侧约 32px，y 对齐 pivot）。
    /// </summary>
    private static void AddM4StockLayer(List<OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;
        // 仅 SKS 场景（贴图宽 158）
        if (baseSprite.texture.width < 150f) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float scale = 1.5f;

        // SKS 枪托区域：pivot 左侧约 32px（SKS 枪托在 x≈0~30，中心约 15）
        // 用户指定：向下 10px、向右 6px（相对原位置），再向上 1px
        float centerX = pivotX - 32f + 6f;
        float centerY = pivotY + 1f - 10f + 1f;
        // 装 20 发弹匣（sks_sksa5 高46）：后托上移 4px 适配（与 Tapco 一致）
        if (baseSprite.texture.height > 41f)
            centerY += 4f;

        list.Add(new OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            Scale = scale,
            // 擦除贴图自身画布范围（后托覆盖原 Tapco 缓冲管末端）
            HasErase = true,
            EraseLeftX = centerX - tex.width * 0.5f * scale,
            EraseRightX = centerX + tex.width * 0.5f * scale,
            EraseTopY = centerY + tex.height * 0.5f * scale,
            EraseBotY = centerY - tex.height * 0.5f * scale,
        });
    }

    // ===== 合成 =====

    internal static Sprite? Compose(Sprite baseSprite, List<OverlayLayer> overlays)
    {
        var baseTex = baseSprite.texture;
        if (baseTex == null) return null;
        int baseW = baseTex.width, baseH = baseTex.height;
        if (baseW <= 0 || baseH <= 0) return null;

        Color[] basePixels;
        try { basePixels = baseTex.GetPixels(); }
        catch
        {
            Plugin.Log.LogWarning($"[GunVisual] Base texture {baseSprite.name} not readable, skipped.");
            return baseSprite;
        }

        // 1. 计算扩展后的画布边界（配件可能伸出枪械贴图边界，如消音器伸出枪口）
        int minX = 0, minY = 0, maxX = baseW, maxY = baseH;
        bool anyLayer = false;
        foreach (var layer in overlays)
        {
            if (layer.Texture == null) continue;
            int l = layer.CenterX - layer.Texture.width / 2;
            int b = layer.CenterY - layer.Texture.height / 2;
            int r = layer.CenterX + layer.Texture.width / 2;
            int t = layer.CenterY + layer.Texture.height / 2;
            if (l < minX) minX = l;
            if (b < minY) minY = b;
            if (r > maxX) maxX = r;
            if (t > maxY) maxY = t;
            anyLayer = true;
        }

        if (!anyLayer) return baseSprite;

        int newW = maxX - minX;
        int newH = maxY - minY;
        int offX = -minX;
        int offY = -minY;

        // 2. 新画布（默认透明）
        var pixels = new Color[newW * newH];
        // 记录已被配件层绘制过的像素（用于擦除优先级：擦除只擦原厂基础贴图，
        // 不擦除后加的配件，如加长枪管擦除不应清掉已绘制的护木）。
        var overlayPainted = new bool[newW * newH];

        // 3. 拷贝基础贴图
        for (int y = 0; y < baseH; y++)
        {
            int dy = y + offY;
            for (int x = 0; x < baseW; x++)
            {
                int dx = x + offX;
                pixels[dy * newW + dx] = basePixels[y * baseW + x];
            }
        }

        // 4. 画配件层（alpha 混合）
        foreach (var layer in overlays)
        {
            // 4a. 先擦除（清透明）下方矩形区域——用于镂空配件（如 OPFOR AA47 枪托）
            if (layer.HasErase)
            {
                // 支持不对称擦除边界；否则用对称 EraseW/EraseH（以 center 为中心）
                float fEl, fEb, fEr, fEt;
                if (layer.EraseLeftX.HasValue)
                {
                    fEl = layer.EraseLeftX.Value;
                    fEr = layer.EraseRightX.Value;
                    fEb = layer.EraseBotY.Value;
                    fEt = layer.EraseTopY.Value;
                }
                else
                {
                    fEl = layer.CenterX - layer.EraseW / 2f;
                    fEr = layer.CenterX + layer.EraseW / 2f;
                    fEb = layer.CenterY - layer.EraseH / 2f;
                    fEt = layer.CenterY + layer.EraseH / 2f;
                }
                int el = (int)Math.Floor(fEl) + offX;
                int er = (int)Math.Ceiling(fEr) + offX;
                int eb = (int)Math.Floor(fEb) + offY;
                int et = (int)Math.Ceiling(fEt) + offY;
                bool triRight = layer.EraseTriangleRight;
                float triWidth = layer.EraseTriangleWidth;
                float triHeight = fEt - fEb;
                float expandBottom = layer.EraseRightExpandBottom;
                for (int y = eb; y < et; y++)
                {
                    if (y < 0 || y >= newH) continue;
                    // 右侧三角形切除：从顶部到底部，右边界线性左收
                    // 斜边从 (rightX, topY) 到 (rightX - triW, botY)
                    int rowRight = er;
                    if (triRight && triWidth > 0f)
                    {
                        float t = triHeight > 0f
                            ? (float)(y - eb) / triHeight
                            : 0f;
                        // t=0（顶部）→ 不缩；t=1（底部）→ 缩 triWidth
                        rowRight = (int)Math.Floor(fEr + offX - triWidth * t);
                    }
                    // 右边界向下线性扩展：顶部不扩展，底部全量扩展
                    if (expandBottom > 0f && !triRight)
                    {
                        // t 从顶部(0)向底部(1)递增：顶部右边界 = rightX，底部右边界 = rightX + expandBottom
                        float t = triHeight > 0f
                            ? (float)(et - y) / triHeight
                            : 0f;
                        rowRight = (int)Math.Ceiling(fEr + offX + expandBottom * t);
                    }
                    for (int x = el; x < rowRight; x++)
                    {
                        if (x < 0 || x >= newW) continue;
                        int idx = y * newW + x;
                        // 擦除优先级：只擦除原厂基础贴图，不擦除已绘制的后加配件
                        if (overlayPainted[idx]) continue;
                        pixels[idx] = Color.clear;
                    }
                }
            }

            var attTex = layer.Texture;
            if (attTex == null) continue;
            Color[] attPixels;
            try { attPixels = attTex.GetPixels(); }
            catch { continue; }

            int aw = attTex.width, ah = attTex.height;
            float scale = layer.Scale > 0f ? layer.Scale : 1f;
            int dw = (int)(aw * scale), dh = (int)(ah * scale);
            int startX = layer.CenterX + offX - dw / 2;
            int startY = layer.CenterY + offY - dh / 2;

            // 绘制裁剪：只绘制贴图 x < CropRightX 的部分（相对贴图坐标，未缩放）
            int cropSxMax = aw;
            if (layer.CropRightX.HasValue)
            {
                float crop = layer.CropRightX.Value;
                cropSxMax = (int)Math.Ceiling(crop);
                if (cropSxMax < 0) cropSxMax = 0;
                if (cropSxMax > aw) cropSxMax = aw;
            }

            for (int y = 0; y < dh; y++)
            {
                int dy = startY + y;
                if (dy < 0 || dy >= newH) continue;
                // 反向映射源像素（最近邻），实现放大
                int sy = (int)(y / scale);
                if (sy >= ah) sy = ah - 1;
                for (int x = 0; x < dw; x++)
                {
                    int dx = startX + x;
                    if (dx < 0 || dx >= newW) continue;

                    int sx = (int)(x / scale);
                    if (sx >= aw) sx = aw - 1;
                    if (sx >= cropSxMax) continue; // 裁剪：跳过 CropRightX 右侧的像素
                    Color fg = attPixels[sy * aw + sx];
                    float a = fg.a;
                    if (a <= 0.02f) continue;

                    int idx = dy * newW + dx;
                    Color bg = pixels[idx];
                    pixels[idx] = new Color(
                        fg.r * a + bg.r * (1f - a),
                        fg.g * a + bg.g * (1f - a),
                        fg.b * a + bg.b * (1f - a),
                        1f);
                    overlayPainted[idx] = true;
                }
            }
        }

        // 5. 创建新纹理
        var tex = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(pixels);
        tex.Apply();

        // 6. 新 pivot
        var sprite = Sprite.Create(tex,
            new Rect(0, 0, newW, newH),
            new Vector2((baseSprite.pivot.x + offX) / newW, (baseSprite.pivot.y + offY) / newH),
            baseSprite.pixelsPerUnit);
        sprite.name = baseSprite.name + "-composed";
        return sprite;
    }

    /// <summary>
    /// 调整 sprite 的 pivot（手持位置）。pivot 向右上移动 = 手持位置向左下挪。
    /// 返回新 sprite（共享原纹理，仅改 pivot）。
    /// 注意：Sprite.pivot 返回的是【像素值】（相对左下角），而 Sprite.Create 的 pivot
    /// 参数是【归一化(0~1)】。必须先转归一化再加偏移，否则 pivot 会严重越界，
    /// 导致手持位置异常（枪械从手上消失）。
    /// </summary>
    private static Sprite AdjustPivot(Sprite src, float dx, float dy)
    {
        if (src == null) return src;
        var tex = src.texture;
        if (tex == null) return src;
        float normX = src.pivot.x / tex.width + dx;
        float normY = src.pivot.y / tex.height + dy;
        var sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(normX, normY),
            src.pixelsPerUnit);
        sprite.name = src.name + "-pivot";
        return sprite;
    }
}