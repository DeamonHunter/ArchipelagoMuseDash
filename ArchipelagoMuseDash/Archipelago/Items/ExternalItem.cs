using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items {
    public class ExternalItem : IMuseDashItem {
        public NetworkItem Item { get; set; }

        public string UnlockSongUid => _songUid ?? ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid;
        public bool UseArchipelagoLogo => _songUid == null;

        public string TitleText => "Sending a New Item!!";
        public string SongText => _itemName;
        public string AuthorText => $"Sent to {_receivingPlayerName}.";

        public string PreUnlockBannerText => "A new item?";
        public string PostUnlockBannerText => GetPostUnlockBannerText();

        private readonly string _receivingPlayerName;
        private readonly string _itemName;
        private readonly string _songUid;

        public ExternalItem(string itemName, string receivingPlayer) {
            _itemName = itemName.Replace('_', ' ');
            _receivingPlayerName = receivingPlayer;

            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(itemName, out var info))
                _songUid = info.uid;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }

        private string GetPostUnlockBannerText() {
            if (_songUid != null)
                return "It ain't yours.";

            if ((Item.Flags & ItemFlags.Advancement) != 0)
                return "Looks good!";

            if ((Item.Flags & ItemFlags.NeverExclude) != 0)
                return "Hope it's useful!";

            if ((Item.Flags & ItemFlags.Trap) != 0)
                return "...";

            return "Looks useless.";
        }
    }
}