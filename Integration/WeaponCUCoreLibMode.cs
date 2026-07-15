using System;
using System.Collections.Generic;
using System.Reflection;
using CUCoreLib.Data;
using CUCoreLib.Helpers;
using CUCoreLib.Registries;
using CUCoreLib.Saving;
using CUTarkovMedicalMod.Framework;
using CUTarkovWeaponMod.Framework;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// CUCoreLib 集成模式（CUCoreLib 为硬依赖）。
///
/// - 不注册 QoLSaveFix，由 CUCoreLib CustomInstantiate 原生处理存档加载
/// - 构建 CustomItemInfo 注册到 CUCoreLib ItemRegistry
/// - 注册 IItemSaveProvider 持久化武器运行时状态（弹药数、弹匣状态）
///
/// 武器特有问题及解决方案：
/// CUCoreLib ChooseTemplateId 只认识 waterbottle/bandage/smallpack/flashlight 模板，
/// 不认识 rifle/pistol/shotgun 等武器模板。
/// 解决：在 OnItemsSetup 中预先创建正确的武器模板（基于 rifle/pistol/shotgun 等基础预制体），
/// 通过反射注入 CustomInstantiate._templateCache，使 GetOrCreateTemplate 直接返回正确模板，
/// 绕过 ChooseTemplateId。
///
/// 存档加载流程：
/// 1. CUCoreLib Transpiler 拦截 Resources.Load("akm") -> null -> GetOrCreateTemplate("akm")
/// 2. GetOrCreateTemplate 在 _templateCache 找到预创建的 rifle 模板 -> 返回
/// 3. InstantiateSavedItem 创建实例并激活
/// 4. Item.Start() -> CUCoreLib ApplyCustomItemRuntime（应用图标/缩放）
/// 5. SaveCoordinator.ApplyPendingRestore -> WeaponItemSaveProvider.Restore
///    -> 调用 ConfigureCustomItem 设置 GunScript 等 -> 恢复弹药/弹匣状态
///
/// KrokMP 多人联机图标修复：
/// 注册到 ItemRegistry 时通过反射调用各 ItemSystem 类的 TryLoadIcon() 获取图标 Sprite。
/// 客户端通过 KrokMP 同步创建的物品在 Item.Start() 时由 ApplyCustomItemRuntime
/// 用此图标替换精灵图，避免显示为原版步枪/弹匣外观。
/// 同时将图标设置到预创建模板的 SpriteRenderer 上作为即时外观。
/// </summary>
public sealed class WeaponCUCoreLibMode
{
    public void Initialize(Harmony harmony)
    {
        // 注册武器运行时状态保存提供者（弹药数、弹匣状态）
        SaveRegistry.RegisterItemProvider("cutarkovweapon.runtime", new WeaponItemSaveProvider());
        Plugin.Log.LogInfo("[WeaponCUCoreLib] Registered WeaponItemSaveProvider (ammo/mag state).");
    }

    public void OnItemsSetup()
    {
        if (Item.GlobalItems == null) return;

        // 获取 CustomInstantiate._templateCache（private static）
        var templateCacheField = typeof(CustomInstantiate).GetField(
            "_templateCache", BindingFlags.NonPublic | BindingFlags.Static);
        var templateCache = templateCacheField?.GetValue(null) as Dictionary<string, GameObject>;

        // 诊断日志：验证反射是否成功
        if (templateCacheField == null)
        {
            Plugin.Log.LogError("[WeaponCUCoreLib] CRITICAL: CustomInstantiate._templateCache field NOT FOUND via reflection! Template injection will fail, items will appear as bandage.");
        }
        else if (templateCache == null)
        {
            Plugin.Log.LogError("[WeaponCUCoreLib] CRITICAL: _templateCache is null! Template injection will fail.");
        }
        else
        {
            Plugin.Log.LogInfo($"[WeaponCUCoreLib] _templateCache resolved OK (current entries: {templateCache.Count}).");
        }

        // 构建 itemId -> ItemSystem 类型的映射，用于反射调用 TryLoadIcon()
        var itemTypeMap = BuildItemTypeMap();

        var registered = 0;
        var templatesInjected = 0;
        foreach (var itemId in WeaponItemRegistration.WeaponItemIds)
        {
            try
            {
                if (!Item.GlobalItems.ContainsKey(itemId)) continue;
                var plainInfo = Item.GlobalItems[itemId];
                if (plainInfo == null) continue;

                // --- 1. 构建 CustomItemInfo ---
                var customInfo = new CustomItemInfo();
                foreach (var field in GetPublicInstanceFields(plainInfo.GetType()))
                    field.SetValue(customInfo, field.GetValue(plainInfo));
                customInfo.capacity = 0;
                customInfo.autoFill = false;
                customInfo.defaultContents = null;

                // --- 2. 解析图标 ---
                Sprite icon = null;
                if (itemTypeMap.TryGetValue(itemId, out var itemSystemType))
                {
                    icon = ResolveIcon(itemSystemType, itemId);
                }
                if (icon == null)
                    Plugin.Log.LogWarning($"[WeaponCUCoreLib] Icon is null for '{itemId}', will keep base prefab sprite.");

                // --- 3. 预创建正确的武器模板 ---
                if (templateCache != null &&
                    ConsoleSpawnPatch.CustomItemPrefabs.TryGetValue(itemId, out var basePrefabId))
                {
                    var basePrefab = Resources.Load<GameObject>(basePrefabId);
                    if (basePrefab != null)
                    {
                        var template = Object.Instantiate(basePrefab);
                        template.SetActive(false);
                        template.name = itemId;
                        Object.DontDestroyOnLoad(template);

                        var itemComp = template.GetComponent<Item>();
                        if (itemComp != null) itemComp.id = itemId;

                        // 调用 ConfigureCustomItem 设置全部武器字段：
                        // - GunScript: normalSprite/rackedSprite/normalSpriteNoMag/rackedSpriteNoMag
                        //   （GunScript.Update 会用这些字段覆盖 sr.sprite，不设置则保持原版贴图）
                        // - GunScript: magCapacity/damage/spread/barrel位置等
                        // - AmmoScript: itemType/maxRounds 等
                        // - SpriteRenderer.sprite + collider
                        // 这样 KrokMP 同步创建的实例从模板继承全部正确配置。
                        try
                        {
                            var request = new MedicalGrantRequest(itemId, itemId, 1, "TemplateSetup", basePrefabId);
                            ConsoleSpawnPatch.ConfigureCustomItem(itemComp, request);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogWarning(
                                $"[WeaponCUCoreLib] ConfigureCustomItem on template '{itemId}' failed: {ex.Message}");
                        }

                        templateCache[itemId] = template;
                        templatesInjected++;
                    }
                    else
                    {
                        Plugin.Log.LogWarning(
                            $"[WeaponCUCoreLib] Base prefab '{basePrefabId}' not found for '{itemId}'.");
                    }
                }

                // --- 4. 注册到 CUCoreLib ItemRegistry ---
                // 不传 icon（传 null）：CUCoreLib GetInventorySprite 会回退到 sr.sprite，
                // 而 sr.sprite 由 GunScript.Update() 根据hasMag 在 normalSprite/normalSpriteNoMag
                // 之间切换，实现背包缩略图的弹匣差分。
                // 模板上 ConfigureCustomItem 已设置全部 GunScript 贴图字段和 sr.sprite。
                ItemRegistry.Register(itemId, customInfo, null);
                registered++;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[WeaponCUCoreLib] Failed to register item '{itemId}': {ex.Message}");
            }
        }

        // 验证模板缓存注入结果
        if (templateCache != null)
        {
            var sampleId = AKMItemSystem.ItemKey;
            var hasSample = templateCache.ContainsKey(sampleId);
            Plugin.Log.LogInfo(
                $"[WeaponCUCoreLib] Registered {registered} items, injected {templatesInjected} templates. Cache now has {templateCache.Count} entries. Sample '{sampleId}' in cache: {hasSample}.");
        }
        else
        {
            Plugin.Log.LogError(
                $"[WeaponCUCoreLib] Registered {registered} items but templateCache is null - 0 templates injected. ALL weapon items will appear as bandage!");
        }
    }

    /// <summary>
    /// 遍历类型层级（从 type 到 ItemInfo）获取所有公共实例字段，去重。
    /// 与 CUCoreLib ItemRegistry.ToCustomItemInfo 的 GetPublicInstanceFields 逻辑一致。
    /// </summary>
    private static IEnumerable<FieldInfo> GetPublicInstanceFields(Type type)
    {
        var seen = new HashSet<string>();
        for (var current = type;
             current != null && typeof(ItemInfo).IsAssignableFrom(current);
             current = current.BaseType)
            foreach (var field in current.GetFields(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (seen.Add(field.Name))
                    yield return field;
    }

    /// <summary>
    /// 扫描武器模组程序集中所有带有 public const string ItemKey 字段的类型，
    /// 构建 itemId -> Type 映射，用于反射调用 TryLoadIcon()。
    /// </summary>
    private static Dictionary<string, Type> BuildItemTypeMap()
    {
        var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(WeaponCUCoreLibMode).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            var keyField = type.GetField("ItemKey",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (keyField == null || !keyField.IsLiteral) continue;
            var itemId = keyField.GetRawConstantValue() as string;
            if (!string.IsNullOrEmpty(itemId))
                map[itemId] = type;
        }
        return map;
    }

    /// <summary>
    /// 通过反射调用 ItemSystem 类的 TryLoadIcon() 方法获取图标 Sprite。
    /// 镜像医疗模组 MedicalItemDef.ResolveIcon() 的实现。
    /// </summary>
    private static Sprite ResolveIcon(Type itemSystemType, string itemId)
    {
        try
        {
            var method = itemSystemType.GetMethod("TryLoadIcon",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method?.Invoke(null, null) as Sprite;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(
                $"[WeaponCUCoreLib] Failed to resolve icon for '{itemId}': {ex.Message}");
            return null;
        }
    }
}
