using Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Platforms.Steam;
using HarmonyLib;
using Il2CppSystem.IO;

namespace ArchipelagoMuseDash.Patches {
    [HarmonyPatch(typeof(SteamSync), "LoadLocal")]
    internal static class SteamSyncLoadLocal {
        static void Prefix(SteamSync __instance) {
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
    internal static class SteamSyncSaveLocalPatch {
        static bool Prefix(SteamSync __instance) {
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
    internal static class SteamSyncRemoveFilePatch {
        //Todo: Patch this instead of completely removing it
        static bool Prefix(SteamSync __instance) {
            return false;
        }
    }
}
