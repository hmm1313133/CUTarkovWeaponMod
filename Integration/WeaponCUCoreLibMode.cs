using System;
using CUCoreLib.Registries;
using CUTarkovWeaponMod.Framework;
using HarmonyLib;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// CUCoreLib 模式。
/// - Initialize: 无操作（武器无持久化效果需要 SaveProvider）
/// - OnItemsSetup: 将已注册到 GlobalItems 的武器物品同步注册到 CUCoreLib ItemRegistry
///   （防止存档加载时 CustomInstantiate.GetOrCreateTemplate 返回 null -> NRE）
/// </summary>
public sealed class WeaponCUCoreLibMode : IWeaponIntegrationMode
{
    public void Initialize(Harmony harmony)
    {
        // 武器无持久化效果（不像医疗模组有 StimEffectController），无需注册 SaveProvider。
        // QoL 存档兼容由 CUCoreLib 非破坏性 SaveCoordinator 接管。
        Plugin.Log.LogInfo("[WeaponCUCoreLib] Initialize. Save handled by CUCoreLib SaveCoordinator.");
    }

    public void OnItemsSetup()
    {
        // 将已注册到 GlobalItems 的武器物品同步注册到 CUCoreLib ItemRegistry。
        // CustomInstantiate.GetOrCreateTemplate(id) 从 RegisteredItems 查找；
        // 未注册的自定义物品 ID 在存档加载时返回 null -> NRE。
        if (Item.GlobalItems == null) return;

        var registered = 0;
        foreach (var itemId in WeaponItemRegistration.WeaponItemIds)
        {
            try
            {
                if (!Item.GlobalItems.ContainsKey(itemId)) continue;
                var info = Item.GlobalItems[itemId];
                if (info == null) continue;

                ItemRegistry.Register(itemId, info, null);
                registered++;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[WeaponCUCoreLib] Failed to register item '{itemId}': {ex.Message}");
            }
        }

        if (registered > 0)
            Plugin.Log.LogInfo($"[WeaponCUCoreLib] Registered {registered} weapon items with ItemRegistry.");
    }
}
