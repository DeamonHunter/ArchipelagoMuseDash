using Assets.Scripts.Database;

namespace ArchipelagoMuseDash.Archipelago.Items {
    public class SongItem : IMuseDashItem {
        public string UnlockSongUid => _song.uid;
        public bool UseArchipelagoLogo => false;

        public string TitleText => "New Song!!";
        public string SongText => null;
        public string AuthorText => null;

        public string PreUnlockBannerText => "A new song?";
        public string PostUnlockBannerText => GetUnlockedBannerText();

        readonly MusicInfo _song;
        bool _isDuplicate;

        public SongItem(MusicInfo song) {
            _song = song;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) {
            if (handler.UnlockedSongUids.Contains(_song.uid)) {
                _isDuplicate = true;
                return;
            }

            handler.UnlockedSongUids.Add(_song.uid);
            GlobalDataBase.dbMusicTag.RemoveHide(_song);

            //If the song hasn't been completed, add it to favourites.
            //Todo: Check against known locations in pool.
            if (!handler.CompletedSongUids.Contains(_song.uid))
                GlobalDataBase.dbMusicTag.AddCollection(_song);

            MusicTagManager.instance.RefreshStageDisplayMusics(-1);
            ArchipelagoStatic.SongSelectPanel?.RefreshMusicFSV();
        }

        private string GetUnlockedBannerText() {
            if (ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid == _song.uid)
                return "Its the Goal!";

            return _isDuplicate ? "Its a duplicate..." : null;
        }
    }
}
