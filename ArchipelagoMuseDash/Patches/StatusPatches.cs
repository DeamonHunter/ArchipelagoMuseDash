using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.GameCore.GameObjectLogics.GameObjectManager;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppAssets.Scripts.UI.Panels;
using Il2CppAssets.Scripts.UI.Specials;
using Il2CppFormulaBase;
using UnityEngine;

namespace ArchipelagoMuseDash.Patches;

/// <summary>
///     Gets when overlays are disabled so we can trigger things at the correct time.
/// </summary>
[HarmonyPatch(typeof(EnableDisableHooker), "OnEnable")]
sealed class EnableDisableHookerOnEnablePatch {
    private static void Postfix(EnableDisableHooker __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("EnableDisableHooker", $"OnEnable {__instance.gameObject.name}");
        ArchipelagoStatic.ActivatedEnableDisableHookers.Add(__instance.name);
    }
}
/// <summary>
///     Gets when overlays are disabled so we can trigger things at the correct time.
/// </summary>
[HarmonyPatch(typeof(EnableDisableHooker), "OnDisable")]
sealed class EnableDisableHookerOnDisablePatch {
    private static void Postfix(EnableDisableHooker __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("EnableDisableHooker", $"OnDisable {__instance.gameObject.name}");
        ArchipelagoStatic.ActivatedEnableDisableHookers.Remove(__instance.name);
    }
}
/// <summary>
///     Gets the Muse Character on the main menu. This allows us to turn off the main menu.
/// </summary>
[HarmonyPatch(typeof(CharacterExpression), "Awake")]
sealed class CharacterExpressionAwakePatch {
    private static void Prefix(CharacterExpression __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("CharacterExpression", "Awake");
        ArchipelagoStatic.MuseCharacter = __instance.gameObject;
    }
}
/// <summary>
///     Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(PnlStage), "Awake")]
sealed class PnlStageAwakePatch {
    private static void Postfix(PnlStage __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("PnlStage", "Awake");
        ArchipelagoStatic.SongSelectPanel = __instance;
    }
}
/// <summary>
/// Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(PnlPreparation), "Awake")]
sealed class PnlPreparationAwakePatch {
    private static void Postfix(PnlPreparation __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("PnlPreparation", "Awake");
        ArchipelagoStatic.PreparationPanel = __instance;
    }
}
/// <summary>
///     Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(PnlUnlock), "Awake")]
sealed class PnlUnlockPatch {
    private static void Postfix(PnlUnlock __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("PnlUnlockStage", "Awake");
        ArchipelagoStatic.UnlockStagePanel = __instance.pnlUnlockStage;
    }
}
/// <summary>
///     Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(StageBattleComponent), "GameStart")]
sealed class StageBattleComponentGameStartPatch {
    private static void Postfix(StageBattleComponent __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("StageBattleComponent", "GameStart");
        ArchipelagoStatic.BattleComponent = __instance;
    }
}
/// <summary>
///     Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(StageBattleComponent), "Dead")]
sealed class StageBattleComponentDeadPatch {
    private static void Postfix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.ArchLogger.LogDebug("StageBattleComponent", "Dead");
        ArchipelagoStatic.SessionHandler.DeathLinkHandler.PlayerDied();
        if (!ArchipelagoStatic.SessionHandler.DeathLinkHandler.HasDeathLinkReason()) {
            ArchipelagoStatic.SessionHandler.BattleHandler.SetTrapFinished();
            ArchipelagoStatic.SessionHandler.BattleHandler.OnBattleEnd(true, "");
        }
    }
}
/// <summary>
///     Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(AttackEffectManager), "InvokeElfinEffect")]
sealed class AttackEffectManagerInvokeElfinEffectPatch {
    private static bool Prefix(AttackEffectManager __instance, ref GameObject __result) {
        __result = null;
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return true;

        if (__instance.m_ElfinEffect?.pool != null && __instance.m_ElfinEffect.pool.sourcePrefab)
            return true;

        if (!ArchipelagoStatic.PlaceholderElfin)
            ArchipelagoStatic.PlaceholderElfin = new GameObject();
        __result = ArchipelagoStatic.PlaceholderElfin;
        return false;
    }
}