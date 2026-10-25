extern alias GSD2;

using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PKRuneSagePlus.Patches;

/// <summary>
/// Suikoden II: Sajah Village Rare Rune Shop Early Unlock Patch.
/// Unlocks the hidden rare item/rune vendor and counter interaction trigger inside
/// the Sajah Village item shop (vj17) before defeating the Beast Rune.
/// Bypasses flag 143 so players can purchase end-game Runes (Rage, Thunder, Flowing, Mother Earth, Cyclone).
/// </summary>
public static class SajahShopUnlockPatch
{
    public static string CurrentMapId = "";

    public static void Initialize(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(SajahShopUnlockPatch));
            Plugin.Log.LogInfo("[SajahShopUnlock] Registered Sajah Village Rare Shop unlock patch successfully.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SajahShopUnlock] Failed to register patch: {ex}");
        }
    }

    [HarmonyPatch(typeof(MACHICON), "OnMapInit")]
    [HarmonyPostfix]
    public static void OnMapInit_Postfix(MACHICON __instance)
    {
        if (__instance != null)
        {
            string raw = __instance.GetMapName();
            if (!string.IsNullOrEmpty(raw))
            {
                CurrentMapId = raw.Split(' ')[0];
            }
        }
    }

    [HarmonyPatch(typeof(MACHICON), "EventInit")]
    [HarmonyPostfix]
    public static void Postfix(MACHICON __instance, EVENTCON __result)
    {
        if (Plugin.Config != null && !Plugin.Config.UnlockSajahRareShopEarly.Value) return;
        if (__result == null || __result.mdat?.eventdata?.mapeventdat == null) return;
        
        string mapId = CurrentMapId;
        if (string.IsNullOrEmpty(mapId) && __instance != null)
        {
            string raw = __instance.GetMapName();
            if (!string.IsNullOrEmpty(raw))
            {
                mapId = raw.Split(' ')[0];
            }
        }

        // Match any submap of vj17 or the underlying vj17 asset itself (vj17.json)
        bool isVj17 = (!string.IsNullOrEmpty(mapId) && mapId.StartsWith("vj17", StringComparison.OrdinalIgnoreCase))
            || string.Equals(__result.mdat.name, "vj17", StringComparison.OrdinalIgnoreCase);

        if (!isVj17) return;
        if (__result.mdat.eventdata.mapeventdat.Count <= 1) return;

        var interior = __result.mdat.eventdata.mapeventdat[1];
        if (interior == null || interior.eventobj == null || interior.eventdat == null) return;

        bool alreadyUnlocked = interior.eventobj.Length > 3 && interior.eventobj[3] != null && interior.eventobj[3].disp == 1;

        // 1. Force vendor sprite visible
        if (interior.eventobj.Length > 3 && interior.eventobj[3] != null)
            interior.eventobj[3].disp = 1;

        // 2. Force counter interaction trigger active
        if (interior.eventobj.Length > 7 && interior.eventobj[7] != null)
            interior.eventobj[7].disp = 1;

        // Neutralize room initialization hide check (prevents re-hiding if flag 143 is off)
        if (interior.eventdat.Length > 2 && interior.eventdat[2] != null)
            interior.eventdat[2].eventForm = new byte[] { 0, 255, 254 };

        // 3. Bypass flag 143 (Beast Rune defeat) in talk scripts
        if (interior.eventdat.Length > 10 && interior.eventdat[10] != null)
            interior.eventdat[10].eventForm = new byte[] { 5, 11, 255, 254 };

        if (interior.eventdat.Length > 11 && interior.eventdat[11] != null)
            interior.eventdat[11].eventForm = new byte[] { 5, 15, 255, 254 };

        if (!alreadyUnlocked)
            Plugin.Log.LogInfo("[PKRuneSagePlus] Sajah Village rare item vendor unlocked early in vj17!");
    }
}

