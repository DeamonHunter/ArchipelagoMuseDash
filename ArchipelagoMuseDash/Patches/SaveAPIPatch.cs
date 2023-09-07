using Account;
using Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Platforms.Steam;
using HarmonyLib;

namespace ArchipelagoMuseDash.Patches
{
    /// <summary>
    /// Removes the file removal part of this
    /// </summary>
    [HarmonyPatch(typeof(SteamSync), "RemoveFile")]
    static class SteamSyncRemoveFilePatch
    {
        //Todo: Patch this instead of completely removing it
        private static bool Prefix()
        {
            return false;
        }
    }

    /// <summary>
    /// Grabs the path in order to back it up.
    /// </summary>
    [HarmonyPatch(typeof(SteamSync), "LoadLocal")]
    static class SteamSyncLoadLocal
    {
        private static void Prefix(SteamSync __instance)
        {
            ArchipelagoStatic.SaveDataPath = __instance.m_FilePath;
        }
    }

    /// <summary>
    /// Blocks synchronising the save if the player is logged in while playing in AP mode.
    /// </summary>
    [HarmonyPatch(typeof(DataManager), "Save")]
    static class DataManagerSavePatch
    {
        //Todo: Patch this instead of completely removing it
        private static bool Prefix()
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            ArchipelagoStatic.ArchLogger.LogDebug("DataManager", "Interrupting save...");
            return false;
        }
    }


    /// <summary>
    /// Blocks synchronising the save if the player is logged in while playing in AP mode.
    /// </summary>
    [HarmonyPatch(typeof(GameAccountSystem), "Synchronize")]
    static class GameAccountSystemSynchronizePatch
    {
        //Todo: Patch this instead of completely removing it
        private static bool Prefix()
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            ArchipelagoStatic.ArchLogger.LogDebug("GameAccountSystem", "Interrupting save...");
            return false;
        }
    }
}