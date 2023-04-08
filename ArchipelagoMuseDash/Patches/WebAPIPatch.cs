//Todo: See what the best course of action for something like this is
//Taken from: https://github.com/gamrguy/CustomAlbums/blob/master/Melon/Patch/WebApiPatch.cs

using HarmonyLib;
using Il2CppAccount;
using Il2CppAssets.Scripts.Database;

namespace ArchipelagoMuseDash.Patches;

/// <summary>
/// Patches the API calls to block certain ones while playing archipelago
/// </summary>
[HarmonyPatch(typeof(GameAccountSystem), "SendToUrl")]
static class WebAPIPatch {
    private static bool Prefix(string url, string method) {
        //If we aren't logged in to an archipelago. Work as normal.
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return true;

        //Todo: Do we need to block other stuff?
        try {
            switch (url) {
                case "statistics/pc-play-statistics-feedback":
                    ArchipelagoStatic.ArchLogger.LogDebug("SendToURLPatch", "Blocked play feedback upload.");
                    return false;
                case "musedash/v2/pcleaderboard/high-score":
                    ArchipelagoStatic.ArchLogger.LogDebug("SendToURLPatch", "Blocked high score upload:" + GlobalDataBase.dbBattleStage.musicUid);
                    return false;
                case "statistics/favorite_music":
                    ArchipelagoStatic.ArchLogger.LogDebug("SendToURLPatch", "Blocked favourite upload.");
                    return false;
            }
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("SendToURLPatch", e);
            return false;
        }

        ArchipelagoStatic.ArchLogger.LogDebug("SendToURLPatch", $"Allowed url:{url} method:{method}");
        return true;
    }
}