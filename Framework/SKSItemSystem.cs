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
/// 西蒙诺夫 SKS 7.62x39 半自动10发弹仓卡宾枪。
/// 基于原版 rifle 预制体克隆，修改 GunScript 字段实现自定义数值。
/// FeedType.Direct 允许子弹直接压入弹仓，无需弹匣。
/// </summary>
public static class SKSItemSystem
{
    public const string ItemKey = "sks";
    public const string BaseGameItemId = "rifle";

    public static string DisplayName => I18n.Tr("sks.name");
    public static string Description => I18n.Tr("sks.desc");

    // === GunScript 数值 ===
    public const int MagCapacity = 10;
    private const float KnockBack = 6f;
    private const float AnimalDamage = 150f;
    private const float StructureDamage = 100f;
    private const float Loudness = 2.9f;
    private const int ShotsPerFire = 1;
    private const float VerticalSpread = 0.1f;
    private const float ConditionLossPerShot = 0.6f;
    private const float DesiredGasTime = 0.1f;

    private static Sprite? _cachedIcon;
    private static Sprite? _cachedMagOutIcon;
    private static Sprite? _cachedSksA5Icon;
    private static AudioClip? _cachedFireSound;

    public static bool IsSKSRequest(MedicalGrantRequest request)
        => request.ItemKey.Equals(ItemKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 配置生成的 SKS 物品实例。
    /// 克隆 rifle 预制体后修改 GunScript 字段。
    /// </summary>
    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (!IsSKSRequest(request)) return;

        EnsureRegisteredInItemTable();

        item.id = ItemKey;
        item.SetCondition(1f);

        // 修改 GunScript 字段
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
            // SKS 是半自动10发弹仓，不需要弹匣，子弹直接压入
            // FiringMode enum: Pump=0, SemiAuto=1, Auto=2
            gun.firingMode = GunScript.FiringMode.SemiAuto; // 半自动
            gun.feedType = GunScript.FeedType.Direct; // Direct feed，子弹可直接压入弹仓

            // 设置自定义开火音效
            var fireSound = TryLoadFireSound();
            if (fireSound != null)
                gun.fireSound = fireSound;

            // 设置拉栓/闭栓音效
            var rackSound = TryLoadSound("sks_open", "sks");
            if (rackSound != null)
                gun.customRack = rackSound;
            var unrackSound = TryLoadSound("sks_close", "sks");
            if (unrackSound != null)
                gun.customUnrack = unrackSound;

            // 设置自定义贴图
            var icon = TryLoadIcon();
            if (icon != null)
            {
                gun.normalSprite = icon;
                gun.rackedSprite = icon;
                gun.normalSpriteNoMag = icon;
                gun.rackedSpriteNoMag = icon;

                var sr = item.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = icon;
            }

            // 调整枪管和火光位置：向外偏移，使弹道起点和火光更靠外
            if (gun.barrel != null)
                gun.barrel.localPosition += new Vector3(1.5f, 0f, 0f);
            if (gun.muzzleParticle != null)
                gun.muzzleParticle.transform.localPosition += new Vector3(2f, 0f, 0f);

            Plugin.Log.LogInfo($"[SKS] Configured GunScript: mag={MagCapacity}, dmg={AnimalDamage}, spread={VerticalSpread}");
        }

        // 默认自带 10 发弹仓改件（Direct 弹仓模式）
        // 预填 GunAttachmentHolder.attachmentIds，使改枪面板左栏显示该改件，可卸下
        var holder = item.GetComponent<GunAttachmentHolder>();
        if (holder == null)
            holder = item.gameObject.AddComponent<GunAttachmentHolder>();
        if (!holder.attachmentIds.Contains(SksIntegralMagItemSystem.ItemKey))
            holder.attachmentIds.Add(SksIntegralMagItemSystem.ItemKey);

        // 调整碰撞箱
        ResizeColliderToSprite(item);

        // 添加标记组件
        var marker = item.gameObject.GetComponent<SKSItemMarker>();
        if (marker == null)
            marker = item.gameObject.AddComponent<SKSItemMarker>();
        marker.displayName = DisplayName;
        marker.description = Description;

        Plugin.Log.LogInfo($"[SKS] Configured spawned item '{ItemKey}' (condition={item.condition}).");
    }

    /// <summary>
    /// 在 Item.GlobalItems 注册 SKS 的 ItemInfo。
    /// 克隆 rifle 的 ItemInfo，修改名称、重量、价值等。
    /// </summary>
    public static bool EnsureRegisteredInItemTable()
    {
        if (Item.GlobalItems.ContainsKey(ItemKey))
            return false;

        try
        {
            if (Item.GlobalItems.TryGetValue(BaseGameItemId, out var source))
            {
                Item.GlobalItems[ItemKey] = CloneItemInfo(source);
                Plugin.Log.LogInfo($"[SKS] Registered '{ItemKey}' (cloned from '{BaseGameItemId}').");
                return true;
            }

            Item.GlobalItems[ItemKey] = CreateFallbackItemInfo();
            Plugin.Log.LogInfo($"[SKS] Registered '{ItemKey}' (fallback).");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SKS] Failed to register '{ItemKey}': {ex}");
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
            weight = 2.04f,
            scaleWeightWithCondition = false,
            combineable = source.combineable,
            value = 20,
            tags = "cangetwet,gun",
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
            category = "utility",
            slotRotation = -90f,
            usable = true,
            usableOnLimb = false,
            usableWithLMB = true,
            autoAttack = true,
            destroyAtZeroCondition = false,
            combineable = true,
            weight = 2.04f,
            scaleWeightWithCondition = false,
            value = 20,
            tags = "cangetwet,gun",
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
            // SKS 基础贴图为 sks_10.png（游戏不支持 webp，均为 png）
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", "sks_10.png");

            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                _cachedIcon = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.30f, 0.5f), 22.5f);
                _cachedIcon.name = "sks-icon";
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SKS] Failed to load icon: {ex.Message}");
        }

        return _cachedIcon;
    }

    /// <summary>公开访问基础贴图（供 GunVisualComposer 合成 UAS 层）。</summary>
    public static Sprite? TryLoadIconPublic() => TryLoadIcon();

    /// <summary>加载无弹匣贴图（sks_magout.png，卸下弹仓/无弹匣时显示）。</summary>
    private static Sprite? TryLoadMagOutIcon()
    {
        if (_cachedMagOutIcon != null) return _cachedMagOutIcon;
        _cachedMagOutIcon = LoadSksSprite("sks_magout.png", "sks-magout");
        return _cachedMagOutIcon;
    }

    /// <summary>加载装 SKS-A5 弹匣贴图（sks_sksa5.png）。</summary>
    private static Sprite? TryLoadSksA5Icon()
    {
        if (_cachedSksA5Icon != null) return _cachedSksA5Icon;
        _cachedSksA5Icon = LoadSksSprite("sks_sksa5.png", "sks-sksa5");
        return _cachedSksA5Icon;
    }

    private static Sprite? LoadSksSprite(string file, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "sks", file);
            if (!File.Exists(iconPath)) return null;
            var bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var spr = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.30f, 0.5f), 22.5f);
            spr.name = name;
            return spr;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[SKS] Failed to load {file}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据 SKS 当前供弹方式返回基础贴图：
    /// - 装 SKS-A5 弹匣（attachmentIds 或 currentMagId）：sks_sksa5.png
    /// - 默认（有弹仓改件）：sks_10.png（带弹仓）
    /// - 卸下弹仓/无弹匣：sks_magout.png
    /// </summary>
    public static Sprite? GetCurrentBaseSprite(Item gunItem)
    {
        if (gunItem == null) return TryLoadIcon();
        var gun = gunItem.GetComponent<GunScript>();
        if (gun == null) return TryLoadIcon();

        // 判断是否装 SKS-A5 弹匣：改枪面板安装（attachmentIds）或原版装弹匣（currentMagId）
        bool hasSksA5 = SuppressorSystem.IsAttachmentInstalled(gunItem, SksA5MagItemSystem.ItemKey);
        if (!hasSksA5)
        {
            var holder = gunItem.GetComponent<GunAttachmentHolder>();
            hasSksA5 = holder != null
                && string.Equals(holder.currentMagId, SksA5MagItemSystem.ItemKey, StringComparison.OrdinalIgnoreCase);
        }

        if (hasSksA5)
            return TryLoadSksA5Icon();          // 装 SKS-A5 弹匣
        if (SuppressorSystem.IsAttachmentInstalled(gunItem, SksIntegralMagItemSystem.ItemKey))
            return TryLoadIcon();               // 默认弹仓改件（带弹仓）
        return TryLoadMagOutIcon();             // 无弹匣（卸下弹仓）
    }

    /// <summary>
    /// 刷新 SKS 渲染贴图：委托给 GunVisualComposer.Rebuild，
    /// 由它根据当前供弹方式选择基础贴图并合成 UAS 等配件层。
    /// 这样卸弹仓/装弹匣后 UAS 贴图不会丢失（此前 UpdateSksVisual 直接覆盖整图，
    /// 会把已合成的 UAS 层冲掉）。
    /// </summary>
    public static void UpdateSksVisual(Item gunItem)
    {
        GunVisualComposer.Rebuild(gunItem);
    }

    // ===== Sounds =====

    private static AudioClip? TryLoadFireSound()
    {
        if (_cachedFireSound != null) return _cachedFireSound;
        _cachedFireSound = TryLoadSound("sks_fire", "sks");
        if (_cachedFireSound != null)
            Plugin.Log.LogInfo("[SKS] Loaded fire sound 'sks_fire.wav'");
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
            Plugin.Log.LogWarning($"[SKS] Failed to load sound '{fileName}': {ex.Message}");
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
/// SKS 物品标记组件。
/// </summary>
public sealed class SKSItemMarker : MonoBehaviour
{
    public string displayName = SKSItemSystem.DisplayName;
    public string description = SKSItemSystem.Description;
}

/// <summary>
/// SKS 悬停描述补丁 - 智力不足时显示"Unknown Object"。
/// </summary>
