extern alias S1;

using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PKRuneSagePlus.Patches;

/// <summary>
/// Suikoden I: Remote Castle Rune Master (Jeane) Access Patch.
/// Enables pressing Right Stick Down (or [F7] on keyboard) to open the Toran Castle
/// Rune Master directly from anywhere on the field, immune to glitches and softlocks.
/// </summary>
public static class S1QuickRunePatch
{
    public static bool IsQuickRuneMasterOpen { get; private set; } = false;

    public static bool IsInBattle { get; internal set; } = false;
    public static S1::Village_c CurrentVillage { get; private set; }
    public static S1::Fmain_c CurrentField { get; private set; }

    private static S1::COMMAND_FUNC_TABLE _activeRuneDelegate = null;
    private static float _lastToggleTime = 0f;
    private static bool _wasRightStickDownPrev = false;

    /// <summary>
    /// Checks if AppRoot/Map/MapBackGround/ has at least one child, ensuring Tir is loaded on the area.
    /// </summary>
    public static bool HasMapBackgroundChild()
    {
        try
        {
            var mapBg = GameObject.Find("AppRoot/Map/MapBackGround");
            return mapBg != null && mapBg.transform != null && mapBg.transform.childCount > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the game is currently in a loading screen, scene change, or screen fade.
    /// </summary>
    public static bool IsGameInLoadingOrTransition(out string reason)
    {
        reason = null;

        try
        {
            // 1. Loading UI overlay
            var uiManager = global::UIManager.Instance;
            if (uiManager != null && uiManager.loading != null && uiManager.loading.gameObject.activeInHierarchy)
            {
                reason = "Loading screen is currently displayed";
                return true;
            }

            // 2. Active UILoadingAnim resource loading
            if (S1::UILoadingAnim.IsLoadingAnimeResources)
            {
                reason = "Loading animation resources active";
                return true;
            }

            // 3. Map background asset loading in village / field
            if (CurrentVillage != null && CurrentVillage.bg_manager != null && CurrentVillage.bg_manager.IsLoading)
            {
                reason = "Map background asset loading in progress";
                return true;
            }

            // 4. World map transition in progress (only valid when actively on world map)
            if (CurrentVillage == null && CurrentField != null)
            {
                if (CurrentField.loadVillage != 0 || CurrentField.field_exit_flg != 0)
                {
                    reason = "World map transition in progress";
                    return true;
                }
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Per-frame update called on the Unity main thread via Plugin.Update().
    /// </summary>
    public static void Update()
    {
        if (CurrentVillage == null && CurrentField == null)
            return;

        try
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneName) && !sceneName.Contains("GSD1", StringComparison.OrdinalIgnoreCase))
                return;
        }
        catch { }

        if (Plugin.Config == null || !Plugin.Config.EnableS1QuickRuneMaster.Value)
            return;

        // Check if Right Stick Down, R3 (Right Stick Press), or [F7] was pressed
        bool triggerPressed = false;
        try
        {
            var pad = Gamepad.current;
            if (pad != null)
            {
                // Right stick down detection (analog threshold + clean edge detection)
                float stickY = pad.rightStick.y.ReadValue();
                bool isStickDown = stickY < -0.65f;
                bool stickDownPressed = isStickDown && !_wasRightStickDownPrev;
                _wasRightStickDownPrev = isStickDown;

                if (stickDownPressed || pad.rightStickButton.wasPressedThisFrame)
                {
                    triggerPressed = true;
                }
            }
            else
            {
                _wasRightStickDownPrev = false;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.f7Key.wasPressedThisFrame)
            {
                triggerPressed = true;
            }
        }
        catch { }

        // If rune master is currently active, advance the native state machine
        if (IsQuickRuneMasterOpen)
        {
            // Allow pressing trigger again to close early
            if (triggerPressed && Time.time - _lastToggleTime > 0.4f)
            {
                _lastToggleTime = Time.time;
                CloseRuneMasterSafely();
                return;
            }

            StepRuneLoop();
            return;
        }

        // Attempt to open rune master on trigger press
        if (triggerPressed && Time.time - _lastToggleTime > 0.4f)
        {
            _lastToggleTime = Time.time;

            if (CanOpenRuneMaster(out string failReason))
            {
                OpenRuneMaster();
            }
            else
            {
                Plugin.Log.LogInfo($"[S1QuickRuneMaster] Cannot open Rune Master right now: {failReason}");
            }
        }
    }

    /// <summary>
    /// Comprehensive safety checks to guarantee immunity to softlocks, crashes, and narrative breaks.
    /// </summary>
    public static bool CanOpenRuneMaster(out string failReason)
    {
        failReason = null;

        // 0. Ensure map background has loaded Tir on the area
        if (!HasMapBackgroundChild())
        {
            failReason = "Map background has not loaded Tir on the area";
            return false;
        }

        // Rejection: Loading screen, scene change, or screen fade
        if (IsGameInLoadingOrTransition(out var loadingReason))
        {
            failReason = loadingReason;
            return false;
        }

        var gw = S1::OldSrcBase.game_work;
        if (gw == null)
        {
            failReason = "Game session not active";
            return false;
        }

        // Ensure castle data is present
        var shiro = gw.shiro_data;
        if (shiro == null)
        {
            shiro = new S1::SHIRO_DATA();
            gw.shiro_data = shiro;
        }

        // Jeane recruitment requirement
        if (Plugin.Config.S1QuickRuneMasterRequireJeane.Value)
        {
            if (shiro.level <= 0)
            {
                failReason = "Jeane is not yet recruited (Toran Castle rune master locked)";
                return false;
            }
        }

        // Rejection: In Battle
        if (IsInBattle)
        {
            failReason = "Currently in battle";
            return false;
        }

        // Rejection: In Village during an active cutscene, event dialogue, or transition
        if (CurrentVillage != null)
        {
            try
            {
                if (!CurrentVillage.isEnableOpenMenu)
                {
                    failReason = "Scene is currently busy with event, dialog, or cutscene";
                    return false;
                }
            }
            catch { }
        }

        // Rejection: WindowManager uninitialized
        if (S1::WindowManager.Instance == null)
        {
            failReason = "UI WindowManager is not available";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Opens the native Rune Master shop UI.
    /// </summary>
    public static void OpenRuneMaster()
    {
        try
        {
            Plugin.Log.LogInfo("[S1QuickRuneMaster] Invoking Castle Rune Master via Right Stick Down...");

            var omiseData = S1::Event_c.shiro_monsyou_data;
            if (omiseData == null)
            {
                omiseData = new S1::OMISE_DATA();
            }

            if (omiseData.syohin == null)
            {
                omiseData.syohin = new Il2CppStructArray<byte>(0);
            }

            if (omiseData.message == null)
            {
                omiseData.message = new Il2CppStructArray<int>(0);
            }

            if (S1::Event_c.shiro_monsyou_data == null)
            {
                S1::Event_c.shiro_monsyou_data = omiseData;
            }

            var initialDelegate = S1::h_monsyo_c.init_monsyo(omiseData);
            if (initialDelegate != null)
            {
                _activeRuneDelegate = initialDelegate;
                IsQuickRuneMasterOpen = true;
                PlaySoundOpen();
                Plugin.Log.LogInfo("[S1QuickRuneMaster] Rune Master opened successfully.");
            }
            else
            {
                Plugin.Log.LogWarning("[S1QuickRuneMaster] init_monsyo() returned null delegate.");
                IsQuickRuneMasterOpen = false;
                _activeRuneDelegate = null;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[S1QuickRuneMaster] Exception opening Rune Master: {ex}");
            IsQuickRuneMasterOpen = false;
            _activeRuneDelegate = null;
        }
    }

    /// <summary>
    /// Steps the native rune shop frame delegate each Unity update.
    /// </summary>
    private static void StepRuneLoop()
    {
        if (_activeRuneDelegate == null)
        {
            CloseRuneMasterSafely();
            return;
        }

        try
        {
            var nextDelegate = _activeRuneDelegate.Invoke();
            if (nextDelegate != null && S1::h_monsyo_c.monsyo_work != null)
            {
                _activeRuneDelegate = nextDelegate;
            }
            else
            {
                // Native rune routine finished cleanly (user chose Exit/Back)
                Plugin.Log.LogInfo("[S1QuickRuneMaster] Rune Master session completed cleanly.");
                PlaySoundClose();
                IsQuickRuneMasterOpen = false;
                _activeRuneDelegate = null;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[S1QuickRuneMaster] Exception in rune master step: {ex}");
            CloseRuneMasterSafely();
        }
    }

    /// <summary>
    /// Safely forces the Rune Master window to close if interrupted or toggled off with Right Stick Down.
    /// </summary>
    public static void CloseRuneMasterSafely()
    {
        if (IsQuickRuneMasterOpen)
        {
            PlaySoundClose();
        }

        try
        {
            S1::h_monsyo_c.monsyo_exit();
        }
        catch { }

        IsQuickRuneMasterOpen = false;
        _activeRuneDelegate = null;
        Plugin.Log.LogInfo("[S1QuickRuneMaster] Rune Master closed and player control restored.");
    }

    /// <summary>
    /// Plays the authentic Suikoden 1 menu open chime.
    /// </summary>
    public static void PlaySoundOpen()
    {
        try
        {
            int seId = S1::G_cmd_c.SE_OK;
            if (seId <= 0) seId = 1;
            S1::P_sound_c.Qsd_call(seId);
        }
        catch { }
    }

    /// <summary>
    /// Plays the authentic Suikoden 1 menu cancel/close chime.
    /// </summary>
    public static void PlaySoundClose()
    {
        try
        {
            int seId = S1::G_cmd_c.SE_CANCEL;
            if (seId <= 0) seId = 2;
            S1::P_sound_c.Qsd_call(seId);
        }
        catch { }
    }

    // =========================================================================
    // HARMONY HOOKS (Player Control, Movement & Encounter Guards)
    // =========================================================================

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.village_main))]
    [HarmonyPrefix]
    public static void Village_village_main_Prefix(S1::Village_c __instance)
    {
        CurrentVillage = __instance;
        CurrentField = null;
        IsInBattle = false;
    }

    [HarmonyPatch(typeof(S1::Fmain_c), nameof(S1::Fmain_c.fieldMain))]
    [HarmonyPrefix]
    public static void Fmain_fieldMain_Prefix(S1::Fmain_c __instance)
    {
        CurrentField = __instance;
        CurrentVillage = null;
        IsInBattle = false;
    }

    // --- Battle State Tracking ---

    [HarmonyPatch(typeof(S1::BattleChapter), nameof(S1::BattleChapter.Start))]
    [HarmonyPrefix]
    public static void BattleChapter_Start_Prefix()
    {
        IsInBattle = true;
    }

    [HarmonyPatch(typeof(S1::BattleChapter), nameof(S1::BattleChapter.BattleEnd))]
    [HarmonyPostfix]
    public static void BattleChapter_BattleEnd_Postfix()
    {
        IsInBattle = false;
    }

    [HarmonyPatch(typeof(S1::BattleChapter), nameof(S1::BattleChapter.OnDestroy))]
    [HarmonyPostfix]
    public static void BattleChapter_OnDestroy_Postfix()
    {
        IsInBattle = false;
    }

    // --- Movement Blockers (Towns, Dungeons, Castles) ---

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.NewPlayerMove))]
    [HarmonyPrefix]
    public static bool Village_NewPlayerMove_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.OldPlayerMove))]
    [HarmonyPrefix]
    public static bool Village_OldPlayerMove_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.playerMoveBase1))]
    [HarmonyPrefix]
    public static bool Village_playerMoveBase1_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.playerMoveBase2))]
    [HarmonyPrefix]
    public static bool Village_playerMoveBase2_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.playerMoveBase3))]
    [HarmonyPrefix]
    public static bool Village_playerMoveBase3_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.player_move))]
    [HarmonyPrefix]
    public static bool Village_player_move_Prefix(ref byte __result)
    {
        if (IsQuickRuneMasterOpen)
        {
            __result = 0;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.CheckArrivedEvent))]
    [HarmonyPrefix]
    public static bool Village_CheckArrivedEvent_Prefix(ref bool __result)
    {
        if (IsQuickRuneMasterOpen)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.playerOP_move))]
    [HarmonyPrefix]
    public static bool Village_playerOP_move_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    // --- Movement & Input Blockers (World Map) ---

    [HarmonyPatch(typeof(S1::Fmain_c), nameof(S1::Fmain_c.userMove))]
    [HarmonyPrefix]
    public static bool Fmain_userMove_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Fmain_c), nameof(S1::Fmain_c.userMoveSet))]
    [HarmonyPrefix]
    public static bool Fmain_userMoveSet_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    [HarmonyPatch(typeof(S1::Fmain_c), nameof(S1::Fmain_c.padReaction))]
    [HarmonyPrefix]
    public static bool Fmain_padReaction_Prefix()
    {
        return !IsQuickRuneMasterOpen;
    }

    // --- Encounter Lockouts ---

    [HarmonyPatch(typeof(S1::Village_c), nameof(S1::Village_c.v_wanderingMonsterChk))]
    [HarmonyPrefix]
    public static bool Village_MonsterChk_Prefix(ref int __result)
    {
        if (IsQuickRuneMasterOpen)
        {
            __result = 0;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(S1::Fmain_c), nameof(S1::Fmain_c.f_wanderingMonsterChk))]
    [HarmonyPrefix]
    public static bool Fmain_MonsterChk_Prefix(ref int __result)
    {
        if (IsQuickRuneMasterOpen)
        {
            __result = 0;
            return false;
        }
        return true;
    }
}
