using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NuclearOption.MissionEditorScripts;
using NuclearOption.SceneLoading;
using UnityEngine;
using UnityEngine.UI;

namespace NOMapLoader;

[HarmonyPatch(typeof(MissionEditorNewMenu), "Awake")]
public class PopupMenusPatch
{
	[HarmonyPrefix]
	static void Prefix(MissionEditorNewMenu __instance)
	{
		Plugin.Log.LogInfo("Attempting to add custom map selectors");
		
		var root = __instance.transform;
		var mapOptions = root.Find("Body")?.Find("Map options");
		if (mapOptions == null)
		{
			Plugin.Log.LogError("Map options not found, aborting!");
			return;
		}
		
		var existingButtons = mapOptions.GetComponentsInChildren<NewMissionMapButton>();
		if (existingButtons.Length <= 0)
		{
			Plugin.Log.LogInfo("No existing map selectors found, aborting!");
			return;
		}

		var contentRectTransform = SetupUI(mapOptions);

		foreach (var btn in existingButtons)
		{
			var rt = btn.GetComponent<RectTransform>();
			rt.SetParent(contentRectTransform, false);
		}
		
		var template = existingButtons[0].gameObject;

		List<NewMissionMapButton> newButtons = new List<NewMissionMapButton>();
		foreach (var map in Plugin.CustomMaps.Values)
		{
			var newButtonGO = Object.Instantiate(template.gameObject, contentRectTransform);
			var newButton = newButtonGO.GetComponent<NewMissionMapButton>();
			newButton.SetMap(map.Details);
			newButtons.Add(newButton);
		}
		
		
		

		FieldInfo mapButtonsField = AccessTools.Field(typeof(MissionEditorNewMenu), "mapButtons");
		NewMissionMapButton[] mapButtons = mapButtonsField.GetValue(__instance) as NewMissionMapButton[];
		
		mapButtonsField.SetValue(__instance, mapButtons.AddRangeToArray(newButtons.ToArray()));
		
		Plugin.Log.LogInfo($"Added {newButtons.Count} new map buttons");
	}

	private static RectTransform SetupUI(Transform mapOptions)
	{
		var scrollRectGO = new GameObject("MapScrollRect", typeof(RectTransform), typeof(ScrollRect));
		scrollRectGO.transform.SetParent(mapOptions, false);
		
		var scrollRect = scrollRectGO.GetComponent<ScrollRect>();
		scrollRect.horizontal = true;
		scrollRect.vertical = false;
		scrollRect.inertia = true;
		scrollRect.movementType = ScrollRect.MovementType.Elastic;
		
		var scrollRectTransform = scrollRectGO.GetComponent<RectTransform>();
		scrollRectTransform.anchorMin = Vector2.zero;
		scrollRectTransform.anchorMax = Vector2.one;
		scrollRectTransform.offsetMin = Vector2.zero;
		scrollRectTransform.offsetMax = Vector3.zero;
		scrollRectTransform.anchoredPosition =  new  Vector2(270f, -90f);
		scrollRectTransform.sizeDelta = new  Vector2(540f, 220f);
		
		var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
		viewportGO.transform.SetParent(scrollRectGO.transform, false);
		
		var viewportRectTransform = viewportGO.GetComponent<RectTransform>();
		viewportRectTransform.anchorMin = Vector2.zero;
		viewportRectTransform.anchorMax = Vector2.one;
		viewportRectTransform.offsetMin = Vector2.zero;
		viewportRectTransform.offsetMax = Vector2.zero;
		
		var viewportImage = viewportGO.GetComponent<Image>();
		viewportImage.color = new Color32(0, 0, 0, 0);

		scrollRect.viewport = viewportRectTransform;
		
		var contentGO = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
		contentGO.transform.SetParent(viewportGO.transform, false);
		
		var contentRectTransform = contentGO.GetComponent<RectTransform>();
		contentRectTransform.anchorMin = new Vector2(0f, 0.5f);
		contentRectTransform.anchorMax = new Vector2(0f, 0.5f);
		contentRectTransform.pivot = new Vector2(0f, 0.5f);
		contentRectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
		
		var contentSizeFitter =  contentGO.GetComponent<ContentSizeFitter>();
		contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		
		scrollRect.content = contentRectTransform;
		
		var layout = contentGO.GetComponent<HorizontalLayoutGroup>();
		layout.spacing = 10f;
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = false;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;
		layout.padding.left = 6;
		layout.padding.right = 6;
		
		var scrollbarGO = new GameObject("HorizontalScrollbar", typeof (RectTransform), typeof(Scrollbar));
		scrollbarGO.transform.SetParent(scrollRectTransform, false);
		
		var scrollbarRectTransform = scrollbarGO.GetComponent<RectTransform>();
		scrollbarRectTransform.anchorMin = Vector2.zero;
		scrollbarRectTransform.anchorMax = new Vector2(1f, 0f);
		scrollbarRectTransform.pivot = new Vector2(0.5f, 0f);
		scrollbarRectTransform.sizeDelta = new Vector2(0f, 14f);
		scrollbarRectTransform.anchoredPosition = new Vector2(0f, 0f);
		
		var scrollbar =  scrollbarGO.GetComponent<Scrollbar>();
		scrollbar.direction = Scrollbar.Direction.LeftToRight;
		
		var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
		slidingArea.transform.SetParent(scrollbarGO.transform, false);
		var slidingRT = slidingArea.GetComponent<RectTransform>();
		slidingRT.anchorMin = Vector2.zero;
		slidingRT.anchorMax = Vector2.one;
		slidingRT.offsetMin = Vector2.zero;
		slidingRT.offsetMax = Vector2.zero;
		
		var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
		handleGO.transform.SetParent(slidingArea.transform, false);

		var handleRT = handleGO.GetComponent<RectTransform>();
		handleRT.anchorMin = Vector2.zero;
		handleRT.anchorMax = Vector2.one;
		handleRT.offsetMin = Vector2.zero;
		handleRT.offsetMax = Vector2.zero;
		
		scrollbar.handleRect = handleRT;
		scrollRect.horizontalScrollbar = scrollbar;
		
		return contentRectTransform;
	}
}