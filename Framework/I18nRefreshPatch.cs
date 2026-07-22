using System;
using HarmonyLib;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 语言切换后刷新自定义物品的 ItemInfo 本地化文本。
/// 游戏在初始化时设置 ItemInfo.fullName/description，切换语言后不会自动更新。
/// 此 Prefix 在 ItemHoverDescription 调用前更新 ItemInfo，使名称和描述跟随当前语言。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class I18nRefreshPatch
{
    [HarmonyPrefix]
    public static void Prefix(Item item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        if (!WeaponItemRegistration.WeaponItemIds.Contains(item.id)) return;

        try
        {
            var key = item.id;
            var newName = I18n.Tr(key + ".name");
            var newDesc = I18n.Tr(key + ".desc");

            // 仅当翻译存在时更新（I18n.Tr 找不到时返回 key 本身）
            if (newName != key + ".name")
                item.Stats.fullName = newName;
            if (newDesc != key + ".desc")
                item.Stats.description = newDesc;
        }
        catch { }
    }
}
