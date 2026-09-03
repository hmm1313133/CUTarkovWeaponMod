using System;
using System.Linq;
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

        // 当前弹匣 ID（多弹匣）
        var magHolder = item.GetComponent<GunAttachmentHolder>();
        if (magHolder != null && !string.IsNullOrEmpty(magHolder.currentMagId))
            data["currentMagId"] = magHolder.currentMagId;

        // 配件状态（安装顺序列表）
        var holder = item.GetComponent<GunAttachmentHolder>();
        if (holder != null && holder.attachmentIds.Count > 0)
        {
            data["attachments"] = new JArray(holder.attachmentIds);
        }

        // 战术手电电量（LAS/TAC 2 / Klesch-2U）
        if (holder != null && holder.lasTacCharge < 1f)
        {
            data["lasTacCharge"] = holder.lasTacCharge;
        }
        if (holder != null && holder.kleschCharge < 1f)
        {
            data["kleschCharge"] = holder.kleschCharge;
        }
        if (holder != null && holder.baldrCharge < 1f)
        {
            data["baldrCharge"] = holder.baldrCharge;
        }
        if (holder != null && holder.tblCharge < 1f)
        {
            data["tblCharge"] = holder.tblCharge;
        }
        if (holder != null && holder.mrsCharge < 1f)
        {
            data["mrsCharge"] = holder.mrsCharge;
        }
        if (holder != null && holder.eotechCharge < 1f)
        {
            data["eotechCharge"] = holder.eotechCharge;
        }
        if (holder != null && holder.hhs1Charge < 1f)
        {
            data["hhs1Charge"] = holder.hhs1Charge;
        }
        if (holder != null && holder.noBatteryAttachments != null && holder.noBatteryAttachments.Count > 0)
        {
            data["noBattery"] = new JArray(holder.noBatteryAttachments);
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

        // 恢复当前弹匣 ID（多弹匣）
        var currentMagToken = obj["currentMagId"];
        if (currentMagToken != null && !string.IsNullOrEmpty(currentMagToken.Value<string>()))
        {
            var magHolder = item.GetComponent<GunAttachmentHolder>();
            if (magHolder == null)
                magHolder = item.gameObject.AddComponent<GunAttachmentHolder>();
            magHolder.currentMagId = currentMagToken.Value<string>();
        }

        // 恢复配件状态（安装顺序列表）
        var attachmentsToken = obj["attachments"];
        if (attachmentsToken is JArray attachments && attachments.Count > 0)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null)
                holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.attachmentIds = attachments
                .Where(t => t.Type == JTokenType.String)
                .Select(t => t.Value<string>())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // 恢复战术手电电量（LAS/TAC 2 / Klesch-2U）
        var lasTacToken = obj["lasTacCharge"];
        if (lasTacToken != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.lasTacCharge = lasTacToken.Value<float>();
            // 如果存档中有手电，恢复时确保 controller 挂载（但不强制激活灯）
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(LasTac2ItemSystem.ItemKey))
                LasTac2Controller.Attach(item, holder.lasTacCharge);
        }
        var kleschToken = obj["kleschCharge"];
        if (kleschToken != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.kleschCharge = kleschToken.Value<float>();
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(Klesch2UItemSystem.ItemKey))
                Klesch2UController.Attach(item);
        }
        var baldrToken = obj["baldrCharge"];
        if (baldrToken != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.baldrCharge = baldrToken.Value<float>();
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(BaldrProItemSystem.ItemKey))
                BaldrProController.Attach(item);
        }
        var tblToken = obj["tblCharge"];
        if (tblToken != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.tblCharge = tblToken.Value<float>();
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(TblItemSystem.ItemKey))
                TblController.Attach(item);
        }
        // MRS / EOTech / HHS-1 电量
        var mrsToken = obj["mrsCharge"];
        if (mrsToken != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.mrsCharge = mrsToken.Value<float>();
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(MrsItemSystem.ItemKey))
                MrsController.Attach(item);
        }
        var eotechToken = obj["eotechCharge"];
        if (eotechToken != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.eotechCharge = eotechToken.Value<float>();
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(Eotech553ItemSystem.ItemKey))
                Eotech553Controller.Attach(item);
        }
        var hhs1Token = obj["hhs1Charge"];
        if (hhs1Token != null)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.hhs1Charge = hhs1Token.Value<float>();
            if (holder.attachmentIds != null && holder.attachmentIds.Contains(Hhs1ItemSystem.ItemKey))
                Hhs1Controller.Attach(item);
        }
        // 无电池配件标记（战术灯卸下时不凭空补电池）
        var noBatteryToken = obj["noBattery"];
        if (noBatteryToken is JArray noBatteryArr)
        {
            var holder = item.GetComponent<GunAttachmentHolder>();
            if (holder == null) holder = item.gameObject.AddComponent<GunAttachmentHolder>();
            holder.noBatteryAttachments.Clear();
            foreach (var tok in noBatteryArr.OfType<JValue>())
            {
                var id = tok.Value<string>();
                if (!string.IsNullOrEmpty(id)) holder.noBatteryAttachments.Add(id);
            }
        }
        // 变倍瞄具（HHS-1 / SpecterDR / Monstr 2x32）：恢复时挂上倍率控制器（无供电机制）
        var zoomHolder = item.GetComponent<GunAttachmentHolder>();
        if (zoomHolder != null && zoomHolder.attachmentIds != null)
        {
            if (zoomHolder.attachmentIds.Contains(Hhs1ItemSystem.ItemKey))
                Hhs1Controller.Attach(item);
            if (zoomHolder.attachmentIds.Contains(SpecterDrItemSystem.ItemKey))
                SpecterDrController.Attach(item);
            if (zoomHolder.attachmentIds.Contains(Monstr2x32ItemSystem.ItemKey))
                Monstr2x32Controller.Attach(item);
            if (zoomHolder.attachmentIds.Contains(Ta01nsnItemSystem.ItemKey))
                Ta01nsnController.Attach(item);
            if (zoomHolder.attachmentIds.Contains(RazorHdItemSystem.ItemKey))
                RazorHdController.Attach(item);
            if (zoomHolder.attachmentIds.Contains(Pm2ItemSystem.ItemKey))
                Pm2Controller.Attach(item);
        }

        // 恢复配件后同步枪械视觉（消音器 + 护木 + 弹鼓外观）
        SuppressorSystem.UpdateSuppressorVisual(item);
        SuppressorSystem.UpdateHandguardVisual(item);
        SuppressorSystem.UpdateMagVisual(item);
        AimSystem.InvalidateAimTimeCache(item);
    }
}
