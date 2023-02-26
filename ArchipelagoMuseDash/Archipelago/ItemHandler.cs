using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMuseDash.Archipelago.Items;
using Assets.Scripts.Database;
using UnityEngine.EventSystems;

namespace ArchipelagoMuseDash.Archipelago {
    public class ItemHandler {
        public ItemUnlockHandler Unlocker { get; }

        public ShownSongMode HiddenSongMode { get; private set; } = ShownSongMode.Unlocks;
        public GradeOption GradeNeeded { get; private set; }
        public MusicInfo GoalSong { get; private set; }
        public int NumberOfMusicSheetsToWin { get; private set; }
        public int CurrentNumberOfMusicSheets { get; private set; }

        public HashSet<string> SongsInLogic = new HashSet<string>();
        public HashSet<string> UnlockedSongUids = new HashSet<string>();
        public HashSet<string> CompletedSongUids = new HashSet<string>();

        public const string ShowingAllSongsText = "Showing: All";
        public const string ShowingUnlockedSongsText = "Showing: Unlocked";
        public const string ShowingUnplayedSongsText = "Showing: Unplayed";
        public const string MusicSheetItemName = "Music Sheet";

        ArchipelagoSession _currentSession;
        int _currentPlayerSlot;

        readonly Random _random = new Random();

        public ItemHandler(ArchipelagoSession session, int playerSlot) {
            _currentSession = session;
            _currentPlayerSlot = playerSlot;

            _currentSession.Locations.CheckedLocationsUpdated += NewLocationChecked;

            Unlocker = new ItemUnlockHandler(this);
        }

        public void Setup(Dictionary<string, object> slotData) {
            ArchipelagoStatic.ArchLogger.Log("ItemHandler", "Setup Called.");

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
                CheckRemoteLocation(name.Substring(0, name.Length - 2));
            }

            foreach (var location in _currentSession.Locations.AllLocations) {
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                name = name.Substring(0, name.Length - 2);

                if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(name, out var info))
                    SongsInLogic.Add(info.uid);
                else
                    ArchipelagoStatic.ArchLogger.Warning("ItemHandler", $"Unknown location: {name}");
            }

            SetVisibilityOfAllSongs(ShownSongMode.Unlocks);
        }

        public void OnUpdate() {
            CheckForNewItems();

            if (ArchipelagoStatic.SessionHandler.SongSelectAdditions.ToggleSongsButton == null)
                return;

            var hideSongText = ArchipelagoStatic.SessionHandler.SongSelectAdditions.ToggleSongsText;

            switch (HiddenSongMode) {
                case ShownSongMode.AllInLogic:
                    hideSongText.text = ShowingAllSongsText;
                    break;
                case ShownSongMode.Unplayed:
                    hideSongText.text = ShowingUnplayedSongsText;
                    break;
                case ShownSongMode.Unlocks:
                    hideSongText.text = ShowingUnlockedSongsText;
                    break;
            }
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

        void NewLocationChecked(IReadOnlyCollection<long> locations) {
            foreach (var location in locations) {
                ArchipelagoStatic.ArchLogger.LogDebug("NewLocationCheck", $"New Location: {location}");
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                CheckRemoteLocation(name.Substring(0, name.Length - 2));
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

                var locationsToCheck = new List<long>();

                var location1 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-0");
                if (location1 != -1 && _currentSession.Locations.AllLocations.Contains(location1)) {
                    if (!_currentSession.Locations.AllLocationsChecked.Contains(location1))
                        locationsToCheck.Add(location1);
                }

                var location2 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-1");
                if (location2 != -1 && _currentSession.Locations.AllLocations.Contains(location2)) {
                    if (!_currentSession.Locations.AllLocationsChecked.Contains(location2))
                        locationsToCheck.Add(location2);
                }

                if (locationsToCheck.Count <= 0)
                    return;

                var locationsArray = locationsToCheck.ToArray();

                //Complete the location check, but also scout to ensure we get the items we are sending to other players.
                await _currentSession.Locations.CompleteLocationChecksAsync(locationsArray);
                var items = await _currentSession.Locations.ScoutLocationsAsync(false, locationsArray);

                ArchipelagoStatic.ArchLogger.Log("CheckLocations", "Received Items Packet.");
                CheckRemoteLocation(locationName);
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

        void CheckRemoteLocation(string locationName) {
            var subSection = locationName.Substring(0, locationName.Length);

            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(subSection, out var singularInfo)) {
                if (CompletedSongUids.Contains(singularInfo.uid))
                    return;

                //If a person collects 1 of 2 locations, we don't want to check it.
                if (!IsSongFullyCompleted(subSection))
                    return;

                //Check to see if the song is favourited, and remove if it is
                if (GlobalDataBase.dbMusicTag.ContainsCollection(singularInfo))
                    GlobalDataBase.dbMusicTag.RemoveCollection(singularInfo);

                if (HiddenSongMode == ShownSongMode.Unplayed)
                    AddHide(singularInfo);

                CompletedSongUids.Add(singularInfo.uid);
                return;
            }

            ArchipelagoStatic.ArchLogger.Warning("HandleLocationChecked", $"Unknown Location: {locationName}");
        }

        private bool IsSongFullyCompleted(string songKey) {
            var location1 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", songKey + "-0");
            if (location1 != -1 && _currentSession.Locations.AllLocations.Contains(location1)) {
                if (!_currentSession.Locations.AllLocationsChecked.Contains(location1))
                    return false;
            }


            var location2 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", songKey + "-1");
            if (location2 != -1 && _currentSession.Locations.AllLocations.Contains(location2)) {
                if (!_currentSession.Locations.AllLocationsChecked.Contains(location2))
                    return false;
            }

            return true;
        }

        #endregion

        public void PickNextSongShownMode() {
            //Try to fix jank button selection logic
            EventSystem.current.SetSelectedGameObject(null);

            ArchipelagoStatic.ArchLogger.Log("ItemHandler", "Choosing next song shown mode.");
            var nextMode = (ShownSongMode)(((int)HiddenSongMode + 1) % ((int)ShownSongMode.AllInLogic + 1));
            SetVisibilityOfAllSongs(nextMode);
        }

        void SetVisibilityOfAllSongs(ShownSongMode mode) {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            ArchipelagoStatic.ArchLogger.Log("ItemHandler", $"Visibility being set to {mode}");
            HiddenSongMode = mode;

            GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo = null;

            foreach (var song in list) {
                if (song == null || song.uid == "?")
                    continue;

                if (!SongsInLogic.Contains(song.uid)) {
                    AddHide(song);
                    GlobalDataBase.dbMusicTag.RemoveCollection(song);
                    continue;
                }

                //Goal should always be visible
                if (GoalSong?.uid == song.uid) {
                    GlobalDataBase.dbMusicTag.RemoveHide(song);
                    GlobalDataBase.dbMusicTag.AddCollection(song);
                    continue;
                }

                switch (mode) {
                    case ShownSongMode.AllInLogic:
                        if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                            GlobalDataBase.dbMusicTag.RemoveHide(song);

                        if (UnlockedSongUids.Contains(song.uid) && !CompletedSongUids.Contains(song.uid) && !GlobalDataBase.dbMusicTag.ContainsCollection(song))
                            GlobalDataBase.dbMusicTag.AddCollection(song);
                        break;

                    case ShownSongMode.Unlocks:
                        if (!UnlockedSongUids.Contains(song.uid)) {
                            AddHide(song);

                            if (GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.RemoveCollection(song);
                        }
                        else {
                            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                                GlobalDataBase.dbMusicTag.RemoveHide(song);

                            if (!CompletedSongUids.Contains(song.uid) && !GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.AddCollection(song);
                        }
                        break;

                    case ShownSongMode.Unplayed:
                        if (!UnlockedSongUids.Contains(song.uid) || CompletedSongUids.Contains(song.uid)) {
                            AddHide(song);

                            if (GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.RemoveCollection(song);
                        }
                        else {
                            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                                GlobalDataBase.dbMusicTag.RemoveHide(song);

                            if (!GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.AddCollection(song);
                        }
                        break;
                }
            }

            MusicTagManager.instance.RefreshDBDisplayMusics();
            ArchipelagoStatic.SongSelectPanel?.RefreshMusicFSV();
        }

        private void AddHide(MusicInfo song) {
            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                return;

            GlobalDataBase.dbMusicTag.AddHide(song);
            GlobalDataBase.dbMusicTag.RemoveShowMusicUid(song);

            //if (GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo?.uid == song?.uid)
            //    GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo = GlobalDataBase.dbMusicTag.SelectRandomMusic();
        }

        public void UnlockSong(MusicInfo song) {
            UnlockedSongUids.Add(song.uid);
            if (HiddenSongMode == ShownSongMode.AllInLogic || !SongsInLogic.Contains(song.uid))
                return;

            if (HiddenSongMode == ShownSongMode.Unlocks || !CompletedSongUids.Contains(song.uid))
                GlobalDataBase.dbMusicTag.RemoveHide(song);

            if (!CompletedSongUids.Contains(song.uid))
                GlobalDataBase.dbMusicTag.AddCollection(song);
        }
    }
}
