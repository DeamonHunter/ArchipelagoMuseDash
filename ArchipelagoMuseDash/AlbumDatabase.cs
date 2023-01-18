using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Il2CppSystem.IO;
using UnityEngine;

namespace ArchipelagoMuseDash {
    /// <summary>
    /// Contains information about the game's songs, in a way that is easier to access for us.
    /// </summary>
    public class AlbumDatabase {
        Dictionary<string, MusicInfo> _songsByItemName = new Dictionary<string, MusicInfo>();
        Dictionary<string, Il2CppSystem.Collections.Generic.List<MusicInfo>> _songsByAlbum = new Dictionary<string, Il2CppSystem.Collections.Generic.List<MusicInfo>>();

        const int english_localisation_idx = 1;
        const string random_song_panel_uid = "?";

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
            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal(english_localisation_idx);

            foreach (var musicInfo in list) {
                if (musicInfo.uid == random_song_panel_uid) {
                    ArchipelagoStatic.ArchLogger.Log("Album Database", "Skipping random.");
                    continue;
                }

                var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
                var songLocal = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(english_localisation_idx).GetLocalAlbumInfoByIndex(musicInfo.listIndex);

                _songsByItemName.Add($"{songLocal.name}[{albumLocal}]", musicInfo);

                if (!_songsByAlbum.TryGetValue(albumLocal, out var albumList)) {
                    albumList = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
                    _songsByAlbum.Add(albumLocal, albumList);
                }
                albumList.Add(musicInfo);
            }

#if DEBUG
            DumpSongs();
#endif
        }

        public bool TryGetMusicInfo(string itemName, out MusicInfo info) => _songsByItemName.TryGetValue(itemName, out info);
        public MusicInfo GetMusicInfo(string itemName) => _songsByItemName[itemName];

        public bool TryGetAlbum(string itemName, out Il2CppSystem.Collections.Generic.List<MusicInfo> infos) => _songsByAlbum.TryGetValue(itemName, out infos);
        public Il2CppSystem.Collections.Generic.List<MusicInfo> GetAlbum(string itemName) => _songsByAlbum[itemName];

        public string GetItemNameFromMusicInfo(MusicInfo musicInfo) {
            var configManager = ConfigManager.instance;
            var albumConfig = configManager.GetConfigObject<DBConfigAlbums>(-1);
            var songLocal = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(english_localisation_idx).GetLocalAlbumInfoByIndex(musicInfo.listIndex);

            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal(english_localisation_idx);
            var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);

            return $"{songLocal.name}[{albumLocal}]";
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

        void DumpSongs() {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            var sb = new StringBuilder();

            var configManager = Singleton<ConfigManager>.instance;
            var albumConfig = configManager.GetConfigObject<DBConfigAlbums>(-1);
            var albumLocalisation = configManager.GetConfigObject<DBConfigAlbums>(-1).GetLocal(english_localisation_idx);

            var originalStreamerMode = AnchorModule.instance.isAnchorMode;
            AnchorModule.instance.isAnchorMode = true;

            foreach (var musicInfo in list) {
                if (musicInfo.uid == random_song_panel_uid)
                    continue;

                var albumLocal = albumLocalisation.GetLocalTitleByIndex(albumConfig.GetAlbumInfoByAlbumJsonIndex(musicInfo.albumJsonIndex).listIndex);
                var songLocal = configManager.GetConfigObject<DBConfigALBUM>(musicInfo.albumJsonIndex).GetLocal(english_localisation_idx).GetLocalAlbumInfoByIndex(musicInfo.listIndex);

                var availableInStreamerMode = !AnchorModule.instance.CheckLockByMusicUid(musicInfo.uid);

                sb.AppendLine($"{songLocal.name}|{albumLocal}|{availableInStreamerMode}|{musicInfo.difficulty1}|{musicInfo.difficulty2}|{musicInfo.difficulty3}|{musicInfo.difficulty4}");
            }

            AnchorModule.instance.isAnchorMode = originalStreamerMode;

            if (!Directory.Exists(Path.Combine(Application.absoluteURL, "Output/")))
                Directory.CreateDirectory(Path.Combine(Application.absoluteURL, "Output/"));
            File.WriteAllText(Path.Combine(Application.absoluteURL, "Output/SongDump.txt"), sb.ToString());
        }
    }
}
