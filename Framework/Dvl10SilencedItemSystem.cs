using System;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// DVL-10 7.62x51 500 毫米消音枪管枪口组合【DVL-10 silenced】。
/// LOBAEV Hummer Barrels 为 DVL-10 狙击步枪制造的 7.62x51 口径竞赛级不锈钢枪管，
/// 长度 500 毫米，装有一体式消音器和配套膛口装置。
///
/// 效果（由 SuppressorSystem.FireEffectsPatch 读取 GunAttachmentHolder 应用）：
/// - 后坐力 -15%（knockBack × 0.85）
/// - 瞄准速度 -0.25s（AimSystem.AttachmentAimTimeDelta -0.25，加快）
/// - 精准度 +5%（散布 × 0.95，更准）
///
/// 安装后：
/// - 直接替换整枪贴图为 dvl10silenced.png（magout 格式也在文件夹）
/// - 更换枪声为消音版 dvl_silenced.wav
/// - 不可安装战术设备（互斥）
///
/// 世界贴图使用 dvl10silenced_piece.png。
/// DVL 专属枪口槽。
/// 交互：改枪面板（G 键）安装/卸下。
/// </summary>
public static class Dvl10SilencedItemSystem
{
    public const string ItemKey = "dvl10_silenced";
    public const string BaseGameItemId = "bruisekit";

    public static string DisplayName => I18n.Tr("dvl10_silenced.name");
    public static string Description => I18n.Tr("dvl10_silenced.desc");

    public const float KnockBackMult = 0.85f;   // 后坐力 -15%
    public const float SpreadMult = 0.95f;      // 精准度 +5%（散布 ×0.95，更准）
    public const float AimTimeDelta = -0.25f;   // 瞄准速度 -0.25s（加快）

    private const float Weight = 0.35f;
    private const int Value = 0;
    private const int RecognitionMin = 5;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedVisualIcon;
    private static AudioClip? _cachedSilencedSound;

    public static bool IsDvl10SilencedRequest(MedicalGrantRequest request)
        => request != null && request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsDvl10SilencedRequest(request)) return;
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
        Plugin.Log.LogInfo($"[DVL-10 silenced] Configured spawned item '{ItemKey}'.");
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
            Plugin.Log.LogInfo($"[DVL-10 silenced] Registered '{ItemKey}' as attachment.");
            return true;
        }
        catch (Exception ex) { Plugin.Log.LogError($"[DVL-10 silenced] Failed: {ex}"); return false; }
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
        Plugin.Log.LogInfo($"[DVL-10 silenced] CUCoreLib: Icon={customInfo.Icon != null}.");
    }

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;
        _cachedIcon = LoadSprite("dvl10silenced_piece.png", "dvl10-silenced-icon");
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
        _cachedVisualIcon = LoadSprite("dvl10silenced_piece.png", "dvl10-silenced-visual");
        return _cachedVisualIcon;
    }

    /// <summary>加载消音整枪贴图（安装后替换 DVL-10 整枪贴图）。</summary>
    public static Sprite? TryLoadSilencedGunIcon()
        => LoadSprite("dvl10silenced.png", "dvl10-silenced-gun");

    /// <summary>加载消音整枪无弹匣贴图（安装后替换 DVL-10 无弹匣贴图）。</summary>
    public static Sprite? TryLoadSilencedGunNoMagIcon()
        => LoadSprite("dvl10silenced_magout.png", "dvl10-silenced-gun-nomag");

    /// <summary>加载消音枪声（安装后替换 DVL-10 开火音效）。</summary>
    public static AudioClip? TryLoadSilencedSound()
    {
        if (_cachedSilencedSound != null) return _cachedSilencedSound;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "dvl10", "dvl_silenced.wav");
            if (File.Exists(soundPath))
                _cachedSilencedSound = LoadWavSync(soundPath);
            if (_cachedSilencedSound != null)
                Plugin.Log.LogInfo("[DVL-10 silenced] Loaded silenced sound 'dvl_silenced.wav'.");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[DVL-10 silenced] Silenced sound: {ex.Message}"); }
        return _cachedSilencedSound;
    }

    private static Sprite? LoadSprite(string file, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "dvl10", file);
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes, false))
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    var spr = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.30f, 0.5f), 13.2f);
                    spr.name = name;
                    return spr;
                }
            }
            else Plugin.Log.LogWarning($"[DVL-10 silenced] Icon not found: {iconPath}");
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[DVL-10 silenced] Icon: {ex.Message}"); }
        return null;
    }

    private static AudioClip? LoadWavSync(string path)
    {
        try
        {
            var uri = new Uri(path);
            var request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
            request.SendWebRequest();
            while (!request.isDone) { }
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
                if (clip != null) clip.name = Path.GetFileNameWithoutExtension(path);
                return clip;
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[DVL-10 silenced] LoadWavSync: {ex.Message}"); }
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
