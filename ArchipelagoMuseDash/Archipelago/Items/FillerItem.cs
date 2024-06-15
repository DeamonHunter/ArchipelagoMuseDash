using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoMuseDash.Archipelago.Items;

public class FillerItem : IMuseDashItem {
    public ItemInfo Item { get; set; }

    public string UnlockSongUid => ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid;
    public bool UseArchipelagoLogo => true;

    public string TitleText => "Getting a new item?";
    public string SongText => "...";
    public string AuthorText => "";

    public string PreUnlockBannerText => "A new item?";
    public string PostUnlockBannerText => Item.ItemId switch {
        2900030 => "Great to Perfect (5x)",
        2900031 => "Miss to Great (5x)",
        2900032 => "Extra Life",
        _ => "Unknown Filler"
    };

    public void UnlockItem(ItemHandler handler, bool immediate) { }
}