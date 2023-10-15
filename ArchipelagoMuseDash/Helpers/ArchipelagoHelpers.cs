using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;

namespace ArchipelagoMuseDash.Helpers;

public static class ArchipelagoHelpers {
    /// <summary>
    /// Chooses the next available song in the shown music list.
    /// - This should be run before any songs are hidden.
    /// </summary>
    public static void SelectNextAvailableSong() {
        var selectedInfo = GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo;
        if (selectedInfo == null)
            return;

        ArchipelagoStatic.ArchLogger.LogDebug("SelectNextAvailableSong", $"Attempting on {GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo?.name}");
        if (IsSongStillShown(selectedInfo.uid))
            return;

        var index = GlobalDataBase.dbMusicTag.m_StageShowMusicUids.IndexOf(selectedInfo.uid);
        if (index < 0) {
            ArchipelagoStatic.ArchLogger.LogDebug("SelectNextAvailableSong", "Song wasn't available. So giving up.");
            return;
        }

        var array = GlobalDataBase.dbMusicTag.m_StageShowMusicUids.ToArray();

        MusicInfo nextSong = null;
        for (int i = index + 1; i < array.Count; i++) {
            var songUid = array[i];
            if (!IsSongStillShown(songUid))
                continue;
            nextSong = GlobalDataBase.dbMusicTag.m_AllMusicInfo[songUid];
            break;
        }

        if (nextSong == null) {
            for (int i = 0; i < index; i++) {
                var songUid = array[i];
                if (!IsSongStillShown(songUid))
                    continue;
                nextSong = GlobalDataBase.dbMusicTag.m_AllMusicInfo[songUid];
                break;
            }
        }

        if (nextSong != null)
            GlobalDataBase.dbMusicTag.SetSelectedMusic(nextSong);
    }

    private static bool IsSongStillShown(string uid) {
        if (uid == AlbumDatabase.RANDOM_PANEL_UID)
            return true;

        var itemHandler = ArchipelagoStatic.SessionHandler.ItemHandler;
        return itemHandler.HiddenSongMode switch {
            ShownSongMode.AllInLogic => itemHandler.SongsInLogic.Contains(uid),
            ShownSongMode.Unlocks => itemHandler.SongsInLogic.Contains(uid) && itemHandler.UnlockedSongUids.Contains(uid),
            ShownSongMode.Unplayed => itemHandler.SongsInLogic.Contains(uid) && itemHandler.UnlockedSongUids.Contains(uid) && !itemHandler.CompletedSongUids.Contains(uid),
            ShownSongMode.Hinted => itemHandler.SongsInLogic.Contains(uid) && ArchipelagoStatic.SessionHandler.HintHandler.HasLocationHint(uid) && !itemHandler.CompletedSongUids.Contains(uid),
            _ => true
        };
    }

    public static bool IsItemDuplicate(NetworkItem itemA, NetworkItem itemB) {
        //If either item is given by the server (cheat or starting item) then never say its a duplicate
        if (itemA is { Player: 0, Location: < 0 })
            return false;
        if (itemB is { Player: 0, Location: < 0 })
            return false;

        return itemA.Item == itemB.Item && itemA.Location == itemB.Location;
    }

    public static void SetBackToDefaultFilter() {
        ArchipelagoStatic.ArchLogger.Log("Helpers", $"Tag Index: {GlobalDataBase.dbMusicTag.selectedTagIndex}");
        GlobalDataBase.dbMusicTag.selectedTagIndex = 1; // 0 is favourite songs, 1 is all songs
        ArchipelagoStatic.SongSelectPanel.m_PnlMusicTag.SetTabIndex(0);
        ArchipelagoStatic.SongSelectPanel.m_PnlMusicTag.RefreshAllShownItem();
        MusicTagManager.instance.RefreshStageDisplayMusics();
    }
}