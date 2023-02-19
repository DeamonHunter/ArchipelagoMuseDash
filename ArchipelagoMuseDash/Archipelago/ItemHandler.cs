using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMuseDash.Archipelago.Items;
using Assets.Scripts.Database;

namespace ArchipelagoMuseDash.Archipelago {
    public class ItemHandler {
        public ItemUnlockHandler Unlocker { get; }

        public GradeOption GradeNeeded { get; private set; }
        public MusicInfo GoalSong { get; private set; }
        public int NumberOfMusicSheetsToWin { get; private set; }
        public int CurrentNumberOfMusicSheets { get; private set; }

        public HashSet<string> SongsInLogic = new HashSet<string>();
        public HashSet<string> UnlockedSongUids = new HashSet<string>();
        public HashSet<string> CompletedSongUids = new HashSet<string>();

        public const string HideSongsText = "Hide Locked Songs";
        public const string ShowSongsText = "Show Locked Songs";
        public const string MusicSheetItemName = "Music Sheet";


        ArchipelagoSession _currentSession;
        int _currentPlayerSlot;
        bool _songsAreHidden = true;

        readonly Random _random = new Random();

        public ItemHandler(ArchipelagoSession session, int playerSlot) {
            _currentSession = session;
            _currentPlayerSlot = playerSlot;

            Unlocker = new ItemUnlockHandler(this);
        }

        public void Setup(Dictionary<string, object> slotData) {
            SongsInLogic.Clear();
            UnlockedSongUids.Clear();
            CompletedSongUids.Clear();

            //Todo: Handle these being missing
            if (slotData.TryGetValue("victoryLocation", out var victoryLocation)) {
                ArchipelagoStatic.ArchLogger.Log("Goal Song", victoryLocation.ToString());
                GoalSong = ArchipelagoStatic.AlbumDatabase.GetMusicInfo((string)victoryLocation);
                GlobalDataBase.dbMusicTag.RemoveHide(GoalSong);
                GlobalDataBase.dbMusicTag.AddCollection(GoalSong);

                SongsInLogic.Add(GoalSong.uid);
            }

            if (slotData.TryGetValue("musicSheetWinCount", out var tokenWinCount)) {
                ArchipelagoStatic.ArchLogger.Log("Music Sheets to Win", ((long)tokenWinCount).ToString());
                NumberOfMusicSheetsToWin = (int)((long)tokenWinCount);
            }

            if (slotData.TryGetValue("gradeNeeded", out var gradeNeeded)) {
                ArchipelagoStatic.ArchLogger.Log("Grade Needed to win", ((GradeOption)((long)gradeNeeded)).ToString());
                GradeNeeded = (GradeOption)((long)gradeNeeded);
            }
            else
                GradeNeeded = GradeOption.Any;

            CurrentNumberOfMusicSheets = 0;

            CheckForNewItems();
            Unlocker.UnlockAllItems();

            foreach (var location in _currentSession.Locations.AllLocationsChecked) {
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                CheckStartingLocation(name.Substring(0, name.Length - 2));
            }

            foreach (var location in _currentSession.Locations.AllLocations) {
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                name = name.Substring(0, name.Length - 2);

                if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(name, out var info))
                    SongsInLogic.Add(info.uid);
                else
                    ArchipelagoStatic.ArchLogger.Warning("ItemHandler", $"Unknown location: {name}");
            }

            SetVisibilityOfAllSongs(_songsAreHidden);
        }

        public void OnUpdate() {
            CheckForNewItems();

            if (ArchipelagoStatic.SessionHandler.SongSelectAdditions.HideSongsButton == null)
                return;

            var hideSongText = ArchipelagoStatic.SessionHandler.SongSelectAdditions.HideSongsText;

            if (_songsAreHidden && hideSongText.text != ShowSongsText)
                hideSongText.text = ShowSongsText;
            else if (!_songsAreHidden && hideSongText.text != HideSongsText)
                hideSongText.text = HideSongsText;
        }

        void CheckForNewItems() {
            while (_currentSession.Items.Any()) {
                var networkItem = _currentSession.Items.DequeueItem();

                //These items should always be for the local player.
                var item = GetItemFromNetworkItem(networkItem, false);
                if (item != null)
                    Unlocker.AddItem(item);
            }
        }

        IMuseDashItem GetItemFromNetworkItem(NetworkItem item, bool otherPlayersItem) {
            var name = _currentSession.Items.GetItemName(item.Item);
            if (otherPlayersItem) {
                var playerName = _currentSession.Players.GetPlayerAlias(item.Player);
                if (string.IsNullOrEmpty(playerName))
                    playerName = "Unknown Player"; //Catch all for certain cases, like cheated items

                name = name ?? $"Unknown Item: {item.Item}";
                ArchipelagoStatic.ArchLogger.LogDebug("ItemHandler", $"External Item: {playerName}, {name}");
                return new ExternalItem(name, playerName);
            }

            if (name == MusicSheetItemName) {
                CurrentNumberOfMusicSheets++;

                if (NumberOfMusicSheetsToWin == CurrentNumberOfMusicSheets)
                    return new SongItem(GoalSong);
                return new MusicSheetItem(NumberOfMusicSheetsToWin - CurrentNumberOfMusicSheets);
            }

            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(name, out var singularInfo))
                return new SongItem(singularInfo);

            if (ArchipelagoStatic.AlbumDatabase.TryGetAlbum(name, out var album))
                return new AlbumItem(name, album);

            if (name != "Nothing" && name != "Victory")
                ArchipelagoStatic.ArchLogger.Warning("ItemHandler", $"Unknown Item was given: {name}");

            return null;
        }

        public MusicInfo GetRandomUnfinishedSong() {
            //Not very efficient, but its a button clicked once.
            var unfinishedSongs = UnlockedSongUids.Where(x => !CompletedSongUids.Contains(x)).ToList();

            if (unfinishedSongs.Count <= 0)
                return null;

            var selectedSong = unfinishedSongs[_random.Next(unfinishedSongs.Count)];

            var ids = new Il2CppSystem.Collections.Generic.List<string>();
            ids.Add(selectedSong);
            var buffer = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetMusicInfosByUids(ids, buffer);

            foreach (var info in buffer)
                return info;
            throw new Exception("Failed to find random music info.");
        }

        #region Locations

        public void CheckLocation(string uid, string locationName) {
            if (CompletedSongUids.Contains(uid)) {
                ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Location already checked for: {locationName}");
                return;
            }

            if (!SongsInLogic.Contains(uid) && GoalSong.uid != uid) {
                ArchipelagoStatic.ArchLogger.Warning("CheckLocations", $"Tried to check location that wasn't in logic: {locationName}");
                return;
            }

            ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Checking location for: {locationName}");
            System.Threading.Tasks.Task.Run(async () => await CheckLocationsInner(uid, locationName));
        }

        async System.Threading.Tasks.Task CheckLocationsInner(string uid, string locationName) {
            try {
                CompletedSongUids.Add(uid);

                if (GoalSong != null && GoalSong.uid == uid) {
                    ArchipelagoStatic.ArchLogger.Log("ItemHandler", "Victory achieved, enqueing visuals for next available time.");

                    //Todo: This maybe should be priority?
                    Unlocker.AddItem(new VictoryItem(_currentSession.Players.GetPlayerAlias(_currentPlayerSlot), uid));

                    var statusUpdatePacket = new StatusUpdatePacket {
                        Status = ArchipelagoClientState.ClientGoal
                    };

                    await _currentSession.Socket.SendPacketAsync(statusUpdatePacket);
                    return;
                }

                var location1 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-0");
                var location2 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-1");

                //Complete the location check, but also scout to ensure we get the items we are sending to other players.

                LocationInfoPacket items;
                if (location2 != -1 && _currentSession.Locations.AllLocations.Contains(location2)) {
                    await _currentSession.Locations.CompleteLocationChecksAsync(location1, location2);
                    items = await _currentSession.Locations.ScoutLocationsAsync(false, location1, location2);
                }
                else {
                    await _currentSession.Locations.CompleteLocationChecksAsync(location1);
                    items = await _currentSession.Locations.ScoutLocationsAsync(false, location1);
                }

                ArchipelagoStatic.ArchLogger.Log("CheckLocations", "Received Items Packet.");
                CheckStartingLocation(locationName);
                foreach (var item in items.Locations) {
                    //The item should already be handled
                    if (item.Player == _currentPlayerSlot)
                        continue;

                    Unlocker.AddItem(GetItemFromNetworkItem(item, true));
                }
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Check Location", e);
            }
        }

        void CheckStartingLocation(string locationName) {
            var subSection = locationName.Substring(0, locationName.Length);

            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(subSection, out var singularInfo)) {
                //Check to see if the song is favourited, and remove if it is
                if (GlobalDataBase.dbMusicTag.ContainsCollection(singularInfo))
                    GlobalDataBase.dbMusicTag.RemoveCollection(singularInfo);
                CompletedSongUids.Add(singularInfo.uid);
                return;
            }

            if (ArchipelagoStatic.AlbumDatabase.TryGetAlbum(subSection, out var album)) {
                foreach (var musicInfo in album) {
                    //Check to see if the song is favourited, and remove if it is
                    if (GlobalDataBase.dbMusicTag.ContainsCollection(musicInfo))
                        GlobalDataBase.dbMusicTag.RemoveCollection(musicInfo);
                    CompletedSongUids.Add(musicInfo.uid);
                }

                return;
            }


            ArchipelagoStatic.ArchLogger.Warning("HandleLocationChecked", $"Unknown Location: {locationName}");
        }

        #endregion

        public void ToggleHiddenSongs() {
            _songsAreHidden = !_songsAreHidden;
            SetVisibilityOfAllSongs(_songsAreHidden);
        }

        void SetVisibilityOfAllSongs(bool visibility) {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            foreach (var song in list) {
                if (song == null || song.uid == "?")
                    continue;

                if (!SongsInLogic.Contains(song.uid)) {
                    GlobalDataBase.dbMusicTag.AddHide(song);
                    GlobalDataBase.dbMusicTag.RemoveCollection(song);
                    continue;
                }

                bool canBeHidden = !UnlockedSongUids.Contains(song.uid) && GoalSong?.uid != song.uid;

                if (canBeHidden && visibility) {
                    GlobalDataBase.dbMusicTag.AddHide(song);
                    GlobalDataBase.dbMusicTag.RemoveCollection(song);
                }
                else {
                    GlobalDataBase.dbMusicTag.RemoveHide(song);
                    if (!CompletedSongUids.Contains(song.uid))
                        GlobalDataBase.dbMusicTag.AddCollection(song);
                }
            }

            MusicTagManager.instance.RefreshStageDisplayMusics(-1);
            ArchipelagoStatic.SongSelectPanel?.RefreshMusicFSV();
        }

        public void UnlockSong(MusicInfo song) {
            UnlockedSongUids.Add(song.uid);
            if (!_songsAreHidden)
                return;
            GlobalDataBase.dbMusicTag.RemoveHide(song);

            if (!CompletedSongUids.Contains(song.uid) && SongsInLogic.Contains(song.uid))
                GlobalDataBase.dbMusicTag.AddCollection(song);
        }
    }
}
