using Archipelago.MultiClient.Net.Models;
using Il2Cpp;

namespace ArchipelagoMuseDash.Archipelago.Items;

public class CompressedItems : IMuseDashItem {

    private readonly List<IMuseDashItem> _items;

    public CompressedItems(List<IMuseDashItem> songs) {
        _items = new List<IMuseDashItem>(songs);
    }
    public ItemInfo Item { get; set; }

    public string UnlockSongUid => _items.Any(x => x is MusicSheetItem) ? ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid : GetFirstSong();
    public bool UseArchipelagoLogo => true;

    public string TitleText => "Too many items!!";
    public string SongText => $"You have received {_items.Count(x => x is SongItem)} songs";
    public string AuthorText => $"and {_items.Count(x => x is MusicSheetItem)} Music Sheets";

    public string PreUnlockBannerText => "Too many items!!";
    public string PostUnlockBannerText => $"You got {_items.Count} items!";

    public void UnlockItem(ItemHandler handler, bool immediate) {
        foreach (var item in _items)
            item.UnlockItem(handler, true);

        if (immediate)
            return;

        MusicTagManager.instance.RefreshDBDisplayMusics();
        if (ArchipelagoStatic.SongSelectPanel)
            ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
    }

    private string GetFirstSong() {
        foreach (var item in _items) {
            if (item is not SongItem songItem)
                continue;
            return songItem.UnlockSongUid;
        }
        return ArchipelagoStatic.AlbumDatabase.GetMusicInfo("Magical Wonderland").uid; //Fallback incase a player gets multiple fillers
    }
}