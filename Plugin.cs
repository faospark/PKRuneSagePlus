using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKRuneSagePlus.Patches;

namespace PKRuneSagePlus;

[BepInPlugin("faospark.pkrunesageplus", "PK Rune Sage Plus", "1.0.0")]
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

        Harmony harmony = new Harmony("faospark.pkrunesageplus");

        // Suikoden 1 Castle Rune Master (Jeane Shop Expansion)
        if (Config.EnableS1JeaneRuneShop.Value)
        {
            Log.LogInfo("Applying Suikoden 1 Jeane Rune Shop expansion patch...");
            S1JeaneRuneShopPatch.Initialize(harmony);
        }

        // Suikoden 2 Sajah Village Rare Shop Early Unlock
        if (Config.UnlockSajahRareShopEarly.Value)
        {
            Log.LogInfo("Applying Suikoden 2 Sajah Village Rare Shop Early Unlock patch...");
            SajahShopUnlockPatch.Initialize(harmony);
        }

        Log.LogInfo("PKRuneSagePlus loaded successfully!");
    }
}
