using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// HHS-1 倍率 UI + 检查弹匣按钮补丁。
/// 在枪械保险 UI（gunMenu 面板的 gunSafeImage）右侧显示：
/// - 倍率 "1x/3x"（zoom.png 背景）
/// - 检查弹匣按钮（checkmag 贴图，点击播放该枪自定义卸弹匣音效）
/// - 弹药量显示（枪械 UI 正上方，绿色像素字体，4 秒后渐隐）
///
/// 检查弹匣：按下瞬间记录弹药量快照，不实时变化；显示期间再按无效。
/// 复用 PlayerCamera.HandleGunMenu 的 Postfix。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.HandleGunMenu))]
public static class Hhs1ZoomUiPatch
{
    private const string ZoomTextObjectName = "Hhs1ZoomText";
    private const string CheckMagButtonName = "Hhs1CheckMagButton";
    private const string AmmoTextObjectName = "Hhs1AmmoText";

    private static Sprite? _zoomSprite;
    private static Sprite? _checkMagSprite;

    // 倍率显示控制器缓存（同一把枪不重复 GetComponent；改装面板装卸后失效）
    private static int _cachedZoomGunId = int.MinValue;
    private static Hhs1Controller? _cachedZoomHhs;
    private static SpecterDrController? _cachedZoomSpec;
    private static RazorHdController? _cachedZoomRazor;
    private static Pm2Controller? _cachedZoomPm2;

    public static void InvalidateZoomLabelCache() => _cachedZoomGunId = int.MinValue;

    // 检查弹匣按钮组件缓存（gunMenu 是单例 UI，按钮只创建一次，避免每帧 GetComponent）
    private static Image? _cachedCheckMagImg;
    private static Button? _cachedCheckMagBtn;
    private static Transform? _cachedCheckMagRoot;

    // 弹药量显示状态（快照）
    private static float _ammoShowStart = -1f;   // 显示开始时间
    private static float _ammoShowUntil = -1f;   // 显示结束时间
    private static string _ammoSnapshot = "";    // 按下瞬间的弹药量快照

    // 检查弹匣按钮按下瞬间抑制开火（下一帧自动重置）
    public static bool SuppressFire { get; private set; }
    private static float _suppressUntil = -1f;

    /// <summary>检查弹匣期间抑制开火（含 0.2 秒点击瞬间 + 1 秒检查进行中）。</summary>
    public static bool ShouldSuppressFire => SuppressFire || _checkingMag;

    // 检查弹匣进行中状态（1 秒内抑制枪械操作：开火/保险/卸弹匣）
    private static bool _checkingMag;
    private static float _checkMagEnd = -1f;     // 检查结束时间（1 秒后播放装弹匣音效）
    private static bool _magInPlayed;            // 装弹匣音效是否已播放
    private static bool _hadMag;                 // 检查时枪上是否有弹匣（Mag 模式枪无弹匣时不播放插拔音效）

    /// <summary>检查弹匣进行中（期间抑制枪械操作）。</summary>
    public static bool IsCheckingMag => _checkingMag;

    [HarmonyPostfix]
    public static void Postfix(PlayerCamera __instance)
    {
        // 重置开火抑制（点击后 0.2 秒恢复）
        if (Time.unscaledTime >= _suppressUntil) SuppressFire = false;

        // 检查弹匣进行中：1 秒后播放装弹匣音效，音效播放完才结束检查
        if (_checkingMag)
        {
            if (Time.unscaledTime >= _checkMagEnd)
            {
                if (!_magInPlayed)
                {
                    _magInPlayed = true;
                    // 播放装弹匣音效，并把检查结束时间延长到音效播放完
                    float soundLen = PlayMagInSound(__instance);
                    _checkMagEnd = Time.unscaledTime + Mathf.Max(soundLen, 0.3f);
                }
                else if (Time.unscaledTime >= _checkMagEnd)
                {
                    _checkingMag = false;
                }
            }
        }

        try
        {
            var body = __instance.body;
            if (body == null || __instance.gunMenu == null || __instance.gunSafeImage == null) return;

            var handItem = body.GetItem(body.handSlot);
            var handGun = handItem != null ? handItem.GetComponent<GunScript>() : null;
            var handHolder = handItem != null ? handItem.GetComponent<GunAttachmentHolder>() : null;
            bool isGun = handGun != null;
            bool HasSightAttachment(string id)
                => handHolder != null && handHolder.attachmentIds != null && handHolder.attachmentIds.Contains(id);
            bool hasHhs1 = isGun && HasSightAttachment(Hhs1ItemSystem.ItemKey);
            bool hasSpecterDr = isGun && HasSightAttachment(SpecterDrItemSystem.ItemKey);
            bool hasRazorHd = isGun && HasSightAttachment(RazorHdItemSystem.ItemKey);
            bool hasPm2 = isGun && HasSightAttachment(Pm2ItemSystem.ItemKey);
            bool hasTa01 = isGun && HasSightAttachment(Ta01nsnItemSystem.ItemKey);
            bool hasMonstr = isGun && HasSightAttachment(Monstr2x32ItemSystem.ItemKey);
            bool hasZoomSight = hasHhs1 || hasSpecterDr || hasRazorHd || hasPm2 || hasTa01 || hasMonstr;

            if (!isGun)
            {
                Hide(__instance.gunMenu.transform, ZoomTextObjectName);
                Hide(__instance.gunMenu.transform, CheckMagButtonName);
                Hide(__instance.gunMenu.transform, AmmoTextObjectName);
                return;
            }

            // 游戏像素字体
            var gameFont = __instance.timescaleText != null ? __instance.timescaleText.font : null;

            // ===== 倍率显示（所有变倍瞄具，zoom.png 背景）=====
            if (hasZoomSight)
            {
                if (_cachedZoomGunId != handItem.GetInstanceID())
                {
                    _cachedZoomGunId = handItem.GetInstanceID();
                    _cachedZoomHhs = handItem.GetComponent<Hhs1Controller>();
                    _cachedZoomSpec = handItem.GetComponent<SpecterDrController>();
                    _cachedZoomRazor = handItem.GetComponent<RazorHdController>();
                    _cachedZoomPm2 = handItem.GetComponent<Pm2Controller>();
                }

                string zoomLabel;
                if (hasHhs1)
                {
                    zoomLabel = _cachedZoomHhs != null && _cachedZoomHhs.IsZoomed ? "3x" : "1x";
                }
                else if (hasSpecterDr)
                {
                    zoomLabel = _cachedZoomSpec != null && _cachedZoomSpec.IsZoomed ? "4x" : "1x";
                }
                else if (hasRazorHd)
                {
                    // Razor HD 三模式：1x / 3x / 6x（GetModeLabel 直接返回当前模式）
                    zoomLabel = _cachedZoomRazor != null ? _cachedZoomRazor.GetModeLabel() : "1x";
                }
                else if (hasPm2)
                {
                    // PM II 四模式：1x / 3x / 6x / 8x
                    zoomLabel = _cachedZoomPm2 != null ? _cachedZoomPm2.GetModeLabel() : "1x";
                }
                else if (hasTa01)
                {
                    // TA01NSN 单模式缩放：固定 4x，只有放大，无 off
                    zoomLabel = "4x";
                }
                else // hasMonstr
                {
                    // Monstr 2x32 单模式缩放：固定 2x，只有放大，无 off
                    zoomLabel = "2x";
                }

                var zoomRoot = EnsureRoot(__instance.gunMenu.transform, ZoomTextObjectName,
                    __instance.gunSafeImage, 250f, 56f, 40f);
                zoomRoot.gameObject.SetActive(true);

                var bg = zoomRoot.GetComponent<Image>();
                if (bg == null) bg = zoomRoot.gameObject.AddComponent<Image>();
                bg.sprite = GetZoomSprite();
                bg.color = Color.white;
                bg.raycastTarget = false;

                var zoomLabelTrans = zoomRoot.transform.Find("Label");
                if (zoomLabelTrans == null)
                {
                    var labelGo = new GameObject("Label");
                    labelGo.transform.SetParent(zoomRoot.transform, false);
                    var lr = labelGo.AddComponent<RectTransform>();
                    lr.anchorMin = Vector2.zero;
                    lr.anchorMax = Vector2.one;
                    lr.offsetMin = Vector2.zero;
                    lr.offsetMax = Vector2.zero;
                    var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                    tmp.fontSize = 28;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                    tmp.raycastTarget = false;
                    tmp.enableWordWrapping = false;
                    if (gameFont != null) tmp.font = gameFont;
                }
                var zoomTmp = zoomLabelTrans.GetComponent<TextMeshProUGUI>();
                if (zoomTmp != null)
                {
                    zoomTmp.text = zoomLabel;
                    if (gameFont != null) zoomTmp.font = gameFont;
                }
            }
            else
            {
                Hide(__instance.gunMenu.transform, ZoomTextObjectName);
            }

            // ===== 检查弹匣按钮（任何枪械都显示，倍率左侧）=====
            // 缓存组件：gunMenu 是单例 UI，按钮只创建一次，避免每帧 GetComponent
            var btnRoot = EnsureRoot(__instance.gunMenu.transform, CheckMagButtonName,
                __instance.gunSafeImage, 250f - 60f, 52f, 52f);
            btnRoot.gameObject.SetActive(true);

            Image btnImg;
            Button btn;
            if (btnRoot == _cachedCheckMagRoot && _cachedCheckMagImg != null && _cachedCheckMagBtn != null)
            {
                btnImg = _cachedCheckMagImg;
                btn = _cachedCheckMagBtn;
            }
            else
            {
                btnImg = btnRoot.GetComponent<Image>();
                if (btnImg == null) btnImg = btnRoot.gameObject.AddComponent<Image>();
                btn = btnRoot.GetComponent<Button>();
                if (btn == null)
                {
                    btn = btnRoot.gameObject.AddComponent<Button>();
                    btn.targetGraphic = btnImg;
                    btn.onClick.AddListener(() => OnCheckMagClicked(__instance));
                }
                _cachedCheckMagRoot = btnRoot;
                _cachedCheckMagImg = btnImg;
                _cachedCheckMagBtn = btn;
            }
            btnImg.sprite = GetCheckMagSprite();
            btnImg.color = Color.white;
            btnImg.raycastTarget = true;
            // 禁用键盘导航（空格/回车触发），只允许鼠标左键点击
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            // ===== 弹药量显示（4 秒渐隐，快照）=====
            if (Time.unscaledTime < _ammoShowUntil)
            {
                EnsureAmmoText(__instance, _ammoSnapshot, gameFont);
            }
            else
            {
                Hide(__instance.gunMenu.transform, AmmoTextObjectName);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[HHS-1 UI] Failed: {ex.Message}");
        }
    }

    // ===== 检查弹匣点击 =====

    public static void OnCheckMagClicked(PlayerCamera cam)
    {
        if (cam == null) return;
        try
        {
            var body = cam.body;
            if (body == null) return;
            var gunItem = body.GetItem(body.handSlot);
            var gun = gunItem != null ? gunItem.GetComponent<GunScript>() : null;
            if (gun == null) return;

            // 显示期间再按无效
            if (Time.unscaledTime < _ammoShowUntil) return;

            // 抑制开火（点击按钮瞬间，避免误开火）
            SuppressFire = true;
            _suppressUntil = Time.unscaledTime + 0.2f;

            // 播放卸下弹匣音效：
            // - SKS 是例外：无论 Direct 还是 Mag 模式，都播放 SKS 自己的拉开枪栓音效
            // - 其他 Direct 枪：播放各自的拉开枪栓音效（gun.customRack，无则回退原版）
            // - 其他 Mag 模式枪：播放该枪自定义卸弹匣音效（无自定义则回退原版）
            //   （若未安装弹匣则不播放插拔音效）
            bool isSks = gunItem.id == SKSItemSystem.ItemKey;
            _hadMag = gun.hasMag;
            if (isSks || gun.feedType == GunScript.FeedType.Direct)
            {
                if (gun.customRack != null)
                    Sound.Play(gun.customRack, gunItem.transform.position, volume: 1.5f);
                else
                    Sound.Play("gunrack", gunItem.transform.position, volume: 1.5f);
            }
            else if (_hadMag)
            {
                var customSound = GunUnloadMagPatch.GetMagOutSound(gunItem);
                if (customSound != null)
                    Sound.Play(customSound, gunItem.transform.position, volume: 1.5f);
                else
                    Sound.Play("gununloadmag", gunItem.transform.position, volume: 1.5f);
            }

            // 按下瞬间快照弹药量
            int rounds = gun.roundsInMag;
            int capacity = GunUnloadMagPatch.GetMagCapacity(gunItem);
            _ammoSnapshot = capacity <= 0 ? WModLoc.Tr("wm.hotkeys.empty", "空") : $"{rounds}/{capacity}";

            // 1 秒显示（期间抑制枪械操作），1 秒后播放装弹匣音效并渐隐
            _ammoShowStart = Time.unscaledTime;
            _ammoShowUntil = Time.unscaledTime + 1f;
            _checkingMag = true;
            _checkMagEnd = Time.unscaledTime + 1f;
            _magInPlayed = false;

            Plugin.Log.LogInfo($"[HHS-1] Check mag: {_ammoSnapshot}.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[HHS-1] CheckMag failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 播放装上弹匣音效（检查弹匣 1 秒后）：
    /// - SKS 是例外：播放 SKS 自己的闭合枪栓音效（gun.customRack，无则回退原版）
    /// - 其他 Direct 枪：播放各自的闭合枪栓音效（gun.customRack，无则回退原版）
    /// - 其他 Mag 模式枪：播放该枪自定义装弹匣音效（无自定义则回退原版）
    /// 返回音效时长（秒），供调用方延长检查结束时间。
    /// </summary>
    private static float PlayMagInSound(PlayerCamera cam)
    {
        try
        {
            var body = cam.body;
            if (body == null) return 0f;
            var gunItem = body.GetItem(body.handSlot);
            var gun = gunItem != null ? gunItem.GetComponent<GunScript>() : null;
            if (gun == null) return 0f;

            bool isSks = gunItem.id == SKSItemSystem.ItemKey;
            if (isSks || gun.feedType == GunScript.FeedType.Direct)
            {
                if (gun.customRack != null)
                {
                    Sound.Play(gun.customRack, gunItem.transform.position, volume: 1.5f);
                    return gun.customRack.length;
                }
                Sound.Play("gunrack", gunItem.transform.position, volume: 1.5f);
                return 0f;
            }
            else
            {
                // Mag 模式枪未安装弹匣：不播放装弹匣音效（检查立即结束）
                if (!_hadMag) return 0f;

                var customSound = GunLoadMagPatch.GetMagInSound(gunItem);
                if (customSound != null)
                {
                    Sound.Play(customSound, gunItem.transform.position, volume: 1.5f);
                    return customSound.length;
                }
                Sound.Play("gunloadmag", gunItem.transform.position, volume: 1.5f);
                return 0f;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[HHS-1] MagIn sound failed: {ex.Message}");
            return 0f;
        }
    }

    private static void EnsureAmmoText(PlayerCamera cam, string snapshot, TMP_FontAsset? gameFont)
    {
        var ammoRoot = cam.gunMenu.transform.Find(AmmoTextObjectName);
        if (ammoRoot == null)
        {
            var go = new GameObject(AmmoTextObjectName);
            go.transform.SetParent(cam.gunMenu.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(200f, 40f);
            rt.anchoredPosition = new Vector2(0f, 60f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.green;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            if (gameFont != null) tmp.font = gameFont;
            ammoRoot = go.transform;
        }

        ammoRoot.gameObject.SetActive(true);
        var ammoTmp = ammoRoot.GetComponent<TextMeshProUGUI>();
        if (ammoTmp != null)
        {
            ammoTmp.text = snapshot;
            if (gameFont != null) ammoTmp.font = gameFont;

            // 1 秒显示：最后 0.5 秒透明度从 1 降到 0（渐隐）
            float elapsed = Time.unscaledTime - _ammoShowStart;
            float fadeStart = 0.5f; // 0.5 秒后开始渐隐，0.5 秒内淡出
            if (elapsed > fadeStart)
            {
                float t = Mathf.Clamp01((elapsed - fadeStart) / 0.5f);
                var c = ammoTmp.color;
                c.a = 1f - t;
                ammoTmp.color = c;
            }
            else
            {
                var c = ammoTmp.color;
                c.a = 1f;
                ammoTmp.color = c;
            }
        }
    }

    // ===== 工具方法 =====

    private static void Hide(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) t.gameObject.SetActive(false);
    }

    private static Transform EnsureRoot(Transform parent, string name, Image anchor, float xOffset, float w, float h)
    {
        var t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(
            anchor.rectTransform.anchoredPosition.x + anchor.rectTransform.sizeDelta.x + xOffset,
            anchor.rectTransform.anchoredPosition.y);
        // 设为 UI 层：让 UIUtil.IsPointerOverUIElement() 检测到鼠标在 UI 上，
        // 从而 HandleAttacks 自动跳过开火（与游戏原版 gunMenu 按钮机制一致）
        go.layer = LayerMask.NameToLayer("UI");
        return go.transform;
    }

    private static Sprite? GetZoomSprite()
    {
        if (_zoomSprite != null) return _zoomSprite;
        try
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var path = Path.Combine(dir, "Framework", "Assets", "guns", "common", "zoom.png");
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(tex, bytes, false))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _zoomSprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch { }
        return _zoomSprite;
    }

    private static Sprite? GetCheckMagSprite()
    {
        if (_checkMagSprite != null) return _checkMagSprite;
        try
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var path = Path.Combine(dir, "Framework", "Assets", "guns", "common", "checkmag.png");
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(tex, bytes, false))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _checkMagSprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch { }
        return _checkMagSprite;
    }
}