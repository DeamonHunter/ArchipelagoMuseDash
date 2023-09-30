using System.ComponentModel;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago;
using Assets.Scripts.Database;

namespace ArchipelagoMuseDash.Helpers
{
    public static class ArchipelagoHelpers
    {
        /// <summary>
        /// Chooses the next available song in the shown music list.
        /// - This should be run before any songs are hidden.
        /// </summary>
        public static void SelectNextAvailableSong()
        {
            var selectedInfo = GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo;
            if (selectedInfo == null)
                return;

            ArchipelagoStatic.ArchLogger.LogDebug("SelectNextAvailableSong", $"Attempting on {GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo?.name}");
            if (IsSongStillShown(selectedInfo.uid))
                return;

            var index = GlobalDataBase.dbMusicTag.m_StageShowMusicUids.IndexOf(selectedInfo.uid);
            if (index < 0)
            {
                ArchipelagoStatic.ArchLogger.LogDebug("SelectNextAvailableSong", "Song wasn't available. So giving up.");
                return;
            }

            var array = GlobalDataBase.dbMusicTag.m_StageShowMusicUids.ToArray();

            MusicInfo nextSong = null;
            for (int i = index + 1; i < array.Count; i++)
            {
                var songUid = array[i];
                if (!IsSongStillShown(songUid))
                    continue;
                nextSong = GlobalDataBase.dbMusicTag.m_AllMusicInfo[songUid];
                break;
            }

            if (nextSong == null)
            {
                for (int i = 0; i < index; i++)
                {
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

        private static bool IsSongStillShown(string uid)
        {
            if (uid == AlbumDatabase.RANDOM_PANEL_UID)
                return true;

            var itemHandler = ArchipelagoStatic.SessionHandler.ItemHandler;

            switch (itemHandler.HiddenSongMode)
            {
                case ShownSongMode.AllInLogic:
                    return itemHandler.SongsInLogic.Contains(uid);
                case ShownSongMode.Unlocks:
                    return itemHandler.SongsInLogic.Contains(uid) && itemHandler.UnlockedSongUids.Contains(uid);
                case ShownSongMode.Unplayed:
                    return itemHandler.SongsInLogic.Contains(uid) && itemHandler.UnlockedSongUids.Contains(uid) && !itemHandler.CompletedSongUids.Contains(uid);
                case ShownSongMode.Hinted:
                    return itemHandler.SongsInLogic.Contains(uid) && ArchipelagoStatic.SessionHandler.HintHandler.HasLocationHint(uid) && !itemHandler.CompletedSongUids.Contains(uid);
                default:
                    throw new InvalidEnumArgumentException(nameof(itemHandler.HiddenSongMode), (int)itemHandler.HiddenSongMode, typeof(ShownSongMode));
            }
        }

        public static bool IsItemDuplicate(NetworkItem itemA, NetworkItem itemB)
        {
            //If either item is given by the server (cheat or starting item) then never say its a duplicate
            if (itemA.Player == 0 && itemA.Location < 0)
                return false;
            if (itemB.Player == 0 && itemB.Location < 0)
                return false;

            return itemA.Item == itemB.Item && itemA.Location == itemB.Location;
        }
    }
}