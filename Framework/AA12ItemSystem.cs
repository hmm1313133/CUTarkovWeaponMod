using System;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// MPS Auto Assault-12 Gen 1（AA-12）自动霰弹枪物品系统。
/// 
/// 使用 "rifle" 基础预制体，原生继承 Mag 供弹 + Auto 射击模式。
/// ammoType 在 ConfigureSpawnedItem 中覆盖为 Shotgun。
/// </summary>
public static class AA12ItemSystem
{
    public const string ItemKey = "aa12";
    public const string BaseGameItemId = "rifle";
    public const int MagCapacity = 20;
    private const float KnockBack = 6f;
    private const float AnimalDamage = 41f;
    private const float StructureDamage = 30f;
    private const float Loudness = 3.3f;
    private const int ShotsPerFire = 8;
    private const float VerticalSpread = 0.22f;
    private const float ConditionLossPerShot = 0.556f; // 100/180 ≈ 180发损坏
    private const float DesiredGasTime = 0.21f;

    public static string DisplayName => I18n.Tr("aa12.name");
    public static string Description => I18n.Tr("aa12.desc");

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedNoMagIcon;
    private static AudioClip? _cachedFireSound;

    public static bool IsAA12Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 配置生成的 AA-12 物品实例。
    /// </summary>
    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsAA12Request(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var gun = item.GetComponent<GunScript>();
        if (gun != null)
        {
            gun.magCapacity = MagCapacity;
            gun.roundsInMag = 0;
            gun.knockBack = KnockBack;
            gun.animalDamage = AnimalDamage;
            gun.structureDamage = StructureDamage;
            gun.loudness = Loudness;
            gun.shotsPerFire = ShotsPerFire;
            gun.verticalSpread = VerticalSpread;
            gun.conditionLossPerShot = ConditionLossPerShot;
            gun.desiredGasTime = DesiredGasTime;
            // rifle 基类原生 feedType=Mag、firingMode=Auto，无需覆盖。
            // 只覆盖弹药类型为 Shotgun。
            gun.ammoType = GunScript.AmmoType.Shotgun;
            Plugin.Log.LogInfo($"[AA12] GunScript ammoType={gun.ammoType}, feedType={gun.feedType}, firingMode={gun.firingMode}");

            var fireSound = TryLoadFireSound();
            if (fireSound != null)
                gun.fireSound = fireSound;

            // 拉栓/闭栓音效
            var rackSound = TryLoadSound("aa12_open", "aa12");
            if (rackSound != null)
                gun.customRack = rackSound;
            var unrackSound = TryLoadSound("aa12_close", "aa12");
            if (unrackSound != null)
                gun.customUnrack = unrackSound;

            // 图标
            var icon = TryLoadIcon();
            var noMagIcon = TryLoadNoMagIcon();
            if (icon != null)
            {
                gun.normalSprite = icon;
                gun.rackedSprite = icon;
                gun.normalSpriteNoMag = noMagIcon ?? icon;
                gun.rackedSpriteNoMag = noMagIcon ?? icon;

                var sr = item.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = icon;
            }

            // 调整枪管和火光位置
            if (gun.barrel != null)
                gun.barrel.localPosition += new Vector3(1.0f, 0f, 0f);
            if (gun.muzzleParticle != null)
                gun.muzzleParticle.transform.localPosition += new Vector3(1.5f, 0f, 0f);

            Plugin.Log.LogInfo($"[AA12] Configured GunScript: mag={MagCapacity}, dmg={AnimalDamage}×8, spread={VerticalSpread}, mode=Auto, feed=Mag");
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<AA12ItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<AA12ItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[AA12] Configured spawned item '{ItemKey}' (condition={item.condition}).");
    }

    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            if (Item.GlobalItems.TryGetValue(BaseGameItemId, out var source))
            {
                Item.GlobalItems[ItemKey] = CloneItemInfo(source);
                Plugin.Log.LogInfo($"[AA12] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[AA12] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[AA12] Failed to register '{ItemKey}': {ex}");
            return false;
        }
    }

    private static ItemInfo CloneItemInfo(ItemInfo source)
    {
        var clone = new ItemInfo
        {
            fullName = DisplayName,
            description = Description,
            category = source.category,
            slotRotation = source.slotRotation,
            usable = true,
            usableOnLimb = false,
            usableWithLMB = true,
            autoAttack = true,
            rotSpeed = source.rotSpeed,
            useAction = source.useAction,
            useLimbAction = null,
            destroyAtZeroCondition = false,
            combineable = true,
            weight = 2.5f,
            scaleWeightWithCondition = false,
            value = 48,
            tags = "cangetwet,gun,belttool",
            rec = new Recognition(8),
        };
        clone.SetTags();
        return clone;
    }

    private static ItemInfo CreateFallbackItemInfo()
    {
        var info = new ItemInfo
        {
            fullName = DisplayName,
            description = Description,
            category = "weapon",
            slotRotation = -90f,
            usable = true,
            usableOnLimb = false,
            usableWithLMB = true,
            autoAttack = true,
            rotSpeed = 3f,
            useLimbAction = null,
            destroyAtZeroCondition = false,
            combineable = true,
            weight = 2.5f,
            scaleWeightWithCondition = false,
            value = 48,
            tags = "cangetwet,gun,belttool",
            rec = new Recognition(8),
        };
        info.SetTags();
        return info;
    }

    // ===== Icon =====

    private static Sprite? TryLoadIcon()
    {
        if (_cachedIcon != null) return _cachedIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "aa12", "aa12.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.45f, 0.5f), 14f);
                _cachedIcon.name = "aa12-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[AA12] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    // ===== No-Mag Icon =====

    private static Sprite? TryLoadNoMagIcon()
    {
        if (_cachedNoMagIcon != null) return _cachedNoMagIcon;

        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "aa12", "aa12_magout.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedNoMagIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.45f, 0.5f), 14f);
                _cachedNoMagIcon.name = "aa12-nomag-icon";
                Plugin.Log.LogInfo("[AA12] Loaded no-mag icon 'aa12_magout.png'");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[AA12] Failed to load no-mag icon: {ex.Message}");
        }

        return _cachedNoMagIcon;
    }

    // ===== Sounds =====

    private static AudioClip? TryLoadFireSound()
    {
        if (_cachedFireSound != null) return _cachedFireSound;
        _cachedFireSound = TryLoadSound("aa12_fire", "aa12");
        if (_cachedFireSound != null)
            Plugin.Log.LogInfo("[AA12] Loaded fire sound 'aa12_fire.wav'");
        return _cachedFireSound;
    }

    private static AudioClip? TryLoadSound(string fileName, string gunDir)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var soundPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", gunDir, $"{fileName}.wav");
            if (File.Exists(soundPath))
                return LoadWavSync(soundPath);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[AA12] Failed to load sound '{fileName}': {ex.Message}");
        }
        return null;
    }

    // ===== Collider =====

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

    // ===== WAV Loader =====

    private static AudioClip? LoadWavSync(string path)
    {
        try
        {
            using var uwr = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.WAV);
            uwr.SendWebRequest();
            while (!uwr.isDone) { }
            if (uwr.result == UnityWebRequest.Result.Success)
                return DownloadHandlerAudioClip.GetContent(uwr);
        }
        catch { }
        return null;
    }
}

/// <summary>
/// AA-12 物品标记组件。
/// </summary>
public sealed class AA12ItemMarker : MonoBehaviour
{
    public string displayName = AA12ItemSystem.DisplayName;
    public string description = AA12ItemSystem.Description;
}

/// <summary>
/// AA-12 悬停描述补丁（已禁用，由 UnifiedHoverPatch 替代）。
/// </summary>
