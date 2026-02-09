using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;

namespace NOMapLoader;

public static class BlueprinterHelper
{
	private static bool checkComplete = false;
	private static Type loaderType;
	private static Type registryType;
	private static object registryInstance;
	private static object instance;
	
	public static bool IsLoaderActive()
	{
		return Plugin.BlueprinterLoaded;
	}

	public static bool IsPatchingComplete()
	{
		Setup();
		var completeField = loaderType?.GetProperty("PatchingComplete", BindingFlags.Public | BindingFlags.Instance);
		var complete = (bool)(completeField?.GetValue(instance) ?? false);

		return complete;
	}
	
	public static List<AssetBundle> GetExternalMapBundles()
	{
		List<AssetBundle> mapBundles = new List<AssetBundle>();
		Setup(); // Ensures registryType and registryInstance are assigned

		if (registryInstance == null) return mapBundles;

		// 1. Get the BundlesByName dictionary field
		var dictionaryField = registryType?.GetProperty("BundlesByName", BindingFlags.Public | BindingFlags.Instance);
		var dictionaryObj = dictionaryField?.GetValue(registryInstance) as IDictionary;

		if (dictionaryObj == null) return mapBundles;

		// 2. Iterate through the dictionary (IDictionary lets us access Keys and Values)
		foreach (DictionaryEntry entry in dictionaryObj)
		{
			string key = entry.Key as string;
        
			// Check if the bundle name contains "map_"
			if (key != null && key.Contains("map_"))
			{
				object loadedBundle = entry.Value;
				if (loadedBundle == null) continue;

				// 3. Extract the "AssetBundle" field from the LoadedBundle object
				var bundleField = loadedBundle.GetType().GetField("AssetBundle", BindingFlags.Public | BindingFlags.Instance);
				var assetBundle = bundleField?.GetValue(loadedBundle) as AssetBundle;

				if (assetBundle != null)
				{
					mapBundles.Add(assetBundle);
				}
			}
		}

		return mapBundles;
	}

	private static void Setup()
	{
		if (instance != null || checkComplete) return;
		
		if (Chainloader.PluginInfos.TryGetValue("com.nikkorap.blueprinter", out var pluginInfo))
		{
			instance = pluginInfo.Instance;
			if (instance != null)
			{
				loaderType = instance.GetType();
			}
		}
		
		var registryField = loaderType?.GetField("bundleRegistry", BindingFlags.NonPublic | BindingFlags.Instance);
		if (instance != null)
		{
			registryInstance = registryField?.GetValue(instance);
		}
		registryType = registryInstance?.GetType();
		checkComplete = true;
	}
}