using Assets.Scripts.UI.Controls;
using Assets.Scripts.UI.Panels;
using Assets.Scripts.UI.Specials;
using HarmonyLib;

namespace ArchipelagoMuseDash.Patches {
    /// <summary>
    /// Gets when overlays are disabled so we can trigger things at the correct time.
    /// </summary>
    [HarmonyPatch(typeof(EnableDisableHooker), "OnEnable")]
    sealed class EnableDisableHookerOnEnablePatch {
        static void Postfix(EnableDisableHooker __instance) {
            ArchipelagoStatic.ArchLogger.Log("EnableDisableHooker", $"OnEnable {__instance.gameObject.name}");
            ArchipelagoStatic.ActivatedEnableDisableHookers.Add(__instance.name);
        }
    }

    /// <summary>
    /// Gets when overlays are disabled so we can trigger things at the correct time.
    /// </summary>
    [HarmonyPatch(typeof(EnableDisableHooker), "OnDisable")]
    sealed class EnableDisableHookerOnDisablePatch {
        static void Postfix(EnableDisableHooker __instance) {
            ArchipelagoStatic.ArchLogger.Log("EnableDisableHooker", $"OnDisable {__instance.gameObject.name}");
            ArchipelagoStatic.ActivatedEnableDisableHookers.Remove(__instance.name);
        }
    }

    /// <summary>
    /// Gets the Muse Character on the main menu. This allows us to turn off the main menu.
    /// </summary>
    [HarmonyPatch(typeof(CharacterExpression), "Awake")]
    sealed class CharacterExpressionAwakePatch {
        static void Prefix(CharacterExpression __instance) {
            ArchipelagoStatic.ArchLogger.Log("CharacterExpression", "Awake");
            ArchipelagoStatic.MuseCharacter = __instance.gameObject;
        }
    }

    /// <summary>
    /// Gets the Unlock Song Panel so that we can trigger it when we want
    /// </summary>
    [HarmonyPatch(typeof(PnlStage), "Awake")]
    sealed class PnlStageAwakePatch {
        static void Postfix(PnlStage __instance) {
            ArchipelagoStatic.ArchLogger.Log("PnlStage", "Awake");
            ArchipelagoStatic.SongSelectPanel = __instance;
        }
    }

    /// <summary>
    /// Gets the Unlock Song Panel so that we can trigger it when we want
    /// </summary>
    [HarmonyPatch(typeof(PnlUnlock), "Awake")]
    sealed class PnlUnlockPatch {
        static void Postfix(PnlUnlock __instance) {
            ArchipelagoStatic.ArchLogger.Log("PnlUnlockStage", "Awake");
            ArchipelagoStatic.UnlockStagePanel = __instance.pnlUnlockStage;
        }
    }
}
