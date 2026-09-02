using HarmonyLib;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 诊断：Settings.SaveSettings 时检查 Settings.settings 里是否有我们的 keybind。
/// </summary>
public static class SettingsSavePatch
{
    [HarmonyPatch(typeof(Settings), nameof(Settings.SaveSettings))]
    [HarmonyPostfix]
    public static void SaveSettings_Postfix()
    {
        try
        {
            if (Settings.settings == null)
            {
                Plugin.Log.LogInfo("[SettingsSave] Settings.settings is null.");
                return;
            }
            int count = 0;
            foreach (var s in Settings.settings)
            {
                if (s != null && s.name != null && s.name.StartsWith("cutarkovweapon."))
                    count++;
            }
            Plugin.Log.LogInfo($"[SettingsSave] Settings.settings has {count} cutarkovweapon.* settings (total {Settings.settings.Count}).");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[SettingsSave] {ex.Message}");
        }
    }
}
