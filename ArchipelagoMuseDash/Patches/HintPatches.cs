using ArchipelagoMuseDash.Archipelago;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.UI.Tips;

namespace ArchipelagoMuseDash.Patches;

[HarmonyPatch(typeof(SongHideAskMsg), "AwakeInit")]
sealed class SongHideAskMsgAwakeInitPatch {
    private static void Postfix(SongHideAskMsg __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("SongHideAskMsg", "AwakeInit");
        ArchipelagoStatic.HideSongDialogue = __instance;
    }
}
[HarmonyPatch(typeof(AbstractMessageBox), "OnYesClicked")]
sealed class AbstractMessageBoxOnYesClickedPatch {
    private static bool Prefix(AbstractMessageBox __instance) {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return true;

        try {
            ArchipelagoStatic.ArchLogger.LogDebug("AbstractMessageBox", "OnYesClicked");
            if (!__instance.m_Title || __instance.m_Title.text != HintHandler.ARCHIPELAGO_DIALOGUE_TITLE)
                return true;

            if (__instance.m_PlayClickAudio)
                AudioManager.instance.PlayOneShot(PnlTipsManager.s_YesClip, DataHelper.sfxVolume);

            ArchipelagoStatic.SessionHandler.HintHandler.HintSong(GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo);

            __instance.Close();
            return false; 
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("AbstractMessageBox", e);
            return false;
        }
    }
}