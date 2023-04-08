using ArchipelagoMuseDash.Archipelago.Traps;
using HarmonyLib;
using Il2CppAssets.Scripts.Database;
using Il2CppGameLogic;

namespace ArchipelagoMuseDash.Patches;

[HarmonyPatch(typeof(GameMusic), "LoadMusicDataByFileName")]
sealed class GameMusicLoadMusicDataByFileNamePatch {
    static void Postfix(GameMusic __instance) {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.ArchLogger.LogDebug("GameMusic", "LoadMusicDataByFileName");
        ArchipelagoStatic.ArchLogger.LogDebug("GameMusic", $"Scene Started: {GlobalDataBase.dbBattleStage.m_BeganScene} : {GlobalDataBase.dbBattleStage.m_BeganSceneIdx}");

        ArchipelagoStatic.SessionHandler.TrapHandler.LoadMusicDataByFilenameHook();
    }
}
/*
[HarmonyPatch(typeof(StageBattleComponent), "GetMusicDataFromStageInfo")]
sealed class StageBattleComponentGetMusicDataFromStageInfo {
    static void Postfix(ref Il2CppSystem.Collections.Generic.List<MusicData> __result) {
        ArchipelagoStatic.ArchLogger.Log("StageBattleComponent", $"GetMusicDataFromStageInfo Trigger");
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.ArchLogger.Log("StageBattleComponent", $"GetMusicDataFromStageInfo {__result.Count}");
        ArchipelagoStatic.SessionHandler.TrapHandler.GetMusicDataFromStageInfoHook(__result);
    }
}
*/
[HarmonyPatch(typeof(DBStageInfo), "SetRuntimeMusicData")]
sealed class DBStageInfoSetRuntimeMusicData {
    static void Postfix(Il2CppSystem.Collections.Generic.List<MusicData> data) {
        for (int i = 0; i < data.Count; i++) {
            var md = data._items[i];
            TrapHelper.OutputNote(md);
        }

        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.ArchLogger.Log("DBStageInfo", $"SetRuntimeMusicData {data.Count}");
        ArchipelagoStatic.SessionHandler.TrapHandler.SetRuntimeMusicDataHook(data);
        for (int i = 0; i < data.Count; i++) {
            var md = data._items[i];
            TrapHelper.OutputNote(md);
        }
    }
}
[HarmonyPatch(typeof(DBTouhou), "AwakeInit")]
sealed class DBTouhouAwakeInitPatch {
    static void Postfix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        //This is one of the earliest things that loads in a game scene. (That we care about that is.)

        ArchipelagoStatic.ArchLogger.LogDebug("DBTouhou", "Awake Trigger");
        ArchipelagoStatic.SessionHandler.TrapHandler.ActivateNextTrap();
        ArchipelagoStatic.SessionHandler.TrapHandler.PreGameSceneLoad();
    }
}