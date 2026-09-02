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
/// Blue Area 地堡：与武器室同尺寸，内部有 SMU06 医疗包、食物箱和自动医疗系统。
/// </summary>
public static class BlueAreaBunker
{
    public const string BunkerId = "blue_area_bunker";
    public const string BlueDoorId = "blue_area_door";
    public const string BlueCardReaderId = "blue_area_card_reader";
    public const string Snu06BagId = "smu06_bag";
    public const string Snu06BagOptionalId = "smu06_bag_optional";
    public const string AutoMedSystemId = "auto_med_system";

    public static float Snu06BagFloorOffset = 0f;
    public static float AutoMedFloorOffset = 1.5f;

    public static Sprite AutoMedNormalSprite;
    public static Sprite AutoMedCoolingSprite;

    public static readonly string[] MedicalItemIds =
    {
        "salewa", "ifak", "afak", "ai2", "cms", "grizzlykit", "ibuprofen", "vaseline", "goldenstar", "multivitamin",
    };

    public static readonly string[] InjectorItemIds =
    {
        "cu_morphine", "adrenaline", "zagustin", "propital", "sj12", "sj6", "mule", "etg_c", "blueblood",
    };

    public static void Register()
    {
        RegisterBlueDoorAndReader();
        RegisterSmu06Bags();
        RegisterAutoMedSystem();
        RegisterBlueAreaStructure();
    }

    private static void RegisterBlueDoorAndReader()
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
            Plugin.Log.LogWarning($"[BlueAreaBunker] Failed to read vanilla reinforceddoor health: {ex.Message}");
        }

        var doorDef = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.blue.door.name", "Blue Area 加固门"),
            Description = WModLoc.Tr("wm.blue.door.desc", "需要 Terragroup Blue Area 钥匙卡开启的加固门。"),
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
        BuildingEntityRegistry.Register(BlueDoorId, doorDef);
        Plugin.Log.LogInfo($"[BlueAreaBunker] Registered blue area door '{BlueDoorId}' health={doorDef.Health:0}.");

        var readerSprite = LoadSprite("card_reader");
        var readerDef = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.blue.reader.name", "Blue Area 刷卡装置"),
            Description = WModLoc.Tr("wm.blue.reader.desc", "使用 Terragroup Blue Area 钥匙卡开启加固门。"),
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
                go.AddComponent<BlueCardReaderDevice>();
            },
        };
        BuildingEntityRegistry.Register(BlueCardReaderId, readerDef);
        Plugin.Log.LogInfo($"[BlueAreaBunker] Registered blue area card reader '{BlueCardReaderId}'.");
    }

    private static void RegisterSmu06Bags()
    {
        RegisterSmu06Bag(Snu06BagId, optional: false);
        RegisterSmu06Bag(Snu06BagOptionalId, optional: true);
    }

    private static void RegisterSmu06Bag(string id, bool optional)
    {
        var sprite = LoadSprite("SMU06bag");
        if (sprite != null)
            Snu06BagFloorOffset = 0.5f * sprite.bounds.size.y * 3f - 0.5f;
        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.blue.smu06.name", "SMU06 医疗包"),
            Description = WModLoc.Tr("wm.blue.smu06.desc", "可直接右键开启的医疗物资包，掉落药品和针剂。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = sprite,
            Scale = new Vector3(3f, 3f, 1f), // SMU06 医疗包 3 格宽
            Health = 600f,
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
                go.AddComponent<SMU06BagDrop>();
                if (optional)
                    go.AddComponent<BlueBagOptionalCuller>();
            },
        };
        BuildingEntityRegistry.Register(id, def);
        Plugin.Log.LogInfo($"[BlueAreaBunker] Registered SMU06 bag '{id}' optional={optional}.");
    }

    private static void RegisterAutoMedSystem()
    {
        var sprite = LoadSpriteWithPpu("auto_med_system", 0f);
        var coolingSprite = LoadSpriteWithPpu("auto_med_system_cooling", 0f);
        if (coolingSprite == null)
            coolingSprite = sprite;

        AutoMedNormalSprite = sprite;
        AutoMedCoolingSprite = coolingSprite;

        // 新贴图 62x48：按 3 倍缩放，宽 3 格，高约 2.3 格。
        if (sprite != null)
            AutoMedFloorOffset = 0.5f * sprite.bounds.size.y * 3f - 0.5f;

        var def = new CustomBuildingEntityDefinition
        {
            Name = WModLoc.Tr("wm.blue.automed.name", "自动医疗系统"),
            Description = WModLoc.Tr("wm.blue.automed.desc", "自动外科与输液系统。使用后减少出血、修复表皮与肌肉、降低辐射并补充血液。冷却 10 分钟。"),
            GenerationStyle = BuildingGenerationStyle.None,
            Sprite = sprite,
            Scale = new Vector3(3f, 3f, 1f),
            Health = 3500f,
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
                go.AddComponent<AutoMedSystemDevice>();
            },
        };
        BuildingEntityRegistry.Register(AutoMedSystemId, def);
        Plugin.Log.LogInfo($"[BlueAreaBunker] Registered auto med system '{AutoMedSystemId}'.");
    }

    private static void RegisterBlueAreaStructure()
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
            ".0.0..0.......0.......0......0.0.HH", // 行9 刷卡(1) 门(3) SMU06(6,14) 食物箱(22) 自动医疗(31) 可选SMU06(29)
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行10 内地板
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH", // 行11 外地板
        };

        string[] bgRows =
        {
            "...................................", // 行0
            "...................................", // 行1
            ".....llllllllllllllllllllllllllll..", // 行2
            ".....llllllllllllllllllllllllllll..", // 行3
            ".....llllllllllllllllllllllllllll..", // 行4
            ".....llllllllllllllllllllllllllll..", // 行5
            ".....llllllllllllllllllllllllllll..", // 行6
            ".....llllllllllllllllllllllllllll..", // 行7
            ".....llllllllllllllllllllllllllll..", // 行8
            ".....llllllllllllllllllllllllllll..", // 行9
            "...................................", // 行10
            "...................................", // 行11
        };

        var objectsByCell = new Dictionary<string, object>
        {
            ["316"] = new { id = BlueCardReaderId },
            ["318"] = new { id = BlueDoorId },
            ["321"] = new { id = Snu06BagId },
            ["329"] = new { id = Snu06BagId },
            ["337"] = new { id = "foodbox" },
            ["346"] = new { id = AutoMedSystemId },
            ["344"] = new { id = Snu06BagOptionalId },
        };

        var precisePlacements = new[]
        {
            new { gridX = 1,  gridY = 2,  offsetX = 0f, offsetY = 0f,   rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 3,  gridY = 2,  offsetX = 0.5f, offsetY = 3.5f, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 6,  gridY = 2,  offsetX = 0f, offsetY = Snu06BagFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 14, gridY = 2,  offsetX = 0f, offsetY = Snu06BagFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 22, gridY = 2,  offsetX = 0f, offsetY = 0f,   rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 29, gridY = 2,  offsetX = 0f, offsetY = Snu06BagFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
            new { gridX = 31, gridY = 2,  offsetX = 0f, offsetY = AutoMedFloorOffset, rotation = 0f, scale = 1f, flipX = false, flipY = false },
        };

        var payload = new
        {
            metadata = new
            {
                schemaVersion = 4,
                spawnCounts = new[] { 1, 1, 2, 1, 1 },
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
        bool ok = StructureRegistry.RegisterFromJson(BunkerId, json, "blue area bunker");
        Plugin.Log.LogInfo($"[BlueAreaBunker] Registered structure '{BunkerId}': {ok}");
    }

    private static Sprite LoadSprite(string name)
    {
        return LoadSpriteWithPpu(name, 0f);
    }

    private static Sprite LoadSpriteWithPpu(string name, float ppuOverride)
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", "guns", name + ".png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Point;
            float ppu = ppuOverride > 0f ? ppuOverride : tex.width;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[BlueAreaBunker] LoadSprite('{name}') failed: {ex.Message}");
            return null;
        }
    }
}

/// <summary>Blue Area 刷卡装置。</summary>
public class BlueCardReaderDevice : MonoBehaviour
{
    public const string AccessCardId = "bluearea_keycard";
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
                if (building.id != BlueAreaBunker.BlueDoorId) continue;

                building.Backgroundify();
                cardItem.SetCondition(Mathf.Clamp(cardItem.condition - 0.05f, 0f, 1f));
                Sound.Play("unlock", transform.position, false, true, null, 1f, 1f, false, false);
                Plugin.Log.LogInfo($"[BlueAreaBunker] Blue area keycard condition: {cardItem.condition:P}.");
                return;
            }

            Sound.Play("beep", transform.position, false, true, null, 1f, 1f, false, false);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[BlueAreaBunker] BlueCardReaderDevice.OnUse failed: {ex.Message}");
        }
    }
}

/// <summary>SMU06 医疗包：右键开启，掉落 2~3 药品和 2~4 针剂。</summary>
public class SMU06BagDrop : MonoBehaviour
{
    private BuildingEntity _building;

    private void Awake()
    {
        _building = GetComponent<BuildingEntity>();
    }

    public void OnUse()
    {
        try
        {
            if (_building == null) return;

            int medCount = UnityEngine.Random.Range(2, 4);
            int injectorCount = UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < medCount; i++)
                SpawnDrop(BlueAreaBunker.MedicalItemIds[UnityEngine.Random.Range(0, BlueAreaBunker.MedicalItemIds.Length)]);
            for (int i = 0; i < injectorCount; i++)
                SpawnDrop(BlueAreaBunker.InjectorItemIds[UnityEngine.Random.Range(0, BlueAreaBunker.InjectorItemIds.Length)]);

            _building.health = 0f;
            Plugin.Log.LogInfo($"[BlueAreaBunker] SMU06 bag opened: {medCount} meds, {injectorCount} injectors.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[BlueAreaBunker] SMU06BagDrop.OnUse failed: {ex.Message}");
        }
    }

    private void SpawnDrop(string id)
    {
        var pos = (Vector2)transform.position
                  + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1.5f));
        var go = Utils.Create(id, pos, UnityEngine.Random.Range(0f, 360f));
        if (go == null)
        {
            Plugin.Log.LogWarning($"[BlueAreaBunker] Failed to spawn SMU06 drop '{id}'.");
            return;
        }

        var item = go.GetComponent<Item>();
        if (item != null)
            item.SetCondition(UnityEngine.Random.Range(0.5f, 1f));

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f));

        go.AddComponent<FreshItemDrop>();
    }
}

/// <summary>SMU06 可选医疗包：50% 概率生成。</summary>
public class BlueBagOptionalCuller : MonoBehaviour
{
    private void Awake()
    {
        if (UnityEngine.Random.value < 0.5f)
            Destroy(gameObject);
    }
}

/// <summary>自动医疗系统：减少出血、修复表皮/肌肉、降低辐射、补血，冷却 10 分钟。</summary>
public class AutoMedSystemDevice : MonoBehaviour
{
    private float _nextUseTime = -1f;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_spriteRenderer == null || BlueAreaBunker.AutoMedNormalSprite == null) return;

        bool cooling = Time.time < _nextUseTime;
        var desired = cooling ? BlueAreaBunker.AutoMedCoolingSprite : BlueAreaBunker.AutoMedNormalSprite;
        if (desired != null && _spriteRenderer.sprite != desired)
            _spriteRenderer.sprite = desired;
    }

    public void OnUse()
    {
        try
        {
            var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            if (body == null) return;

            if (Time.time < _nextUseTime)
            {
                var coolPos = (Vector2)transform.position;
                Sound.Play("beep", coolPos, false, true, null, 1f, 1f, false, false);
                CUCoreUtils.DelayCall(0.5f, () => Sound.Play("beep", coolPos, false, true, null, 1f, 1f, false, false));
                CUCoreUtils.DelayCall(1f, () => Sound.Play("beep", coolPos, false, true, null, 1f, 1f, false, false));
                return;
            }

            _nextUseTime = Time.time + 600f;

            foreach (var limb in body.limbs)
            {
                if (limb == null) continue;
                limb.bleedAmount *= 0.5f;
                limb.skinHealth = Mathf.Min(limb.skinHealth + 30f, 100f);
                limb.muscleHealth = Mathf.Min(limb.muscleHealth + 30f, 100f);
            }

            body.radiationSickness = Mathf.Max(body.radiationSickness - 20f, 0f);
            body.bloodVolume = Mathf.Min(body.bloodVolume + 40f, 100f);

            var pos = (Vector2)transform.position;
            Sound.Play("inject", pos, false, true, null, 1f, 1f, false, false);
            Sound.Play("beep", pos, false, true, null, 1f, 1f, false, false);
            CUCoreUtils.DelayCall(1f, () =>
            {
                Sound.Play("beep", pos, false, true, null, 1f, 1f, false, false);
            });

            if (_spriteRenderer != null && BlueAreaBunker.AutoMedCoolingSprite != null)
                _spriteRenderer.sprite = BlueAreaBunker.AutoMedCoolingSprite;

            Plugin.Log.LogInfo($"[BlueAreaBunker] AutoMedSystem used. radiation={body.radiationSickness:0} bloodVolume={body.bloodVolume:0}.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[BlueAreaBunker] AutoMedSystemDevice.OnUse failed: {ex.Message}");
        }
    }
}

/// <summary>Blue Area 背景补丁：拦截 l（limestone, block 19），使用蓝底白字长条。</summary>
public static class BlueAreaBackgroundPatch
{
    public const int LimestoneBlockId = 19;

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
        if (blockId != LimestoneBlockId) return true;

        var world = WorldGeneration.world;
        if (world?.tiles == null || blockId < 0 || blockId >= world.tiles.Length) return false;
        if (!(world.tiles[blockId] is Tile tile) || tile.sprite == null) return false;

        var sprite = GetBackgroundSprite(tile, position);
        if (sprite == null) return false;

        var template = GetTemplate();
        if (template == null) return false;

        var backgroundObject = UnityEngine.Object.Instantiate(template, position, Quaternion.identity);
        backgroundObject.SetActive(true);
        backgroundObject.name = "CUCoreLib_BGTile_" + blockId + "_blue_custom";
        if (world.worldGrid != null)
            backgroundObject.transform.SetParent(world.worldGrid.transform);

        var renderer = backgroundObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -998;

        _backgroundTileCount++;
        int roomIndex = (_backgroundTileCount - 1) % 224;
        if (roomIndex == 0)
            SpawnBlueStrip(position + new Vector2(13.5f, 3.5f), world.worldGrid != null ? world.worldGrid.transform : null);

        return false;
    }

    private static void SpawnBlueStrip(Vector2 position, Transform parent)
    {
        try
        {
            var strip = GetStripSprite();
            if (strip == null) return;

            var go = new GameObject("CUTarkovWeaponMod_BlueAreaStrip");
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
            Plugin.Log.LogWarning($"[BlueAreaBunker] SpawnBlueStrip failed: {ex.Message}");
        }
    }

    private static Sprite GetStripSprite()
    {
        if (!_stripSpriteLoaded)
        {
            _stripSpriteLoaded = true;
            _stripSprite = LoadCustomSprite("structures", "lab_background_strip_blue");
        }
        return _stripSprite;
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
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Paths.PluginPath;
            var path = Path.Combine(assemblyDir, "Framework", "Assets", folder, name + ".png");
            if (!File.Exists(path)) return null;

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Bilinear;

            float ppu = tex.width;
            if (name == "lab_background_strip_blue")
                ppu = tex.width / 28f;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[BlueAreaBunker] LoadCustomSprite('{folder}/{name}') failed: {ex.Message}");
            return null;
        }
    }

    private static GameObject GetTemplate()
    {
        if (_template != null) return _template;

        _template = new GameObject("CUTarkovWeaponMod_BlueAreaBackgroundTileTemplate");
        _template.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(_template);
        _template.AddComponent<SpriteRenderer>();
        return _template;
    }
}
