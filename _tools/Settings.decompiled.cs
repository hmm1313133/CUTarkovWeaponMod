using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class Settings
{
	public static List<Setting> settings;

	private static bool initialized;

	public static List<Setting> GetAllSettings()
	{
		EnsureLoaded();
		return settings;
	}

	public static List<Setting> DefaultSettings()
	{
		return new List<Setting>
		{
			new SettingInt
			{
				name = "reswidth",
				value = Screen.resolutions[Screen.resolutions.Length - 1].width,
				min = 800,
				max = 7680,
				apply = delegate
				{
					SettingInt setting11 = GetSetting<SettingInt>("reswidth");
					SettingInt setting12 = GetSetting<SettingInt>("resheight");
					Screen.SetResolution(fullscreenMode: GetSetting<SettingBool>("fullscreen").value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, width: setting11.value, height: setting12.value);
				},
				category = Setting.SettingCategory.Video
			},
			new SettingInt
			{
				name = "resheight",
				value = Screen.resolutions[Screen.resolutions.Length - 1].height,
				min = 600,
				max = 2160,
				apply = delegate
				{
					SettingInt setting9 = GetSetting<SettingInt>("reswidth");
					SettingInt setting10 = GetSetting<SettingInt>("resheight");
					Screen.SetResolution(fullscreenMode: GetSetting<SettingBool>("fullscreen").value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, width: setting9.value, height: setting10.value);
				},
				category = Setting.SettingCategory.Video
			},
			new SettingBool
			{
				name = "fullscreen",
				value = true,
				apply = delegate
				{
					SettingInt setting7 = GetSetting<SettingInt>("reswidth");
					SettingInt setting8 = GetSetting<SettingInt>("resheight");
					Screen.SetResolution(fullscreenMode: GetSetting<SettingBool>("fullscreen").value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, width: setting7.value, height: setting8.value);
				},
				category = Setting.SettingCategory.Video
			},
			new SettingBool
			{
				name = "vsync",
				value = true,
				apply = delegate
				{
					QualitySettings.vSyncCount = (GetSetting<SettingBool>("vsync").value ? 1 : 0);
				},
				category = Setting.SettingCategory.Video
			},
			new SettingInt
			{
				name = "framerate",
				value = 0,
				min = 0,
				max = 240,
				apply = delegate
				{
					SettingInt setting6 = GetSetting<SettingInt>("framerate");
					Application.targetFrameRate = ((setting6.value == 0) ? (-1) : Math.Max(10, setting6.value));
				},
				category = Setting.SettingCategory.Video
			},
			new SettingBool
			{
				name = "pixelate",
				value = true,
				apply = delegate
				{
					SettingBool setting5 = GetSetting<SettingBool>("pixelate");
					if ((bool)PlayerCamera.main)
					{
						PlayerCamera.main.GetComponent<PixelPerfectCamera>().enabled = setting5.value;
					}
				},
				category = Setting.SettingCategory.Video
			},
			new SettingBool
			{
				name = "darkerbackground",
				value = false,
				apply = delegate
				{
					if ((bool)WorldGeneration.world)
					{
						WorldGeneration.world.UpdateAllBackgroundColors();
					}
				},
				category = Setting.SettingCategory.Video
			},
			new SettingBool
			{
				name = "bloom",
				value = true,
				apply = delegate
				{
					if ((bool)WorldGeneration.world)
					{
						WorldGeneration.world.UpdateBiomePostProcess();
					}
				},
				category = Setting.SettingCategory.Video
			},
			new SettingFloat
			{
				name = "filmgrain",
				value = 1f,
				max = 1f,
				min = 0f,
				apply = delegate
				{
					if ((bool)WorldGeneration.world)
					{
						WorldGeneration.world.UpdateBiomePostProcess();
					}
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Video
			},
			new SettingFloat
			{
				name = "brightness",
				value = 0f,
				max = 1f,
				min = 0f,
				apply = delegate
				{
					PlayerCamera.brightness = GetSetting<SettingFloat>("brightness").value;
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Video
			},
			new SettingInt
			{
				name = "lineofsightcount",
				value = 256,
				min = 128,
				max = 512,
				apply = delegate
				{
					PlayerCamera.lineOfSightRes = GetSetting<SettingInt>("lineofsightcount").value;
				},
				category = Setting.SettingCategory.Video
			},
			new SettingFloat
			{
				name = "screenshake",
				value = 1f,
				max = 1f,
				min = 0f,
				apply = delegate
				{
					PlayerCamera.screenShakeMult = GetSetting<SettingFloat>("screenshake").value;
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Video
			},
			new SettingBool
			{
				name = "censorpain",
				value = false,
				apply = delegate
				{
					Body.censorPain = GetSetting<SettingBool>("censorpain").value;
				},
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "censormood",
				value = false,
				apply = delegate
				{
					Body.censorMood = GetSetting<SettingBool>("censormood").value;
				},
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "alwaysexpand",
				value = false,
				apply = delegate
				{
					PlayerCamera.alwaysExpandDescriptions = GetSetting<SettingBool>("alwaysexpand").value;
				},
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "showhiddenmoodles",
				value = false,
				apply = delegate
				{
					MoodleManager.alwaysShowHidden = GetSetting<SettingBool>("showhiddenmoodles").value;
				},
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "itemfloating",
				value = true,
				apply = delegate
				{
					Item.itemFloating = GetSetting<SettingBool>("itemfloating").value;
				},
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "clearerrecipes",
				value = false,
				apply = delegate
				{
					PlayerCamera.clearerRecipes = GetSetting<SettingBool>("clearerrecipes").value;
				},
				category = Setting.SettingCategory.Game
			},
			new SettingFloat
			{
				name = "inventoryuseleniency",
				value = 0.95f,
				max = 0.99f,
				min = 0.1f,
				apply = delegate
				{
					PlayerCamera.inventoryUseLeniency = GetSetting<SettingFloat>("inventoryuseleniency").value;
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "skipintro",
				value = false,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Game
			},
			new SettingBool
			{
				name = "rpc",
				value = true,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Game
			},
			new SettingFloat
			{
				name = "mastervolume",
				value = 1f,
				max = 1f,
				min = 0f,
				apply = delegate
				{
					AudioListener.volume = GetSetting<SettingFloat>("mastervolume").value;
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Audio
			},
			new SettingFloat
			{
				name = "musicvolume",
				value = 0.45f,
				max = 1f,
				min = 0f,
				apply = delegate
				{
					SettingFloat setting4 = GetSetting<SettingFloat>("musicvolume");
					if ((bool)MusicManager.main)
					{
						MusicManager.main.origVolume = setting4.value;
					}
					if ((bool)PreRunScript.instance)
					{
						PreRunScript.instance.menuSource.volume = Get<SettingFloat>("musicvolume").value * 0.6666f;
					}
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Audio
			},
			new SettingFloat
			{
				name = "dronevolume",
				value = 1f,
				max = 1f,
				min = 0f,
				apply = delegate
				{
					SettingFloat setting3 = GetSetting<SettingFloat>("dronevolume");
					if ((bool)MusicManager.main)
					{
						MusicManager.main.droneVolume = setting3.value;
					}
				},
				formatValue = (float v) => Mathf.RoundToInt(v * 100f) + "%",
				category = Setting.SettingCategory.Audio
			},
			new SettingFloat
			{
				name = "trackinterval",
				value = 170f,
				max = 1200f,
				min = 10f,
				apply = delegate
				{
					SettingFloat setting2 = GetSetting<SettingFloat>("trackinterval");
					if ((bool)MusicManager.main)
					{
						MusicManager.main.trackInterval = setting2.value;
					}
				},
				formatValue = (float v) => Mathf.RoundToInt(v) + "s",
				category = Setting.SettingCategory.Audio
			},
			new SettingBool
			{
				name = "sadnessdistortion",
				value = true,
				apply = delegate
				{
					SettingBool setting = GetSetting<SettingBool>("sadnessdistortion");
					if ((bool)MusicManager.main)
					{
						MusicManager.main.sadnessDistortion = setting.value;
					}
				},
				category = Setting.SettingCategory.Audio
			},
			new SettingBool
			{
				name = "autojump",
				value = false,
				apply = delegate
				{
					PlayerCamera.autoJump = GetSetting<SettingBool>("autojump").value;
				},
				category = Setting.SettingCategory.Input
			},
			new SettingBool
			{
				name = "togglelabels",
				value = false,
				apply = delegate
				{
					AltHoverScript.toggleActivated = GetSetting<SettingBool>("togglelabels").value;
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "jump",
				value = KeyCode.Space,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "left",
				value = KeyCode.A,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "right",
				value = KeyCode.D,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "down",
				value = KeyCode.S,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "up",
				value = KeyCode.W,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "attack",
				value = KeyCode.Mouse0,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "iteminteract",
				value = KeyCode.Mouse1,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "throw",
				value = KeyCode.T,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "switchhands",
				value = KeyCode.Q,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "ragdoll",
				value = KeyCode.X,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "woundview",
				value = KeyCode.R,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "craft",
				value = KeyCode.C,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "favourite",
				value = KeyCode.F,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "toggleinventory",
				value = KeyCode.Tab,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "speed1",
				value = KeyCode.Alpha1,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "speed2",
				value = KeyCode.Alpha2,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "speed3",
				value = KeyCode.Alpha3,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "expanddesc",
				value = KeyCode.LeftShift,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "altview",
				value = KeyCode.LeftControl,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "bark",
				value = KeyCode.B,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "console",
				value = KeyCode.BackQuote,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			},
			new SettingKeybind
			{
				name = "pause",
				value = KeyCode.Escape,
				apply = delegate
				{
				},
				category = Setting.SettingCategory.Input
			}
		};
	}

	public static void EnsureLoaded()
	{
		if (!initialized)
		{
			LoadSettings();
		}
	}

	private static T GetSetting<T>(string name) where T : Setting
	{
		return settings.Find((Setting s) => s.name == name) as T;
	}

	public static T Get<T>(string name) where T : Setting
	{
		EnsureLoaded();
		Setting setting = settings.Find((Setting s) => s.name == name);
		if (setting == null)
		{
			Debug.LogError("Setting '" + name + "' not found.");
			return null;
		}
		if (!(setting is T result))
		{
			Debug.LogError("Setting '" + name + "' is of type " + setting.GetType().Name + ", not " + typeof(T).Name);
			return null;
		}
		return result;
	}

	public static void LoadSettings()
	{
		string path = Application.persistentDataPath + "/settings.json";
		settings = DefaultSettings();
		if (!File.Exists(path))
		{
			SaveSettings();
			return;
		}
		try
		{
			foreach (SettingSaveData saved in JsonConvert.DeserializeObject<List<SettingSaveData>>(File.ReadAllText(path)))
			{
				Setting setting = settings.Find((Setting s) => s.name == saved.name);
				if (setting != null)
				{
					if (setting is SettingFloat)
					{
						setting.SetValue(Convert.ToSingle(saved.value));
					}
					else if (setting is SettingInt)
					{
						setting.SetValue(Convert.ToInt32(saved.value));
					}
					else if (setting is SettingDropdown)
					{
						setting.SetValue(Convert.ToInt32(saved.value));
					}
					else if (setting is SettingBool)
					{
						setting.SetValue(Convert.ToBoolean(saved.value));
					}
					else if (setting is SettingKeybind)
					{
						setting.SetValue(Convert.ToInt32(saved.value));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to load settings! " + ex);
		}
		initialized = true;
		ApplyAll();
	}

	public static void ApplyAll()
	{
		foreach (Setting setting in settings)
		{
			setting.Apply();
		}
	}

	public static void SaveSettings()
	{
		List<SettingSaveData> list = new List<SettingSaveData>();
		foreach (Setting setting in settings)
		{
			list.Add(new SettingSaveData
			{
				name = setting.name,
				value = setting.GetValue()
			});
		}
		string contents = JsonConvert.SerializeObject(list, Formatting.Indented);
		File.WriteAllText(Application.persistentDataPath + "/settings.json", contents);
	}
}
