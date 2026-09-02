using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 改枪预设：全局 3 个槽位，保存时记录枪械 ID 和配件方案。
/// </summary>
public static class GunsmithPreset
{
    private const string KeyPrefix = "cutarkov.gunsmith.preset.slot.";

    public static string Key(int slot) => KeyPrefix + slot;

    public static bool Exists(int slot)
        => PlayerPrefs.HasKey(Key(slot));

    public static void Save(Item gun, int slot)
    {
        var holder = gun.GetComponent<GunAttachmentHolder>();
        var ids = holder != null ? holder.attachmentIds : new List<string>();
        PlayerPrefs.SetString(Key(slot), gun.id + "\n" + string.Join(",", ids));
        PlayerPrefs.Save();
        Plugin.Log.LogInfo($"[Gunsmith] Preset slot {slot} saved: gun={gun.id}, attachments={string.Join(",", ids)}");
    }

    public static string GetSlotGunId(int slot)
    {
        string raw = PlayerPrefs.GetString(Key(slot), "");
        if (string.IsNullOrWhiteSpace(raw)) return "";
        int sep = raw.IndexOf('\n');
        return sep > 0 ? raw.Substring(0, sep).Trim() : "";
    }

    public static string GetSlotSummary(int slot)
    {
        string raw = PlayerPrefs.GetString(Key(slot), "");
        if (string.IsNullOrWhiteSpace(raw)) return WModLoc.Tr("wm.preset.empty", "空");

        int sep = raw.IndexOf('\n');
        string gunId = sep > 0 ? raw.Substring(0, sep).Trim() : "";
        string attachments = sep > 0 ? raw.Substring(sep + 1) : "";
        int count = string.IsNullOrWhiteSpace(attachments) ? 0 : attachments.Split(',').Length;
        if (string.IsNullOrWhiteSpace(gunId)) return WModLoc.Tr("wm.preset.empty", "空");
        return string.Format(WModLoc.Tr("wm.preset.summary", "{0} ({1}配件)"), GetGunShortName(gunId), count);
    }

    private static string ShortName(string id)
    {
        var name = CUTarkovMedicalMod.Framework.I18n.Tr(id + ".name");
        if (string.IsNullOrEmpty(name) || name == id + ".name")
        {
            if (Item.GlobalItems != null && Item.GlobalItems.TryGetValue(id, out var info))
                name = info.fullName ?? id;
            else
                name = id;
        }
        int start = name.IndexOf('【');
        int end = name.IndexOf('】');
        if (start >= 0 && end > start)
            return name.Substring(start + 1, end - start - 1);
        int bstart = name.IndexOf('[');
        int bend = name.IndexOf(']');
        if (bstart >= 0 && bend > bstart)
            return name.Substring(bstart + 1, bend - bstart - 1);
        return name;
    }

    private static string GetGunShortName(string gunId)
    {
        var name = CUTarkovMedicalMod.Framework.I18n.Tr(gunId + ".name");
        if (string.IsNullOrEmpty(name) || name == gunId + ".name")
        {
            if (Item.GlobalItems != null && Item.GlobalItems.TryGetValue(gunId, out var info))
                name = info.fullName ?? gunId;
            else
                name = gunId;
        }
        int start = name.IndexOf('【');
        int end = name.IndexOf('】');
        if (start >= 0 && end > start)
            return name.Substring(start + 1, end - start - 1);
        int bstart = name.IndexOf('[');
        int bend = name.IndexOf(']');
        if (bstart >= 0 && bend > bstart)
            return name.Substring(bstart + 1, bend - bstart - 1);
        return gunId.ToUpperInvariant();
    }

    public static string? TryLoad(Item gun, int slot)
    {
        string raw = PlayerPrefs.GetString(Key(slot), "");
        if (string.IsNullOrWhiteSpace(raw))
            return WModLoc.Tr("wm.preset.empty_slot", "该槽位为空");

        int sep = raw.IndexOf('\n');
        string savedGunId = sep > 0 ? raw.Substring(0, sep).Trim() : "";
        if (string.IsNullOrWhiteSpace(savedGunId))
            return WModLoc.Tr("wm.preset.empty_slot", "该槽位为空");

        if (!string.Equals(savedGunId, gun.id, StringComparison.OrdinalIgnoreCase))
            return string.Format(WModLoc.Tr("wm.preset.wrong_gun", "该槽位保存的是 {0}，不适用于当前枪"), GetGunShortName(savedGunId));

        var presetIds = sep > 0
            ? raw.Substring(sep + 1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList()
            : new List<string>();

        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            return WModLoc.Tr("wm.preset.no_holder", "枪械配件状态不可用");

        var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;

        var inventoryItems = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
        if (body != null)
        {
            foreach (var slotItem in body.slots)
            {
                if (slotItem == null) continue;
                var item = body.GetItem(slotItem.slot);
                if (item == null || !SuppressorSystem.IsAttachmentItem(item)) continue;
                if (!inventoryItems.ContainsKey(item.id))
                    inventoryItems[item.id] = item;
            }
        }

        var presetSet = new HashSet<string>(presetIds, StringComparer.OrdinalIgnoreCase);
        var installedSet = new HashSet<string>(holder.attachmentIds, StringComparer.OrdinalIgnoreCase);

        // 收集缺失配件
        var missing = presetSet.Where(id => !installedSet.Contains(id) && !inventoryItems.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return string.Format(WModLoc.Tr("wm.preset.missing_parts", "缺少配件：{0}"), string.Join("、", missing.Select(ShortName)));

        var missingToolIds = presetSet.Where(id => !installedSet.Contains(id)
            && ToolSystem.AttachmentRequiresLeatherman(id)
            && (body == null || !ToolSystem.HasTool(body, LeathermanItemSystem.ItemKey))).ToList();
        if (missingToolIds.Count > 0)
            return string.Format(WModLoc.Tr("wm.preset.needs_tool", "需要工具钳才能安装：{0}"), string.Join("、", missingToolIds.Select(ShortName)));

        var simulated = new HashSet<string>(installedSet, StringComparer.OrdinalIgnoreCase);
        var ordered = SortByDependency(gun, presetIds, simulated);
        if (ordered == null)
            return WModLoc.Tr("wm.preset.dependency_failed", "配件依赖关系无法满足（缺少护木等前提）");

        var toRemove = holder.attachmentIds.Where(id => !presetSet.Contains(id)).ToList();
        for (int i = toRemove.Count - 1; i >= 0; i--)
        {
            if (!holder.attachmentIds.Contains(toRemove[i])) continue;
            SuppressorSystem.DetachSingle(gun, toRemove[i]);
        }

        foreach (var id in ordered)
        {
            if (installedSet.Contains(id))
            {
                simulated.Add(id);
                continue;
            }

            if (!inventoryItems.TryGetValue(id, out var attachment) || attachment == null)
                return string.Format(WModLoc.Tr("wm.preset.part_not_found", "找不到配件：{0}"), id);

            if (!SuppressorSystem.AttachToGun(gun, attachment))
                return string.Format(WModLoc.Tr("wm.preset.install_failed", "安装失败：{0}"), id);
            inventoryItems.Remove(id);
            simulated.Add(id);
        }

        return null;
    }

    private static List<string>? SortByDependency(Item gun, List<string> ids, HashSet<string> simulatedInstalled)
    {
        var remaining = new List<string>(ids);
        var result = new List<string>();

        while (remaining.Count > 0)
        {
            bool progressed = false;
            for (int i = 0; i < remaining.Count; i++)
            {
                var id = remaining[i];
                if (CanInstallWithSimulated(gun, id, simulatedInstalled))
                {
                    result.Add(id);
                    simulatedInstalled.Add(id);
                    remaining.RemoveAt(i);
                    progressed = true;
                    break;
                }
            }
            if (!progressed)
                return null;
        }
        return result;
    }

    private static bool CanInstallWithSimulated(Item gun, string id, HashSet<string> simulatedInstalled)
    {
        var prereqs = ToolSystem.GetPrerequisites(id);
        if (prereqs != null)
        {
            bool gunSightExempt = SuppressorSystem.IsSightItem(id) && SuppressorSystem.IsGunSightExempt(gun);
            bool sksUasSightExempt = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsAttachmentInstalled(gun, UasSksItemSystem.ItemKey)
                && SuppressorSystem.IsSightItem(id)
                && SuppressorSystem.IsSightAllowedForUas(id);
            bool sksMtu017SightExempt = gun != null
                && SuppressorSystem.IsSksGun(gun)
                && SuppressorSystem.IsAttachmentInstalled(gun, Mtu017ItemSystem.ItemKey)
                && SuppressorSystem.IsSightItem(id);

            foreach (var prereq in prereqs)
                if (!gunSightExempt && !sksUasSightExempt && !sksMtu017SightExempt
                    && !simulatedInstalled.Contains(prereq) && !SuppressorSystem.IsAttachmentInstalled(gun, prereq))
                    return false;
        }

        bool isAkmLike = gun != null && gun.id.IndexOf("akm", StringComparison.OrdinalIgnoreCase) >= 0;

        if (isAkmLike && ToolSystem.OrPrerequisiteGroups.TryGetValue(id, out var alts) && alts != null && alts.Count > 0)
        {
            bool any = false;
            foreach (var alt in alts)
                if (simulatedInstalled.Contains(alt) || SuppressorSystem.IsAttachmentInstalled(gun, alt)) { any = true; break; }
            if (!any && !SuppressorSystem.HasAnyHandguardExceptWasr(gun)) return false;
        }

        if (isAkmLike && (SuppressorSystem.IsForegripItem(id) || SuppressorSystem.IsTacticalDevice(id)))
        {
            if (!simulatedInstalled.Any(sid => SuppressorSystem.IsHandguardItem(sid))
                && !SuppressorSystem.IsHandguardInstalled(gun))
                return false;
        }

        return true;
    }
}
