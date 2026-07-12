using HarmonyLib;

using CUTarkovWeaponMod.Framework;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// 非 CUCoreLib 模式（遗留模式）。
/// QoL 存档兼容由 CUTarkovMedicalMod 的 QoLSaveFix 统一处理
/// （QoLSaveFix_ItemMap.CustomIds 已包含全部武器物品 ID 和 PrefabMap 映射），
/// WeaponMod 无需任何额外操作。
/// </summary>
public sealed class WeaponLegacyMode : IWeaponIntegrationMode
{
    public void Initialize(Harmony harmony)
    {
        Plugin.Log.LogInfo("[WeaponLegacyMode] Initialize. QoL save compat handled by MedicalMod QoLSaveFix.");
    }

    public void OnItemsSetup()
    {
        // 遗留模式无需额外操作，EnsureRegisteredInItemTable 已将武器物品注册到 GlobalItems。
    }
}
