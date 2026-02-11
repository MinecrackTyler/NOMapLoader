using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using NuclearOption.SceneLoading;
using Unity.Loading;
using UnityEngine;

namespace NOMapLoader
{

    public class CustomMapData
    {
        public MapDetails Details;
        public MapSettings Settings;
        public GameObject Prefab;
    }
    
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.nikkorap.blueprinter", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Dictionary<string, CustomMapData> CustomMaps = new Dictionary<string, CustomMapData>();
        internal static string[] PreloadedMaps = ["Terrain_naval", "Terrain1"];
        internal static bool BlueprinterLoaded;
        
        private async void Awake()
        {
            Log = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            if (Chainloader.PluginInfos.ContainsKey("com.nikkorap.blueprinter"))
            {
                Log.LogInfo("Blueprinter detected, scanning for external bundles!");
                BlueprinterLoaded = true;
            }
            else
            {
                BlueprinterLoaded = false;
            }
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            
            await BuildMapTable();

            if (BlueprinterLoaded)
            {
                MapLoader mapLoader = Resources.Load<MapLoader>("MapLoader");
                if (mapLoader == null)
                {
                    Plugin.Log.LogError("No MapLoader found");
                    return;
                }
                foreach (var mapName in CustomMaps.Keys)
                {
                    CustomMapData customMapData = Plugin.CustomMaps[mapName];
                    mapLoader.Maps = mapLoader.Maps.AddToArray(customMapData.Details);
                }
                Log.LogInfo($"Injected {CustomMaps.Count} custom maps into MapLoader");
            }
            
            harmony.PatchAll();
        }

        private async Task<List<AssetBundle>> ScanExternalBundles()
        {
            List<AssetBundle> list = new List<AssetBundle>();
            if (!BlueprinterLoaded) return list;
            
            Log.LogInfo("Awaiting blueprinter patching!");
            while (!BlueprinterHelper.IsPatchingComplete())
            {
                await Task.Delay(10);
            }

            foreach (AssetBundle bundle in BlueprinterHelper.GetExternalMapBundles())
            {
                Log.LogInfo("Detected external bundle: " + bundle.name);
                list.Add(bundle);
            }

            return list;

        }

        private async Task BuildMapTable()
        {
            string pluginDir = Path.GetDirectoryName(Info.Location);

            if (pluginDir == null)
            {
                Log.LogError("Plugin Directory is null!");
                return;
            }
            
            var mapBundles = Directory.GetFiles(pluginDir, "*")
                .Where(f => f.Contains("map_")).ToList();
            
            Log.LogInfo("Detected local Bundles:");
            foreach (string s in mapBundles)
            {
                Log.LogInfo(Path.GetFileName(s));
            }

            var excludeBundles = new List<string>();
            if (BlueprinterLoaded)
            {
                excludeBundles = mapBundles.Where(f => Path.GetExtension(f).Contains("nobp")).ToList();
            }
            
            
            var externalBundles = await ScanExternalBundles();
            var localBundles = mapBundles.Except(excludeBundles).ToList();
            
            Log.LogInfo($"Found {localBundles.Count + externalBundles.Count} map bundles");
            Log.LogInfo($"Details: {externalBundles.Count} external bundles");
            Log.LogInfo($"Details: {localBundles.Count} local bundles");

            foreach (var detail in Resources.FindObjectsOfTypeAll<MapDetails>()) //load preloaded map details from blueprinter
            {
                if (PreloadedMaps.Contains(detail.PrefabName)) continue;
                var mapPrefab = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(x => x.name == detail.PrefabName);
                
                if (mapPrefab == null)
                {
                    Log.LogError($"Failed to load map prefab: {detail.PrefabName}");
                    continue;
                }
                MapSettings settings = mapPrefab.GetComponent<MapSettings>();
                if (settings == null)
                {
                    Log.LogError($"Failed to load map settings: {detail.PrefabName}");
                    continue;
                }
                
                CustomMapData mapData = new CustomMapData
                {
                    Details = detail,
                    Settings = settings,
                    Prefab = mapPrefab
                };
                    
                CustomMaps[detail.PrefabName] = mapData;
            }
            
            
            foreach (var bundleFile in localBundles)
            {
                var fileName = Path.GetFileName(bundleFile);
                Log.LogInfo($"Attempting to load map bundle: {fileName}");
                
                AssetBundle bundle = AssetBundle.LoadFromFile(bundleFile);
                if (bundle == null)
                {
                    Log.LogError($"Failed to load map bundle: {fileName}");
                    continue;
                }
                
                var mapDetails = bundle.LoadAllAssets<MapDetails>().ToList();

                foreach (var mapDetail in mapDetails)
                {
                    GameObject mapPrefab = bundle.LoadAsset<GameObject>(mapDetail.PrefabName);

                    if (mapPrefab == null)
                    {
                        Log.LogError($"Failed to load map prefab: {mapDetail.PrefabName}");
                        continue;
                    }
                    MapSettings settings = mapPrefab.GetComponent<MapSettings>();
                    if (settings == null)
                    {
                        Log.LogError($"Failed to load map settings: {mapDetail.PrefabName}");
                        continue;
                    }
                    
                    CustomMapData mapData = new CustomMapData
                    {
                        Details = mapDetail,
                        Settings = settings,
                        Prefab = mapPrefab
                    };
                    
                    CustomMaps[mapDetail.PrefabName] = mapData;
                }
                bundle.Unload(false);
            }

            
        }
    }
}
