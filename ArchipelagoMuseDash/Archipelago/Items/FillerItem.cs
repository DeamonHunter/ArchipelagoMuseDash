using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items
{
    public class FillerItem : IMuseDashItem
    {
        public NetworkItem Item { get; set; }

        public string UnlockSongUid => ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid;
        public bool UseArchipelagoLogo => true;

        public string TitleText => "Getting a new item?";
        public string SongText => "...";
        public string AuthorText => "";

        public string PreUnlockBannerText => "A new item?";
        public string PostUnlockBannerText
        {
            get
            {
                switch (Item.Item)
                {
                    case 2900030:
                        return "Great to Perfect (5x)";
                    case 2900031:
                        return "Miss to Great (5x)";
                    case 2900032:
                        return "Extra Life";
                    default:
                        return "Unknown Filler";
                }
            }
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}