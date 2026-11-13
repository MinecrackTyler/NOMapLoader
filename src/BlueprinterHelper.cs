using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using Blueprinter;
using HarmonyLib;
using UnityEngine;

namespace NOMapLoader;

[HarmonyPatch]
public static class BlueprinterHelper
{
    private static bool initialized = false;
    private static bool patchingComplete = false;

    public static bool PatchingComplete
    {
        get
        {
            if (!initialized)
                Initialize();
            
            return patchingComplete;
        }
    }

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        Blueprinter.Api.StateChanged += StateChanged;
        
        initialized = true;
    }

    private static void StateChanged(Blueprinter.Api.State state)
    {
        if (state == Api.State.Ready) PatchingCompleted();
    }
        
    private static void PatchingCompleted()
    {
        if (patchingComplete)
            return;
        
        patchingComplete = true;
        Plugin.DebugLog("Blueprinter patching completed");
    }
}