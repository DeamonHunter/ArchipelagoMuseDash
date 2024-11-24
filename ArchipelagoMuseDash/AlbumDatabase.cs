using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Managers;

namespace ArchipelagoMuseDash;

/// <summary>
///     Contains information about the game's songs, in a way that is easier to access for us.
/// </summary>
public class AlbumDatabase {

    public const int CHINESE_LOC_INDEX = 0;
    public const int ENGLISH_LOC_INDEX = 1;
    public const string RANDOM_PANEL_UID = "?";
    private const int starting_music_item_id = 2900000 + 50; //Start ID + Music ID Offset

    private static readonly Dictionary<string, string> _currentNamesToOldNames = new() {
        { "Crimson Nightingale", "Crimson Nightingle" },
        { "Tsukuyomi Ni Naru", "Territory Battles" }
    };

    public static readonly Dictionary<string, string> UidOverrides = new() {
        { "74-2", "74-6" }
    };

    private Dictionary<string, MusicInfo> _songsByItemName = new();
    private Dictionary<string, List<MusicInfo>> _songsByAlbum = new();

    private Dictionary<string, MusicInfo> _songsByUid = new();
    private readonly Dictionary<long, string> _songIDToUid = new();
    private readonly Dictionary<long, string> _albumIDToAlbumString = new(); //Not Used yet
    
#if DEBUG
    //For file writing purposes though may be helpful elsewhere
    public readonly Dictionary<string, long> SongUidToId = new();
#endif

    public void Setup() {
        _songsByAlbum.Clear();
        _songsByItemName.Clear();

        var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
        GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

        _songsByAlbum = new Dictionary<string, List<MusicInfo>>();
        _songsByItemName = new Dictionary<string, MusicInfo>();
        _songsByUid = new Dictionary<string, MusicInfo>();

        var configManager = ConfigManager.instance;
        if (configManager == null)
            throw new Exception("Config Manage was null when trying to load songs.");

        var albumConfig = configManager.GetConfigObject<DBConfigAlbums>();
        var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>().GetLocal(ENGLISH_LOC_INDEX);

        foreach (var musicInfo in list) {
            if (musicInfo.uid == RANDOM_PANEL_UID || musicInfo.uid.StartsWith("999"))
                continue;

            var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);

            var songName = GetItemNameFromMusicInfo(musicInfo);
            if (!_songsByItemName.TryAdd(songName, musicInfo)) {
                ArchipelagoStatic.ArchLogger.Warning("[Album Database]", $"Duplicate Song Name found. Id: {musicInfo.uid}");
                continue;
            }

            _songsByUid.Add(musicInfo.uid, musicInfo);

            if (_currentNamesToOldNames.TryGetValue(songName, out var oldName))
                _songsByItemName.Add(oldName, musicInfo);

            if (!_songsByAlbum.TryGetValue(albumLocal, out var albumList)) {
                albumList = new List<MusicInfo>();
                _songsByAlbum.Add(albumLocal, albumList);
            }

            albumList.Add(musicInfo);
        }
    }

    public void LoadMusicList(Stream dataTextStream) {
        //This section is to help improve compatibility between versions
        var itemID = starting_music_item_id;
        using var sr = new StreamReader(dataTextStream);

        var knownItems = new HashSet<string>();

        while (!sr.EndOfStream) {
            var line = sr.ReadLine();
            if (string.IsNullOrEmpty(line))
                continue;

            var sections = line.Split('|');
            if (sections.Length < 2)
                continue;

            if (sections.Length >= 3 && !knownItems.Contains(sections[2])) {
                knownItems.Add(sections[2]);
                _albumIDToAlbumString.Add(itemID, sections[2]);
                itemID++;
            }

            var uid = sections[1];
            _songIDToUid[itemID] = uid;
            SongUidToId[uid] = itemID;
            itemID++;
        }
    }

    public bool TryGetOldName(string newName, out string oldName) {
        return _currentNamesToOldNames.TryGetValue(newName, out oldName);
    }
    public bool TryGetMusicInfo(string itemName, out MusicInfo info) {
        return _songsByItemName.TryGetValue(itemName, out info);
    }
    public bool TryGetMusicInfoFromUid(string uid, out MusicInfo info) {
        return _songsByUid.TryGetValue(uid, out info);
    }
    
    public MusicInfo GetMusicInfo(string itemName) {
        return _songsByItemName[itemName];
    }

    public bool TryGetAlbum(string itemName, out List<MusicInfo> infos) {
        return _songsByAlbum.TryGetValue(itemName, out infos);
    }

    public List<MusicInfo> GetAlbum(string itemName) {
        return _songsByAlbum[itemName];
    }

    public string GetItemNameFromMusicInfo(MusicInfo musicInfo) {
        var localisedSongName = ArchipelagoStatic.SongNameChanger.GetSongName(musicInfo);
        return $"{localisedSongName}";
    }
    public string GetItemNameFromUid(string uid) {
        if (!_songsByUid.TryGetValue(uid, out var musicInfo))
            return "";

        var localisedSongName = ArchipelagoStatic.SongNameChanger.GetSongName(musicInfo);
        return $"{localisedSongName}";
    }

    public bool TryGetSongFromItemId(long itemId, out MusicInfo info) {
        info = null;
        if (!_songIDToUid.TryGetValue(itemId, out var uid))
            return false;

        if (UidOverrides.TryGetValue(uid, out var replacementUid))
            uid = replacementUid;

        return _songsByUid.TryGetValue(uid, out info);
    }

    public long GetItemIdForSong(MusicInfo info) {
        var pair = _songIDToUid.FirstOrDefault(x => x.Value == info.uid);
        return pair.Value != null ? pair.Key : long.MaxValue;
    }

    public string GetLocalisedSongNameForMusicInfo(MusicInfo musicInfo) {
        var configManager = ConfigManager.instance;
        var songLocal = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal().GetLocalAlbumInfoByIndex(musicInfo.listIndex);
        return songLocal.name;
    }

    public string GetLocalisedAlbumNameForMusicInfo(MusicInfo musicInfo) {
        var configManager = ConfigManager.instance;
        var albumConfig = configManager.GetConfigObject<DBConfigAlbums>();
        var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>().GetLocal();
        var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
        return albumLocal;
    }
}