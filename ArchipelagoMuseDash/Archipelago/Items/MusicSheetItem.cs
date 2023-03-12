using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items {
    public class MusicSheetItem : IMuseDashItem {
        public NetworkItem Item { get; set; }

        public string UnlockSongUid => ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid;
        public bool UseArchipelagoLogo => false;

        public string TitleText => "Got a Music Sheet!!";

        public string SongText => TokensLeft() > 0 ? $"Only {TokensLeft()} left" : "Can't hurt to have spares!";

        public string AuthorText => TokensLeft() > 0 ? "To unlock the final song!" : "";

        public string PreUnlockBannerText => "A new song?";
        public string PostUnlockBannerText => $"Music Sheet No. {ArchipelagoStatic.SessionHandler.ItemHandler.NumberOfMusicSheetsToWin - (TokensLeft() + 1)}";

        float TokensLeft() => (ArchipelagoStatic.SessionHandler.ItemHandler.NumberOfMusicSheetsToWin - ArchipelagoStatic.SessionHandler.ItemHandler.CurrentNumberOfMusicSheets) - 1;

        public void UnlockItem(ItemHandler handler, bool immediate) {
            ArchipelagoStatic.SessionHandler.ItemHandler.AddMusicSheet();
        }
    }
}
