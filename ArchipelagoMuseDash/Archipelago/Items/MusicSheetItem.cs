namespace ArchipelagoMuseDash.Archipelago.Items {
    public class MusicSheetItem : IMuseDashItem {
        public string UnlockSongUid => ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid;
        public bool UseArchipelagoLogo => false;

        public string TitleText => "Got a Music Sheet!!";

        public string SongText => _amountOfTokensLeft > 0 ? $"Only {_amountOfTokensLeft} left" : "Can't hurt to have spares!";

        public string AuthorText => _amountOfTokensLeft > 0 ? "To unlock the final song!" : "";

        public string PreUnlockBannerText => "A new song?";
        public string PostUnlockBannerText => $"Music Sheet No. {ArchipelagoStatic.SessionHandler.ItemHandler.NumberOfMusicSheetsToWin - _amountOfTokensLeft}";

        readonly int _amountOfTokensLeft;

        public MusicSheetItem(int amountLeft) {
            _amountOfTokensLeft = amountLeft;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}
