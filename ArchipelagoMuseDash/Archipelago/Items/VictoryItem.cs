using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items {
    public class VictoryItem : IMuseDashItem {
        public NetworkItem Item { get; set; }

        public string UnlockSongUid => _lastPlayedSongUID;
        public bool UseArchipelagoLogo => true;

        public string TitleText => "You've Won!!";
        public string SongText => $"Congratulations {_playerName}!";
        public string AuthorText => "You've completed your goal.";

        public string PreUnlockBannerText => "It looks like...";
        public string PostUnlockBannerText => null;

        string _playerName;
        string _lastPlayedSongUID;

        public VictoryItem(string localPlayerName, string lastPlayedSongUID) {
            _playerName = localPlayerName;
            _lastPlayedSongUID = lastPlayedSongUID;
        }

        public void UnlockItem(ItemHandler handler, bool immediate) { }
    }
}
