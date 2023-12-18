using Assets.Scripts.Database;
using GameLogic;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;

namespace ArchipelagoMuseDash.Patches
{
    [HarmonyPatch(typeof(GameMusic), "LoadMusicDataByFileName")]
    sealed class GameMusicLoadMusicDataByFileNamePatch
    {
        private static void Postfix(GameMusic __instance)
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            ArchipelagoStatic.ArchLogger.LogDebug("GameMusic", "LoadMusicDataByFileName");
            ArchipelagoStatic.ArchLogger.LogDebug("GameMusic", $"Scene Started: {GlobalDataBase.dbBattleStage.m_BeganScene} : {GlobalDataBase.dbBattleStage.m_BeganSceneIdx}");

            ArchipelagoStatic.SessionHandler.BattleHandler.LoadMusicDataByFilenameHook();
        }
    }

    [HarmonyPatch(typeof(DBStageInfo), "SetRuntimeMusicData")]
    sealed class DBStageInfoSetRuntimeMusicData
    {
        private static void Postfix(List<MusicData> data)
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            ArchipelagoStatic.ArchLogger.Log("DBStageInfo", $"SetRuntimeMusicData {data.Count}");
            ArchipelagoStatic.SessionHandler.BattleHandler.SetRuntimeMusicDataHook(data);
        }
    }
    [HarmonyPatch(typeof(DBTouhou), "AwakeInit")]
    sealed class DBTouhouAwakeInitPatch
    {
        private static void Postfix()
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            //This is one of the earliest things that loads in a game scene. (That we care about that is.)

            ArchipelagoStatic.ArchLogger.LogDebug("DBTouhou", "Awake Trigger");
            ArchipelagoStatic.SessionHandler.BattleHandler.ActivateNextTrap();
            ArchipelagoStatic.SessionHandler.BattleHandler.PreGameSceneLoad();
        }
    }
}