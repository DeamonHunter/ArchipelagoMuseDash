using ArchipelagoMuseDash.Archipelago;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Tips;
using HarmonyLib;

namespace ArchipelagoMuseDash.Patches
{
    [HarmonyPatch(typeof(SongHideAskMsg), "AwakeInit")]
    sealed class SongHideAskMsgAwakeInitPatch
    {
        private static void Postfix(SongHideAskMsg __instance)
        {
            ArchipelagoStatic.ArchLogger.LogDebug("SongHideAskMsg", "AwakeInit");
            ArchipelagoStatic.HideSongDialogue = __instance;
        }
    }

    [HarmonyPatch(typeof(AbstractMessageBox), "OnYesClicked")]
    sealed class AbstractMessageBoxOnYesClickedPatch
    {
        private static bool Prefix(AbstractMessageBox __instance)
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            ArchipelagoStatic.ArchLogger.LogDebug("AbstractMessageBox", "OnYesClicked");
            if (!__instance.m_Title || __instance.m_Title.text != HintHandler.ARCHIPELAGO_DIALOGUE_TITLE)
                return true;

            if (__instance.m_PlayClickAudio)
                AudioManager.instance.PlayOneShot(PnlTipsManager.s_YesClip, DataHelper.sfxVolume);

            ArchipelagoStatic.SessionHandler.HintHandler.HintSong(GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo);

            __instance.Close();
            return false;
        }
    }
}