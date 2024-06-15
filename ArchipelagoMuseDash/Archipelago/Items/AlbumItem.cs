using Archipelago.MultiClient.Net.Models;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;

namespace ArchipelagoMuseDash.Archipelago.Items;

public class AlbumItem : IMuseDashItem {

    private readonly List<MusicInfo> _songList;
    private readonly MusicInfo _firstSong;
    //bool _isDuplicate; //Todo: Support finding duplicates

    public AlbumItem(string albumName, List<MusicInfo> songList) {
        if (songList.Count <= 0)
            throw new ArgumentException(@"Cannot have an empty album list.", nameof(songList));

        SongText = albumName;
        _songList = songList;

        foreach (var song in _songList) {
            _firstSong = song;
            break;
        }
    }
    public ItemInfo Item { get; set; }

    public string UnlockSongUid => _firstSong.uid;
    public bool UseArchipelagoLogo => false;

    public string TitleText => "New Album!!";
    public string SongText { get; }
    public string AuthorText => "";

    public string PreUnlockBannerText => "A new song?";
    public string PostUnlockBannerText => null;

    public void UnlockItem(ItemHandler handler, bool immediate) {
        foreach (var song in _songList) {
            if (handler.UnlockedSongUids.Contains(song.uid))
                return;

            handler.UnlockedSongUids.Add(song.uid);
            GlobalDataBase.dbMusicTag.RemoveHide(song);

            //If the song hasn't been completed, add it to favourites.
            //Todo: Check against known locations in pool.
            if (!handler.CompletedSongUids.Contains(song.uid))
                GlobalDataBase.dbMusicTag.AddCollection(song);
        }

        MusicTagManager.instance.RefreshDBDisplayMusics();
        if (ArchipelagoStatic.SongSelectPanel)
            ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
    }
}