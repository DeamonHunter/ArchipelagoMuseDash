using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppAssets.Scripts.UI.Panels;
using Il2CppAssets.Scripts.UI.Specials;
using Il2CppFormulaBase;

namespace ArchipelagoMuseDash.Patches;

/// <summary>
/// Gets when overlays are disabled so we can trigger things at the correct time.
/// </summary>
[HarmonyPatch(typeof(EnableDisableHooker), "OnEnable")]
sealed class EnableDisableHookerOnEnablePatch {
    private static void Postfix(EnableDisableHooker __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("EnableDisableHooker", $"OnEnable {__instance.gameObject.name}");
        ArchipelagoStatic.ActivatedEnableDisableHookers.Add(__instance.name);
    }
}
/// <summary>
/// Gets when overlays are disabled so we can trigger things at the correct time.
/// </summary>
[HarmonyPatch(typeof(EnableDisableHooker), "OnDisable")]
sealed class EnableDisableHookerOnDisablePatch {
    private static void Postfix(EnableDisableHooker __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("EnableDisableHooker", $"OnDisable {__instance.gameObject.name}");
        ArchipelagoStatic.ActivatedEnableDisableHookers.Remove(__instance.name);
    }
}
/// <summary>
/// Gets the Muse Character on the main menu. This allows us to turn off the main menu.
/// </summary>
[HarmonyPatch(typeof(CharacterExpression), "Awake")]
sealed class CharacterExpressionAwakePatch {
    private static void Prefix(CharacterExpression __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("CharacterExpression", "Awake");
        ArchipelagoStatic.MuseCharacter = __instance.gameObject;
    }
}
/// <summary>
/// Gets the Unlock Song Panel so that we can trigger it when we want
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
[HarmonyPatch(typeof(PnlUnlock), "Awake")]
sealed class PnlUnlockPatch {
    private static void Postfix(PnlUnlock __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("PnlUnlockStage", "Awake");
        ArchipelagoStatic.UnlockStagePanel = __instance.pnlUnlockStage;
    }
}
/// <summary>
/// Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(StageBattleComponent), "GameStart")]
sealed class StageBattleComponentGameStartPatch {
    private static void Postfix(StageBattleComponent __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("StageBattleComponent", "GameStart");
        ArchipelagoStatic.BattleComponent = __instance;
    }
}
/// <summary>
/// Gets the Unlock Song Panel so that we can trigger it when we want
/// </summary>
[HarmonyPatch(typeof(StageBattleComponent), "Dead")]
sealed class StageBattleComponentDeadPatch {
    private static void Postfix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.ArchLogger.LogDebug("StageBattleComponent", "Dead");
        ArchipelagoStatic.SessionHandler.DeathLinkHandler.PlayerDied();
        ArchipelagoStatic.SessionHandler.TrapHandler.SetTrapFinished();
    }
}