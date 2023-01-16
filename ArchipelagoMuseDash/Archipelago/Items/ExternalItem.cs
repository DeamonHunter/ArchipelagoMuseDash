namespace ArchipelagoMuseDash.Archipelago.Items {
    public class ExternalItem : IMuseDashItem {
        public string UnlockSongUid => ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland (More colorful mix)[Default Music]").uid;
        public bool UseArchipelagoLogo => true;

        public string TitleText => "New Item!!";
        public string SongText => _itemName;
        public string AuthorText => $"Sent to {_receivingPlayerName}.";

        public string PreUnlockBannerText => "A new item?";
        public string PostUnlockBannerText => "Hope it's good!"; //Todo: See if we can get type info.

        readonly string _receivingPlayerName;
        readonly string _itemName;

        public ExternalItem(string itemName, string receivingPlayer) {
            _itemName = itemName.Replace('_', ' ');
            _receivingPlayerName = receivingPlayer;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}
