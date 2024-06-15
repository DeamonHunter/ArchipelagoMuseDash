using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items;

public class VictoryItem : IMuseDashItem {

    private readonly string _playerName;

    public VictoryItem(string localPlayerName, string lastPlayedSongUid) {
        _playerName = localPlayerName;
        UnlockSongUid = lastPlayedSongUid;
    }
    public ItemInfo Item { get; set; }

    public string UnlockSongUid { get; }
    public bool UseArchipelagoLogo => true;

    public string TitleText => "You've Won!!";
    public string SongText => $"Congratulations {_playerName}!";
    public string AuthorText => "You've completed your goal.";

    public string PreUnlockBannerText => "It looks like...";
    public string PostUnlockBannerText => null;

    public void UnlockItem(ItemHandler handler, bool immediate) {
        ArchipelagoStatic.SessionHandler.ItemHandler.VictoryAchieved = true;
    }
}