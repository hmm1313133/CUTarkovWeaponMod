using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Night vision effect controller.
/// Press N to toggle NVG on/off.
/// Optimized: pre-generated noise textures, cached object references.
/// </summary>
public static class NightVisionController
{
    private static bool _nvgActive;
    private static bool _initialized;

    private const KeyCode ToggleKey = KeyCode.N;

    // Per-NVG drain rates
    private const float StandardDrainRate = 1f / 600f; // 10 minutes per full charge
    private const float Gpnvg18DrainRate = 1f / 360f;  // 6 minutes per full charge
    private const float Pvs31aDrainRate = 1f / 960f;   // 16 minutes per full charge

    // Current NVG visual profile (set at TurnOn based on equipped NVG)
    private static float _curVignetteClearRadius = 0.8f;
    private static float _curNoiseAlpha = 0.05f;
    private static Color _curGreenTint = new(0f, 0.6f, 0.15f, 0.3f);
    private static float _curAmbientIntensity = 2.5f;
    private static Color _curAmbientColor = new(0.2f, 0.85f, 0.2f, 1f);
    private static float _vignetteClearRadius = -1f; // Track which radius the cached texture uses

    // Cached references (found once in TurnOn)
    private static Light2D? _ambientLight;
    private static float _origAmbientIntensity;
    private static Color _origAmbientColor;
    private static bool _origsSaved;

    // Pre-generated noise textures (cycled, no per-frame generation)
    private static readonly Texture2D[] _noiseTextures = new Texture2D[4];
    private static int _noiseIndex;
    private static int _noiseFrameCounter;

    // Canvas + overlay elements
    private static GameObject? _canvasObj;
    private static Image? _greenImg;
    private static Image? _noiseImg;
    private static Texture2D? _noiseWorkTex;
    private static Image? _vignetteImg;
    private static Texture2D? _vignetteTex;

    private static AudioClip? _cachedToggleSound;

    private static int _debugFrameCounter;

    public static bool IsNvgActive => _nvgActive;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        PreGenerateNoiseTextures();
        Plugin.Log.LogInfo("[NVG] NightVisionController initialized (static).");
    }

    private static void PreGenerateNoiseTextures()
    {
        for (int i = 0; i < _noiseTextures.Length; i++)
        {
            _noiseTextures[i] = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            _noiseTextures[i].filterMode = FilterMode.Bilinear;
            _noiseTextures[i].wrapMode = TextureWrapMode.Repeat;
            var pixels = _noiseTextures[i].GetPixels();
            for (int j = 0; j < pixels.Length; j++)
            {
                // Sparse subtle noise: only 15% of pixels are dim
                if (UnityEngine.Random.value < 0.15f)
                {
                    var v = UnityEngine.Random.Range(0.2f, 0.6f);
                    pixels[j] = new Color(v, v, v, 1f);
                }
                else
                {
                    pixels[j] = new Color(0f, 0f, 0f, 1f);
                }
            }
            _noiseTextures[i].SetPixels(pixels);
            _noiseTextures[i].Apply();
        }
    }

    public static void Tick()
    {
        if (!_initialized) return;

        _debugFrameCounter++;
        if (_debugFrameCounter % 300 == 0)
        {
            var nvg = GetEquippedNVG();
            Plugin.Log.LogInfo(
                $"[NVG] Tick #{_debugFrameCounter}, active={_nvgActive}, " +
                $"nvg={(nvg != null ? nvg.id : "null")}, " +
                $"battery={nvg?.battery?.hasCharge}");
        }

        if (Input.GetKeyDown(ToggleKey))
        {
            Plugin.Log.LogInfo("[NVG] N key detected, toggling.");
            ToggleNVG();
        }

        // Check: if NVG is equipped but helmet removed, drop NVG
        var equippedNvg = GetEquippedNVG();
        if (equippedNvg != null)
        {
            var body = PlayerCamera.main?.body;
            if (body != null)
            {
                var helmet = body.GetWearableBySlotID("hat");
                if (helmet == null || !IsCompatibleHelmetId(helmet.id, equippedNvg.id))
                {
                    if (_nvgActive) TurnOff();
                    body.DropWearable(equippedNvg);
                    Plugin.Log.LogInfo("[NVG] Helmet removed, NVG dropped to ground.");
                }
            }
        }

        if (_nvgActive)
        {
            var nvg = GetEquippedNVG();
            if (nvg == null)
            {
                TurnOff();
                Plugin.Log.LogInfo("[NVG] NVG unequipped, turning off.");
            }
            else if (nvg.condition <= 0f)
            {
                TurnOff();
                Plugin.Log.LogInfo("[NVG] Battery depleted, turning off.");
            }
            else
            {
                // Check helmet still equipped
                var body = PlayerCamera.main?.body;
                if (body != null)
                {
                    var helmet = body.GetWearableBySlotID("hat");
                    if (helmet == null || !IsCompatibleHelmetId(helmet.id, nvg.id))
                    {
                        TurnOff();
                        Plugin.Log.LogInfo("[NVG] Helmet removed, turning off NVG.");
                    }
                    else
                    {
                        nvg.condition -= GetDrainRate() * Time.deltaTime;
                        if (nvg.condition <= 0f)
                        {
                            nvg.condition = 0f;
                            TurnOff();
                            Plugin.Log.LogInfo("[NVG] Battery depleted, turning off.");
                        }
                    }
                }
            }

            // Re-apply AmbientLight using cached reference (no GameObject.Find)
            if (_ambientLight != null)
            {
                _ambientLight.intensity = _curAmbientIntensity;
                _ambientLight.color = _curAmbientColor;
            }

            // Cycle noise texture every 8 frames (just swap sprite, no generation)
            _noiseFrameCounter++;
            if (_noiseFrameCounter % 8 == 0)
            {
                _noiseIndex = (_noiseIndex + 1) % _noiseTextures.Length;
                if (_noiseImg != null && _noiseWorkTex != null)
                {
                    _noiseWorkTex.SetPixels(_noiseTextures[_noiseIndex].GetPixels());
                    _noiseWorkTex.Apply();
                }
            }
        }
    }

    public static void OnGUI()
    {
        // No OnGUI - vignette uses Canvas (ScreenSpaceCamera, doesn't block UI)
    }

    private static void ToggleNVG()
    {
        if (_nvgActive) { TurnOff(); return; }

        var nvg = GetEquippedNVG();
        if (nvg == null)
        {
            Plugin.Log.LogInfo("[NVG] No NVG equipped.");
            return;
        }

        // Ensure BatteryItem component exists (may be lost during save/load)
        EnsureNVGBattery(nvg);

        if (nvg.condition <= 0f)
        {
            Plugin.Log.LogInfo($"[NVG] Battery dead (condition={nvg.condition}), cannot activate.");
            return;
        }

        TurnOn();
    }

    /// <summary>
    /// 确保 NVG 物品上有 BatteryItem 组件（存档加载后可能丢失）。
    /// </summary>
    private static void EnsureNVGBattery(Item nvg)
    {
        var bat = nvg.GetComponent<BatteryItem>();
        if (bat == null)
        {
            bat = nvg.gameObject.AddComponent<BatteryItem>();
            bat.preset = BatteryItem.BatteryPreset.Medium;
            bat.maxAllowedCharge = 100f;
            bat.batteryType = "mediumbattery";
            bat.maxCharge = 100f;
            if (nvg.condition <= 0f)
                nvg.SetCondition(1f);
            Plugin.Log.LogInfo($"[NVG] BatteryItem was missing on '{nvg.id}', added dynamically (condition={nvg.condition}).");
        }
    }

    private static void PlayToggleSound()
    {
        if (_cachedToggleSound == null)
        {
            try
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? BepInEx.Paths.PluginPath;
                var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "equipment", "trigger.wav");
                if (File.Exists(soundPath))
                {
                    using var uwr = UnityWebRequestMultimedia.GetAudioClip("file:///" + soundPath, AudioType.WAV);
                    uwr.SendWebRequest();
                    while (!uwr.isDone) { }
                    if (uwr.result == UnityWebRequest.Result.Success)
                        _cachedToggleSound = DownloadHandlerAudioClip.GetContent(uwr);
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning($"[NVG] Failed to load toggle sound: {ex.Message}"); }
        }
        if (_cachedToggleSound != null)
            AudioSource.PlayClipAtPoint(_cachedToggleSound, Camera.main?.transform.position ?? Vector3.zero, 0.8f);
    }

    private static void TurnOn()
    {
        _nvgActive = true;
        PlayToggleSound();

        // Find and cache AmbientLight ONCE
        if (!_origsSaved)
        {
            var ambientObj = GameObject.Find("AmbientLight");
            if (ambientObj != null)
            {
                _ambientLight = ambientObj.GetComponent<Light2D>();
                if (_ambientLight != null)
                {
                    _origAmbientIntensity = _ambientLight.intensity;
                    _origAmbientColor = _ambientLight.color;
                }
            }
            _origsSaved = true;
        }

        ApplyNvgProfile();
        CreateOverlayCanvas();
        Plugin.Log.LogInfo("[NVG] Night vision activated.");
    }

    private static void CreateOverlayCanvas()
    {
        if (_canvasObj != null) return;

        var cam = Camera.main;
        if (cam == null) return;

        _canvasObj = new GameObject("NVG_Canvas");
        var canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // Below game UI and status effect overlays (e.g. temperature frost)
        var scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _canvasObj.AddComponent<GraphicRaycaster>();

        // Green tint
        var greenObj = new GameObject("GreenTint");
        greenObj.transform.SetParent(_canvasObj.transform, false);
        _greenImg = greenObj.AddComponent<Image>();
        _greenImg.color = _curGreenTint;
        _greenImg.raycastTarget = false;
        SetFullStretch(_greenImg.rectTransform);

        // Noise (using pre-generated texture)
        EnsureVignetteTexture(_curVignetteClearRadius);
        var noiseObj = new GameObject("Noise");
        noiseObj.transform.SetParent(_canvasObj.transform, false);
        _noiseImg = noiseObj.AddComponent<Image>();
        _noiseImg.color = new Color(1f, 1f, 1f, _curNoiseAlpha);
        _noiseImg.raycastTarget = false;
        _noiseWorkTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        _noiseWorkTex.filterMode = FilterMode.Bilinear;
        _noiseWorkTex.wrapMode = TextureWrapMode.Repeat;
        _noiseWorkTex.SetPixels(_noiseTextures[0].GetPixels());
        _noiseWorkTex.Apply();
        _noiseImg.sprite = Sprite.Create(_noiseWorkTex,
            new Rect(0, 0, 128, 128), Vector2.one * 0.5f, 100f);
        SetFullStretch(_noiseImg.rectTransform);

        // Vignette: 4 solid black gradient Images (top/bottom/left/right)
        CreateVignetteBorders(_canvasObj);

        Plugin.Log.LogInfo("[NVG] Overlay canvas created (ScreenSpaceOverlay, sortingOrder=0).");
    }

    private static void CreateVignetteBorders(GameObject parent)
    {
        // Single radial gradient texture - smooth circular vignette
        EnsureVignetteTexture(_curVignetteClearRadius);
        if (_vignetteTex == null) return;

        var vigObj = new GameObject("Vignette");
        vigObj.transform.SetParent(parent.transform, false);
        _vignetteImg = vigObj.AddComponent<Image>();
        _vignetteImg.color = Color.white;
        _vignetteImg.raycastTarget = false;
        _vignetteImg.sprite = Sprite.Create(_vignetteTex,
            new Rect(0, 0, _vignetteTex.width, _vignetteTex.height), Vector2.one * 0.5f, 100f);
        SetFullStretch(_vignetteImg.rectTransform);
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void EnsureVignetteTexture(float clearRadius = 0.8f)
    {
        if (_vignetteTex != null && Mathf.Approximately(_vignetteClearRadius, clearRadius)) return;
        const int size = 512;
        _vignetteTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _vignetteTex.filterMode = FilterMode.Bilinear;
        _vignetteTex.wrapMode = TextureWrapMode.Clamp; // Prevent edge sampling from transparent center
        var center = new Vector2(size / 2f, size / 2f);
        var maxDist = size / 2f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Power curve: smooth gradient up to clearRadius, pure black beyond
                float alpha;
                if (dist >= clearRadius)
                    alpha = 1f;
                else
                    alpha = Mathf.Pow(dist / clearRadius, 2.5f);
                pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
            }
        }
        // Force outermost 8px border to fully opaque
        for (int i = 0; i < size; i++)
        {
            for (int b = 0; b < 8; b++)
            {
                pixels[b * size + i] = new Color(0, 0, 0, 1f);
                pixels[(size - 1 - b) * size + i] = new Color(0, 0, 0, 1f);
                pixels[i * size + b] = new Color(0, 0, 0, 1f);
                pixels[i * size + (size - 1 - b)] = new Color(0, 0, 0, 1f);
            }
        }
        _vignetteTex.SetPixels(pixels);
        _vignetteTex.Apply();
        _vignetteClearRadius = clearRadius;
    }

    private static void TurnOff()
    {
        if (!_nvgActive) return;
        _nvgActive = false;
        PlayToggleSound();

        if (_canvasObj != null)
        {
            UnityEngine.Object.Destroy(_canvasObj);
            _canvasObj = null;
            _greenImg = null;
            _noiseImg = null;
            _vignetteImg = null;
        }

        if (_origsSaved && _ambientLight != null)
        {
            _ambientLight.intensity = _origAmbientIntensity;
            _ambientLight.color = _origAmbientColor;
        }

        Plugin.Log.LogInfo("[NVG] Night vision deactivated.");
    }

    private static Item? GetEquippedNVG()
    {
        var body = PlayerCamera.main?.body;
        if (body == null) return null;
        var item = body.GetWearableBySlotID("eyes");
        if (item == null) return null;
        if (!item.id.Equals(Pvs14ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase) &&
            !item.id.Equals(Gpnvg18ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase) &&
            !item.id.Equals(Pvs31aItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            return null;
        return item;
    }

    /// <summary>
    /// Returns the drain rate for the currently equipped NVG.
    /// GPNVG-18: 6 minutes (1/360), standard NVG/PVS-14: 10 minutes (1/600).
    /// </summary>
    private static float GetDrainRate()
    {
        var nvg = GetEquippedNVG();
        if (nvg != null)
        {
            if (nvg.id.Equals(Gpnvg18ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                return Gpnvg18DrainRate;
            if (nvg.id.Equals(Pvs31aItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
                return Pvs31aDrainRate;
        }
        return StandardDrainRate;
    }

    /// <summary>
    /// Sets the visual profile based on which NVG is equipped.
    /// GPNVG-18: wider clear area (95%), minimal noise, brighter green.
    /// Standard: 80% clear area, normal noise, standard green.
    /// </summary>
    private static void ApplyNvgProfile()
    {
        var nvg = GetEquippedNVG();
        if (nvg != null &&
            nvg.id.Equals(Gpnvg18ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
        {
            // GPNVG-18: panoramic 97° FOV → 95% visible area, almost no noise, brighter green
            _curVignetteClearRadius = 0.95f;
            _curNoiseAlpha = 0.01f;
            _curGreenTint = new Color(0.12f, 0.8f, 0.35f, 0.22f);
            _curAmbientIntensity = 3.8f;
            _curAmbientColor = new Color(0.45f, 0.95f, 0.45f, 1f);
            Plugin.Log.LogInfo("[NVG] Profile: GPNVG-18 (wide FOV, bright green, minimal noise).");
        }
        else if (nvg != null &&
            nvg.id.Equals(Pvs31aItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
        {
            // PVS-31A: white phosphor → light cyan tint, 99.4% visible area, zero noise
            _curVignetteClearRadius = 0.994f;
            _curNoiseAlpha = 0f;
            _curGreenTint = new Color(0.25f, 0.88f, 0.95f, 0.15f);
            _curAmbientIntensity = 4.2f;
            _curAmbientColor = new Color(0.5f, 0.95f, 1f, 1f);
            Plugin.Log.LogInfo("[NVG] Profile: PVS-31A (99.4% visible, no noise, light cyan).");
        }
        else
        {
            // Standard NVG / PVS-14
            _curVignetteClearRadius = 0.8f;
            _curNoiseAlpha = 0.05f;
            _curGreenTint = new Color(0f, 0.6f, 0.15f, 0.3f);
            _curAmbientIntensity = 2.5f;
            _curAmbientColor = new Color(0.2f, 0.85f, 0.2f, 1f);
            Plugin.Log.LogInfo("[NVG] Profile: Standard (80% vignette, normal noise).");
        }
    }

    public static void Shutdown()
    {
        TurnOff();
        _initialized = false;
    }

    // ─── WearWearable helmet check (called via manual Harmony patch) ───

    private static bool IsCompatibleHelmetId(string helmetId, string nvgId)
    {
        // GPNVG-18 and PVS-31A are NOT compatible with 6B47
        if (nvgId.Equals(Gpnvg18ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase) ||
            nvgId.Equals(Pvs31aItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
        {
            return helmetId.Equals("calman", StringComparison.OrdinalIgnoreCase) ||
                   helmetId.Equals("fastmt", StringComparison.OrdinalIgnoreCase);
        }
        // PVS-14 and standard NVG are compatible with all three helmets
        return helmetId.Equals("6b47", StringComparison.OrdinalIgnoreCase) ||
               helmetId.Equals("calman", StringComparison.OrdinalIgnoreCase) ||
               helmetId.Equals("fastmt", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Harmony Prefix for Body.WearWearable. Prevents wearing NVG/PVS-14 without compatible helmet.
    /// </summary>
    public static bool WearWearablePrefix(Body __instance, Item item)
    {
        if (item == null) return true;
        var id = item.id;
        if (!id.Equals("pvs14", StringComparison.OrdinalIgnoreCase) &&
            !id.Equals(Gpnvg18ItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase) &&
            !id.Equals(Pvs31aItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase))
            return true;

        var helmet = __instance.GetWearableBySlotID("hat");
        if (helmet == null || !IsCompatibleHelmetId(helmet.id, id))
        {
            Plugin.Log.LogInfo(
                $"[NVG] Cannot wear {id}: no compatible helmet (has: {helmet?.id ?? "none"}).");
            return false;
        }
        Plugin.Log.LogInfo($"[NVG] {id} equipped with helmet '{helmet.id}'.");
        return true;
    }
}
