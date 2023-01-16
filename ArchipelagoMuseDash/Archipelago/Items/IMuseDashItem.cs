namespace ArchipelagoMuseDash.Archipelago.Items {
    public interface IMuseDashItem {
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
