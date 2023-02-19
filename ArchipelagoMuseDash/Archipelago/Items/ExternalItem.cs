namespace ArchipelagoMuseDash.Archipelago.Items {
    public class ExternalItem : IMuseDashItem {
        public string UnlockSongUid => _songUID ?? ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid;

        public bool UseArchipelagoLogo => _songUID == null;

        public string TitleText => "New Item!!";
        public string SongText => _itemName;
        public string AuthorText => $"Sent to {_receivingPlayerName}.";

        public string PreUnlockBannerText => "A new item?";
        public string PostUnlockBannerText => _songUID != null ? "It ain't yours." : "Hope it's good!"; //Todo: See if we can get type info.

        readonly string _receivingPlayerName;
        readonly string _itemName;

        readonly string _songUID;

        public ExternalItem(string itemName, string receivingPlayer) {
            _itemName = itemName.Replace('_', ' ');
            _receivingPlayerName = receivingPlayer;

            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(itemName, out var info))
                _songUID = info.uid;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}
