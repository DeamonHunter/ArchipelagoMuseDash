using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items {
    public interface IMuseDashItem {
        NetworkItem Item { get; set; }

        string UnlockSongUid { get; }
        bool UseArchipelagoLogo { get; }

        string TitleText { get; }
        string SongText { get; }
        string AuthorText { get; }

        string PreUnlockBannerText { get; }
        string PostUnlockBannerText { get; }

        void UnlockItem(ItemHandler handler, bool immediate);
    }
}
