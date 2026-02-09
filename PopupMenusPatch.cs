using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NuclearOption.MissionEditorScripts;
using NuclearOption.SceneLoading;
using UnityEngine;

namespace NOMapLoader;

[HarmonyPatch(typeof(MissionEditorNewMenu), "Awake")]
public class PopupMenusPatch
{
	[HarmonyPrefix]
	static void Prefix(MissionEditorNewMenu __instance)
	{
		Plugin.Log.LogInfo("Attempting to add custom map selectors");
		
		var root = __instance.transform;
		var mapOptions = root.Find("Body").Find("Map options").gameObject;
		if (mapOptions == null)
		{
			Plugin.Log.LogError("No Map options found");
			return;
		}
		
		var buttons = mapOptions.GetComponentsInChildren<NewMissionMapButton>();
		if (buttons.Length <= 0) return;
		var template = buttons[0].gameObject;
		
		GameObject newButton = Object.Instantiate(template, template.transform.parent);
		newButton.transform.localPosition = new Vector3(412f, -80f, 0f);
		NewMissionMapButton newButtonScript = newButton.GetComponent<NewMissionMapButton>();
		newButtonScript.SetMap(Plugin.CustomMaps["Terrain_naval_squished"].Details);

		FieldInfo mapButtonsField = AccessTools.Field(typeof(MissionEditorNewMenu), "mapButtons");
		NewMissionMapButton[] mapButtons = mapButtonsField.GetValue(__instance) as NewMissionMapButton[];
		
		mapButtonsField.SetValue(__instance, mapButtons.AddToArray(newButtonScript));
		
		Plugin.Log.LogInfo("Added new map button");
	}
}