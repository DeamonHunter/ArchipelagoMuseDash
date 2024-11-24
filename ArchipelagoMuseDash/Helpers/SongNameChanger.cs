using System.Text;
using System.Text.RegularExpressions;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Managers;
using MelonLoader.TinyJSON;

namespace ArchipelagoMuseDash.Helpers;

/// <summary>
/// Handles changing the names of <see cref="MusicInfo"/> to a form that is compatible with Archipelago.
/// </summary>
public class SongNameChanger {
    private readonly Dictionary<string, string> _songUidToReplacementNames = new();

    private readonly HashSet<char> _allowedCharacters = new() {
        //Standard Extra Chars
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
        ' ', '-', '+', '.', '_',
        //More Questionable ASCII Todo: Check to see what should be allowed
        '\'', '!', '&', '*', '#', '/', ',', '?', ';'
    };

    private readonly Dictionary<char, char> _characterReplacements = new() {
        //Replaced to make names a bit clearer.
        //Though anything with these is likely just to be manually cleaned up
        { '[', ' ' },
        { ']', ' ' },
        { '(', ' ' },
        { ')', ' ' },
        { '~', char.MinValue },

        //Full Width Chars
        { '：', ':' },
        { '！', '!' },

        //Unicode Shenanigans 
        { '\u00A0', ' ' }, //No Break Space
        { '☆', '*' },
        { '★', '*' },
        { '↑', ' ' },
        { '↓', ' ' },
        { '†', ' ' },
        { 'Ⅰ', '1' },
        { 'Ⅱ', '2' },
        { 'Ⅲ', '3' },
        { 'ä', 'a' },
        { 'ë', 'e' },
        { 'Я', 'R' },
        { 'í', 'i' },
        { '·', ' ' }
    };

    private HashSet<string> _difficultyOverrides = new() {
        "MuseDash ka nanika hi",
        "Rush-Hour",
        "Find this Month's Featured Playlist",
        "PeroPero in the Universe",
        "umpopoff",
        "P E R O P E R O Brother Dance",
    };

    /// <summary>
    /// Creates a <see cref="SongNameChanger"/> from a <see cref="Stream"/> containing JSON.
    /// </summary>
    /// <param name="stream"></param>
    public SongNameChanger(Stream stream) {
        using (var sr = new StreamReader(stream)) {
            var text = sr.ReadToEnd();

            //TinyJSON does not handle comments, so strip out all the comments in the file
            text = Regex.Replace(text, @"//.*\n", "");

            var variant = (ProxyObject)JSON.Load(text);
            foreach (var pair in variant) {
                ArchipelagoStatic.ArchLogger.LogDebug("Song Name Changer", $"{pair.Key}/{pair.Value}");
                if (pair.Value == null)
                    return;

                _songUidToReplacementNames.Add(pair.Key, pair.Value);
            }
        }
    }

    /// <summary>
    /// Gets the modified Archipelago song name. This name is in ASCII characters and can be used for hinting.
    /// </summary>
    /// <param name="musicInfo">The <see cref="MusicInfo"/> that you want the name of.</param>
    /// <returns>The Archipelago Song Name</returns>
    public string GetSongName(MusicInfo musicInfo) {
        var configManager = ConfigManager.instance;
        if (!_songUidToReplacementNames.TryGetValue(musicInfo.uid, out var englishName)) {
            var englishSongLoc = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
            englishName = englishSongLoc.name;
        }

        if (englishName.Any(c => !_allowedCharacters.Contains(c)))
            englishName = ReplaceKnownCharacters(englishName);

        return englishName;
    }

    /// <summary>
    /// Outputs a file which is can be used by the Archipelago Randomiser to create items/locations.
    /// Also outputs another file for any songs containing non-ASCII characters that may need to be translated to become items/locations.
    /// </summary>
    /// <param name="filePath">The file to create.</param>
    public void DumpSongsToTextFile(string filePath) {
        var databaseList = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
        GlobalDataBase.dbMusicTag.GetAllMusicInfo(databaseList);

        //Transfer to c# list to make it easier to sort.
        var sortedList = new List<MusicInfo>(databaseList.Count);
        foreach (var item in databaseList)
            sortedList.Add(item);
        
        sortedList.Add(new MusicInfo() {
            name = "Tsukuyomi Ni Naru Replaced",
            uid = "74-2",
            difficulty1 = "5",
            difficulty2 = "7",
            difficulty3 = "9"
        });

        sortedList.Sort(CompareItemIds);
        var index = sortedList.FindIndex(x => x.uid == "74-2");
        sortedList[index].m_MusicExInfo = sortedList[index + 1].m_MusicExInfo;

        var sb = new StringBuilder();
        var failedSongsSB = new StringBuilder();
        var pythonSb = new StringBuilder();
        pythonSb.AppendLine("from .Items import SongData\nfrom typing import Dict\n\n\n# Auto Generated\nSONG_DATA: Dict[str, SongData] = {");

        var originalStreamerMode = AnchorModule.instance.isAnchorMode;
        AnchorModule.instance.isAnchorMode = true;

        var configManager = ConfigManager.instance;
        var albumConfig = configManager.GetConfigObject<DBConfigAlbums>();

        long lastId = -1;

        foreach (var musicInfo in sortedList) {
            if (musicInfo.uid == AlbumDatabase.RANDOM_PANEL_UID || musicInfo.uid.Contains("999"))
                continue;

            var englishName = GetSongName(musicInfo);

            if (englishName.Any(c => !_allowedCharacters.Contains(c))) {
                var chineseLoc = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.CHINESE_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                var englishLoc = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                ArchipelagoStatic.ArchLogger.Log("Dump Songs", $"English Song contains unknown char: {musicInfo.uid}. Name: {englishLoc.name}");
                failedSongsSB.AppendLine($"{musicInfo.uid}|{chineseLoc.name}|{englishLoc.name}");
            }

            if (musicInfo.uid == "74-2")
                englishName = "Tsukuyomi Ni Naru Replaced";

            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>().GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX);
            var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
            if (albumLocal.Any(c => !_allowedCharacters.Contains(c))) {
                albumLocal = ReplaceKnownCharacters(albumLocal);
                if (albumLocal.Any(c => !_allowedCharacters.Contains(c))) {
                    ArchipelagoStatic.ArchLogger.Log("Dump Songs", $"English Album contains unknown char: Song: {musicInfo.uid}. Name: {albumLocal}");
                    failedSongsSB.AppendLine($"Album: {albumLocal}");
                }
            }

            var availableInStreamerMode = !AnchorModule.instance.CheckLockByMusicUid(musicInfo.uid);
            sb.AppendLine($"{englishName}|{musicInfo.uid}|{albumLocal}|{availableInStreamerMode}|{musicInfo.difficulty1}|{musicInfo.difficulty2}|{musicInfo.difficulty3}|{musicInfo.difficulty4}");

            lastId = WritePythonLine(pythonSb, lastId, englishName, musicInfo.uid, albumLocal, availableInStreamerMode, musicInfo.difficulty1, musicInfo.difficulty2, musicInfo.difficulty3);
        }
        pythonSb.AppendLine("}");

        AnchorModule.instance.isAnchorMode = originalStreamerMode;

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath + ".txt", sb.ToString());
        File.WriteAllText(filePath + ".py", pythonSb.ToString());
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(filePath)!, "FailedOutput.txt"), failedSongsSB.ToString());
    }
    
    private long WritePythonLine(StringBuilder sb, long lastId,
        string englishName, string uid, string album, bool streamerMode, string diff1, string diff2, string diff3) {
        if (!ArchipelagoStatic.AlbumDatabase.SongUidToId.TryGetValue(uid, out var id))
            id = lastId + 1;

        if (_difficultyOverrides.Contains(englishName)) {
            ArchipelagoStatic.ArchLogger.Log("info", $"{englishName} : {diff1} : {diff2} : {diff3}");
            if (!string.IsNullOrEmpty(diff1) && diff1 != "0")
                diff1 = "4";
            else
                diff1 = "None";
            if (!string.IsNullOrEmpty(diff2) && diff2 != "0")
                diff2 = "7";
            else
                diff2 = "None";
            if (!string.IsNullOrEmpty(diff3) && diff3 != "0")
                diff3 = "10";
            else
                diff3 = "None";
        }
        else {
            diff1 = ParseDiffToNumberOrNull(diff1);
            diff2 = ParseDiffToNumberOrNull(diff2);
            diff3 = ParseDiffToNumberOrNull(diff3);
        }
        
        sb.AppendLine($"    \"{englishName}\": SongData({id}, \"{uid}\", \"{album}\", {streamerMode}, {diff1}, {diff2}, {diff3}),");
        return id;
    }

    private string ParseDiffToNumberOrNull(string diff) {
        if (string.IsNullOrEmpty(diff) || diff == "?" || diff == "¿" || diff == "0")
            return "None";

        if (diff == "〇")
            return "10";
        return diff;
    }

    private int CompareItemIds(MusicInfo a, MusicInfo b) {
        var itemIdA = ArchipelagoStatic.AlbumDatabase.GetItemIdForSong(a);
        var itemIdB = ArchipelagoStatic.AlbumDatabase.GetItemIdForSong(b);
        return itemIdA != itemIdB ? itemIdA.CompareTo(itemIdB) : string.Compare(a.uid, b.uid, StringComparison.Ordinal);
    }

    /// <summary>
    /// Goes through a string and replaces all characters that we have defined to be replaced.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private string ReplaceKnownCharacters(string str) {
        var sb = new StringBuilder(str);

        var idx = 0;
        while (idx < sb.Length) {
            var c = sb[idx];
            if (_allowedCharacters.Contains(c)) {
                idx++;
                continue;
            }

            if (!_characterReplacements.TryGetValue(c, out var replacement)) {
                idx++;
                continue;
            }

            if (replacement != char.MinValue) {
                sb[idx] = replacement;
                idx++;
            }
            else
                sb.Remove(idx, 1); //Doesn't increase idx as we are moving everything down 1.
        }

        var output = sb.ToString();
        output = output.Replace("  ", " ").Trim();
        return output;
    }
}