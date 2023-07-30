using HarmonyLib;
using Il2CppAccount;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Platforms.Steam;

namespace ArchipelagoMuseDash.Patches;

[HarmonyPatch(typeof(SteamSync), "LoadLocal")]
static class SteamSyncLoadLocal {
    private static void Prefix(SteamSync __instance) {
        if (ArchipelagoStatic.SteamSync != null)
            return;

        ArchipelagoStatic.SteamSync = __instance;
        ArchipelagoStatic.OriginalFolderName = __instance.m_FolderPath;
        ArchipelagoStatic.OriginalFilePath = __instance.m_FilePath;
    }
}
/// <summary>
/// Removes the file removal part of this function if we are dealing with our own saves
/// </summary>
[HarmonyPatch(typeof(SteamSync), "SaveLocal")]
static class SteamSyncSaveLocalPatch {
    private static bool Prefix(SteamSync __instance) {
        if (ArchipelagoStatic.OriginalFolderName == __instance.m_FolderPath)
            return true;

        DataManager.instance["GameConfig"].Save();
        byte[] bytes = DataManager.instance.ToBytes();
        if (!Directory.Exists(__instance.m_FolderPath))
            Directory.CreateDirectory(__instance.m_FolderPath);
        File.WriteAllBytes(__instance.m_FilePath, bytes);
        return false;
    }
}
/// <summary>
/// Removes the file removal part of this
/// </summary>
[HarmonyPatch(typeof(SteamSync), "RemoveFile")]
static class SteamSyncRemoveFilePatch {
    //Todo: Patch this instead of completely removing it
    private static bool Prefix() {
        return false;
    }
}
/// <summary>
/// Blocks synchronising the save if the player is logged in while playing in AP mode.
/// </summary>
[HarmonyPatch(typeof(GameAccountSystem), "Synchronize")]
static class GameAccountSystemSynchronizePatch {
    //Todo: Patch this instead of completely removing it
    private static bool Prefix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return true;

        ArchipelagoStatic.ArchLogger.LogDebug("GameAccountSystem", "Interrupting save...");
        return false;
    }
}