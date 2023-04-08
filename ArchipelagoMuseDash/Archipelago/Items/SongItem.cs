using Archipelago.MultiClient.Net.Models;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;

namespace ArchipelagoMuseDash.Archipelago.Items {
    public class SongItem : IMuseDashItem {
        public NetworkItem Item { get; set; }

        public string UnlockSongUid => _song.uid;
        public bool UseArchipelagoLogo => false;

        public string TitleText => "New Song!!";
        public string SongText => null;
        public string AuthorText => null;

        public string PreUnlockBannerText => "A new song?";
        public string PostUnlockBannerText => GetUnlockedBannerText();

        private readonly MusicInfo _song;
        private bool _isDuplicate;

        public SongItem(MusicInfo song) {
            _song = song;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) {
            ArchipelagoStatic.ArchLogger.LogDebug("Song Item", $"Unlocking item: {_song.uid}. Is duplicate: {handler.UnlockedSongUids.Contains(_song.uid)}");
            if (handler.UnlockedSongUids.Contains(_song.uid)) {
                _isDuplicate = true;
                return;
            }

            handler.UnlockSong(_song);
            if (immediate)
                return;

            MusicTagManager.instance.RefreshDBDisplayMusics();
            if (ArchipelagoStatic.SongSelectPanel)
                ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
        }

        private string GetUnlockedBannerText() {
            if (ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid == _song.uid)
                return "Its the Goal!";

            return _isDuplicate ? "Its a duplicate..." : null;
        }
    }
}