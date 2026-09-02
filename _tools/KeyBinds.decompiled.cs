using System.Collections.Generic;
using UnityEngine;

public static class KeyBinds
{
	private static Dictionary<string, KeyCode> cachedBinds = new Dictionary<string, KeyCode>();

	public static void CacheBind(SettingKeybind keybind)
	{
		cachedBinds[keybind.name] = keybind.value;
	}

	public static KeyCode GetBind(string action)
	{
		if (cachedBinds.TryGetValue(action, out var value))
		{
			return value;
		}
		return KeyCode.None;
	}

	public static string GetBindName(string action)
	{
		return GetBind(action).ToString();
	}
}
