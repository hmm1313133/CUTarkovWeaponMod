using System;
using UnityEngine;
using UnityEngine.UI;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 自定义瞄准准星（4 条线，实时跟踪精准度）。
/// 间距与当前枪械实际精准度正相关：精准度 0 → 最小间距，0.8 → 最大间距。
/// </summary>
public static class AimCrosshair
{
    private const string RootName = "AimCrosshair";
    private const float MaxGap = 40f;   // 精准度 0.8（最宽）时十字间距
    private const float MinGap = 4f;    // 精准度 0（最小）时十字间距
    private const float LineLength = 12f;
    private const float LineThickness = 5f;   // 加粗

    private static GameObject? _root;
    private static RectTransform[] _lines = new RectTransform[4]; // 上、下、左、右
    private static bool _initialized;

    // ===== 每帧缓存（降低开销）=====
    private static Camera? _cachedCamera;
    private static int _cameraRefreshCounter;
    private static CanvasGroup? _cachedCrosshairCg;
    private static int _cachedCgCamId = int.MinValue;
    private static int _cachedGunItemId = int.MinValue;
    private static GunScript? _cachedGunScript;

    /// <summary>设置瞄准进度（0~1），按当前枪械精准度实时调整十字间距。</summary>
    public static void SetProgress(float progress, Item gunItem)
    {
        var cam = PlayerCamera.main;
        if (cam == null || cam.gunCrosshair == null) return;

        // 每帧隐藏游戏准星（游戏每帧重新激活它，需持续覆盖）
        int camId = cam.GetInstanceID();
        CanvasGroup cg;
        if (camId == _cachedCgCamId && _cachedCrosshairCg != null)
            cg = _cachedCrosshairCg;
        else
        {
            cg = cam.gunCrosshair.GetComponent<CanvasGroup>();
            if (cg == null) cg = cam.gunCrosshair.gameObject.AddComponent<CanvasGroup>();
            _cachedCrosshairCg = cg;
            _cachedCgCamId = camId;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        EnsureCreated(cam);
        if (_root == null) return;

        if (!_root.activeSelf) _root.SetActive(true);

        // 缓存 GunScript（同一把枪不重复 GetComponent）
        int gid = gunItem.GetInstanceID();
        if (gid != _cachedGunItemId) { _cachedGunItemId = gid; _cachedGunScript = gunItem.GetComponent<GunScript>(); }
        PositionAtGun(cam, gunItem, _cachedGunScript);

        // 间距与当前枪械精准度正相关：精准度 0 → 最小间距，0.8 → 最大间距
        var gun = _cachedGunScript;
        float currentSpread = 0.8f;
        if (gun != null)
        {
            float unaimedMult = AimSystem.GetUnaimedSpreadMult(gunItem, gun);
            float aimMult = Mathf.Lerp(unaimedMult, 1f, progress);
            currentSpread = gun.verticalSpread * aimMult;
        }
        float precisionT = Mathf.Clamp01(currentSpread / 0.8f);
        float gap = Mathf.Lerp(MinGap, MaxGap, precisionT);
        float half = gap * 0.5f + LineLength * 0.5f;

        SetLine(_lines[0], new Vector2(0f, half), new Vector2(LineThickness, LineLength));
        SetLine(_lines[1], new Vector2(0f, -half), new Vector2(LineThickness, LineLength));
        SetLine(_lines[2], new Vector2(-half, 0f), new Vector2(LineLength, LineThickness));
        SetLine(_lines[3], new Vector2(half, 0f), new Vector2(LineLength, LineThickness));
    }

    /// <summary>隐藏准星（无手持枪时调用，如丢枪/换背包）。</summary>
    public static void Hide()
    {
        if (_root != null && _root.activeSelf) _root.SetActive(false);
    }

    private static void PositionAtGun(PlayerCamera cam, Item gunItem, GunScript? gun)
    {
        try
        {
            var body = cam.body;
            if (body == null || gunItem == null || gun == null || gun.barrel == null) return;

            float num = Vector2.Distance(gun.barrel.transform.position, body.targetLookPos);
            if (!body.isRight) num *= -1f;
            Vector2 vector = gun.barrel.transform.position + gun.transform.right * num;
            var camera = GetMainCamera();
            if (camera == null) return;

            Vector3 screenPos = camera.WorldToScreenPoint(vector);
            if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y)
                || float.IsInfinity(screenPos.x) || float.IsInfinity(screenPos.y))
                return;
            _root.transform.position = screenPos;
        }
        catch { }
    }

    private static void SetLine(RectTransform line, Vector2 pos, Vector2 size)
    {
        if (line == null) return;
        line.anchoredPosition = pos;
        line.sizeDelta = size;
    }

    private static Camera? GetMainCamera()
    {
        if (_cachedCamera == null || (_cameraRefreshCounter++ % 60) == 0)
            _cachedCamera = Camera.main;
        return _cachedCamera;
    }

    private static void EnsureCreated(PlayerCamera cam)
    {
        if (_initialized && _root != null) return;
        _initialized = true;

        _root = new GameObject(RootName);
        _root.transform.SetParent(cam.mainCanvas.transform, false);
        var rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = Vector2.zero;

        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Line" + i);
            go.transform.SetParent(_root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            _lines[i] = rt;
        }
    }
}
