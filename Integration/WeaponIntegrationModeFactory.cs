using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;

namespace CUTarkovWeaponMod.Integration;

/// <summary>
/// 根据运行时是否安装 CUCoreLib 选择武器集成模式。
/// 镜像 CUTarkovMedicalMod.Integration.IntegrationModeFactory 结构。
/// </summary>
public static class WeaponIntegrationModeFactory
{
    public static IWeaponIntegrationMode Create()
    {
        var hasCUCoreLib = IsCUCoreLibPresent();
        Plugin.Log.LogInfo($"[WeaponIntegrationModeFactory] CUCoreLib present: {hasCUCoreLib}");
        if (hasCUCoreLib)
            return CreateCUCoreLibMode();
        return new WeaponLegacyMode();
    }

    private static bool IsCUCoreLibPresent()
    {
        try
        {
            return Chainloader.PluginInfos.ContainsKey("net.cucorelib");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 隔离 CUCoreLib 类型的实例化，确保未安装 CUCoreLib 时不会触发程序集加载。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IWeaponIntegrationMode CreateCUCoreLibMode()
    {
        return new WeaponCUCoreLibMode();
    }
}
