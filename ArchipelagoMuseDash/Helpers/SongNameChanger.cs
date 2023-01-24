using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Managers;
using MelonLoader.TinyJSON;

namespace ArchipelagoMuseDash.Helpers {
    public class SongNameChanger {
        HashSet<char> _allowedCharacters = new HashSet<char>() {
            //Standard Extra Chars
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
            ' ', '-', '+', '.', '_',
            //More Questionable ASCII Todo: Check to see what should be allowed
            '\'', '!', '~', ':', '&', '*', '#', '/', ',', '?', ';'
        };

        Dictionary<char, char> _characterReplacements = new Dictionary<char, char>() {
            //Replaced to make names a bit clearer.
            //Though anything with these is likely just to be manually cleaned up
            { '[', ' ' },
            { ']', ' ' },
            { '(', ' ' },
            { ')', ' ' },

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
            { 'Ⅱ', '2' },
            { 'ä', 'a' },
            { 'ë', 'e' },
            { 'Я', 'R' },
            { 'í', 'i' },
        };

        Dictionary<string, string> _songUIDToReplacementNames = new Dictionary<string, string>();

        public SongNameChanger(Stream stream) {
            using (var sr = new StreamReader(stream)) {
                var text = sr.ReadToEnd();
                text = Regex.Replace(text, @"//.*\n", ""); //TinyJSON does not handle comments

                var variant = (ProxyObject)JSON.Load(text);
                foreach (var pair in variant) {
                    ArchipelagoStatic.ArchLogger.LogDebug("Song Name Changer", $"{pair.Key}/{pair.Value}");
                    if (pair.Value == null)
                        return;

                    _songUIDToReplacementNames.Add(pair.Key, pair.Value);
                }
            }
        }

        public string GetSongName(MusicInfo musicInfo) {
            var configManager = ConfigManager.instance;
            if (!_songUIDToReplacementNames.TryGetValue(musicInfo.uid, out var englishName)) {
                var englishSongLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                englishName = englishSongLOC.name;
            }
#if DEBUG
            else {
                //This is to make sure that its correct for now
                var englishSongLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                ArchipelagoStatic.ArchLogger.Log("Dump Songs", $"Loaded name '{englishName} for '{musicInfo.uid}|{englishSongLOC.name}'");
            }
#endif

            if (englishName.Any(c => !_allowedCharacters.Contains(c))) {
                englishName = ReplaceKnownCharacters(englishName);
#if DEBUG
                var englishSongLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                ArchipelagoStatic.ArchLogger.Log("Dump Songs", $"Changed name from: '{englishSongLOC.name}' to '{englishName}'");
#endif
            }

            return englishName;
        }

        public void DumpSongsToTextFile(string filePath) {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            var sb = new StringBuilder();
            var failedSongsSB = new StringBuilder();

            var originalStreamerMode = AnchorModule.instance.isAnchorMode;
            AnchorModule.instance.isAnchorMode = true;

            var configManager = ConfigManager.instance;
            var albumConfig = configManager.GetConfigObject<DBConfigAlbums>(-1);

            foreach (var musicInfo in list) {
                if (musicInfo.uid == AlbumDatabase.RANDOM_PANEL_UID)
                    continue;

                var englishName = GetSongName(musicInfo);

                if (englishName.Contains("Shit"))
                    ArchipelagoStatic.ArchLogger.Log("Dump Songs", $"Holy Shit Grass Snake: {musicInfo.uid}");

                if (englishName.Any(c => !_allowedCharacters.Contains(c))) {
                    var chineseLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.CHINESE_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                    var englishLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                    var japaneseLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(2).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                    var koreanLOC = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(3).GetLocalAlbumInfoByIndex(musicInfo.listIndex);
                    ArchipelagoStatic.ArchLogger.Log("Dump Songs", $"English contains unknown char: {musicInfo.uid}. Name: {englishLOC.name}");
                    failedSongsSB.AppendLine($"{musicInfo.uid}|{chineseLOC.name}|{englishLOC.name}|{japaneseLOC.name}|{koreanLOC.name}");
                }

                var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal(AlbumDatabase.ENGLISH_LOC_INDEX);
                var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
                var availableInStreamerMode = !AnchorModule.instance.CheckLockByMusicUid(musicInfo.uid);
                sb.AppendLine($"{englishName}|{albumLocal}|{availableInStreamerMode}|{musicInfo.difficulty1}|{musicInfo.difficulty2}|{musicInfo.difficulty3}|{musicInfo.difficulty4}");
            }

            AnchorModule.instance.isAnchorMode = originalStreamerMode;

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, sb.ToString());
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(filePath), "FailedOutput.txt"), failedSongsSB.ToString());
        }

        string ReplaceKnownCharacters(string str) {
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
                    sb.Remove(idx, 1); //Don't increase idx as we are moving everything down 1.
            }

            var output = sb.ToString();

            //Fix up cases where there may be more than 1 space.
            output = output.Replace("  ", " ").Trim();
            return output;
        }
    }
}
