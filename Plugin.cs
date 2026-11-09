using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKRuneSagePlus.Patches;
using UnityEngine;

namespace PKRuneSagePlus;

[BepInPlugin("faospark.pkrunesageplus", "PK Rune Sage Plus", "1.1.0")]
[BepInDependency("faospark.pkcore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public static Plugin Instance { get; private set; }
    public static new ManualLogSource Log;
    public static new ModConfiguration Config;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        Log.LogInfo("Loading PKRuneSagePlus (Suikoden I & II Rune Sage Enhancements)...");

        Config = new ModConfiguration(base.Config);
        Config.Init();

        // Register custom MonoBehaviour to drive the Unity Update loop
        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<PkRuneSagePlusMainLoop>();

            var obj = new GameObject("PkRuneSagePlusMainLoop");
            GameObject.DontDestroyOnLoad(obj);
            var component = obj.AddComponent<PkRuneSagePlusMainLoop>();
            obj.hideFlags = HideFlags.HideAndDontSave;

            PkRuneSagePlusMainLoop.Instance = component;
            Log.LogInfo("Initialized PkRuneSagePlusMainLoop for Update events.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to register PkRuneSagePlusMainLoop: {ex.Message}");
        }

        Harmony harmony = new Harmony("faospark.pkrunesageplus");

        // Suikoden 1 Castle Rune Master (Jeane Shop Expansion)
        if (Config.EnableS1JeaneRuneShop.Value)
        {
            Log.LogInfo("Applying Suikoden 1 Jeane Rune Shop expansion patch...");
            S1JeaneRuneShopPatch.Initialize(harmony);
        }

        // Suikoden 1 Remote Castle Rune Master (Quick Rune Access)
        if (Config.EnableS1QuickRuneMaster.Value)
        {
            Log.LogInfo("Applying Suikoden 1 Remote Castle Rune Master (Quick Rune) patch...");
            harmony.PatchAll(typeof(S1QuickRunePatch));
        }

        // Suikoden 2 Sajah Village Rare Shop Early Unlock
        if (Config.UnlockSajahRareShopEarly.Value)
        {
            Log.LogInfo("Applying Suikoden 2 Sajah Village Rare Shop Early Unlock patch...");
            SajahShopUnlockPatch.Initialize(harmony);
        }

        Log.LogInfo("PKRuneSagePlus loaded successfully!");
    }

    public void Update()
    {
        if (Config != null && Config.EnableS1QuickRuneMaster.Value)
        {
            S1QuickRunePatch.Update();
        }
    }
}

/// <summary>
/// Helper MonoBehaviour to drive the Update loop for PKRuneSagePlus.
/// </summary>
public class PkRuneSagePlusMainLoop : MonoBehaviour
{
    public static PkRuneSagePlusMainLoop Instance { get; set; }

    public void Update()
    {
        if (Plugin.Instance != null)
        {
            Plugin.Instance.Update();
        }
    }
}
