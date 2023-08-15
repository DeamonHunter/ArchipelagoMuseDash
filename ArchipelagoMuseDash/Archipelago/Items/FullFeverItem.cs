using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items
{
    public class FeverRefillItem : IMuseDashItem
    {
        public NetworkItem Item { get; set; }

        public string UnlockSongUid => ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid;
        public bool UseArchipelagoLogo => true;

        public string TitleText => "Getting a new item?";
        public string SongText => "...";
        public string AuthorText => "";

        public string PreUnlockBannerText => "A new item?";
        public string PostUnlockBannerText => "Fever Refill";

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}