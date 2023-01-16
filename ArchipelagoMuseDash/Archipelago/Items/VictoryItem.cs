namespace ArchipelagoMuseDash.Archipelago.Items {
    public class VictoryItem : IMuseDashItem {
        public string UnlockSongUid => ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland (More colorful mix)[Default Music]").uid;
        public bool UseArchipelagoLogo => true;

        public string TitleText => "You've Won!!";
        public string SongText => $"Congratulations {_playerName}!";
        public string AuthorText => "You've completed your goal.";

        public string PreUnlockBannerText => "It looks like...";
        public string PostUnlockBannerText => null;

        string _playerName;

        public VictoryItem(string localPlayerName) {
            _playerName = localPlayerName;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}
