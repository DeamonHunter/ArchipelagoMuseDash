using System;
using System.Collections.Generic;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Managers;

namespace ArchipelagoMuseDash {
    /// <summary>
    /// Contains information about the game's songs, in a way that is easier to access for us.
    /// </summary>
    public class AlbumDatabase {
        Dictionary<string, MusicInfo> _songsByItemName = new Dictionary<string, MusicInfo>();
        Dictionary<string, Il2CppSystem.Collections.Generic.List<MusicInfo>> _songsByAlbum = new Dictionary<string, Il2CppSystem.Collections.Generic.List<MusicInfo>>();

        public const int CHINESE_LOC_INDEX = 0;
        public const int ENGLISH_LOC_INDEX = 1;
        public const string RANDOM_PANEL_UID = "?";

        public void Setup() {
            _songsByAlbum.Clear();
            _songsByItemName.Clear();

            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            _songsByAlbum = new Dictionary<string, Il2CppSystem.Collections.Generic.List<MusicInfo>>();
            _songsByItemName = new Dictionary<string, MusicInfo>();

            var configManager = ConfigManager.instance;
            if (configManager == null)
                throw new Exception("Config Manage was null when trying to load songs.");

            var albumConfig = configManager.GetConfigObject<DBConfigAlbums>(-1);
            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal(ENGLISH_LOC_INDEX);

            foreach (var musicInfo in list) {
                if (musicInfo.uid == RANDOM_PANEL_UID) {
                    ArchipelagoStatic.ArchLogger.Log("Album Database", "Skipping random.");
                    continue;
                }

                var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
                var localisedSongName = ArchipelagoStatic.SongNameChanger.GetSongName(musicInfo);
                _songsByItemName.Add($"{localisedSongName}[{albumLocal}]", musicInfo);

                if (!_songsByAlbum.TryGetValue(albumLocal, out var albumList)) {
                    albumList = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
                    _songsByAlbum.Add(albumLocal, albumList);
                }
                albumList.Add(musicInfo);
            }
        }

        public bool TryGetMusicInfo(string itemName, out MusicInfo info) => _songsByItemName.TryGetValue(itemName, out info);
        public MusicInfo GetMusicInfo(string itemName) => _songsByItemName[itemName];

        public bool TryGetAlbum(string itemName, out Il2CppSystem.Collections.Generic.List<MusicInfo> infos) => _songsByAlbum.TryGetValue(itemName, out infos);
        public Il2CppSystem.Collections.Generic.List<MusicInfo> GetAlbum(string itemName) => _songsByAlbum[itemName];

        public string GetItemNameFromMusicInfo(MusicInfo musicInfo) {
            var configManager = ConfigManager.instance;
            var albumConfig = configManager.GetConfigObject<DBConfigAlbums>(-1);
            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal(ENGLISH_LOC_INDEX);
            var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);

            var localisedSongName = ArchipelagoStatic.SongNameChanger.GetSongName(musicInfo);
            return $"{localisedSongName}[{albumLocal}]";
        }

        public string GetLocalisedSongNameForMusicInfo(MusicInfo musicInfo) {
            var configManager = ConfigManager.instance;
            var songLocal = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal().GetLocalAlbumInfoByIndex(musicInfo.listIndex);
            return songLocal.name;
        }

        public string GetLocalisedAlbumNameForMusicInfo(MusicInfo musicInfo) {
            var configManager = ConfigManager.instance;
            var albumConfig = configManager.GetConfigObject<DBConfigAlbums>(-1);
            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal();
            var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
            return albumLocal;
        }
    }
}
