using Assets.Scripts.PeroTools.Others;
using HarmonyLib;

namespace ArchipelagoMuseDash.Patches
{
    [HarmonyPatch(typeof(LoadingScene), "OnEnable")]
    public static class LoadingSceneOnEnablePatch
    {
        private static void Postfix()
        {
            ArchipelagoStatic.ArchLogger.LogDebug("Loading", "Started.");
            ArchipelagoStatic.LoadingSceneActive = true;
        }
    }
    [HarmonyPatch(typeof(LoadingScene), "Complete")]
    public static class LoadingSceneCompletePatch
    {
        private static void Postfix()
        {
            ArchipelagoStatic.ArchLogger.LogDebug("Loading", "Finished.");
            ArchipelagoStatic.LoadingSceneActive = false;
        }
    }
}