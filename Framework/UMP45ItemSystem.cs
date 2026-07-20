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
/// HK UMP 45 .45 ACP 全自动冲锋枪。
/// 基于原版 rifle 预制体克隆，修改 GunScript 字段实现自定义数值。
/// 配备消音器，射击声音极低。
/// </summary>
public static class UMP45ItemSystem
{
    public const string ItemKey = "ump45";
    public const string BaseGameItemId = "rifle";

    public static string DisplayName => I18n.Tr("ump45.name");
    public static string Description => I18n.Tr("ump45.desc");

    // === GunScript 数值 ===
    private const int MagCapacity = 25;
    private const float KnockBack = 2.8f;
    private const float AnimalDamage = 44f;
    private const float StructureDamage = 27f;
    private const float Loudness = 0.2f;
    private const int ShotsPerFire = 1;
    private const float VerticalSpread = 0.1f;
    private const float ConditionLossPerShot = 0.13f;
    private const float DesiredGasTime = 0.1f;
    private const int FiringModeOverride = 2; // Auto

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedNoMagIcon;
    private static AudioClip? _cachedFireSound;

    public static bool IsUMP45Request(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsUMP45Request(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        var gun = item.GetComponent<GunScript>();
        if (gun != null)
        {
            gun.firingMode = (GunScript.FiringMode)FiringModeOverride;
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

            var fireSound = TryLoadFireSound();
            if (fireSound != null)
                gun.fireSound = fireSound;

            var rackSound = TryLoadSound("ump_open", "ump45");
            if (rackSound != null)
                gun.customRack = rackSound;
            var unrackSound = TryLoadSound("ump_close", "ump45");
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

            // 调整枪管位置：与其他枪一致
            if (gun.barrel != null)
                gun.barrel.localPosition += new Vector3(1.5f, 0f, 0f);

            // 去掉开枪火光（消音器效果）
            if (gun.muzzleParticle != null)
            {
                var emission = gun.muzzleParticle.emission;
                emission.enabled = false;
            }

            Plugin.Log.LogInfo($"[UMP45] Configured GunScript: mag={MagCapacity}, dmg={AnimalDamage}, spread={VerticalSpread}, mode=Auto, loudness={Loudness}");
        }

        ResizeColliderToSprite(item);

        var marker = item.gameObject.GetComponent<UMP45ItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<UMP45ItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[UMP45] Configured spawned item '{ItemKey}' (condition={item.condition}).");
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
                Plugin.Log.LogInfo($"[UMP45] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[UMP45] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[UMP45] Failed to register '{ItemKey}': {ex}");
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
            weight = 2.1f,
            scaleWeightWithCondition = false,
            combineable = source.combineable,
            value = 44,
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
            weight = 2.1f,
            scaleWeightWithCondition = false,
            value = 44,
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
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "ump45", "ump45.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.45f, 0.5f), 12f);
                _cachedIcon.name = "ump45-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[UMP45] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    private static Sprite? TryLoadNoMagIcon()
    {
        if (_cachedNoMagIcon != null) return _cachedNoMagIcon;
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "ump45", "ump45_magout.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                _cachedNoMagIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.45f, 0.5f), 12f);
                _cachedNoMagIcon.name = "ump45-nomag-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[UMP45] Failed to load no-mag icon: {ex.Message}");
        }
        return _cachedNoMagIcon;
    }

    // ===== Sounds =====

    private static AudioClip? TryLoadFireSound()
    {
        if (_cachedFireSound != null) return _cachedFireSound;
        _cachedFireSound = TryLoadSound("ump_fire", "ump45");
        if (_cachedFireSound != null)
            Plugin.Log.LogInfo("[UMP45] Loaded fire sound 'ump_fire.wav'");
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
            Plugin.Log.LogWarning($"[UMP45] Failed to load sound '{fileName}': {ex.Message}");
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
/// UMP 45 物品标记组件。
/// </summary>
public sealed class UMP45ItemMarker : MonoBehaviour
{
    public string displayName = UMP45ItemSystem.DisplayName;
    public string description = UMP45ItemSystem.Description;
}

/// <summary>
/// UMP 45 悬停描述补丁。
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.ItemHoverDescription))]
public static class UMP45HoverPatch
{
    [HarmonyPostfix]
    public static void Postfix(Item item, ref (string, string) __result)
    {
        var marker = item.GetComponent<UMP45ItemMarker>();
        if (marker == null) return;
        if (!item.Stats.rec.recognizable) return;
        __result.Item1 = marker.displayName;
        HoverDescriptionHelper.StripEffectsWhenNotExpanded(ref __result);
    }
}
