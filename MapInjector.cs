using System.Reflection;
using HarmonyLib;
using NuclearOption.SceneLoading;
using UnityEngine;

namespace NOMapLoader;

[HarmonyPatch(typeof(SceneSingleton<MapSettingsManager>), "Awake")]
public static class SceneSingletonPatch
{
	static void Postfix(object __instance)
	{
		if (__instance is MapSettingsManager mapSettingsManager)
		{
			foreach (var mapName in Plugin.CustomMaps.Keys)
			{
				CustomMapData customMapData = Plugin.CustomMaps[mapName];
				MapSettingsManager.Map mapData = new MapSettingsManager.Map
				{
					Details = customMapData.Details,
					Prefab = customMapData.Settings
				};
				mapSettingsManager.Maps = mapSettingsManager.Maps.AddToArray(mapData);
			}
			Plugin.Log.LogInfo($"Injected {Plugin.CustomMaps.Count} custom maps into MapSettingsManager");
		}
	}
}

[HarmonyPatch(typeof(MainMenu), "Start")]
public static class MainMenuPatch
{
	static void Postfix()
	{
		if (Plugin.BlueprinterLoaded)  return; //delegate to later load.
		MapLoader mapLoader = Resources.Load<MapLoader>("MapLoader");
		if (mapLoader == null)
		{
			Plugin.Log.LogError("No MapLoader found");
			return;
		}
		foreach (var mapName in Plugin.CustomMaps.Keys)
		{
			CustomMapData customMapData = Plugin.CustomMaps[mapName];
			mapLoader.Maps = mapLoader.Maps.AddToArray(customMapData.Details);
		}
		Plugin.Log.LogInfo($"Injected {Plugin.CustomMaps.Count} custom maps into MapLoader");
	}
}
