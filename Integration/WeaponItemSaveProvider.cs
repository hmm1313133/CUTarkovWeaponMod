using System;
using System.Reflection;
using CUCoreLib.Saving;
using CUTarkovMedicalMod.Framework;
using CUTarkovWeaponMod.Framework;
using Newtonsoft.Json.Linq;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// CUCoreLib IItemSaveProvider for weapon items.
///
/// 保存/恢复武器运行时状态：
/// - AmmoScript.rounds（弹匣内子弹数）
/// - GunScript.hasMag / GunScript.roundsInMag（枪内弹匣状态）
///
/// Restore 阶段还会调用 ConfigureCustomItem 设置 GunScript 字段（射速、伤害、枪管位置等），
/// 因为 CUCoreLib CreateTemplate 只创建基础预制体克隆，不配置武器特有字段。
///
/// 流程：
/// 1. Capture: 玩家存档时，遍历身上所有物品，对武器物品保存 ammo/mag 状态
/// 2. Restore: 存档加载后（ApplyPendingRestore），对武器物品：
///    a. 保存当前 condition（由游戏从存档设置）
///    b. 调用 ConfigureCustomItem（设置 GunScript 等，可能重置 condition）
///    c. 恢复 condition
///    d. 恢复 ammo rounds
///    e. 恢复 gun mag 状态
/// </summary>
public sealed class WeaponItemSaveProvider : IItemSaveProvider
{
    public int GetVersion() => 1;

    public JToken Capture(Item item, string itemKey)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return null!;
        if (!WeaponItemRegistration.IsWeaponItem(item.id)) return null!;

        var data = new JObject();

        // 弹匣子弹数
        var ammo = item.GetComponent<AmmoScript>();
        if (ammo != null)
            data["ammo"] = ammo.rounds;

        // 枪内弹匣状态
        var gun = item.GetComponent<GunScript>();
        if (gun != null)
        {
            data["hasMag"] = gun.hasMag;
            data["roundsInMag"] = gun.roundsInMag;
        }

        return data.HasValues ? data : null!;
    }

    public void Restore(Item item, string itemKey, JToken payload, int version, SaveRestoreContext context)
    {
        if (item == null || payload is not JObject obj) return;
        if (string.IsNullOrEmpty(item.id) || !WeaponItemRegistration.IsWeaponItem(item.id)) return;

        // 保存 condition（由游戏从存档数据设置，ConfigureCustomItem 可能覆盖）
        var savedCondition = item.condition;

        // 调用 ConfigureCustomItem 设置 GunScript/AmmoScript/sprite 等
        // CUCoreLib CreateTemplate 只创建基础预制体克隆（rifle/pistol/shotgun），
        // 不配置武器特有字段（射速、伤害、枪管位置、弹匣兼容性等）。
        try
        {
            var configureMethod = typeof(ConsoleSpawnPatch).GetMethod(
                "ConfigureCustomItem", BindingFlags.NonPublic | BindingFlags.Static);
            configureMethod?.Invoke(null,
                new object[] { item, new MedicalGrantRequest(item.id, item.id, 1, "SaveLoad") });
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(
                $"[WeaponSave] ConfigureCustomItem failed for '{item.id}': {ex.Message}");
        }

        // 恢复 condition（ConfigureCustomItem 可能将其设为 1.0）
        item.condition = savedCondition;

        // 恢复弹匣子弹数
        var ammoToken = obj["ammo"];
        if (ammoToken != null)
        {
            var ammo = item.GetComponent<AmmoScript>();
            if (ammo != null)
                ammo.rounds = ammoToken.Value<int>();
        }

        // 恢复枪内弹匣状态
        var hasMagToken = obj["hasMag"];
        var roundsToken = obj["roundsInMag"];
        if (hasMagToken != null || roundsToken != null)
        {
            var gun = item.GetComponent<GunScript>();
            if (gun != null)
            {
                if (hasMagToken != null) gun.hasMag = hasMagToken.Value<bool>();
                if (roundsToken != null) gun.roundsInMag = roundsToken.Value<int>();
            }
        }
    }
}
