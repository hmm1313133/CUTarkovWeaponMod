using System;
using System.Collections.Generic;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// M4A1 枪械配件视觉合成器（方案 A：运行时纹理合成）。
///
/// 把配件像素在运行时直接 alpha 合成进主枪械贴图，生成一张**新的完整枪械贴图**，
/// 替换 GunScript.normalSprite 等 4 个状态引用。配件与枪械变成同一张图，
/// 任何来源的抖动（帧动画 / transform 旋转）都作用于整张图，物理上不可能不同步。
///
/// 支持 M4A1 的配件叠加：
/// - MOE SL 护木（护木区域，pivot 附近偏右）
/// - 加长枪管（枪口处，向右延伸）
/// - 弹鼓（MAG5-60，通过基础贴图切换实现）
///
/// 合成时机（一次性，非每帧）：安装/卸下配件、装/卸弹匣、存档恢复。
/// </summary>
public static class M4VisualComposer
{
    // 护木相对枪械 pivot 的像素偏移（PPI 15.5，M4 贴图 82x41，pivot.x=0.30）
    // 用户指定：MOE SL 27px 宽，往右20像素、往上10像素（相对原 pivot+7 / +0），
    // 再往右3像素、往下5像素，再往左1像素。
    private const float HandguardOffsetPxX = 29f;
    private const float HandguardOffsetPxY = 5f;

    // M4 前握把相对枪械 pivot 的像素偏移（AKM 前握把为 pivot+20 / pivot-3，
    // M4 往右10像素、往下1像素 → pivot+30 / pivot-4，再往左4像素 → pivot+26）。
    private const float ForegripOffsetPxX = 26f;
    private const float ForegripOffsetPxY = -4f;

    // M4 战术设备（手电/激光）相对枪械 pivot 的像素偏移。
    // 战术设备装在护木下方导轨，位置与护木一致（pivot+29/+5），
    // 略向下偏移以贴合护木下沿。用户要求往左4像素。
    private const float TacticalOffsetPxX = 25f;
    private const float TacticalOffsetPxY = 2f;

    // M4 瞄准镜相对枪械 pivot 的像素偏移（机匣顶部，pivot 上方偏右）。
    // 用户要求向上2像素。
    private const float SightOffsetPxX = 7f;
    private const float SightOffsetPxY = 10f;

    // 加长枪管相对枪械 pivot 的像素偏移（枪口在 x≈80，pivot.x=0.30*82≈24.6）
    // 用户指定：往左10像素、往上6像素（相对原 +55 / -3），
    // 再往上1像素、往右2像素，再向上1像素、向左2像素。
    // 加长枪管贴图 53x10（单枪管，无护木），从枪口向右延伸。
    private const float BarrelOffsetPxX = 45f;
    private const float BarrelOffsetPxY = 5f;

    // 长枪管专属护木相对枪械 pivot 的像素偏移（与加长枪管同 X，Y 与普通护木一致）。
    private const float LongBarrelHandguardOffsetPxY = 5f;

    /// <summary>
    /// 根据当前配件/弹匣状态，重新合成 M4A1 的 4 个状态贴图并刷新渲染。
    /// </summary>
    public static void Rebuild(Item gunItem)
    {
        if (gunItem == null) return;
        try
        {
            var gun = gunItem.GetComponent<GunScript>();
            if (gun == null) return;

            // 仅处理 M4A1
            if (!string.Equals(gunItem.id, M4A1ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                return;

            var holder = gunItem.GetComponent<GunAttachmentHolder>();

            bool isDrum = holder != null
                && holder.currentMagId.Equals(M4A1Mag560ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
            bool hasMoeSl = SuppressorSystem.IsAttachmentInstalled(gunItem, MoeSlItemSystem.ItemKey);
            bool hasViper = SuppressorSystem.IsAttachmentInstalled(gunItem, ViperItemSystem.ItemKey);
            bool hasKacRis = SuppressorSystem.IsAttachmentInstalled(gunItem, KacRisItemSystem.ItemKey);
            bool hasSmrMk16 = SuppressorSystem.IsAttachmentInstalled(gunItem, SmrMk16ItemSystem.ItemKey);
            bool hasAdarWood = SuppressorSystem.IsAttachmentInstalled(gunItem, AdarWoodItemSystem.ItemKey);
            bool hasLvoa = SuppressorSystem.IsAttachmentInstalled(gunItem, LvoaItemSystem.ItemKey);
            bool hasLongBarrel = SuppressorSystem.IsAttachmentInstalled(gunItem, M4LongBarrelItemSystem.ItemKey);
            bool hasLasTac2 = SuppressorSystem.IsAttachmentInstalled(gunItem, LasTac2ItemSystem.ItemKey);
            bool hasKlesch2U = SuppressorSystem.IsAttachmentInstalled(gunItem, Klesch2UItemSystem.ItemKey);
            bool hasBaldrPro = SuppressorSystem.IsAttachmentInstalled(gunItem, BaldrProItemSystem.ItemKey);
            bool hasTbl = SuppressorSystem.IsAttachmentInstalled(gunItem, TblItemSystem.ItemKey);
            bool hasMrs = SuppressorSystem.IsAttachmentInstalled(gunItem, MrsItemSystem.ItemKey);
            bool hasEotech553 = SuppressorSystem.IsAttachmentInstalled(gunItem, Eotech553ItemSystem.ItemKey);
            bool hasHhs1 = SuppressorSystem.IsAttachmentInstalled(gunItem, Hhs1ItemSystem.ItemKey);
            bool hasSpecterDr = SuppressorSystem.IsAttachmentInstalled(gunItem, SpecterDrItemSystem.ItemKey);
            bool hasMonstr = SuppressorSystem.IsAttachmentInstalled(gunItem, Monstr2x32ItemSystem.ItemKey);
            bool hasTa01nsn = SuppressorSystem.IsAttachmentInstalled(gunItem, Ta01nsnItemSystem.ItemKey);
            bool hasRazorHd = SuppressorSystem.IsAttachmentInstalled(gunItem, RazorHdItemSystem.ItemKey);
            bool hasPm2 = SuppressorSystem.IsAttachmentInstalled(gunItem, Pm2ItemSystem.ItemKey);

            // 1. 基础贴图（有弹匣态 / 无弹匣态）
            Sprite? baseWithMag;
            Sprite? baseNoMag;
            if (isDrum)
            {
                baseWithMag = M4A1ItemSystem.TryLoadDrumIconPublic() ?? M4A1ItemSystem.TryLoadIconPublic();
                baseNoMag = baseWithMag;
            }
            else
            {
                baseWithMag = M4A1ItemSystem.TryLoadIconPublic();
                baseNoMag = M4A1ItemSystem.TryLoadNoMagIconPublic() ?? baseWithMag;
            }
            if (baseWithMag == null) return;

            // 2. 收集配件叠加层（绘制顺序：先画的在底层，后画的在上层/前方）
            //    枪管先画（底层），护木后画（覆盖在枪管前方），前握把最后画。
            var overlays = new List<GunVisualComposer.OverlayLayer>();
            if (hasLongBarrel) AddLongBarrelLayer(overlays, baseWithMag);
            AddMuzzleLayer(overlays, baseWithMag, gunItem, hasLongBarrel);
            if (hasMoeSl) AddHandguardLayer(overlays, baseWithMag, MoeSlItemSystem.TryLoadOverlayTexturePublic());
            if (hasViper) AddHandguardLayer(overlays, baseWithMag, ViperItemSystem.TryLoadOverlayTexturePublic());
            if (hasKacRis) AddHandguardLayer(overlays, baseWithMag, KacRisItemSystem.TryLoadOverlayTexturePublic());
            // 长枪管专属护木（与加长枪管同尺寸，覆盖整个枪管区域）
            if (hasSmrMk16) AddLongBarrelHandguardLayer(overlays, baseWithMag, SmrMk16ItemSystem.TryLoadOverlayTexturePublic());
            if (hasAdarWood) AddLongBarrelHandguardLayer(overlays, baseWithMag, AdarWoodItemSystem.TryLoadOverlayTexturePublic());
            if (hasLvoa) AddLongBarrelHandguardLayer(overlays, baseWithMag, LvoaItemSystem.TryLoadOverlayTexturePublic());
            AddForegripLayer(overlays, baseWithMag, gunItem);
            AddTacticalLayer(overlays, baseWithMag, gunItem);
            AddSightLayer(overlays, baseWithMag, gunItem);
            AddGripLayer(overlays, baseWithMag, gunItem);
            AddStockLayer(overlays, baseWithMag, gunItem);

            // 3. 合成两个基础态
            var spriteWithMag = GunVisualComposer.Compose(baseWithMag, overlays);
            var spriteNoMag = baseNoMag != null ? GunVisualComposer.Compose(baseNoMag, overlays) : spriteWithMag;
            if (spriteWithMag == null) return;

            // 4. 替换 GunScript 状态贴图
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

            Plugin.Log.LogInfo($"[M4Visual] Rebuilt visual for '{gunItem.id}' (drum={isDrum}, moesl={hasMoeSl}, longbarrel={hasLongBarrel}).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[M4Visual] Rebuild failed: {ex.Message}");
        }
    }

    // ===== 配件层构造 =====

    private static void AddHandguardLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + HandguardOffsetPxX),
            CenterY = (int)(pivotY + HandguardOffsetPxY),
        });
    }

    /// <summary>M4 前握把层（垂直握把，装在护木下方导轨）。位置相对 AKM 前握把往右10、往下1。</summary>
    private static void AddForegripLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Item gunItem)
    {
        Texture2D? tex = null;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, ShiftForegripItemSystem.ItemKey)) tex = ShiftForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Se5ForegripItemSystem.ItemKey)) tex = Se5ForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Rk0ForegripItemSystem.ItemKey)) tex = Rk0ForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Rk2ForegripItemSystem.ItemKey)) tex = Rk2ForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, B25ur1ForegripItemSystem.ItemKey)) tex = B25ur1ForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, CobraForegripItemSystem.ItemKey)) tex = CobraForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, P2ForegripItemSystem.ItemKey)) tex = P2ForegripItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, AfgForegripItemSystem.ItemKey)) tex = AfgForegripItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + ForegripOffsetPxX),
            CenterY = (int)(pivotY + ForegripOffsetPxY),
        });
    }

    /// <summary>M4 战术设备层（手电/激光，装在护木下方导轨）。</summary>
    private static void AddTacticalLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Item gunItem)
    {
        Texture2D? tex = null;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, LasTac2ItemSystem.ItemKey)) tex = LasTac2ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Klesch2UItemSystem.ItemKey)) tex = Klesch2UItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, BaldrProItemSystem.ItemKey)) tex = BaldrProItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, TblItemSystem.ItemKey)) tex = TblItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)(pivotX + TacticalOffsetPxX),
            CenterY = (int)(pivotY + TacticalOffsetPxY),
        });
    }

    /// <summary>M4 瞄准镜层（装在机匣顶部皮卡汀尼导轨上）。</summary>
    private static void AddSightLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Item gunItem)
    {
        Texture2D? tex = null;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, MrsItemSystem.ItemKey)) tex = MrsItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Eotech553ItemSystem.ItemKey)) tex = Eotech553ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Hhs1ItemSystem.ItemKey)) tex = Hhs1ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, SpecterDrItemSystem.ItemKey)) tex = SpecterDrItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Monstr2x32ItemSystem.ItemKey)) tex = Monstr2x32ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Ta01nsnItemSystem.ItemKey)) tex = Ta01nsnItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, RazorHdItemSystem.ItemKey)) tex = RazorHdItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Pm2ItemSystem.ItemKey)) tex = Pm2ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + SightOffsetPxX;
        float centerY = pivotY + SightOffsetPxY;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 只擦除贴图自身画布范围（瞄具下方由机匣导轨托住）
            HasErase = true,
            EraseLeftX = centerX - tex.width * 0.5f,
            EraseRightX = centerX + tex.width * 0.5f,
            EraseTopY = centerY + tex.height * 0.5f,
            EraseBotY = centerY - tex.height * 0.5f,
        });
    }

    /// <summary>
    /// M4 后握把层（TD120001 / Stark AR RG / MIAD / F1 St2 PC / Ergo）。
    /// 位置：贴图顶部对齐 M4 贴图 y=23 行，水平 x=17~30（用户指定）。
    /// M4 贴图 82x41，pivot.x=0.30（像素 x=24.6），pivot.y=0.5（像素 y=20.5）。
    /// 后握把贴图 14x13（ergo 15x13，向左拓展1px），顶部在 y=23：
    ///   中心 x = 23.5，中心 y = 16.5 → 相对 pivot：X=-1.1≈-1，Y=-4
    /// ergo 中心 x = 23（左移1px）→ 相对 pivot：X=-1.6≈-2
    /// 用户指定整体往下3px、往左1px：普通 X=-2 / Y=-7，ergo X=-3 / Y=-7
    /// </summary>
    private static void AddGripLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Item gunItem)
    {
        Texture2D? tex = null;
        float centerOffsetX = -2f;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, Td120001ItemSystem.ItemKey)) tex = Td120001ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, StarkArrgItemSystem.ItemKey)) tex = StarkArrgItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, MiadItemSystem.ItemKey)) tex = MiadItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, F1st2pcItemSystem.ItemKey)) tex = F1st2pcItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, ErgoItemSystem.ItemKey)) { tex = ErgoItemSystem.TryLoadOverlayTexturePublic(); centerOffsetX = -3f; }
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + centerOffsetX;
        float centerY = pivotY - 7f;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 只擦除贴图自身画布范围（后握把覆盖原 M4 握把区域）
            HasErase = true,
            EraseLeftX = centerX - tex.width * 0.5f,
            EraseRightX = centerX + tex.width * 0.5f,
            EraseTopY = centerY + tex.height * 0.5f,
            EraseBotY = centerY - tex.height * 0.5f,
        });
    }

    /// <summary>
    /// M4 后托层（Viper Mod.1 / CTR / DS150 FDE / ACS / MOE FG/FDE/SG）。
    /// 位置：M4 枪托区域（枪身尾部，pivot 左侧）。
    /// M4 贴图 82x41，pivot.x=0.30（像素 x=24.6），pivot.y=0.5（像素 y=20.5）。
    /// 后托贴图 23x16，绘制在枪托区域：中心 x ≈ pivot-15（约 x=9.6），中心 y ≈ pivot+1（约 y=21.5）。
    /// </summary>
    private static void AddStockLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Item gunItem)
    {
        Texture2D? tex = null;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, Vipermod1ItemSystem.ItemKey)) tex = Vipermod1ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, CtrItemSystem.ItemKey)) tex = CtrItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Ds150fdeItemSystem.ItemKey)) tex = Ds150fdeItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, AcsItemSystem.ItemKey)) tex = AcsItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, MoefgItemSystem.ItemKey)) tex = MoefgItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, MoefdeItemSystem.ItemKey)) tex = MoefdeItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, MoesgItemSystem.ItemKey)) tex = MoesgItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX - 15f;
        float centerY = pivotY + 1f;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 只擦除贴图自身画布范围（后托覆盖原 M4 枪托区域）
            HasErase = true,
            EraseLeftX = centerX - tex.width * 0.5f,
            EraseRightX = centerX + tex.width * 0.5f,
            EraseTopY = centerY + tex.height * 0.5f,
            EraseBotY = centerY - tex.height * 0.5f,
        });
    }

    private static void AddLongBarrelLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite)
    {
        var tex = M4LongBarrelItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + BarrelOffsetPxX;
        float centerY = pivotY + BarrelOffsetPxY;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
            // 加长枪管为单枪管：绘制前擦除（清透明）下方枪管区域，
            // 避免原枪管/护木残留。擦除贴图自身画布范围。
            HasErase = true,
            EraseLeftX = centerX - tex.width * 0.5f,
            EraseRightX = centerX + tex.width * 0.5f,
            EraseTopY = centerY + tex.height * 0.5f,
            EraseBotY = centerY - tex.height * 0.5f,
        });
    }

    /// <summary>
    /// M4 枪口层（Rotor 43 / NT-4 / SAKER / KX3 / VP-09）。
    /// 位置：枪口处（M4 贴图 82x41，pivot.x=0.30→24.6）。
    /// 普通枪管：中心 x = pivot+65（原 pivot+55 往右挪 10px），y 对齐枪管（pivot+3）。
    /// 长枪管：枪口随枪管变长更靠右，额外 +16px（长枪管枪口比普通枪口靠右约 16px）。
    /// </summary>
    private static void AddMuzzleLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Item gunItem, bool hasLongBarrel)
    {
        Texture2D? tex = null;
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, Rotor43ItemSystem.ItemKey)) tex = Rotor43ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Nt4ItemSystem.ItemKey)) tex = Nt4ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, SakerItemSystem.ItemKey)) tex = SakerItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Kx3ItemSystem.ItemKey)) tex = Kx3ItemSystem.TryLoadOverlayTexturePublic();
        else if (SuppressorSystem.IsAttachmentInstalled(gunItem, Vp09ItemSystem.ItemKey)) tex = Vp09ItemSystem.TryLoadOverlayTexturePublic();
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + 65f + (hasLongBarrel ? 16f : 0f);
        float centerY = pivotY + 3f;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
        });
    }

    /// <summary>长枪管专属护木层（与加长枪管同尺寸，覆盖整个枪管区域，无需擦除）。</summary>
    private static void AddLongBarrelHandguardLayer(List<GunVisualComposer.OverlayLayer> list, Sprite baseSprite, Texture2D? tex)
    {
        if (tex == null) return;

        float pivotX = baseSprite.pivot.x;
        float pivotY = baseSprite.pivot.y;
        float centerX = pivotX + BarrelOffsetPxX;
        float centerY = pivotY + LongBarrelHandguardOffsetPxY;

        list.Add(new GunVisualComposer.OverlayLayer
        {
            Texture = tex,
            CenterX = (int)centerX,
            CenterY = (int)centerY,
        });
    }
}
