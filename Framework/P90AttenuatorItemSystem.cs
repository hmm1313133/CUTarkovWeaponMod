using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// FN P90 Attenuator 5.7x28 消音器【Attenuator】。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 后坐力 -10%（knockBack × 0.90）
/// - 瞄准速度 +0.3s（AimSystem.AttachmentAimTimeDelta）
/// - 消音枪声（p90_silenced.wav，P90 专属）
/// - 取消火光 + 枪管后移（与消音器一致）
///
/// 安装要求：
/// - 占用枪口槽：一把枪只能装一个枪口装置
/// - 仅 P90 可安装（P90 原厂只能改装枪口）
///
/// 视觉：20x4 枪口小贴图，叠加在 P90 枪口位置。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class P90AttenuatorItemSystem
{
    public const string ItemKey = "p90attenuator";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("p90attenuator.name");
    public static string Description => I18n.Tr("p90attenuator.desc");

    // 效果参数（用户指定）
    public const float AimTimeDelta = 0.3f;     // 瞄准速度 +0.3s
    public const float KnockBackMult = 0.90f;   // 后坐力 -10%

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedVisualIcon;
    private static AudioClip? _cachedSilencedSound;

    public static bool IsP90AttenuatorRequest(MedicalGrantRequest request) =>
        request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsP90AttenuatorRequest(request)) return;
        item.id = ItemKey;
        item.SetCondition(1f);
        item.Stats.usable = false;
        item.Stats.usableOnLimb = false;
        item.Stats.usableWithLMB = false;
        item.Stats.autoAttack = false;
        item.Stats.wearable = false;
        item.Stats.category = "utility";
        item.Stats.tags = "attachment,backflip";
        item.Stats.SetTags();
        item.Stats.weight = Weight;
        item.Stats.value = Value;

        var icon = TryLoadIcon();
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
        ResizeColliderToSprite(item);
        Plugin.Log.LogInfo($"[P90 Attenuator] Configured spawned item '{ItemKey}'.");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey)) return false;
        try
        {
            var info = new ItemInfo
            {
                fullName = DisplayName,
                description = Description,
                category = "utility",
                slotRotation = 0f,
                usable = false,
                usableOnLimb = false,
                usableWithLMB = false,
                autoAttack = false,
                destroyAtZeroCondition = true,
                weight = Weight,
                value = Value,
                tags = "attachment,backflip",
                rec = new Recognition(RecognitionMin),
            };
            info.SetTags();
            Item.GlobalItems[ItemKey] = info;
            Plugin.Log.LogInfo($"[P90 Attenuator] Registered '{ItemKey}' as attachment (tag=attachment).");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[P90 Attenuator] Failed: {ex}"); return false; }
    }

    public static void RegisterWithCUCoreLib(CustomItemInfo customInfo)
    {
        var icon = TryLoadIcon();
        if (icon != null)
        {
            customInfo.Icon = icon;
            customInfo.WornSprite = icon;
            customInfo.WornSpriteOffset = Vector2.zero;
        }
        Plugin.Log.LogInfo($"[P90 Attenuator] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "p90", "Attenuator.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedIcon = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 14f);
                    _cachedIcon.name = "p90attenuator-icon";
                }
            }
            else Plugin.Log.LogWarning($"[P90 Attenuator] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[P90 Attenuator] Icon: {ex.Message}"); }
        return _cachedIcon;
    }

    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    public static Sprite? TryLoadVisualIconPublic() => TryLoadVisualIcon();

    public static Texture2D? TryLoadOverlayTexturePublic()
    {
        var spr = TryLoadVisualIcon();
        return spr != null ? spr.texture : null;
    }

    private static Sprite? TryLoadVisualIcon()
    {
        if (_cachedVisualIcon != null) return _cachedVisualIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "p90", "Attenuator.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _cachedVisualIcon = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 14f);
                    _cachedVisualIcon.name = "p90attenuator-visual";
                }
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[P90 Attenuator] Visual icon: {ex.Message}"); }
        return _cachedVisualIcon;
    }

    /// <summary>加载 P90 专属消音枪声（p90_silenced.wav）。</summary>
    public static AudioClip? TryLoadSilencedSound()
    {
        if (_cachedSilencedSound != null) return _cachedSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "p90", "p90_silenced.wav");
            if (File.Exists(soundPath))
                _cachedSilencedSound = LoadWavSync(soundPath);
            if (_cachedSilencedSound != null)
                Plugin.Log.LogInfo("[P90 Attenuator] Loaded silenced sound 'p90_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[P90 Attenuator] Silenced sound: {ex.Message}"); }
        return _cachedSilencedSound;
    }

    private static AudioClip? LoadWavSync(string path)
    {
        try
        {
            using var uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.WAV);
            uwr.SendWebRequest();
            while (!uwr.isDone) { }
            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
        }
        catch { }
        return null;
    }

    private static void ResizeColliderToSprite(Item item)
    {
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        var col = item.GetComponent<BoxCollider2D>();
        if (col == null) col = item.gameObject.AddComponent<BoxCollider2D>();
        var bounds = sr.sprite.bounds;
        col.size = new Vector2(bounds.size.x, bounds.size.y);
        col.offset = Vector2.zero;
    }
}
