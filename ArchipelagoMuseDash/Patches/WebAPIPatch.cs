//Todo: See what the best course of action for something like this is
//Taken from: https://github.com/gamrguy/CustomAlbums/blob/master/Melon/Patch/WebApiPatch.cs

using System;
using System.Collections.Generic;
using Account;
using Assets.Scripts.Database;
using HarmonyLib;
using MelonLoader;

namespace ArchipelagoMuseDash.Patches {
    /// <summary>
    /// Patches the API calls to block certain ones while playing archipelago
    /// </summary>
    static class WebAPIPatch {
        public static void DoPatching(HarmonyLib.Harmony harmony) {
            var targetMethod = AccessTools.Method(typeof(GameAccountSystem), "SendToUrl");
            var patchMethod = AccessTools.Method(typeof(WebAPIPatch), "SendToURLPatch");
            harmony.Patch(targetMethod, patchMethod.ToNewHarmonyMethod());
        }

        public static bool SendToURLPatch(string url, string method, Dictionary<string, object> datas) {
            ArchipelagoStatic.ArchLogger.Log("WebAPIPatch", $"url:{url} method:{method}");

            //If we aren't logged in to an archipelago. Work as normal.
            if (!ArchipelagoStatic.LoggedInToGame)
                return true;

            //Todo: Do we need to block other stuff?
            try {
                switch (url) {
                    case "statistics/pc-play-statistics-feedback":
                        ArchipelagoStatic.ArchLogger.Log("SendToURLPatch", "Blocked play feedback upload.");
                        return false;
                    case "musedash/v2/pcleaderboard/high-score":
                        ArchipelagoStatic.ArchLogger.Log("SendToURLPatch", "Blocked high score upload:" + GlobalDataBase.dbBattleStage.musicUid);
                        return false;
                    case "statistics/favorite_music":
                        ArchipelagoStatic.ArchLogger.Log("SendToURLPatch", "Blocked favourite upload.");
                        return false;
                }
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("SendToURLPatch", e);
                return false;
            }
            return true;
        }
    }
}
