using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppAssets.Scripts.UI.Panels;

namespace ArchipelagoMuseDash.Patches;

/// <summary>
///     Overrides the Play Song Button click so that we can enforce our own restrictions, and bypass the level restriction
/// </summary>
[HarmonyPatch(typeof(PnlStage), "OnBtnPlayClicked")]
sealed class PnlStageOnBtnPlayClickedPatch {
    private static bool Prefix(PnlStage __instance, out int __state) {
        __state = DataHelper.Level;
        //Don't override normal gameplay
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return true;

        try {
            ArchipelagoStatic.ArchLogger.LogDebug("PnlStage", "OnBtnPlayClicked");
            var musicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();
            if (musicInfo.uid == AlbumDatabase.RANDOM_PANEL_UID || ArchipelagoStatic.SessionHandler.ItemHandler.UnlockedSongUids.Contains(musicInfo.uid)) {
                //This bypasses level checks in order to allow players to play everything
                DataHelper.Level = 999;
                return true;
            }

            AudioManager.instance.PlayOneShot(__instance.clickSfx, DataHelper.sfxVolume);
            ShowText.ShowInfo("The song isn't unlocked yet. Try another one.");
            return false;
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlStageOnBtnPlayClickedPatch", e);
            return false;
        }
    }

    private static void Postfix(int __state) {
        //If we aren't logged in, don't bypass
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        //This should fix the level bypass
        DataHelper.Level = __state;
    }
}

[HarmonyPatch(typeof(SpecialSongUnLockDataModel), "CheckSpecialSongIsUnlocked")]
public static class UnlockPatches {
    private static bool Prefix(PnlStage __instance, out bool __result) {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn) {
            __result = false;
            return true;
        }

        __result = true;
        return false;
    }
}