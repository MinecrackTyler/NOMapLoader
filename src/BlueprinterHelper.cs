using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace NOMapLoader;

public static class BlueprinterHelper
{
    private static bool initialized = false;
    private static Type registryType;
    private static object registryInstance;
    private static bool patchingComplete = false;

    public static bool PatchingComplete
    {
        get
        {
            if (!initialized)
                InitializeHarmonyHooks();
            
            return patchingComplete;
        }
    }

    public static List<AssetBundle> GetExternalMapBundles()
    {
        List<AssetBundle> mapBundles = new List<AssetBundle>();

        if (registryInstance == null) return mapBundles;

        var dictionaryField = registryType?.GetProperty("BundlesByName", BindingFlags.Public | BindingFlags.Instance);
        var dictionaryObj = dictionaryField?.GetValue(registryInstance) as IDictionary;

        if (dictionaryObj == null) return mapBundles;

        foreach (DictionaryEntry entry in dictionaryObj)
        {
            string key = entry.Key as string;

            if (key != null && key.Contains("map_"))
            {
                object loadedBundle = entry.Value;
                if (loadedBundle == null) continue;

                var bundleField = loadedBundle.GetType()
                    .GetField("AssetBundle", BindingFlags.Public | BindingFlags.Instance);
                var assetBundle = bundleField?.GetValue(loadedBundle) as AssetBundle;

                if (assetBundle != null)
                {
                    mapBundles.Add(assetBundle);
                }
            }
        }

        return mapBundles;
    }

    private static void InitializeHarmonyHooks()
    {
        if (initialized)
        {
            return;
        }

        if (!Chainloader.PluginInfos.TryGetValue("com.nikkorap.blueprinter", out var pluginInfo) || pluginInfo?.Instance == null)
        {
            Plugin.DebugLog("Blueprinter instance not available yet");
            return;
        }
        
        Assembly blueprinterAssembly = pluginInfo.Instance.GetType().Assembly;

        Harmony harmony = new Harmony("com.minec.nomaploader.blueprinterhelper");
        
        bool patchedAny = false;

        Type loadingScreenType = blueprinterAssembly.GetType("Blueprinter.BlueprinterLoadingScreen");

        if (loadingScreenType != null)
        {
            MethodInfo destroyInstanceMethod = loadingScreenType.GetMethod("DestroyInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (destroyInstanceMethod != null)
            {
                MethodInfo postfix = typeof(BlueprinterHelper).GetMethod(nameof(PatchingCompleted), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (postfix != null)
                {
                    harmony.Patch(destroyInstanceMethod, postfix: new HarmonyMethod(postfix));
                    patchedAny = true;
                    Plugin.DebugLog("Successfully patched DestroyInstance");
                }
            }
        }

        Type issuePopupType = blueprinterAssembly.GetType("Blueprinter.BlueprinterIssuePopup");

        if (issuePopupType != null)
        {
            MethodInfo showMethod = issuePopupType.GetMethod("Show", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (showMethod != null)
            {
                MethodInfo postfix = typeof(BlueprinterHelper).GetMethod(nameof(PatchingCompleted), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (postfix != null)
                {
                    harmony.Patch(showMethod, postfix: new HarmonyMethod(postfix));
                    patchedAny = true;
                    Plugin.DebugLog("Successfully patched Show");
                }
            }
        }
        
        var registryField = pluginInfo.Instance.GetType()?.GetField("bundleRegistry", BindingFlags.NonPublic | BindingFlags.Instance);
        if (pluginInfo.Instance != null)
        {
            registryInstance = registryField?.GetValue(pluginInfo.Instance);
        }
        registryType = registryInstance?.GetType();
        
        initialized = patchedAny;
    }
        
    private static void PatchingCompleted()
    {
        if (patchingComplete)
            return;
        
        patchingComplete = true;
        Plugin.DebugLog("Blueprinter patching completed");
    }
}