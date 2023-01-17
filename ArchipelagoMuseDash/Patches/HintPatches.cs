using ArchipelagoMuseDash.Archipelago;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Tips;
using Harmony;

namespace ArchipelagoMuseDash.Patches {
    [HarmonyPatch(typeof(SongHideAskMsg), "AwakeInit")]
    sealed class SongHideAskMsgAwakeInitPatch {
        static void Postfix(SongHideAskMsg __instance) {
            ArchipelagoStatic.ArchLogger.Log("SongHideAskMsg", "AwakeInit");
            ArchipelagoStatic.HideSongDialogue = __instance;
        }
    }

    [HarmonyPatch(typeof(AbstractMessageBox), "OnYesClicked")]
    sealed class AbstractMessageBoxOnYesClickedPatch {
        static bool Prefix(AbstractMessageBox __instance) {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            ArchipelagoStatic.ArchLogger.Log("AbstractMessageBox", "OnYesClicked");
            if (__instance.m_Title.text != HintHandler.ArchipelagoDialogueTitle)
                return true;

            if (__instance.m_PlayClickAudio)
                AudioManager.instance.PlayOneShot(PnlTipsManager.s_YesClip, DataHelper.sfxVolume);

            ArchipelagoStatic.SessionHandler.HintHandler.HintSong(GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo);

            __instance.Close();
            return false;
        }
    }
}
