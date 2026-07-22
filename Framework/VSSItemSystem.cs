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
/// VSS "绞丝机" 9x39 特种狙击步枪。
/// 基于原版 rifle 预制体克隆，修改 GunScript 字段实现自定义数值。
/// 全自动射击模式，30发弹匣供弹，整体式消音器。
/// </summary>
public static class VSSItemSystem
{
    public const string ItemKey = "vss";
    public const string BaseGameItemId = "rifle";

    public static string DisplayName => I18n.Tr("vss.name");
    public static string Description => I18n.Tr("vss.desc");

    // === GunScript 数值 ===
    private const int MagCapacity = 30;
    private const float KnockBack = 2.8f;
    private const float AnimalDamage = 105f;
    private const float StructureDamage = 88f;
    private const float Loudness = 0.32f;
    private const int ShotsPerFire = 1;
    private const float VerticalSpread = 0f;
    private const float ConditionLossPerShot = 0.5f;
    private const float DesiredGasTime = 0.08f;
    // FiringMode enum actual values: Pump=0, SemiAuto=1, Auto=2
    private const int FiringModeOverride = 2; // Auto (全自动)

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedNoMagIcon;
    private static AudioClip? _cachedFireSound;

    public static bool IsVSSRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 配置生成的物品实例。
    /// </summary>
    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsVSSRequest(request)) return;

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
            if (FiringModeOverride >= 0)
                gun.firingMode = (GunScript.FiringMode)FiringModeOverride;

            var fireSound = TryLoadFireSound();
            if (fireSound != null)
                gun.fireSound = fireSound;

            // 设置拉栓/闭栓音效
            var rackSound = TryLoadSound("vss_open", "vss");
            if (rackSound != null)
                gun.customRack = rackSound;
            var unrackSound = TryLoadSound("vss_close", "vss");
            if (unrackSound != null)
                gun.customUnrack = unrackSound;

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

            // 调整枪管位置：VSS有整体式消音器，枪管较长
            if (gun.barrel != null)
                gun.barrel.localPosition += new Vector3(3f, 0f, 0f);
            // 禁用枪口火光：VSS整体式消音器不产生可见火光
            if (gun.muzzleParticle != null)
            {
                gun.muzzleParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var em = gun.muzzleParticle.emission;
                em.enabled = false;
                gun.muzzleParticle.gameObject.SetActive(false);
            }

            Plugin.Log.LogInfo($"[VSS] Configured GunScript: mag={MagCapacity}, dmg={AnimalDamage}, spread={VerticalSpread}, mode=Auto, loudness={Loudness}");
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<VSSItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<VSSItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[VSS] Configured spawned item '{ItemKey}' (condition={item.condition}).");
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
                Plugin.Log.LogInfo($"[VSS] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[VSS] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[VSS] Failed to register '{ItemKey}': {ex}");
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
            destroyAtZeroCondition = true,
            weight = 1.8f,
            scaleWeightWithCondition = false,
            combineable = source.combineable,
            value = 78,
            tags = "cangetwet,gun",
            rec = new Recognition(10),
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
            category = "utility",
            slotRotation = -90f,
            usable = true,
            usableOnLimb = false,
            usableWithLMB = true,
            autoAttack = true,
            destroyAtZeroCondition = true,
            combineable = true,
            weight = 1.8f,
            scaleWeightWithCondition = false,
            value = 78,
            tags = "cangetwet,gun",
            rec = new Recognition(10),
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "vss", "vss.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.30f, 0.5f), 14f);
                _cachedIcon.name = "vss-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[VSS] Failed to load icon: {ex.Message}");
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "vss", "vss_magout.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedNoMagIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.30f, 0.5f), 14f);
                _cachedNoMagIcon.name = "vss-nomag-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[VSS] Failed to load no-mag icon: {ex.Message}");
        }

        return _cachedNoMagIcon;
    }

    // ===== Sounds =====

    private static AudioClip? TryLoadFireSound()
    {
        if (_cachedFireSound != null) return _cachedFireSound;
        _cachedFireSound = TryLoadSound("vss_fire", "vss");
        if (_cachedFireSound != null)
            Plugin.Log.LogInfo("[VSS] Loaded fire sound 'vss_fire.wav'");
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
            Plugin.Log.LogWarning($"[VSS] Failed to load sound '{fileName}': {ex.Message}");
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
/// 物品标记组件。
/// </summary>
public sealed class VSSItemMarker : MonoBehaviour
{
    public string displayName = VSSItemSystem.DisplayName;
    public string description = VSSItemSystem.Description;
}

/// <summary>
/// 悬停描述补丁 - 智力不足时显示"Unknown Object"。
/// </summary>
// [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class VSSHoverPatch
{
    [HarmonyPostfix]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        return; // Disabled: replaced by UnifiedHoverPatch
        var marker = item.GetComponent<VSSItemMarker>();
        if (marker == null) return;

        if (!item.Stats.rec.recognizable) return;

        // Name updated by I18nRefreshPatch Prefix
        HoverDescriptionHelper.StripEffectsWhenNotExpanded(ref __result);
    }
}
