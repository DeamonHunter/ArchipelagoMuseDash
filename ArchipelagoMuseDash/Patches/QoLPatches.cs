using HarmonyLib;

namespace ArchipelagoMuseDash.Patches {
    /// <summary>
    /// Ensure the start tutorial cannot be started.
    /// </summary>
    [HarmonyPatch(typeof(GameMainPnlTutorial), "Awake")]
    sealed class GameMainPnlTutorialAwakePatch {
        static bool Prefix(GameMainPnlTutorial __instance) {
            __instance.gameObject.SetActive(false);
            __instance.nsTutorial.SetActive(false);
            __instance.mobileTutorial.SetActive(false);
            __instance.pcTutorial.SetActive(false);
            return false;
        }
    }
}
