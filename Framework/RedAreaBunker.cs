using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using CUCoreLib.Data;
using CUCoreLib.Helpers;
using CUCoreLib.Registries;
using Newtonsoft.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Tilemaps;
using CUTarkovMedicalMod.Framework;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// Red Area 地堡：外形与 Blue Area 相同，内部为红底白字“Red Area”，使用红卡解锁。
/// 内部固定刷新：2 个大型武器箱、2 个装备物资箱、1 个子弹箱 + 1 个可选子弹箱。
/// </summary>
public static class RedAreaBunker
{
    public const string BunkerId = "red_area_bunker";
    public const string RedDoorId = "red_area_door";
    public const string RedCardReaderId = "red_area_card_reader";
    public const string EquipmentCrateId = "red_equipment_crate";
    public const string AmmoCrateId = "red_ammo_crate";
    public const string AmmoCrateOptionalId = "red_ammo_crate_optional";

    public static float EquipmentCrateFloorOffset = 0f;
    public static float AmmoCrateFloorOffset = 0f;

    private static readonly (string id, int weight)[] ArmorPool =
    {
        ("6b45", 1), ("blackrock", 1), ("gzhel_k", 1), ("hgrid", 1), ("hpc", 1), ("lbcr", 1),
        ("lv119", 1), ("redut_t5", 1), ("sieger", 1), ("6b43", 1), ("slick", 1), ("ttsk", 1),
    };

    private static readonly (string id, int weight)[] HelmetPool =
    {
        ("ryst", 1), ("fastmt", 1), ("exfil", 1), ("fastvisor", 1),
    };

    private static readonly (string id, int weight)[] NvgHeadsetPool =
    {
        ("pvs14", 5), ("pvs31a", 2), ("gpnvg18", 4), ("proflextac", 4), ("tep300", 4),
    };

    private static readonly (string id, int weight)[] BackpackPool =
    {
        ("mysteryranch2day", 1), ("6sh118", 1), ("ssoattack2", 1), ("berkut", 1), ("daypack", 1), ("6lbt2670", 1),
    };

    public static void Register()
    {
        RegisterRedDoorAndReader();
        RegisterEquipmentCrate();
        RegisterAmmoCrate(AmmoCrateId, optional: false);
        RegisterAmmoCrate(AmmoCrateOptionalId, optional: true);
        RegisterRedAreaStructure();
    }

    private static void RegisterRedDoorAndReader()
    {
        var doorSprite = LoadSprite("door_bluewhite");

        float vanillaDoorHealth = 250f;
        try
        {
            var vanillaDoor = Resources.Load<GameObject>("reinforceddoor");
            if (vanillaDoor != null && vanillaDoor.TryGetComponent<BuildingEntity>(out var vanillaBuilding))
                vanillaDoorHealth = vanillaBuilding.health;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] Failed to read vanilla reinforceddoor health: {ex.Message}");
        }

        var doorDef = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.red.door.name", "Red Area 加固门"),
            Description = WModLoc.Tr("wm.red.door.desc", "需要 Terragroup Red Area 钥匙卡开启的加固门。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = doorSprite,
            Scale = new Vector3(2f, 1f, 1f),
            Health = 1_000_000_000f, // 不可破坏：仅能通过刷卡开启
            Metallic = false,
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true,
            Layer = LayerMask.NameToLayer("Ground"),
            HitSoundReferenceId = "metal",
        };
        BuildingEntityRegistry.Register(RedDoorId, doorDef);
        Plugin.Log.LogInfo($"[RedAreaBunker] Registered red door '{RedDoorId}' health={doorDef.Health:0}.");

        var readerSprite = LoadSprite("card_reader");
        var readerDef = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.red.reader.name", "Red Area 刷卡装置"),
            Description = WModLoc.Tr("wm.red.reader.desc", "使用 Terragroup Red Area 钥匙卡开启 Red Area 加固门。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = readerSprite,
            Health = 2500f,
            Metallic = false,
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true,
            Layer = LayerMask.NameToLayer("Ground"),
            HitSoundReferenceId = "rubber",
            Components = new[] { typeof(UsableObject) },
            ConfigureInstance = (go) =>
            {
                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";
                go.AddComponent<RedCardReaderDevice>();
            },
        };
        BuildingEntityRegistry.Register(RedCardReaderId, readerDef);
        Plugin.Log.LogInfo($"[RedAreaBunker] Registered red card reader '{RedCardReaderId}'.");
    }

    private static void RegisterEquipmentCrate()
    {
        var sprite = LoadSprite("armor_crate");
        if (sprite != null)
            EquipmentCrateFloorOffset = 0.5f * sprite.bounds.size.y * 3.4f - 0.5f;

        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.red.equipment_crate.name", "装备物资箱"),
            Description = WModLoc.Tr("wm.red.equipment_crate.desc", "上锁的装备物资箱，开锁后随机掉落 2~4 件高级装备。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = sprite,
            Scale = new Vector3(3.4f, 3.4f, 1f), // 略大于大型武器箱（3x3）
            Health = 6000f,
            Metallic = false,
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true,
            Layer = LayerMask.NameToLayer("Ground"),
            HitSoundReferenceId = "metal",
            Components = new[] { typeof(UsableObject), typeof(Openable) },
            ConfigureInstance = (go) =>
            {
                var openable = go.GetComponent<Openable>();
                if (openable != null)
                    openable.lockpickAnglePrecision = 0.5f;

                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";
                go.AddComponent<EquipmentCrateDrop>();
            },
        };
        BuildingEntityRegistry.Register(EquipmentCrateId, def);
        Plugin.Log.LogInfo($"[RedAreaBunker] Registered equipment crate '{EquipmentCrateId}'.");
    }

    private static void RegisterAmmoCrate(string id, bool optional)
    {
        var sprite = LoadSprite("ammo_crate");
        if (sprite != null)
            AmmoCrateFloorOffset = 0.5f * sprite.bounds.size.y * 2.6f - 0.5f;

        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.red.ammo_crate.name", "子弹箱"),
            Description = WModLoc.Tr("wm.red.ammo_crate.desc", "开启后随机掉落两个口径的满弹弹药盒。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = sprite,
            Scale = new Vector3(2.6f, 2.6f, 1f), // 略小于医疗包（3x3）
            Health = 4000f,
            Metallic = false,
            AddRigidbody2D = true,
            RigidbodyBodyType = RigidbodyType2D.Static,
            IgnoreBodyOptimize = true,
            Layer = LayerMask.NameToLayer("Ground"),
            HitSoundReferenceId = "metal",
            Components = new[] { typeof(UsableObject) },
            ConfigureInstance = (go) =>
            {
                var usable = go.GetComponent<UsableObject>();
                if (usable != null)
                    usable.langToggleString = "open";
                go.AddComponent<AmmoCrateDrop>();
                if (optional)
                    go.AddComponent<RedAmmoCrateOptionalCuller>();
            },
        };
        BuildingEntityRegistry.Register(id, def);
        Plugin.Log.LogInfo($"[RedAreaBunker] Registered ammo crate '{id}' optional={optional}.");
    }

    private static void RegisterRedAreaStructure()
    {
        string[] fgRows =
        {
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行0 外天花板
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行1 内天花板
            ".................................HH", // 行2
            ".................................HH", // 行3
            ".................................HH", // 行4
            ".................................HH", // 行5
            ".................................HH", // 行6
            ".................................HH", // 行7
            ".................................HH", // 行8
            ".0.0..0.......0.......0..0...0.0.HH", // 行9 刷卡(1) 门(3) 大箱(6,14) 装备(22,29) 可选弹箱(27) 弹箱(31)
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行10 内地板
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行11 外地板
        };

        string[] bgRows =
        {
            "...................................", // 行0
            "...................................", // 行1
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行2
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行3
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行4
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行5
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行6
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行7
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行8
            ".....nnnnnnnnnnnnnnnnnnnnnnnnnnnn..", // 行9
            "...................................", // 行10
            "...................................", // 行11
        };

        var objectsByCell = new Dictionary<string, object>
        {
            ["316"] = new { id = RedCardReaderId },
            ["318"] = new { id = RedDoorId },
            ["321"] = new { id = WeaponCacheBunker.WeaponCacheBoxLargeId },
            ["329"] = new { id = WeaponCacheBunker.WeaponCacheBoxLargeId },
            ["337"] = new { id = EquipmentCrateId },
            ["344"] = new { id = EquipmentCrateId },
            ["342"] = new { id = AmmoCrateOptionalId },
            ["346"] = new { id = AmmoCrateId },
        };

        float largeCrateOffset = WeaponCacheBunker.LargeCrateFloorOffset;

        var precisePlacements = new[]
        {
            new { gridX = 1,  gridY = 2,  offsetX = 0f, offsetY = 0f,   rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 3,  gridY = 2,  offsetX = 0.5f, offsetY = 3.5f, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 6,  gridY = 2,  offsetX = 0f, offsetY = largeCrateOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 14, gridY = 2,  offsetX = 0f, offsetY = largeCrateOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 22, gridY = 2,  offsetX = 0f, offsetY = EquipmentCrateFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 27, gridY = 2,  offsetX = 0f, offsetY = AmmoCrateFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 29, gridY = 2,  offsetX = 0f, offsetY = EquipmentCrateFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 31, gridY = 2,  offsetX = 0f, offsetY = AmmoCrateFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
        };

        var payload = new
        {
            metadata = new
            {
                schemaVersion = 4,
                spawnCounts = new[] { 0, 0, 1, 1, 1 },
                avoidOverlap = true,
            },
            width = 35,
            height = 12,
            layers = new[]
            {
                new { id = "fg", kind = "fg", visible = true, rows = fgRows },
                new { id = "bg", kind = "bg", visible = true, rows = bgRows },
            },
            objectsByCell = objectsByCell,
            precisePlacements = precisePlacements,
        };

        string json = JsonConvert.SerializeObject(payload);
        bool ok = StructureRegistry.RegisterFromJson(BunkerId, json, "red area bunker");
        Plugin.Log.LogInfo($"[RedAreaBunker] Registered structure '{BunkerId}': {ok}");
    }

    private static Sprite LoadSprite(string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", "guns", name + ".png");
            if (!File.Exists(path)) return null;
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] LoadSprite('{name}') failed: {ex.Message}");
            return null;
        }
    }
}

/// <summary>Red Area 刷卡装置：使用红卡开启 Red Area 加固门，每次消耗 10% 耐久（10 次）。</summary>
public class RedCardReaderDevice : MonoBehaviour
{
    public const string AccessCardId = "redarea_keycard";
    public const float OpenRadius = 6f;

    public void OnUse()
    {
        try
        {
            var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            if (body == null) return;

            if (!body.FindByIdSurface(AccessCardId, out var cardItem) || cardItem == null || cardItem.condition <= 0f)
            {
                Sound.Play("beep", transform.position, false, true, null, 1f, 1f, false, false);
                return;
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, OpenRadius);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (!hit.TryGetComponent<BuildingEntity>(out var building)) continue;
                if (building.id != RedAreaBunker.RedDoorId) continue;

                building.Backgroundify();
                cardItem.SetCondition(Mathf.Clamp(cardItem.condition - 0.1f, 0f, 1f));
                Sound.Play("unlock", transform.position, false, true, null, 1f, 1f, false, false);
                Plugin.Log.LogInfo($"[RedAreaBunker] Red keycard condition: {cardItem.condition:P}.");
                return;
            }

            Sound.Play("beep", transform.position, false, true, null, 1f, 1f, false, false);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] RedCardReaderDevice.OnUse failed: {ex.Message}");
        }
    }
}

/// <summary>装备物资箱：需要开锁。开锁成功（health 归零）或被破坏时从 4 个物资池中随机选 2~4 个池子掉落。</summary>
public class EquipmentCrateDrop : MonoBehaviour
{
    private BuildingEntity _building;
    private bool _isQuitting;

    private void Awake()
    {
        _building = GetComponent<BuildingEntity>();
    }

    private void OnApplicationQuit() { _isQuitting = true; }

    private void OnDestroy()
    {
        if (_isQuitting) return;
        if (_building == null || _building.health >= 0.5f) return;

        try
        {
            int poolCount = UnityEngine.Random.Range(2, 5); // 2~4 个物资池
            var poolIndices = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < 4 - poolCount; i++)
                poolIndices.RemoveAt(UnityEngine.Random.Range(0, poolIndices.Count));

            foreach (int poolIndex in poolIndices)
            {
                string? id = poolIndex switch
                {
                    0 => PickWeighted(new[] { ("6b45",1), ("blackrock",1), ("gzhel_k",1), ("hgrid",1), ("hpc",1), ("lbcr",1), ("lv119",1), ("redut_t5",1), ("sieger",1), ("6b43",1), ("slick",1), ("ttsk",1) }),
                    1 => PickWeighted(new[] { ("ryst",1), ("fastmt",1), ("exfil",1), ("fastvisor",1) }),
                    2 => PickWeighted(new[] { ("pvs14",5), ("pvs31a",2), ("gpnvg18",4), ("proflextac",4), ("tep300",4) }),
                    _ => PickWeighted(new[] { ("mysteryranch2day",1), ("6sh118",1), ("ssoattack2",1), ("berkut",1), ("daypack",1), ("6lbt2670",1) }),
                };
                if (string.IsNullOrEmpty(id)) continue;

                var pos = (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1.5f));
                var go = Utils.Create(id, pos, UnityEngine.Random.Range(0f, 360f));
                if (go == null) continue;
                var item = go.GetComponent<Item>();
                if (item != null) item.SetCondition(1f);
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f));
                go.AddComponent<FreshItemDrop>();
            }

            Sound.Play("unlock", transform.position, false, true, null, 1f, 1f, false, false);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] EquipmentCrateDrop.OnDestroy failed: {ex.Message}");
        }
    }

    private static string? PickWeighted((string id, int weight)[] table)
    {
        int total = 0;
        foreach (var t in table) total += t.weight;
        int w = UnityEngine.Random.Range(1, total + 1);
        int accum = 0;
        foreach (var t in table)
        {
            accum += t.weight;
            if (w <= accum) return t.id;
        }
        return table[table.Length - 1].id;
    }
}

/// <summary>子弹箱：开启后随机掉落两个不同口径的满弹弹药盒。</summary>
public class AmmoCrateDrop : MonoBehaviour
{
    private BuildingEntity _building;
    private bool _isQuitting;
    private bool _opened;

    private void Awake()
    {
        _building = GetComponent<BuildingEntity>();
    }

    private void OnApplicationQuit() { _isQuitting = true; }

    private void OnDestroy()
    {
        if (_isQuitting) return;
        if (_opened) return;
        OnUse();
    }

    public void OnUse()
    {
        try
        {
            if (_building == null) return;

            var boxIds = new List<string>
            {
                "box_338ucw", "box_76251bpz", "box_50copper", "box_12g85", "box_76239sp",
                "box_55645fmj", "box_939sp5", "box_45fmj", "box_919pso", "box_5728sb193",
            };

            for (int i = 0; i < 2; i++)
            {
                int index = UnityEngine.Random.Range(0, boxIds.Count);
                string id = boxIds[index];
                boxIds.RemoveAt(index);

                var pos = (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1.5f));
                var go = Utils.Create(id, pos, UnityEngine.Random.Range(0f, 360f));
                if (go == null) continue;
                var item = go.GetComponent<Item>();
                if (item != null) item.SetCondition(1f);
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f));
                go.AddComponent<FreshItemDrop>();
            }

            Sound.Play("unlock", transform.position, false, true, null, 1f, 1f, false, false);
            _opened = true;
            _building.health = 0f;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] AmmoCrateDrop.OnUse failed: {ex.Message}");
        }
    }
}

/// <summary>可选子弹箱：50% 概率不生成。</summary>
public class RedAmmoCrateOptionalCuller : MonoBehaviour
{
    private void Awake()
    {
        if (UnityEngine.Random.value < 0.5f)
            Destroy(gameObject);
    }
}

/// <summary>Red Area 背景补丁：拦截 n（block 17）背景砖，替换为实验室背景并生成红底白字长条。</summary>
public static class RedAreaBackgroundPatch
{
    public const int RedBackgroundBlockId = 17; // n

    private static GameObject _template;
    private static Sprite _customBackground;
    private static bool _customBackgroundLoaded;
    private static Sprite _customBackgroundDirty;
    private static bool _customBackgroundDirtyLoaded;
    private static Sprite _stripSprite;
    private static bool _stripSpriteLoaded;
    private static int _backgroundTileCount;

    public static bool Prefix(int blockId, Vector2 position)
    {
        if (blockId != RedBackgroundBlockId) return true;

        var world = WorldGeneration.world;
        if (world?.tiles == null || blockId < 0 || blockId >= world.tiles.Length) return false;
        if (!(world.tiles[blockId] is Tile tile) || tile.sprite == null) return false;

        var sprite = GetBackgroundSprite(tile, position);
        if (sprite == null) return false;

        var template = GetTemplate();
        if (template == null) return false;

        var backgroundObject = UnityEngine.Object.Instantiate(template, position, Quaternion.identity);
        backgroundObject.SetActive(true);
        backgroundObject.name = "CUCoreLib_BGTile_" + blockId + "_red_custom";
        if (world.worldGrid != null)
            backgroundObject.transform.SetParent(world.worldGrid.transform);

        var renderer = backgroundObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -998;

        _backgroundTileCount++;
        int roomIndex = (_backgroundTileCount - 1) % 224;
        if (roomIndex == 0)
            SpawnRedStrip(position + new Vector2(13.5f, 3.5f), world.worldGrid != null ? world.worldGrid.transform : null);

        return false;
    }

    private static void SpawnRedStrip(Vector2 position, Transform parent)
    {
        try
        {
            var strip = GetStripSprite();
            if (strip == null) return;

            var go = new GameObject("CUTarkovWeaponMod_RedAreaStrip");
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = strip;
            sr.color = Color.white;
            sr.sortingOrder = -950;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] SpawnRedStrip failed: {ex.Message}");
        }
    }

    private static Sprite GetStripSprite()
    {
        if (!_stripSpriteLoaded)
        {
            _stripSpriteLoaded = true;
            _stripSprite = LoadRedStripSprite();
        }
        return _stripSprite;
    }

    /// <summary>加载 Red Area 红底白字长条贴图。</summary>
    private static Sprite LoadRedStripSprite()
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", "structures", "lab_background_strip_red.png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Bilinear;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 28f);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] LoadRedStripSprite failed: {ex.Message}");
            return null;
        }
    }

    private static Sprite GetBackgroundSprite(Tile tile, Vector2 position)
    {
        if (!_customBackgroundLoaded)
        {
            _customBackgroundLoaded = true;
            _customBackground = LoadCustomSprite("structures", "lab_background");
        }
        if (!_customBackgroundDirtyLoaded)
        {
            _customBackgroundDirtyLoaded = true;
            _customBackgroundDirty = LoadCustomSprite("structures", "lab_background_dirty");
        }

        var clean = _customBackground != null ? _customBackground : tile.sprite;
        var dirty = _customBackgroundDirty != null ? _customBackgroundDirty : clean;

        int h = position.GetHashCode() & 0x7fffffff;
        return h % 10 < 3 ? dirty : clean;
    }

    private static Sprite LoadCustomSprite(string folder, string name)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", folder, name + ".png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[RedAreaBunker] LoadCustomSprite('{folder}/{name}') failed: {ex.Message}");
            return null;
        }
    }

    private static GameObject GetTemplate()
    {
        if (_template != null) return _template;
        _template = new GameObject("CUTarkovWeaponMod_RedAreaBackgroundTileTemplate");
        _template.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(_template);
        _template.AddComponent<SpriteRenderer>();
        return _template;
    }
}
