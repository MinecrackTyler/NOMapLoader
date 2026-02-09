using HarmonyLib;
using Mirage;

namespace NOMapLoader;

public class DebugPatches
{
	[HarmonyPatch(typeof(ClientObjectManager), "ThrowIfExists")]
	static class ClientObjectManagerPatch
	{
		static void Prefix(ClientObjectManager __instance, int prefabHash, Mirage.NetworkIdentity newPrefab)
		{
			Plugin.Log.LogInfo($"Details: {prefabHash} | {newPrefab?.name} - {newPrefab?.PrefabHash}");
		}
	}

	[HarmonyPatch(typeof(Unit), "Awake")]
	static class UnitAwakePatch
	{
		static void Prefix(Unit __instance)
		{
			Plugin.Log.LogInfo($"Details: {__instance.Identity} | {__instance.definition} | {__instance.MapUniqueName}");
		}
	}
}