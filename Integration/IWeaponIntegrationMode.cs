using HarmonyLib;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// 武器模组集成模式接口。封装 CUCoreLib 和非 CUCoreLib 两种模式的差异。
/// 镜像 CUTarkovMedicalMod.Integration.IIntegrationMode 结构，便于统一维护。
/// </summary>
public interface IWeaponIntegrationMode
{
    /// <summary>
    /// 在 harmony.PatchAll() 之后调用，负责注册存档相关补丁或提供者。
    /// </summary>
    void Initialize(Harmony harmony);

    /// <summary>
    /// 在 WeaponItemRegistryPatch.Postfix 中所有 EnsureRegisteredInItemTable 调用之后触发。
    /// CUCoreLib 模式下将已注册到 GlobalItems 的武器物品同步注册到 CUCoreLib ItemRegistry。
    /// </summary>
    void OnItemsSetup();
}
