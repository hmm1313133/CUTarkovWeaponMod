using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUTarkovMedicalMod.Framework;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 弹药盒系统：仿原版 boxof12gauge 逻辑。
/// 使用弹盒时取出一发对应弹药；价值 = 单发弹药价值 × 容量；重量 = 满弹重量 × 0.7。
/// 弹药盒不在世界刷新（加入 HiddenFromLootPoolIds）。
/// </summary>
public static class AmmoBoxItemSystem
{
    public const string BaseGameItemId = "boxof12gauge";

    public sealed class BoxDef
    {
        public string BoxId;
        public string AmmoItemId;
        public int Capacity;
        public GunScript.AmmoType AmmoType;
        public int UnitValue;
        public float UnitWeight;

        public BoxDef(string boxId, string ammoItemId, int capacity, GunScript.AmmoType ammoType, int unitValue, float unitWeight)
        {
            BoxId = boxId;
            AmmoItemId = ammoItemId;
            Capacity = capacity;
            AmmoType = ammoType;
            UnitValue = unitValue;
            UnitWeight = unitWeight;
        }
    }

    public static readonly BoxDef[] Boxes =
    {
        new BoxDef("box_338ucw",     "338ucw",     10, GunScript.AmmoType.Rifle,   2, 0.07f),
        new BoxDef("box_76251bpz",   "76251bpz",   30, GunScript.AmmoType.Rifle,   1, 0.04f),
        new BoxDef("box_50copper",   "50copper",   30, GunScript.AmmoType.Pistol,  2, 0.03f),
        new BoxDef("box_12g85",      "12g85",      30, GunScript.AmmoType.Shotgun, 1, 0.04f),
        new BoxDef("box_76239sp",    "76239sp",    60, GunScript.AmmoType.Rifle,   1, 0.03f),
        new BoxDef("box_55645fmj",   "55645fmj",   60, GunScript.AmmoType.Rifle,   1, 0.04f),
        new BoxDef("box_939sp5",     "939sp5",     60, GunScript.AmmoType.Rifle,   1, 0.04f),
        new BoxDef("box_45fmj",      "45fmj",      60, GunScript.AmmoType.Pistol,  1, 0.02f),
        new BoxDef("box_919pso",     "919pso",     60, GunScript.AmmoType.Pistol,  1, 0.02f),
        new BoxDef("box_5728sb193",  "5728sb193",  80, GunScript.AmmoType.Rifle,   1, 0.03f),
    };

    // 贴图文件名（缺失则使用 boxof12gauge 预制体贴图）
    private static readonly Dictionary<string, string> BoxIconFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        { "box_338ucw", "boxof338ucw.png" },
        { "box_76251bpz", "boxof76251bpz.png" },
        { "box_50copper", "boxof50copper.png" },
        { "box_12g85", "boxof12g85.png" },
        { "box_76239sp", "boxof76239sp.png" },
        { "box_55645fmj", "boxof54539fmj.png" },
        { "box_939sp5", "boxof939sp5.png" },
        { "box_45fmj", "boxof45fmj.png" },
        { "box_919pso", "boxof919pso.png" },
        { "box_5728sb193", "boxof5728sb193.png" },
    };

    private static readonly Dictionary<string, Sprite?> _cachedIcons = new(StringComparer.OrdinalIgnoreCase);

    private static Sprite? TryLoadIcon(string boxId)
    {
        if (_cachedIcons.TryGetValue(boxId, out var cached)) return cached;
        Sprite? sprite = null;
        try
        {
            if (BoxIconFiles.TryGetValue(boxId, out var fileName) && !string.IsNullOrEmpty(fileName))
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? BepInEx.Paths.PluginPath;
                var iconPath = Path.Combine(assemblyDir, "Framework", "Assets", "guns", "ammobox", fileName);
                if (File.Exists(iconPath))
                {
                    var bytes = File.ReadAllBytes(iconPath);
                    var texture = new Texture2D(2, 2);
                    if (ImageConversion.LoadImage(texture, bytes, false))
                    {
                        texture.filterMode = FilterMode.Point;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        sprite = Sprite.Create(texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f), 16f);
                        sprite.name = boxId + "-icon";
                    }
                }
                else
                {
                    Plugin.Log.LogWarning($"[AmmoBox] Icon not found: {iconPath}");
                }
            }
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"[AmmoBox] Icon: {ex.Message}"); }
        _cachedIcons[boxId] = sprite;
        return sprite;
    }

    public static bool IsAmmoBoxRequest(MedicalGrantRequest request)
    {
        if (request == null) return false;
        foreach (var box in Boxes)
            if (string.Equals(request.ItemKey, box.BoxId, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static BoxDef? FindBox(string boxId)
    {
        foreach (var box in Boxes)
            if (string.Equals(box.BoxId, boxId, StringComparison.OrdinalIgnoreCase))
                return box;
        return null;
    }

    public static bool IsAmmoBoxId(string boxId) => FindBox(boxId) != null;

    public static void ConfigureSpawnedItem(Item item, MedicalGrantRequest request)
    {
        if (item == null || request == null) return;
        var box = FindBox(request.ItemKey);
        if (box == null) return;

        item.id = box.BoxId;
        item.SetCondition(1f);

        var ammo = item.GetComponent<AmmoScript>();
        if (ammo != null)
        {
            ammo.itemType = AmmoScript.AmmoItemType.Magazine;
            ammo.ammoType = box.AmmoType;
            ammo.maxRounds = box.Capacity;
            ammo.rounds = box.Capacity; // 满盒
            Plugin.Log.LogInfo($"[AmmoBox] Configured '{box.BoxId}': {ammo.rounds}/{ammo.maxRounds} rounds, type={ammo.ammoType}.");
        }
        else
        {
            Plugin.Log.LogWarning($"[AmmoBox] AmmoScript missing on '{box.BoxId}'.");
        }

        var icon = TryLoadIcon(box.BoxId);
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && icon != null) sr.sprite = icon;
    }

    public static void EnsureRegisteredInItemTable()
    {
        foreach (var box in Boxes)
        {
            if (Item.GlobalItems.ContainsKey(box.BoxId)) continue;
            try
            {
                ItemInfo info;
                if (Item.GlobalItems.TryGetValue(BaseGameItemId, out var source))
                {
                    info = new ItemInfo
                    {
                        fullName = WModLoc.Tr(box.BoxId + ".name", box.BoxId),
                        description = WModLoc.Tr(box.BoxId + ".desc", box.BoxId),
                        category = source.category,
                        slotRotation = source.slotRotation,
                        usable = true,
                        usableOnLimb = false,
                        usableWithLMB = true,
                        autoAttack = false,
                        rotSpeed = source.rotSpeed,
                        useAction = (body, item) =>
                        {
                            var ammo = item.GetComponent<AmmoScript>();
                            if (ammo != null)
                                ammo.UnloadRound();
                        },
                        useLimbAction = null,
                        destroyAtZeroCondition = true,
                        weight = box.Capacity * box.UnitWeight * 0.7f,
                        scaleWeightWithCondition = false,
                        combineable = true,
                        value = box.Capacity * box.UnitValue,
                        tags = source.tags,
                        rec = new Recognition(4),
                    };
                }
                else
                {
                    info = new ItemInfo
                    {
                        fullName = WModLoc.Tr(box.BoxId + ".name", box.BoxId),
                        description = WModLoc.Tr(box.BoxId + ".desc", box.BoxId),
                        category = "ammo",
                        slotRotation = 0f,
                        usable = true,
                        usableOnLimb = false,
                        usableWithLMB = true,
                        autoAttack = false,
                        useAction = (body, item) =>
                        {
                            var ammo = item.GetComponent<AmmoScript>();
                            if (ammo != null)
                                ammo.UnloadRound();
                        },
                        useLimbAction = null,
                        destroyAtZeroCondition = true,
                        weight = box.Capacity * box.UnitWeight * 0.7f,
                        scaleWeightWithCondition = false,
                        combineable = true,
                        value = box.Capacity * box.UnitValue,
                        tags = "ammo",
                        rec = new Recognition(4),
                    };
                }
                info.SetTags();
                Item.GlobalItems[box.BoxId] = info;
                Plugin.Log.LogInfo($"[AmmoBox] Registered '{box.BoxId}' (cap={box.Capacity}, value={info.value}, weight={info.weight:0.###}).");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AmmoBox] Failed to register '{box.BoxId}': {ex}");
            }
        }
    }
}
