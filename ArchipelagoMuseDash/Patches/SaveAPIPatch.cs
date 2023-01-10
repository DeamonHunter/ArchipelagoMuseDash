using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Platforms.Steam;
using HarmonyLib;
using Il2CppSystem.IO;
using UnityEngine;

namespace ArchipelagoMuseDash.Patches {
    [HarmonyPatch(typeof(SteamSync), "LoadLocal")]
    internal static class SteamSyncLoadLocal {
        static void Prefix(SteamSync __instance) {
            if (ArchipelagoStatic.SteamSync == null) {
                ArchipelagoStatic.SteamSync = __instance;
                ArchipelagoStatic.OriginalFolderName = __instance.m_FolderPath;
                ArchipelagoStatic.OriginalFilePath = __instance.m_FilePath;

                var path = Path.Combine(Application.absoluteURL, "UserData/ArchSaves");
                __instance.m_FolderPath = path;
                __instance.m_FilePath = __instance.m_FolderPath + "/" + __instance.m_FileName;

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                //Copy over current data
                if (!File.Exists(__instance.m_FilePath) && File.Exists(ArchipelagoStatic.OriginalFilePath))
                    File.Copy(ArchipelagoStatic.OriginalFilePath, __instance.m_FilePath);
            }
        }

        static void Postfix() {
            if (DataHelper.isNew)
                DataHelper.isNew = false;
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
