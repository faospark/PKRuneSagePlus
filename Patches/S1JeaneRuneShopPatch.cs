extern alias S1;

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace PKRuneSagePlus.Patches;

/// <summary>
/// Suikoden I: Castle Rune Master (Jeane) Shop Expansion Patch.
/// Re-indexes Jeane's menu to: [0] Attach, [1] Remove, [2] Buy, [3] Exit.
/// Selecting "Buy" invokes the native item purchase flow (S1::h_omise_c),
/// selling Rune Crystals progressively unlocked from town rune shops visited across the world.
/// Seamlessly compatible with direct in-castle interaction and remote access.
/// </summary>
public static class S1JeaneRuneShopPatch
{
    private static short _origKauWinH = 0;
    private static bool _isSubShopActive = false;
    private static bool _hasEnteredOmiseKau = false;
    private static Il2CppStructArray<int> _cachedMenuMsgs = null;
    private static S1::OMISE_DATA _cachedMonsyoData = null;
    private static S1::COMMAND_FUNC_TABLE _activeSubShopDelegate = null;

    // Track towns visited in real-time
    private static readonly HashSet<string> VisitedTownCodes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Canonical mapping of Suikoden 1 Rune IDs to Sealed Orb Item IDs (fuuinkyu).
    /// Used as guaranteed fallback if native monsyo_fuuin_table is not yet initialized.
    /// </summary>
    private static readonly Dictionary<int, byte> CanonicalRuneToItemMap = new()
    {
        [2] = 11,   // Fire Orb (hi_fuuinkyu)
        [3] = 12,   // Water Orb (mizu_fuuinkyu)
        [4] = 13,   // Wind Orb (kaze_fuuinkyu)
        [5] = 14,   // Lightning Orb (kaminari_fuuinkyu)
        [6] = 15,   // Earth Orb (tuti_fuuinkyu - Level 1)
        [7] = 106,  // Resurrection Orb (hama_fuuinkyu)
        [8] = 77,   // Boar Orb (pan_fuuinkyu)
        [9] = 78,   // Shrike Orb (kas_fuuinkyu)
        [10] = 79,  // Falcon Orb (vre_fuuinkyu)
        [11] = 80,  // Blaze Orb (bel_fuuinkyu)
        [12] = 81,  // Gadget Orb (jup_fuuinkyu)
        [13] = 82,  // Clone Orb (wakemi_fuuinkyu)
        [14] = 83,  // Double-Jab Orb (kaeshiba_fuuinkyu)
        [15] = 84,  // Killer Orb (hissatsu_fuuinkyu)
        [16] = 85,  // Counter Orb (mikiri_fuuinkyu)
        [17] = 86,  // Spark Orb (shiden_fuuinkyu)
        [18] = 87,  // Haziness Orb (oboro_fuuinkyu)
        [19] = 88,  // Gale Orb (shippuu_fuuinkyu)
        [20] = 89,  // Sunbeam Orb (komorebi_fuuinkyu)
        [21] = 90,  // Speed Orb (shinkoho_fuuinkyu)
        [23] = 91,  // Fortune Orb (koun_fuuinkyu)
        [24] = 92,  // Prosperity Orb (kinun_fuuinkyu)
        [25] = 93,  // Champion's Orb (ouja_fuuinkyu)
        [26] = 94,  // Turtle Orb (kame_fuuinkyu)
        [27] = 95,  // Phero Orb (fuero_fuuinkyu)
        [28] = 107, // Rage Orb (rekka_fuuinkyu)
        [29] = 108, // Flowing Orb (ryuusui_fuuinkyu)
        [30] = 109, // Cyclone Orb (senpuu_fuuinkyu)
        [31] = 110, // Mother Earth Orb (daiti_fuuinkyu)
        [32] = 111  // Thunder Orb (raimei_fuuinkyu)
    };

    /// <summary>
    /// Town Rune Offerings mapping (Option A - Progressive Chandler style).
    /// Maps town area codes to canonical rune IDs:
    /// 2=Fire, 3=Water, 4=Wind, 5=Lightning, 6=Earth (Level 1), 15=Killer, 25=Champion.
    /// </summary>
    private static readonly Dictionary<string, int[]> TownRuneCatalog = new(StringComparer.OrdinalIgnoreCase)
    {
        // Gregminster (Imperial Capital - available from prologue): Water, Wind, Lightning
        ["va"] = new[] { 3, 4, 5 },
        ["gregminster"] = new[] { 3, 4, 5 },

        // Lenankamp: Fire, Water, Wind
        ["vc"] = new[] { 2, 3, 4 },
        ["lenankamp"] = new[] { 2, 3, 4 },

        // Kaku: Water
        ["vd2"] = new[] { 3 },
        ["kaku"] = new[] { 3 },

        // Kouan: Fire, Earth
        ["vd1"] = new[] { 2, 6 },
        ["kouan"] = new[] { 2, 6 },

        // Sarady: Water, Wind, Earth
        ["vb3"] = new[] { 3, 4, 6 },
        ["sarady"] = new[] { 3, 4, 6 },

        // Antei: Lightning, Wind
        ["vh"] = new[] { 5, 4 },
        ["antei"] = new[] { 5, 4 },

        // Rikon: Fire, Water
        ["vi"] = new[] { 2, 3 },
        ["rikon"] = new[] { 2, 3 },

        // Warriors' Village: Killer Rune, Wind
        ["vj"] = new[] { 15, 4 },
        ["warriors"] = new[] { 15, 4 },

        // Kirov: Fire, Earth
        ["vk"] = new[] { 2, 6 },
        ["kirov"] = new[] { 2, 6 },

        // Qlon Temple / Seek Valley: Champion
        ["vl"] = new[] { 25 },
        ["qlon"] = new[] { 25 }
    };

    public static void Initialize(Harmony harmony)
    {
        if (Plugin.Config == null || !Plugin.Config.EnableS1JeaneRuneShop.Value)
            return;

        try
        {
            Plugin.Log.LogInfo("[S1JeaneRuneShop] Initializing Castle Rune Master Shop expansion...");
            harmony.PatchAll(typeof(S1JeaneRuneShopPatch));
            Plugin.Log.LogInfo("[S1JeaneRuneShop] Initialized successfully.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[S1JeaneRuneShop] Failed to initialize: {ex}");
        }
    }

    /// <summary>
    /// Tracks town visits as Tir travels between maps.
    /// </summary>
    public static void OnMapLoaded(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
            return;

        string lower = mapName.ToLowerInvariant();
        VisitedTownCodes.Add(lower);

        // Extract 2-letter area prefix (e.g. "vc" from "vc1", "vd" from "vd1")
        if (lower.Length >= 2)
        {
            VisitedTownCodes.Add(lower.Substring(0, 2));
        }
    }

    /// <summary>
    /// Evaluates which Rune Crystals Tir has unlocked based on visited town rune shops and story progression.
    /// </summary>
    public static byte[] GetUnlockedRuneItemIds()
    {
        var unlockedRuneIds = new HashSet<int>();

        // 1. Starter / Base elemental catalog (always unlocked)
        unlockedRuneIds.Add(2); // Fire
        unlockedRuneIds.Add(3); // Water
        unlockedRuneIds.Add(4); // Wind
        unlockedRuneIds.Add(5); // Lightning
        unlockedRuneIds.Add(6); // Earth (Level 1)

        // 2. Check all visited towns
        foreach (var kvp in TownRuneCatalog)
        {
            if (VisitedTownCodes.Contains(kvp.Key))
            {
                foreach (int runeId in kvp.Value)
                    unlockedRuneIds.Add(runeId);
            }
        }

        // 3. Check save data point checks (all_point_data / point_chk)
        try
        {
            var pointData = S1::Event_c.all_point_data;
            if (pointData != null && pointData.Count > 0)
            {
                for (int i = 0; i < pointData.Count; i++)
                {
                    var pop = pointData[i];
                    if (pop != null && S1::Event_c.point_chk(pop))
                    {
                        // Pop area_no maps to visited area
                        // Area mappings in S1:
                        // 1=Gregminster, 2=Sarady, 3=Lenankamp, 4=Kouan/Kaku, 7=Antei, 8=Rikon, 9=Warriors, 10=Kirov, 11=Qlon
                        int area = pop.area_no;
                        if (area == 3) { unlockedRuneIds.Add(2); unlockedRuneIds.Add(3); unlockedRuneIds.Add(4); } // Lenankamp: Fire, Water, Wind
                        else if (area == 4) { unlockedRuneIds.Add(2); unlockedRuneIds.Add(6); } // Kouan: Fire, Earth
                        else if (area == 2) { unlockedRuneIds.Add(3); unlockedRuneIds.Add(4); unlockedRuneIds.Add(6); } // Sarady: Water, Wind, Earth
                        else if (area == 7) { unlockedRuneIds.Add(5); unlockedRuneIds.Add(4); } // Antei: Lightning, Wind
                        else if (area == 8) { unlockedRuneIds.Add(2); unlockedRuneIds.Add(3); } // Rikon: Fire, Water
                        else if (area == 9) { unlockedRuneIds.Add(15); unlockedRuneIds.Add(4); } // Warriors: Killer, Wind
                        else if (area == 10) { unlockedRuneIds.Add(2); unlockedRuneIds.Add(6); } // Kirov: Fire, Earth
                        else if (area == 11) { unlockedRuneIds.Add(25); } // Qlon: Champion
                    }
                }
            }
        }
        catch { }

        // 4. Toran Castle Level progression (unlocks higher rune tiers as castle expands)
        try
        {
            var gw = S1::OldSrcBase.game_work;
            if (gw != null && gw.shiro_data != null)
            {
                int castleLevel = gw.shiro_data.level;

                // Castle Level 1: Basic Elementals & Fortune/Prosperity
                if (castleLevel >= 1)
                {
                    unlockedRuneIds.Add(2);  // Fire
                    unlockedRuneIds.Add(3);  // Water
                    unlockedRuneIds.Add(4);  // Wind
                    unlockedRuneIds.Add(5);  // Lightning
                    unlockedRuneIds.Add(6);  // Earth (Level 1)
                    unlockedRuneIds.Add(23); // Fortune
                    unlockedRuneIds.Add(24); // Prosperity
                }

                // Castle Level 2: Advanced Combat & Utility
                if (castleLevel >= 2)
                {
                    unlockedRuneIds.Add(7);  // Resurrection
                    unlockedRuneIds.Add(13); // Clone
                    unlockedRuneIds.Add(14); // Double-Jab
                    unlockedRuneIds.Add(15); // Killer
                    unlockedRuneIds.Add(16); // Counter
                    unlockedRuneIds.Add(17); // Spark
                    unlockedRuneIds.Add(18); // Haziness
                    unlockedRuneIds.Add(19); // Gale
                    unlockedRuneIds.Add(27); // Phero (Pheromone)
                }

                // Castle Level 3: Advanced High-Tier Magic Runes
                if (castleLevel >= 3)
                {
                    unlockedRuneIds.Add(28); // Rage
                    unlockedRuneIds.Add(29); // Flowing
                    unlockedRuneIds.Add(30); // Cyclone
                    unlockedRuneIds.Add(31); // Mother Earth
                    unlockedRuneIds.Add(32); // Thunder
                }

                // Castle Level 4: Master Runes
                if (castleLevel >= 4)
                {
                    unlockedRuneIds.Add(20); // Sunbeam
                    unlockedRuneIds.Add(25); // Champion's
                    unlockedRuneIds.Add(26); // Turtle
                }
            }
        }
        catch { }

        // Convert Rune IDs to exact Item IDs via game's native GetMonsyoItemId with monsyo_fuuin_table lookup and canonical fallback
        var itemIds = new List<byte>();

        var fuuinMap = new Dictionary<int, int>();
        try
        {
            var table = S1::h_monsyo_c.monsyo_fuuin_table;
            if (table != null)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    var entry = table[i];
                    if (entry != null && entry.monsyo_id > 0 && entry.item_id > 0)
                    {
                        fuuinMap[entry.monsyo_id] = entry.item_id;
                    }
                }
            }
        }
        catch { }

        foreach (int runeId in unlockedRuneIds)
        {
            try
            {
                int itemId = 0;
                if (!fuuinMap.TryGetValue(runeId, out itemId) || itemId <= 0)
                {
                    itemId = S1::h_monsyo_c.GetMonsyoItemId(runeId);
                }

                if (itemId <= 0 && CanonicalRuneToItemMap.TryGetValue(runeId, out byte fallbackId))
                {
                    itemId = fallbackId;
                }

                if (itemId > 0 && itemId <= 255)
                {
                    byte bId = (byte)itemId;
                    if (!itemIds.Contains(bId))
                        itemIds.Add(bId);
                }
            }
            catch { }
        }

        // Sort items for a clean, consistent store presentation
        itemIds.Sort();
        return itemIds.ToArray();
    }

    // =========================================================================
    // HARMONY HOOKS
    // =========================================================================

    /// <summary>
    /// Configures the 4-choice menu layout and text strings before the windows are constructed.
    /// </summary>
    [HarmonyPatch(typeof(S1::h_monsyo_c), nameof(S1::h_monsyo_c.init_monsyo))]
    [HarmonyPrefix]
    public static void InitMonsyo_Prefix(S1::OMISE_DATA monsyo_data)
    {
        if (Plugin.Config == null || !Plugin.Config.EnableS1JeaneRuneShop.Value)
            return;

        try
        {
            _cachedMonsyoData = monsyo_data;
            if (monsyo_data != null)
            {
                Plugin.Log.LogInfo($"[S1JeaneRuneShop] Cached monsyo_data: syohin len={(monsyo_data.syohin != null ? monsyo_data.syohin.Length : 0)}, message len={(monsyo_data.message != null ? monsyo_data.message.Length : 0)}");
            }

            int kauWinIdx = S1::h_monsyo_c.KAU_WIN;

            // Expand KAU_WIN height from 3 to 4 in window_position table before create_window runs.
            // This ensures create_window instantiates all 4 TextMeshProUGUI row GameObjects cleanly.
            var winPos = S1::h_monsyo_c.window_position;
            if (winPos != null && kauWinIdx >= 0 && kauWinIdx < winPos.Count)
            {
                var kauPos = winPos[kauWinIdx];
                if (kauPos != null && kauPos.Count > 4)
                {
                    if (_origKauWinH == 0)
                        _origKauWinH = kauPos[4];
                    kauPos[4] = 4; // Exactly 4 rows: Attach, Remove, Buy, Exit
                }
            }

            // Build 4-choice message array: [0] Attach, [1] Remove, [2] Buy, [3] Exit
            var menuMsgs = new Il2CppStructArray<int>(4);
            menuMsgs[0] = S1::h_monsyo_c.kau_window_message0; // Attach
            menuMsgs[1] = S1::h_monsyo_c.kau_window_message1; // Remove
            menuMsgs[2] = S1::h_omise_c.kau_window_message0;  // Buy (from shop handler)
            menuMsgs[3] = S1::h_monsyo_c.kau_window_message2; // Exit
            _cachedMenuMsgs = menuMsgs;
            S1::h_monsyo_c.kau_window_message = menuMsgs;

            var winMsgs = S1::h_monsyo_c.window_message;
            if (winMsgs != null && kauWinIdx >= 0 && kauWinIdx < winMsgs.Count)
            {
                winMsgs[kauWinIdx] = menuMsgs;
            }

            Plugin.Log.LogInfo($"[S1JeaneRuneShop] Initialized 4-choice KAU_WIN: [0] Attach, [1] Remove, [2] Buy, [3] Exit");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[S1JeaneRuneShop] Error initializing 4-choice menu in init_monsyo: {ex}");
        }
    }

    /// <summary>
    /// Expands the native cursor navigation boundary from 3 to 4 items so native up/down scrolling
    /// handles all 4 menu options natively with full controller, analog stick, and keyboard support.
    /// </summary>
    [HarmonyPatch(typeof(S1::h_monsyo_c), nameof(S1::h_monsyo_c.window_cursor_updown))]
    [HarmonyPrefix]
    public static void WindowCursorUpdown_Prefix(ref int max)
    {
        if (Plugin.Config != null && Plugin.Config.EnableS1JeaneRuneShop.Value)
        {
            if (max == 3)
            {
                max = 4;
            }
        }
    }

    /// <summary>
    /// Safely redirects description lookups for the 4-choice menu so native code never crashes on index 3.
    /// Index 3 (Exit) redirects to vanilla 2 (Exit).
    /// </summary>
    [HarmonyPatch(typeof(S1::h_monsyo_c), nameof(S1::h_monsyo_c.remake_kau_window))]
    [HarmonyPrefix]
    public static bool RemakeKauWindow_Prefix(ref int command_no)
    {
        if (Plugin.Config == null || !Plugin.Config.EnableS1JeaneRuneShop.Value)
            return true;

        if (command_no == 3)
        {
            command_no = 2; // Map 3 (Exit) to vanilla 2 (Exit)
        }
        return true;
    }

    /// <summary>
    /// Intercepts input handling on the command selection window.
    /// Allows native monsyo_command to run for window animation and 4-way cursor navigation,
    /// intercepting confirmation on [2] Buy or [3] Exit.
    /// </summary>
    [HarmonyPatch(typeof(S1::h_monsyo_c), "monsyo_command")]
    [HarmonyPrefix]
    public static bool MonsyoCommand_Prefix(int d, ref int __result)
    {
        if (Plugin.Config == null || !Plugin.Config.EnableS1JeaneRuneShop.Value)
            return true;

        var work = S1::h_monsyo_c.monsyo_work;
        if (work == null)
            return true;

        // In Step 1, intercept user confirmation on Buy (2) or Exit (3)
        if (work.step == 1)
        {
            uint padR = S1::Pad.PadRRead();
            bool isConfirm = (padR & S1::Pad.P1_OK) != 0;
            bool isCancel = (padR & S1::Pad.P1_CANCEL) != 0;

            if (isConfirm)
            {
                if (work.index == 2)
                {
                    // [2] BUY (Rune Crystals Shop)
                    PlayDecideSound();
                    work.selected_command = 2;
                    LaunchJeaneRuneBuy(work);
                    __result = -1;
                    return false;
                }
                else if (work.index == 3)
                {
                    // [3] EXIT
                    PlayDecideSound();
                    work.selected_command = 3;
                    work.func = new System.Func<int, int>(S1::h_monsyo_c.monsyo_owari);
                    __result = -1;
                    return false;
                }
            }
            else if (isCancel)
            {
                PlayCancelSound();
                work.selected_command = 3;
                work.func = new System.Func<int, int>(S1::h_monsyo_c.monsyo_owari);
                __result = -1;
                return false;
            }
        }

        // Allow native monsyo_command to run:
        // Native code handles window animation (Step 0) and cursor up/down navigation across 4 items (Step 1).
        return true;
    }

    /// <summary>
    /// Launches the native Shop (S1::h_omise_c) Buy catalog populated with unlocked Rune Crystals.
    /// </summary>
    private static void LaunchJeaneRuneBuy(S1::h_monsyo_c.MONSYO_WORK work)
    {
        try
        {
            byte[] runeItemIds = GetUnlockedRuneItemIds();
            Plugin.Log.LogInfo($"[S1JeaneRuneShop] Tir opened Jeane's Rune Shop! Offering {runeItemIds.Length} unlocked crystal(s)...");

            var omiseData = new S1::OMISE_DATA();
            var syohinArray = new Il2CppStructArray<byte>(runeItemIds.Length);
            for (int i = 0; i < runeItemIds.Length; i++)
                syohinArray[i] = runeItemIds[i];

            omiseData.syohin = syohinArray;

            // Copy dialogue messages from cached monsyo_data or provide a safe 16-element fallback
            int msgCount = (_cachedMonsyoData?.message != null && _cachedMonsyoData.message.Length > 0)
                ? Math.Max(16, _cachedMonsyoData.message.Length)
                : 16;
            var msgArray = new Il2CppStructArray<int>(msgCount);
            if (_cachedMonsyoData?.message != null)
            {
                for (int i = 0; i < _cachedMonsyoData.message.Length; i++)
                    msgArray[i] = _cachedMonsyoData.message[i];
            }
            omiseData.message = msgArray;

            // Hide Jeane's main command window temporarily while in buy catalog
            SetWindowVisibility(work, S1::h_monsyo_c.KAU_WIN, false);

            // Initialize shop flow
            var shopDelegate = S1::h_omise_c.init_omise(omiseData);
            if (shopDelegate != null && S1::h_omise_c.omise_work != null)
            {
                _hasEnteredOmiseKau = false;
                _activeSubShopDelegate = shopDelegate;
                _isSubShopActive = true;

                // Step Buy catalog inside Jeane's state machine
                work.func = new System.Func<int, int>(StepSubShop);
            }
            else
            {
                // Fallback: restore command window
                SetWindowVisibility(work, S1::h_monsyo_c.KAU_WIN, true);
                work.func = new System.Func<int, int>(S1::h_monsyo_c.monsyo_command);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[S1JeaneRuneShop] Exception launching rune buy: {ex}");
            SetWindowVisibility(work, S1::h_monsyo_c.KAU_WIN, true);
            work.func = new System.Func<int, int>(S1::h_monsyo_c.monsyo_command);
        }
    }

    /// <summary>
    /// Stepper delegate that advances the shop buying flow each frame until user exits.
    /// </summary>
    private static int StepSubShop(int d)
    {
        if (!_isSubShopActive || _activeSubShopDelegate == null || S1::h_omise_c.omise_work == null)
        {
            ReturnToJeaneMenu();
            return -1;
        }

        try
        {
            var shopWork = S1::h_omise_c.omise_work;
            if (shopWork.func != null)
            {
                string funcName = shopWork.func.Method?.Name ?? "";

                // If currently executing omise_kau, flag that we have entered the buying catalog
                if (funcName.Contains("omise_kau"))
                {
                    _hasEnteredOmiseKau = true;
                }
                // If shop reached omise_command (the Buy/Sell/Exit menu)
                else if (funcName.Contains("omise_command"))
                {
                    if (_hasEnteredOmiseKau)
                    {
                        // User backed out of the buy item list to the shop menu -> return directly to Jeane
                        ReturnToJeaneMenu();
                        return -1;
                    }
                    else
                    {
                        // Direct bypass: automatically enter omise_kau (Buy)
                        shopWork.selected_command = 0;
                        shopWork.index = 0;
                        shopWork.step = 0;
                        shopWork.func = new System.Func<int, int>(S1::h_omise_c.omise_kau);
                    }
                }
                else if (funcName.Contains("omise_owari"))
                {
                    ReturnToJeaneMenu();
                    return -1;
                }
            }

            var nextDelegate = _activeSubShopDelegate.Invoke();
            if (nextDelegate == null || S1::h_omise_c.omise_work == null)
            {
                ReturnToJeaneMenu();
                return -1;
            }

            _activeSubShopDelegate = nextDelegate;
            return -1;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[S1JeaneRuneShop] Error in StepSubShop: {ex}");
            ReturnToJeaneMenu();
            return -1;
        }
    }

    /// <summary>
    /// Cleans up the buy catalog and returns smoothly to Jeane's main menu.
    /// </summary>
    private static void ReturnToJeaneMenu()
    {
        try
        {
            S1::h_omise_c.omise_exit();
        }
        catch { }

        _isSubShopActive = false;
        _hasEnteredOmiseKau = false;
        _activeSubShopDelegate = null;

        var work = S1::h_monsyo_c.monsyo_work;
        if (work != null)
        {
            int kauWinIdx = S1::h_monsyo_c.KAU_WIN;

            // Unhide Jeane's main command window
            SetWindowVisibility(work, kauWinIdx, true);

            // Re-render all 4 options into the window
            if (work.window != null && kauWinIdx >= 0 && kauWinIdx < work.window.Count)
            {
                var win = work.window[kauWinIdx];
                if (win != null)
                {
                    if (win.h < 4) win.h = 4;
                    if (_cachedMenuMsgs != null)
                    {
                        S1::h_monsyo_c.make_window(win, _cachedMenuMsgs);
                    }
                }
            }

            // Re-render selection box highlighting Buy (Index 2)
            work.step = 1;
            work.index = 2;
            work.selected_command = 2;
            S1::h_monsyo_c.remake_kau_window(2);

            // Restore command selection delegate
            work.func = new System.Func<int, int>(S1::h_monsyo_c.monsyo_command);
        }

        Plugin.Log.LogInfo("[S1JeaneRuneShop] Exited rune buy catalog. Returned to Jeane's main menu.");
    }

    /// <summary>
    /// Restores the original window dimensions when Jeane's session exits.
    /// </summary>
    [HarmonyPatch(typeof(S1::h_monsyo_c), nameof(S1::h_monsyo_c.monsyo_exit))]
    [HarmonyPostfix]
    public static void MonsyoExit_Postfix()
    {
        if (_origKauWinH > 0)
        {
            try
            {
                var winPos = S1::h_monsyo_c.window_position;
                int kauWinIdx = S1::h_monsyo_c.KAU_WIN;
                if (winPos != null && kauWinIdx >= 0 && kauWinIdx < winPos.Count)
                {
                    var kauPos = winPos[kauWinIdx];
                    if (kauPos != null && kauPos.Count > 4)
                    {
                        kauPos[4] = _origKauWinH;
                    }
                }
            }
            catch { }
        }

        try
        {
            if (_isSubShopActive)
            {
                S1::h_omise_c.omise_exit();
            }
        }
        catch { }

        _isSubShopActive = false;
        _hasEnteredOmiseKau = false;
        _activeSubShopDelegate = null;
    }

    /// <summary>
    /// Monitors village transitions in Suikoden 1 to automatically register visited town rune shops.
    /// </summary>
    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.village_main))]
    [HarmonyPrefix]
    public static void Village_village_main_Prefix(S1::Village_c __instance)
    {
        try
        {
            string vName = S1::Village_c.get_village_name();
            if (!string.IsNullOrEmpty(vName))
            {
                OnMapLoaded(vName);
            }
        }
        catch { }
    }

    // =========================================================================
    // HELPER UTILITIES
    // =========================================================================

    private static void SetWindowVisibility(S1::h_monsyo_c.MONSYO_WORK work, int windowIndex, bool visible)
    {
        if (work == null || work.window == null || windowIndex < 0 || windowIndex >= work.window.Count)
            return;

        try
        {
            var win = work.window[windowIndex];
            if (win != null)
            {
                win.disp_flg = (byte)(visible ? 1 : 0);
                if (win.ui != null && win.ui.gameObject != null)
                {
                    win.ui.gameObject.SetActive(visible);
                }
            }
        }
        catch { }
    }

    private static void PlayCursorSound()
    {
        try
        {
            int seId = S1::D_azukar_c.SE_MOVE;
            if (seId <= 0) seId = 0;
            S1::P_sound_c.Qsd_call(seId);
        }
        catch { }
    }

    private static void PlayDecideSound()
    {
        try
        {
            int seId = S1::G_cmd_c.SE_OK;
            if (seId <= 0) seId = 1;
            S1::P_sound_c.Qsd_call(seId);
        }
        catch { }
    }

    private static void PlayCancelSound()
    {
        try
        {
            int seId = S1::G_cmd_c.SE_CANCEL;
            if (seId <= 0) seId = 2;
            S1::P_sound_c.Qsd_call(seId);
        }
        catch { }
    }
}
