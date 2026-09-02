using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 改枪面板（纯代码 UI）。按专属按键（默认 G）打开/关闭。
/// 显示当前手持枪械，管理其配件：点击已装配件卸下，点击背包配件安装。
/// </summary>
public static class GunsmithPanel
{
    private static GameObject? _panel;
    private static Item? _currentGun;
    private static TextMeshProUGUI? _compareText;
    private static ScrollRect? _installedScrollRect;
    private static ScrollRect? _availableScrollRect;
    private static float _installedScrollPos;
    private static float _availableScrollPos;
    private static string _installedSignature = "";
    private static string _availableSignature = "";

    // ===== 开关动画 =====
    private static CanvasGroup? _panelCanvasGroup;
    private static bool _closing;
    private static float _openAnimT = 1f;
    private static float _closeAnimT;
    private const float OpenAnimDuration = 0.18f;
    private const float CloseAnimDuration = 0.12f;

    public static bool IsOpen => _panel != null && _panel.activeSelf;

    // ===== 按键切换 =====

    public static void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // ===== 打开 =====

    public static void Open()
    {
        if (IsOpen) return;
        var gun = GetHeldGun();
        if (gun == null)
        {
            Plugin.Log.LogInfo("[Gunsmith] No gun in hand to modify.");
            return;
        }

        BuildPanel(gun);
        StartOpenAnimation();
    }

    // ===== 关闭 =====

    public static void Close()
    {
        if (_panel == null) { CloseImmediate(); return; }
        if (_closing) return;
        _closing = true;
        _closeAnimT = 0f;
    }

    private static void CloseImmediate()
    {
        if (_panel != null)
        {
            UnityEngine.Object.Destroy(_panel);
            _panel = null;
        }
        _currentGun = null;
        _compareText = null;
        _installedScrollRect = null;
        _availableScrollRect = null;
        _panelCanvasGroup = null;
        _closing = false;
        _openAnimT = 1f;
    }

    private static void StartOpenAnimation()
    {
        if (_panel == null) return;
        _closing = false;
        _openAnimT = 0f;
        _closeAnimT = 0f;
        _panelCanvasGroup = _panel.GetComponent<CanvasGroup>();
        if (_panelCanvasGroup == null) _panelCanvasGroup = _panel.AddComponent<CanvasGroup>();
        _panelCanvasGroup.alpha = 0f;
        _panel.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
    }

    private static void UpdateCloseAnimation()
    {
        _closeAnimT += Time.unscaledDeltaTime / CloseAnimDuration;
        float t = Mathf.Clamp01(_closeAnimT);
        if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f - t;
        if (_panel != null) _panel.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, t);
        if (t >= 1f) CloseImmediate();
    }

    // ===== 每帧 =====

    private static float _refreshTimer;

    /// <summary>面板打开时若武器已不在手上则自动关闭；每 1 秒检查配件/背包变化，有变化才刷新界面。</summary>
    public static void Tick()
    {
        if (!IsOpen) return;

        // 关闭动画播放期间只推进动画，不再处理输入/内容刷新
        if (_closing)
        {
            UpdateCloseAnimation();
            return;
        }

        // 打开动画
        if (_openAnimT < 1f)
        {
            _openAnimT += Time.unscaledDeltaTime / OpenAnimDuration;
            float t = Mathf.Clamp01(_openAnimT);
            float scale = Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, t));
            if (_panel != null) _panel.transform.localScale = new Vector3(scale, scale, 1f);
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        var heldGun = GetHeldGun();
        if (heldGun == null)
        {
            Close();
            return;
        }
        if (heldGun != _currentGun)
        {
            _currentGun = heldGun;
            Rebuild();
            return;
        }

        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer >= 1f)
        {
            _refreshTimer = 0f;
            var gun = _currentGun;
            if (gun == null) { Close(); return; }
            // 只有配件/背包内容真的变化时才重建，避免滚动列表和悬停提示每 1 秒被打断
            if (GetInstalledSignature(gun) != _installedSignature
                || GetAvailableSignature() != _availableSignature)
            {
                Rebuild();
            }
        }
    }

    // ===== 手持枪械 =====

    private static Item? GetHeldGun()
    {
        var body = PlayerCamera.main?.body;
        if (body == null) return null;
        var item = body.GetItem(body.handSlot);
        if (item == null || item.GetComponent<GunScript>() == null) return null;
        return item;
    }

    // ===== 背包配件 =====

    private static List<Item> GetAvailableAttachments()
    {
        var list = new List<Item>();
        var body = PlayerCamera.main?.body;
        if (body == null) return list;
        try
        {
            foreach (var slot in body.slots)
            {
                if (slot == null) continue;
                var slotItem = body.GetItem(slot.slot);
                if (slotItem != null && SuppressorSystem.IsAttachmentItem(slotItem))
                    list.Add(slotItem);
            }
        }
        catch { }
        return list;
    }

    private static string GetInstalledSignature(Item gun)
    {
        var holder = gun.GetComponent<GunAttachmentHolder>();
        if (holder == null || holder.attachmentIds == null || holder.attachmentIds.Count == 0) return "";
        var ids = holder.attachmentIds.ToList();
        ids.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(",", ids);
    }

    private static string GetAvailableSignature()
    {
        var ids = new List<string>();
        foreach (var att in GetAvailableAttachments())
            ids.Add(att.id);
        ids.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(",", ids);
    }

    // ===== UI 构建 =====

    private static void BuildPanel(Item gun)
    {
        var mainCanvas = PlayerCamera.main?.mainCanvas;
        if (mainCanvas == null)
        {
            Plugin.Log.LogWarning("[Gunsmith] mainCanvas not found.");
            return;
        }

        _currentGun = gun;
        _panel = new GameObject("GunsmithPanel");
        _panel.transform.SetParent(mainCanvas.transform, false);

        var rect = _panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 540f);

        // 背景
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.13f, 0.97f);

        // 外边框（暗金/橄榄色细边）
        var border = CreatePanelBorder(_panel.transform, new Vector2(800f, 540f));
        if (border != null) border.color = new Color(0.55f, 0.5f, 0.35f, 0.9f);

        // 标题栏背景
        var headerBg = new GameObject("Header");
        headerBg.transform.SetParent(_panel.transform, false);
        var headerRt = headerBg.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(0f, 96f);
        headerRt.anchoredPosition = Vector2.zero;
        var headerImg = headerBg.AddComponent<Image>();
        headerImg.color = new Color(0.16f, 0.17f, 0.2f, 1f);

        // 标题：改装界面（分行）武器简称（如【akm】）
        var title = CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.title", "改装界面") + "\n" + GetGunShortName(gun), new Vector2(0f, 212f), 22, 520f);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.95f, 0.92f, 0.8f, 1f);

        // ===== 左右两栏 =====
        const float colX = 130f;      // 左栏(-130) 右栏(+130)，给右侧预设区让位
        const float listTop = 120f;   // 列表起始 Y

        // 左栏：已装配件（分组显示：主配件在上，附属配件在其下方缩进并标示附属关系）
        CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.installed", "已装配件（点击卸下）"), new Vector2(-colX, 160f), 16);
        var body = PlayerCamera.main?.body;
        var installedHolder = gun.GetComponent<GunAttachmentHolder>();
        if (installedHolder != null && installedHolder.attachmentIds.Count > 0)
        {
            // A3：已装配件列改为滚动列表
            var installedContent = CreateScrollList(_panel.transform, new Vector2(-colX, -30f), 220f, 330f);
            _installedScrollRect = installedContent.GetComponentInParent<ScrollRect>();
            RestoreScrollPosition(_installedScrollRect, _installedScrollPos);
            // 分组显示：每个主配件后紧跟其附属配件（手电/激光/前握把）
            var rendered = new HashSet<string>();
            foreach (var id in installedHolder.attachmentIds)
            {
                if (SuppressorSystem.GetParentAttachmentId(gun, id) != null) continue;
                // 主配件
                // 卸下需要工具钳的配件：没有工具钳时禁用 + 黄色提示
                bool needsToolDetach = ToolSystem.AttachmentRequiresLeatherman(id);
                bool hasToolDetach = !needsToolDetach || ToolSystem.HasTool(body, LeathermanItemSystem.ItemKey);
                var label = "▣ " + GetSlotLabel(id) + GetAttachmentShortName(id)
                    + (needsToolDetach && !hasToolDetach ? "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.needs_tool", "需工具钳") + ")</color>" : "");
                CreateListButton(installedContent, label,
                    () => { DetachAndRefresh(gun, id); }, hasToolDetach, GetAttachmentButtonColor(id));
                rendered.Add(id);
                // 紧跟其附属配件
                foreach (var sub in installedHolder.attachmentIds)
                {
                    if (rendered.Contains(sub)) continue;
                    if (SuppressorSystem.GetParentAttachmentId(gun, sub) == id)
                    {
                        var subLabel = "   ↳ " + GetSlotLabel(sub) + GetAttachmentShortName(sub)
                            + "  <color=#7fd4ff>(" + string.Format(WModLoc.Tr("wm.gunsmith.attached_to", "附属于 {0}"), GetAttachmentShortName(id)) + ")</color>";
                        CreateListButton(installedContent, subLabel,
                            () => { DetachAndRefresh(gun, sub); }, true, GetAttachmentButtonColor(sub));
                        rendered.Add(sub);
                    }
                }
            }
            // 剩余未分组的附属配件补在最后
            foreach (var id in installedHolder.attachmentIds)
            {
                if (rendered.Contains(id)) continue;
                var parent = SuppressorSystem.GetParentAttachmentId(gun, id);
                var tailLabel = (parent != null
                        ? "   ↳ " + GetSlotLabel(id) + GetAttachmentShortName(id) + "  <color=#7fd4ff>(" + string.Format(WModLoc.Tr("wm.gunsmith.attached_to", "附属于 {0}"), GetAttachmentShortName(parent)) + ")</color>"
                        : "▣ " + GetSlotLabel(id) + GetAttachmentShortName(id));
                CreateListButton(installedContent, tailLabel,
                    () => { DetachAndRefresh(gun, id); }, true, GetAttachmentButtonColor(id));
                rendered.Add(id);
            }
        }
        else
        {
            CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.empty_installed", "（空槽位：可安装瞄具/枪口/护木/握把等）"), new Vector2(-colX, listTop), 13);
        }

        // 右栏：可安装配件
        CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.available", "可安装配件（点击安装）"), new Vector2(colX, 160f), 16, 220f);
        var available = GetAvailableAttachments();
        _availableSignature = string.Join(",", available.Select(i => i.id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        _installedSignature = GetInstalledSignature(gun);
        if (available.Count > 0)
        {
            // A3：可安装配件列改为滚动列表
            var availableContent = CreateScrollList(_panel.transform, new Vector2(colX, -30f), 220f, 330f);
            _availableScrollRect = availableContent.GetComponentInParent<ScrollRect>();
            RestoreScrollPosition(_availableScrollRect, _availableScrollPos);

            // 渲染单个配件按钮（parentId 非空表示是附属配件，需标示附属关系）
            void RenderAttachment(Item att, string? parentId)
            {
                var attCopy = att;
                // 1) 是否需要工具钳
                bool needsTool = ToolSystem.AttachmentRequiresLeatherman(attCopy.id);
                bool hasTool = needsTool && ToolSystem.HasTool(body, LeathermanItemSystem.ItemKey);
                // 2) 是否有未满足的安装前提（如握把需要先装护木）
                var missingPrereqs = ToolSystem.GetMissingPrerequisites(gun, attCopy.id);
                // 缺前提的配件直接隐藏（如 AKM 未装护木时手电不显示），
                // 装好前提后再显示。非 AKM 枪（M4/沙鹰等）无护木前提，手电始终显示。
                // 3) 战术设备互斥：枪上已装其他战术设备
                bool tacticalConflict = SuppressorSystem.IsTacticalDevice(attCopy.id)
                    && SuppressorSystem.HasOtherTacticalDevice(gun, attCopy.id);
                // 4) 后握把槽互斥：一体式枪托与独立后握把不能同装
                bool gripConflict = SuppressorSystem.IsGripSlotItem(attCopy.id)
                    && SuppressorSystem.HasOtherGripSlotItem(gun, attCopy.id);
                // 5) 护木槽互斥：一把枪只能装一个护木
                bool handguardConflict = SuppressorSystem.IsHandguardItem(attCopy.id)
                    && SuppressorSystem.HasOtherHandguard(gun, attCopy.id);
                // 6) 后托槽互斥：一把枪只能装一个后托
                bool stockConflict = SuppressorSystem.IsStockItem(attCopy.id)
                    && SuppressorSystem.HasOtherStock(gun, attCopy.id);
                // 7) 枪口槽互斥：一把枪只能装一个枪口装置
                bool muzzleConflict = SuppressorSystem.IsMuzzleItem(attCopy.id)
                    && SuppressorSystem.HasOtherMuzzle(gun, attCopy.id);
                // 8) 防尘盖槽互斥：一把枪只能装一个防尘盖
                bool dustCoverConflict = SuppressorSystem.IsDustCoverItem(attCopy.id)
                    && SuppressorSystem.HasOtherDustCover(gun, attCopy.id);
                // 9) 瞄准镜槽互斥：一把枪只能装一个瞄准镜
                bool sightConflict = SuppressorSystem.IsSightItem(attCopy.id)
                    && SuppressorSystem.HasOtherSight(gun, attCopy.id);
                // 10) 前握把槽互斥：一把枪只能装一个前握把
                bool foregripConflict = SuppressorSystem.IsForegripItem(attCopy.id)
                    && SuppressorSystem.HasOtherForegrip(gun, attCopy.id);
                // 11) SKS 供弹方式冲突：已装弹匣时禁止装弹仓改件；已装弹仓改件时禁止装 SKS-A5 弹匣
                bool sksFeedConflict = false;
                // 12) 其余显式槽位互斥（枪管/套筒/基座/弹匣）
                bool barrelConflict = SuppressorSystem.IsBarrelItem(attCopy.id)
                    && SuppressorSystem.HasOtherBarrel(gun, attCopy.id);
                bool slideConflict = SuppressorSystem.IsSlideItem(attCopy.id)
                    && SuppressorSystem.HasOtherSlide(gun, attCopy.id);
                bool baseConflict = SuppressorSystem.IsBaseItem(attCopy.id)
                    && SuppressorSystem.HasOtherBase(gun, attCopy.id);
                bool magConflict = SuppressorSystem.IsMagItem(attCopy.id)
                    && SuppressorSystem.HasOtherMag(gun, attCopy.id);
                if (gun.id == SKSItemSystem.ItemKey)
                {
                    var gs = gun.GetComponent<GunScript>();
                    if (attCopy.id == SksIntegralMagItemSystem.ItemKey && gs != null && gs.hasMag)
                        sksFeedConflict = true; // 已装弹匣，禁止装弹仓
                    else if (attCopy.id == SksA5MagItemSystem.ItemKey
                        && SuppressorSystem.IsAttachmentInstalled(gun, SksIntegralMagItemSystem.ItemKey))
                        sksFeedConflict = true; // 已装弹仓改件，禁止装 SKS-A5 弹匣
                }

                bool dryRunOk = missingPrereqs.Count == 0
                    && !tacticalConflict && !gripConflict && !handguardConflict
                    && !stockConflict && !muzzleConflict && !dustCoverConflict
                    && !sightConflict && !foregripConflict && !sksFeedConflict
                    && !barrelConflict && !slideConflict && !baseConflict && !magConflict
                    && (!needsTool || hasTool)
                    && SuppressorSystem.AttachToGun(gun, attCopy, true);

                string label;
                bool interactable = true;
                if (missingPrereqs.Count > 0)
                {
                    // 不兼容/缺前提：按钮禁用 + 显示原因（优先显示枪械不兼容，其次前提）
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id)
                        + $"  <color=#ffcc4d>({GetMissingReasonText(attCopy.id, missingPrereqs)})</color>";
                    interactable = false;
                }
                else if (needsTool && !hasTool)
                {
                    // 没有工具：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.needs_tool", "需工具钳") + ")</color>";
                    interactable = false;
                }
                else if (tacticalConflict)
                {
                    // 已装其他战术设备：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.tactical_conflict", "已装其他战术设备") + ")</color>";
                    interactable = false;
                }
                else if (gripConflict)
                {
                    // 已装其他后握把槽配件：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.grip_conflict", "已装其他握把配件") + ")</color>";
                    interactable = false;
                }
                else if (handguardConflict)
                {
                    // 已装其他护木：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.handguard_conflict", "已装其他护木") + ")</color>";
                    interactable = false;
                }
                else if (stockConflict)
                {
                    // 已装其他后托：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.stock_conflict", "已装其他后托") + ")</color>";
                    interactable = false;
                }
                else if (muzzleConflict)
                {
                    // 已装其他枪口装置：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.muzzle_conflict", "已装其他枪口配件") + ")</color>";
                    interactable = false;
                }
                else if (dustCoverConflict)
                {
                    // 已装其他防尘盖：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.dust_cover_conflict", "已装其他防尘盖") + ")</color>";
                    interactable = false;
                }
                else if (sightConflict)
                {
                    // 已装其他瞄准镜：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.sight_conflict", "已装其他瞄准镜") + ")</color>";
                    interactable = false;
                }
                else if (foregripConflict)
                {
                    // 已装其他前握把：按钮禁用 + 黄色提示
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.foregrip_conflict", "已装其他前握把") + ")</color>";
                    interactable = false;
                }
                else if (sksFeedConflict)
                {
                    // SKS 供弹方式冲突：按钮禁用 + 黄色提示
                    string reason = (attCopy.id == SksIntegralMagItemSystem.ItemKey)
                        ? WModLoc.Tr("wm.gunsmith.reason.sks_mag_block", "已装弹匣，需先卸下弹匣") : WModLoc.Tr("wm.gunsmith.reason.sks_integral_block", "已装弹仓，需先卸下弹仓");
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + $"  <color=#ffcc4d>({reason})</color>";
                    interactable = false;
                }
                else if (barrelConflict)
                {
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.barrel_conflict", "已装其他枪管") + ")</color>";
                    interactable = false;
                }
                else if (slideConflict)
                {
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.slide_conflict", "已装其他套筒") + ")</color>";
                    interactable = false;
                }
                else if (baseConflict)
                {
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.base_conflict", "已装其他基座") + ")</color>";
                    interactable = false;
                }
                else if (magConflict)
                {
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.mag_conflict", "已装其他弹匣/供弹") + ")</color>";
                    interactable = false;
                }
                else if (!dryRunOk)
                {
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id) + "  <color=#ffcc4d>(" + WModLoc.Tr("wm.gunsmith.reason.cannot_install", "该枪无法安装") + ")</color>";
                    interactable = false;
                }
                else
                {
                    label = WModLoc.Tr("wm.gunsmith.install", "安装 ") + GetSlotLabel(attCopy.id) + GetAttachmentShortName(attCopy.id);
                    if (needsTool && hasTool)
                        label += "  <color=#90ee90>(" + WModLoc.Tr("wm.gunsmith.reason.has_tool_ok", "需工具钳 ✓") + ")</color>";
                }

                // 附属配件：缩进 + 蓝色标示"附属于 XX"
                if (parentId != null)
                    label = "   ↳ " + label + "  <color=#7fd4ff>(" + string.Format(WModLoc.Tr("wm.gunsmith.attached_to", "附属于 {0}"), GetAttachmentDependencyName(attCopy.id)) + ")</color>";

                var attBtn = CreateListButton(availableContent, label,
                    () => { AttachAndRefresh(gun, attCopy); }, interactable, GetAttachmentButtonColor(attCopy.id));
                var hoverText = BuildAttachmentStatText(gun, attCopy.id);
                var hover = attBtn.gameObject.AddComponent<AttachmentButtonHover>();
                hover.SetCallbacks(() => ShowCompareText(hoverText), () => ShowCompareText(""));
            }

            // 分组显示：每个主配件后紧跟其附属配件（手电/激光/前握把）
            // 先收集已渲染的主配件，避免重复
            var rendered = new HashSet<string>();
            foreach (var att in available)
            {
                if (SuppressorSystem.IsDependentAttachment(att.id)) continue;
                // 主配件
                RenderAttachment(att, null);
                rendered.Add(att.id);
                // 紧跟其附属配件
                foreach (var sub in available)
                {
                    if (!SuppressorSystem.IsDependentAttachment(sub.id)) continue;
                    if (rendered.Contains(sub.id)) continue;
                    // 附属配件依附于当前主配件（护木）
                    if (SuppressorSystem.GetParentAttachmentId(gun, sub.id) == att.id)
                    {
                        RenderAttachment(sub, GetAttachmentDependencyId(sub.id));
                        rendered.Add(sub.id);
                    }
                }
            }
            // 剩余未分组的附属配件（如无主配件时）补在最后
            foreach (var att in available)
            {
                if (SuppressorSystem.IsDependentAttachment(att.id) && !rendered.Contains(att.id))
                {
                    RenderAttachment(att, GetAttachmentDependencyId(att.id));
                    rendered.Add(att.id);
                }
            }
        }
        else
        {
            CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.empty_available", "（没有可安装配件，请先拾取或确认背包）"), new Vector2(colX, listTop), 13);
        }

        // ===== 右侧：预设改装（全局 3 个槽位） =====
        const float presetX = 330f;
        CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.preset", "预设槽位"), new Vector2(presetX, 160f), 15, 120f);
        for (int slot = 1; slot <= 3; slot++)
        {
            int slotCopy = slot;
            float y = 120f - (slot - 1) * 90f;
            CreateText(_panel.transform, WModLoc.Tr("wm.gunsmith.slot", "槽 ") + slot, new Vector2(presetX, y + 18f), 13, 120f);
            CreateButton(_panel.transform, WModLoc.Tr("wm.gunsmith.save", "存"), new Vector2(presetX - 30f, y), 60f, 24f,
                () => { SavePreset(gun, slotCopy); });
            CreateButton(_panel.transform, WModLoc.Tr("wm.gunsmith.load", "读"), new Vector2(presetX + 30f, y), 60f, 24f,
                () => { LoadPreset(gun, slotCopy); }, GunsmithPreset.Exists(slotCopy));
            CreateText(_panel.transform, GunsmithPreset.GetSlotSummary(slot), new Vector2(presetX, y - 26f), 11, 120f);
        }

        // 关闭按钮
        CreateButton(_panel.transform, WModLoc.Tr("wm.gunsmith.close", "关闭"), new Vector2(0f, -238f), 120f, 28f, Close);

        // C2：槽位对比提示条（悬停可安装配件时显示当前槽位配件）
        _compareText = CreateText(_panel.transform, "", new Vector2(0f, -210f), 13, 760f);
        _compareText.color = new Color(0.65f, 0.75f, 1f, 1f);
        _compareText.raycastTarget = false;
        _compareText.enableAutoSizing = true;
        _compareText.fontSizeMin = 10;
        _compareText.fontSizeMax = 13;
    }

    // ===== 安装 / 卸下并刷新 =====

    private static void AttachAndRefresh(Item gun, Item attachment)
    {
        if (attachment == null) return;
        // 安装失败（如 SKS 已装弹匣时阻止装弹仓）时不消耗配件、不刷新
        bool ok = SuppressorSystem.AttachToGun(gun, attachment);
        if (ok)
        {
            Hhs1ZoomUiPatch.InvalidateZoomLabelCache();
            Rebuild();
        }
    }

    private static void DetachAndRefresh(Item gun, string attachmentId)
    {
        // 卸下需要工具钳的配件（如 SKS 弹仓改件）时，同样需要工具钳
        if (ToolSystem.AttachmentRequiresLeatherman(attachmentId))
        {
            var body = PlayerCamera.main?.body;
            if (body == null || !ToolSystem.HasTool(body, LeathermanItemSystem.ItemKey))
            {
                Plugin.Log.LogInfo($"[Gunsmith] Detach blocked '{attachmentId}': Leatherman required.");
                return; // 没有工具钳，阻止卸下
            }
        }

        // 级联卸下：先卸依赖它的附属配件（如手电），再卸自身（护木）
        SuppressorSystem.DetachCascade(gun, attachmentId);
        Hhs1ZoomUiPatch.InvalidateZoomLabelCache();
        Rebuild();
    }

    private static void SavePreset(Item gun, int slot)
    {
        GunsmithPreset.Save(gun, slot);
        Rebuild();
    }

    private static void LoadPreset(Item gun, int slot)
    {
        string? error = GunsmithPreset.TryLoad(gun, slot);
        if (error == null)
        {
            Rebuild();
        }
        else
        {
            Plugin.Log.LogInfo($"[Gunsmith] Preset {slot} load failed: {error}");
            ShowToast(error);
        }
    }

    private static void ShowToast(string message)
    {
        if (_panel == null) return;
        var go = new GameObject("GunsmithToast");
        go.transform.SetParent(_panel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480f, 60f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.6f, 0.1f, 0.1f, 0.92f);
        var tmp = CreateText(go.transform, message, Vector2.zero, 15, 460f);
        tmp.color = Color.white;
        UnityEngine.Object.Destroy(go, 5f);
    }

    private static void Rebuild()
    {
        CaptureScrollPositions();
        var gun = _currentGun;
        CloseImmediate();
        if (gun != null)
            BuildPanel(gun);
    }

    /// <summary>重建前记录两个滚动列表的位置，避免自动刷新把滚动条拉回顶部。</summary>
    private static void CaptureScrollPositions()
    {
        if (_installedScrollRect != null)
            _installedScrollPos = _installedScrollRect.verticalNormalizedPosition;
        if (_availableScrollRect != null)
            _availableScrollPos = _availableScrollRect.verticalNormalizedPosition;
    }

    /// <summary>给新建的 ScrollRect 挂一个一帧后恢复滚动位置的组件（等待布局重建完成）。</summary>
    private static void RestoreScrollPosition(ScrollRect? sr, float savedPos)
    {
        if (sr == null || savedPos < 0.01f) return;
        var restore = sr.gameObject.AddComponent<ScrollPosRestore>();
        restore.Init(sr, savedPos);
    }

    private static string GetAttachmentDisplayName(string id)
    {
        if (Item.GlobalItems != null && Item.GlobalItems.TryGetValue(id, out var info))
            return info.fullName ?? id;
        return id;
    }

    /// <summary>取物品名称中"【】"内的简称（用于面板提示），无简称则返回全名。</summary>
    private static string GetMissingReasonText(string attachmentId, List<string> missing)
    {
        if (missing == null || missing.Count == 0) return "";
        string first = missing[0];

        // OR 前提组：列出所有可安装的护木选项
        if (ToolSystem.OrPrerequisiteGroups.TryGetValue(attachmentId, out var alts) && alts != null && alts.Count > 0)
        {
            bool allAlts = true;
            foreach (var m in missing)
                if (!alts.Contains(m)) { allAlts = false; break; }
            if (allAlts)
                return string.Format(WModLoc.Tr("wm.gunsmith.reason.need_one_of", "需先装 {0} 之一"), string.Join(" / ", alts.Select(GetAttachmentShortName)));
        }

        if (missing.Count > 1)
            return string.Format(WModLoc.Tr("wm.gunsmith.reason.need_install", "需先装 {0}"), string.Join(" / ", missing.Select(GetAttachmentShortName)));

        switch (first)
        {
            case "sks_only": return WModLoc.Tr("wm.gunsmith.reason.sks_only", "仅 SKS 可用");
            case "axmc_only": return WModLoc.Tr("wm.gunsmith.reason.axmc_only", "仅 AXMC 可用");
            case "dvl10_only": return WModLoc.Tr("wm.gunsmith.reason.dvl10_only", "仅 DVL-10 可用");
            case "akm_only": return WModLoc.Tr("wm.gunsmith.reason.akm_only", "仅 AKM 可用");
            case "m4_only": return WModLoc.Tr("wm.gunsmith.reason.m4_only", "仅 M4A1 可用");
            case "m4_no_rail": return WModLoc.Tr("wm.gunsmith.reason.m4_no_rail", "M4 原厂护木无导轨");
            case "sks_restriction": return WModLoc.Tr("wm.gunsmith.reason.sks_restriction", "SKS 原厂限制，需先改装");
            case "deagle_sight_whitelist": return WModLoc.Tr("wm.gunsmith.reason.deagle_sight_whitelist", "沙鹰仅可装白名单瞄具");
            case "long_barrel_conflict": return WModLoc.Tr("wm.gunsmith.reason.long_barrel_conflict", "与加长枪管冲突");
            case "long_barrel_required": return WModLoc.Tr("wm.gunsmith.reason.long_barrel_required", "需先装加长枪管");
            case "dvl10_silenced_tactical_conflict": return WModLoc.Tr("wm.gunsmith.reason.dvl10_silenced_tactical_conflict", "DVL 消音套件冲突");
            case "dvl10_no_foregrip": return WModLoc.Tr("wm.gunsmith.reason.dvl10_no_foregrip", "DVL-10 不可安装前握把");
            case "glock_only": return WModLoc.Tr("wm.gunsmith.reason.glock_only", "仅格洛克可用");
            case "um3_required": return WModLoc.Tr("wm.gunsmith.reason.um3_required", "需先装 UM3 基座");
            case "glock_restriction": return WModLoc.Tr("wm.gunsmith.reason.glock_restriction", "格洛克不可安装该配件");
            case "p90_restriction": return WModLoc.Tr("wm.gunsmith.reason.p90_restriction", "P90 仅可改装枪口（Attenuator）");
            case "ump_only": return WModLoc.Tr("wm.gunsmith.reason.ump_only", "仅 UMP45 可用");
            case "ump_sight_restriction": return WModLoc.Tr("wm.gunsmith.reason.ump_sight_restriction", "UMP 原厂不可安装 Razor HD / PM II");
            case "ump_restriction": return WModLoc.Tr("wm.gunsmith.reason.ump_restriction", "UMP 原厂仅可安装前握把/枪口/手电/瞄准镜");
            case "tmb338lm_required": return WModLoc.Tr("wm.gunsmith.reason.tmb338lm_required", "需先装 TMB 338LM");
            default: return string.Format(WModLoc.Tr("wm.gunsmith.reason.need_install", "需先装 {0}"), GetAttachmentShortName(first));
        }
    }

    private static void ShowCompareText(string text)
    {
        if (_compareText != null)
            _compareText.text = string.IsNullOrEmpty(text) ? "" : text;
    }

    /// <summary>悬停可安装配件时，在底部显示该配件自身的属性加成。</summary>
    private static string BuildAttachmentStatText(Item gun, string attachmentId)
    {
        string name = GetAttachmentShortName(attachmentId);
        var stats = SuppressorSystem.GetAttachmentStatMults(attachmentId, gun);
        float aimDelta = AimSystem.GetAttachmentAimTimeDelta(attachmentId, gun);

        var parts = new List<string>();

        float kbPct = (stats.knockBackMult - 1f) * 100f;
        if (Mathf.Abs(kbPct) > 0.05f) parts.Add(WModLoc.Tr("wm.gunsmith.stat.recoil", "后坐力") + FormatPct(kbPct));

        // spreadMult > 1 表示散布变大（精度下降），所以精度加成取负号
        float spPct = (stats.spreadMult - 1f) * 100f;
        if (Mathf.Abs(spPct) > 0.05f) parts.Add(WModLoc.Tr("wm.gunsmith.stat.accuracy", "精度") + FormatPct(-spPct));

        float ldPct = (stats.loudnessMult - 1f) * 100f;
        if (Mathf.Abs(ldPct) > 0.05f) parts.Add(WModLoc.Tr("wm.gunsmith.stat.noise", "噪音") + FormatPct(ldPct));

        float cdPct = (stats.conditionMult - 1f) * 100f;
        if (Mathf.Abs(cdPct) > 0.05f) parts.Add(WModLoc.Tr("wm.gunsmith.stat.durability", "耐久损耗") + FormatPct(cdPct));

        if (Mathf.Abs(aimDelta) > 0.005f) parts.Add(WModLoc.Tr("wm.gunsmith.stat.aim_speed", "瞄准速度") + FormatSeconds(aimDelta));

        if (parts.Count == 0)
            return name + " - " + WModLoc.Tr("wm.gunsmith.no_special_stats", "无特殊属性");
        return name + " - " + string.Join(", ", parts);
    }

    private static string FormatPct(float pct)
    {
        return (pct >= 0f ? "+" : "") + pct.ToString("0.0") + "%";
    }

    private static string FormatSeconds(float seconds)
    {
        return (seconds >= 0f ? "+" : "") + seconds.ToString("0.00") + "s";
    }

    private static string GetSlotLabel(string id)
    {
        var slot = GunAttachmentSlots.GetSlotType(id);
        return slot == AttachmentSlotType.None ? "" : GunAttachmentSlots.GetSlotShortTag(slot) + " ";
    }

    private static string GetAttachmentShortName(string id)
    {
        var name = GetAttachmentDisplayName(id);
        int start = name.IndexOf('【');
        int end = name.IndexOf('】');
        if (start >= 0 && end > start)
            return name.Substring(start + 1, end - start - 1);
        // 英文语言使用方括号简称，如 AKM 7.62x39 Assault Rifle [AKM]
        int bstart = name.IndexOf('[');
        int bend = name.IndexOf(']');
        if (bstart >= 0 && bend > bstart)
            return name.Substring(bstart + 1, bend - bstart - 1);
        return name;
    }

    /// <summary>
    /// 取枪械简称（如【akm】），用于改装界面标题。
    /// 用 I18n 获取当前语言枪械名称（含【】），提取【】内内容。
    /// </summary>
    private static string GetGunShortName(Item gun)
    {
        if (gun == null) return "";
        var name = CUTarkovMedicalMod.Framework.I18n.Tr(gun.id + ".name");
        if (string.IsNullOrEmpty(name) || name == gun.id + ".name")
            name = gun.Stats.fullName; // I18n 未命中时回退 fullName
        int start = name.IndexOf('【');
        int end = name.IndexOf('】');
        if (start >= 0 && end > start)
            return name.Substring(start + 1, end - start - 1);
        // 英文语言使用方括号简称，如 AKM 7.62x39 Assault Rifle [AKM]
        int bstart = name.IndexOf('[');
        int bend = name.IndexOf(']');
        if (bstart >= 0 && bend > bstart)
            return name.Substring(bstart + 1, bend - bstart - 1);
        return gun.id.ToUpperInvariant();
    }

    /// <summary>
    /// 返回附属配件依赖的主配件 ID（用于分组遍历）。
    /// 前握把/战术设备依赖"护木"（无单一父 ID），返回 null，由 GetAttachmentDependencyName 显示"护木"。
    /// </summary>
    private static string? GetAttachmentDependencyId(string id)
    {
        if (SuppressorSystem.IsForegripItem(id) || SuppressorSystem.IsTacticalDevice(id))
            return null;
        var prereqs = ToolSystem.GetPrerequisites(id);
        if (prereqs != null && prereqs.Count > 0) return prereqs[0];
        if (ToolSystem.OrPrerequisiteGroups.TryGetValue(id, out var alts) && alts.Count > 0)
            return alts[0];
        return null;
    }

    /// <summary>返回附属配件依赖的主配件显示名（用于"附属于 XX"标示）。</summary>
    private static string GetAttachmentDependencyName(string id)
    {
        if (SuppressorSystem.IsForegripItem(id) || SuppressorSystem.IsTacticalDevice(id))
            return WModLoc.Tr("wm.slot.handguard", "护木");
        var depId = GetAttachmentDependencyId(id);
        return depId != null ? GetAttachmentShortName(depId) : WModLoc.Tr("wm.gunsmith.dependency_main", "主配件");
    }

    // ===== UI 助手 =====

    private static Image? CreatePanelBorder(Transform parent, Vector2 size)
    {
        try
        {
            var go = new GameObject("PanelBorder");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsFirstSibling();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.55f, 0.5f, 0.35f, 0.9f);
            return img;
        }
        catch { return null; }
    }

    private static Color GetAttachmentButtonColor(string id)
    {
        if (SuppressorSystem.IsSightItem(id)) return new Color(0.2f, 0.42f, 0.5f, 0.9f);
        if (SuppressorSystem.IsMuzzleItem(id)) return new Color(0.55f, 0.38f, 0.2f, 0.9f);
        if (SuppressorSystem.IsHandguardItem(id)) return new Color(0.25f, 0.35f, 0.55f, 0.9f);
        if (SuppressorSystem.IsForegripItem(id) || SuppressorSystem.IsGripSlotItem(id)) return new Color(0.3f, 0.5f, 0.3f, 0.9f);
        if (SuppressorSystem.IsStockItem(id)) return new Color(0.45f, 0.3f, 0.5f, 0.9f);
        if (SuppressorSystem.IsTacticalDevice(id)) return new Color(0.55f, 0.5f, 0.2f, 0.9f);
        if (SuppressorSystem.IsMagItem(id)) return new Color(0.35f, 0.35f, 0.4f, 0.9f);
        return new Color(0.22f, 0.22f, 0.27f, 0.9f);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string text, Vector2 pos, float size, float width = 320f)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, Mathf.Max(30f, size * 3.5f));
        rt.anchoredPosition = pos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        // 英文字符串较长时自动缩字，避免重叠/溢出
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 8f;
        tmp.fontSizeMax = size;
        ApplyGameFont(tmp);
        return tmp;
    }

    /// <summary>应用游戏原版像素字体（与游戏医疗/配方/介绍等界面一致）。</summary>
    private static void ApplyGameFont(TextMeshProUGUI tmp)
    {
        try
        {
            var cam = PlayerCamera.main;
            if (cam != null && cam.timescaleText != null && cam.timescaleText.font != null)
                tmp.font = cam.timescaleText.font;
        }
        catch { }
    }

    // ===== A3：滚动列表助手 =====

    /// <summary>创建带遮罩的垂直滚动列表（ScrollRect + RectMask2D + VerticalLayoutGroup）。返回 content 供按钮挂载。</summary>
    private static Transform CreateScrollList(Transform parent, Vector2 pos, float width, float height)
    {
        var rootGo = new GameObject("ScrollList");
        rootGo.transform.SetParent(parent, false);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(width, height);
        rootRt.anchoredPosition = pos;

        // 遮罩视口
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(rootGo.transform, false);
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;
        var viewportImg = viewportGo.AddComponent<Image>();
        viewportImg.color = new Color(0f, 0f, 0f, 0.30f);
        viewportImg.raycastTarget = true;
        viewportGo.AddComponent<RectMask2D>();

        // 内容：锚定视口顶部，宽度跟随视口，高度由 VerticalLayoutGroup + ContentSizeFitter 撑开
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 0f);
        contentRt.anchoredPosition = Vector2.zero;

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = rootGo.AddComponent<ScrollRect>();
        sr.content = contentRt;
        sr.viewport = viewportRt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.inertia = false;
        sr.scrollSensitivity = 22f;

        return contentRt;
    }

    /// <summary>在滚动列表内容中创建按钮（位置交给 VerticalLayoutGroup，不手动设 anchoredPosition）。</summary>
    private static Button CreateListButton(Transform content, string label, Action onClick, bool interactable = true, Color? buttonColor = null)
    {
        var go = new GameObject("ListButton");
        go.transform.SetParent(content, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220f, 26f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 220f;
        le.preferredHeight = 26f;
        le.minHeight = 26f;

        var img = go.AddComponent<Image>();
        img.color = interactable
            ? (buttonColor ?? new Color(0.2f, 0.2f, 0.25f, 0.9f))
            : new Color(0.12f, 0.12f, 0.15f, 0.6f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.32f, 0.32f, 0.4f, 1f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        btn.colors = colors;
        btn.interactable = interactable;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        tmp.raycastTarget = false; // 让点击穿透到按钮
        // 长文本自动缩字，避免被滚动条/列宽截断
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 8;
        tmp.fontSizeMax = 14;
        ApplyGameFont(tmp);
        return btn;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 pos, float width, float height, Action onClick, bool interactable = true, Color? buttonColor = null)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        // 禁用按钮：颜色更暗（视觉反馈）
        img.color = interactable
            ? (buttonColor ?? new Color(0.2f, 0.2f, 0.25f, 0.9f))
            : new Color(0.12f, 0.12f, 0.15f, 0.6f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.32f, 0.32f, 0.4f, 1f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        btn.colors = colors;
        btn.interactable = interactable;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        tmp.raycastTarget = false; // 让点击穿透到按钮
        // 英文字符串较长时自动缩字，避免按钮文字重叠
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 9f;
        tmp.fontSizeMax = 15f;
        ApplyGameFont(tmp);
        return btn;
    }
}

/// <summary>按钮悬停组件：指针进入/离开时触发回调（用于 C2 槽位对比）。</summary>
public class AttachmentButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action? _onEnter;
    private Action? _onExit;

    public void SetCallbacks(Action? onEnter, Action? onExit)
    {
        _onEnter = onEnter;
        _onExit = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData) => _onEnter?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();
}

/// <summary>等待布局重建完成后恢复 ScrollRect 的滚动位置，防止面板自动刷新时滚动条跳回顶部。</summary>
public class ScrollPosRestore : MonoBehaviour
{
    private ScrollRect? _scrollRect;
    private float _savedPos;

    public void Init(ScrollRect scrollRect, float savedPos)
    {
        _scrollRect = scrollRect;
        _savedPos = savedPos;
    }

    private void Start()
    {
        if (_scrollRect == null)
        {
            Destroy(this);
            return;
        }
        // 强制布局重建，让 ContentSizeFitter 先算出内容高度，再恢复滚动位置
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_savedPos);
        Destroy(this);
    }
}

