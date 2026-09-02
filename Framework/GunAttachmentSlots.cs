using System;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 显式槽位系统：将配件映射到唯一槽位，便于 UI 展示和互斥判断。
/// </summary>
public enum AttachmentSlotType
{
    None,
    Sight,      // 瞄具
    Muzzle,     // 枪口
    Handguard,  // 护木
    Stock,      // 后托
    Grip,       // 后握把
    Foregrip,   // 前握把
    Tactical,   // 战术设备（手电/激光）
    Magazine,   // 弹匣/供弹
    DustCover,  // 防尘盖
    Barrel,     // 枪管
    Slide,      // 套筒
    Base,       // 基座
    Other       // 其它（工具、特殊）
}

public static class GunAttachmentSlots
{
    public static AttachmentSlotType GetSlotType(string attachmentId)
    {
        if (string.IsNullOrEmpty(attachmentId)) return AttachmentSlotType.None;
        if (SuppressorSystem.IsSightItem(attachmentId)) return AttachmentSlotType.Sight;
        if (SuppressorSystem.IsMuzzleItem(attachmentId)) return AttachmentSlotType.Muzzle;
        if (SuppressorSystem.IsHandguardItem(attachmentId)) return AttachmentSlotType.Handguard;
        if (SuppressorSystem.IsStockItem(attachmentId)) return AttachmentSlotType.Stock;
        if (SuppressorSystem.IsForegripItem(attachmentId)) return AttachmentSlotType.Foregrip;
        if (SuppressorSystem.IsGripSlotItem(attachmentId)) return AttachmentSlotType.Grip;
        if (SuppressorSystem.IsTacticalDevice(attachmentId)) return AttachmentSlotType.Tactical;
        if (SuppressorSystem.IsMagItem(attachmentId)) return AttachmentSlotType.Magazine;
        if (SuppressorSystem.IsDustCoverItem(attachmentId)) return AttachmentSlotType.DustCover;
        if (SuppressorSystem.IsBarrelItem(attachmentId)) return AttachmentSlotType.Barrel;
        if (SuppressorSystem.IsSlideItem(attachmentId)) return AttachmentSlotType.Slide;
        if (SuppressorSystem.IsBaseItem(attachmentId)) return AttachmentSlotType.Base;
        return AttachmentSlotType.Other;
    }

    public static string GetSlotDisplayName(AttachmentSlotType slot)
    {
        switch (slot)
        {
            case AttachmentSlotType.Sight: return WModLoc.Tr("wm.slot.sight", "瞄具");
            case AttachmentSlotType.Muzzle: return WModLoc.Tr("wm.slot.muzzle", "枪口");
            case AttachmentSlotType.Handguard: return WModLoc.Tr("wm.slot.handguard", "护木");
            case AttachmentSlotType.Stock: return WModLoc.Tr("wm.slot.stock", "后托");
            case AttachmentSlotType.Grip: return WModLoc.Tr("wm.slot.grip", "后握把");
            case AttachmentSlotType.Foregrip: return WModLoc.Tr("wm.slot.foregrip", "前握把");
            case AttachmentSlotType.Tactical: return WModLoc.Tr("wm.slot.tactical", "战术设备");
            case AttachmentSlotType.Magazine: return WModLoc.Tr("wm.slot.magazine", "弹匣/供弹");
            case AttachmentSlotType.DustCover: return WModLoc.Tr("wm.slot.dust_cover", "防尘盖");
            case AttachmentSlotType.Barrel: return WModLoc.Tr("wm.slot.barrel", "枪管");
            case AttachmentSlotType.Slide: return WModLoc.Tr("wm.slot.slide", "套筒");
            case AttachmentSlotType.Base: return WModLoc.Tr("wm.slot.base", "基座");
            default: return WModLoc.Tr("wm.slot.other", "其它");
        }
    }

    public static string GetSlotShortTag(AttachmentSlotType slot)
    {
        switch (slot)
        {
            case AttachmentSlotType.Sight: return WModLoc.Tr("wm.slot.tag.sight", "[瞄]");
            case AttachmentSlotType.Muzzle: return WModLoc.Tr("wm.slot.tag.muzzle", "[口]");
            case AttachmentSlotType.Handguard: return WModLoc.Tr("wm.slot.tag.handguard", "[护]");
            case AttachmentSlotType.Stock: return WModLoc.Tr("wm.slot.tag.stock", "[托]");
            case AttachmentSlotType.Grip: return WModLoc.Tr("wm.slot.tag.grip", "[握]");
            case AttachmentSlotType.Foregrip: return WModLoc.Tr("wm.slot.tag.foregrip", "[前握]");
            case AttachmentSlotType.Tactical: return WModLoc.Tr("wm.slot.tag.tactical", "[灯]");
            case AttachmentSlotType.Magazine: return WModLoc.Tr("wm.slot.tag.magazine", "[匣]");
            case AttachmentSlotType.DustCover: return WModLoc.Tr("wm.slot.tag.dust_cover", "[盖]");
            case AttachmentSlotType.Barrel: return WModLoc.Tr("wm.slot.tag.barrel", "[管]");
            case AttachmentSlotType.Slide: return WModLoc.Tr("wm.slot.tag.slide", "[套]");
            case AttachmentSlotType.Base: return WModLoc.Tr("wm.slot.tag.base", "[座]");
            default: return WModLoc.Tr("wm.slot.tag.other", "[件]");
        }
    }
}
