using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMuseDash.Patches;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Nice.Interface;
using UnityEngine;
using Random = System.Random;

// Due to how IL2CPP works, some things can't be invoked as an extension.
// ReSharper disable InvokeAsExtensionMethod

namespace ArchipelagoMuseDash {
    /// <summary>
    /// Handles the Archipelago session after we've logged in.
    /// </summary>
    public class ArchipelagoSessionHandler {
        public MusicInfo GoalSong { get; private set; }

        readonly HashSet<string> _unlockedSongs = new HashSet<string>();
        readonly HashSet<string> _completedSongs = new HashSet<string>();
        readonly Queue<QueuedItem> _enqueuedItems = new Queue<QueuedItem>();
        readonly Random random = new Random();

        ArchipelagoSession _currentSession;
        int _slot;
        Dictionary<string, object> _slotData;

        const float default_loading_screen_delay = 0.75f;
        const float default_give_delay = 0.1f;
        const string default_music_name = "Magical Wonderland (More colorful mix)[Default Music]";
        float _itemGiveDelay = default_give_delay;

        bool _showWin;
        ShowBannerTextOnUnlock _showBannerText;

        public void RegisterSession(ArchipelagoSession session, int slot, Dictionary<string, object> slotData) {
            if (_currentSession != null)
                throw new NotImplementedException("Changing sessions is not implemented atm.");

            _slot = slot;
            _slotData = slotData;

            _currentSession = session;
            try {
                Setup();
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("ItemHandler", e);
            }
        }

        void Setup() {
            _unlockedSongs.Clear();
            ArchipelagoStatic.AlbumDatabase.Setup();

            if (_slotData.TryGetValue("victoryLocation", out var value)) {
                ArchipelagoStatic.ArchLogger.Log("Goal Song", (string)value);
                GoalSong = ArchipelagoStatic.AlbumDatabase.GetMusicInfo((string)value);
                GlobalDataBase.dbMusicTag.RemoveHide(GoalSong);
                GlobalDataBase.dbMusicTag.AddCollection(GoalSong);
            }

            CheckForNewItems(true);

            foreach (var location in _currentSession.Locations.AllLocationsChecked) {
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                HandleLocationChecked(name.Substring(0, name.Length - 2));
            }
        }

        public void CheckForNewItems(bool addItemsImmediately) {
            while (_currentSession.Items.Any()) {
                var item = _currentSession.Items.DequeueItem();

                //These items should always be for the local player.
                EnqueueItem(item, false);
            }

            if (addItemsImmediately) {
                lock (_enqueuedItems) {
                    while (_enqueuedItems.Count > 0) {
                        var music = _enqueuedItems.Dequeue().NewMusic;
                        if (music != null)
                            TryUnlockMusic(music);
                    }
                }
                //Forces a refresh of the song select so no songs show up. (Also probably gets rid of the current tag if any?)
                MusicTagManager.instance.RefreshStageDisplayMusics(-1);
            }
        }

        void EnqueueItem(NetworkItem item, bool otherPlayerItem) {
            var name = _currentSession.Items.GetItemName(item.Item);
            ArchipelagoStatic.ArchLogger.Log("ItemHandler", $"Attempting to enqueue network item: {name}");

            //Todo: Should we show who the item was from?

            if (!otherPlayerItem) {
                if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(name, out var singularInfo)) {
                    lock (_enqueuedItems)
                        _enqueuedItems.Enqueue(new QueuedItem(singularInfo));
                }
                else if (ArchipelagoStatic.AlbumDatabase.TryGetAlbum(name, out var album)) {
                    foreach (var musicInfo in album) {
                        lock (_enqueuedItems)
                            _enqueuedItems.Enqueue(new QueuedItem(musicInfo));
                    }
                }
                else if (name != "Nothing" && name != "Victory")
                    ArchipelagoStatic.ArchLogger.Warning("ItemHandler", $"Unknown Item was given: {name}");
            }
            else {
                var playerName = _currentSession.Players.GetPlayerAlias(item.Player);
                if (string.IsNullOrEmpty(playerName))
                    playerName = "Unknown Player"; //Catch all for certain cases, like cheated items

                ArchipelagoStatic.ArchLogger.Log("ItemHandler", $"{playerName}, {name}");
                lock (_enqueuedItems)
                    _enqueuedItems.Enqueue(new QueuedItem(name, playerName));
            }
        }

        public void HandleNewItems() {
            //Ensure that the stage select is shown. This helps avoid new items triggering while the load screen is still visible
            if (!ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlStage")) {
                _itemGiveDelay = default_loading_screen_delay;
                return;
            }

            //Check if we can even display the panel for unlocking
            if (ArchipelagoStatic.UnlockStagePanel == null || ArchipelagoStatic.UnlockStagePanel.gameObject.activeInHierarchy) {
                _itemGiveDelay = default_give_delay;
                return;
            }

            //Give some delay between items, and after other things enable to try and *ensure* that things are ok
            _itemGiveDelay -= Time.unscaledDeltaTime;
            if (_itemGiveDelay > 0)
                return;

            if (_showWin) {
                Data victoryData = new Data();
                VariableUtils.SetResult(victoryData["uid"], ArchipelagoStatic.AlbumDatabase.GetMusicInfo(default_music_name).uid);
                _ = victoryData["victory"];
                ArchipelagoStatic.UnlockStagePanel.UnlockNewSong(victoryData.Cast<IData>());
                VariableUtils.SetResult(victoryData["archPlayer"], (Il2CppSystem.String)(_currentSession.Players.GetPlayerAlias(_slot)));

                _itemGiveDelay = default_loading_screen_delay;
                _showWin = false;
                return;
            }

            QueuedItem newItem;
            lock (_enqueuedItems) {
                if (_enqueuedItems.Count <= 0)
                    return;

                newItem = _enqueuedItems.Dequeue();
            }

            Data data = new Data();
            if (newItem.NewMusic == null) {
                VariableUtils.SetResult(data["uid"], (Il2CppSystem.String)ArchipelagoStatic.AlbumDatabase.GetMusicInfo(default_music_name).uid);

                var name = (Il2CppSystem.String)newItem.ItemName;
                ArchipelagoStatic.ArchLogger.Log("HandleItem", name);

                VariableUtils.SetResult(data["archName"], name);
                VariableUtils.SetResult(data["archPlayer"], (Il2CppSystem.String)newItem.PlayerName);
            }
            else {
                VariableUtils.SetResult(data["uid"], (Il2CppSystem.String)newItem.NewMusic.uid);
                if (_unlockedSongs.Contains(newItem.NewMusic.uid))
                    _showBannerText = ShowBannerTextOnUnlock.DuplicateSong;
                else if (GoalSong?.uid == newItem.NewMusic.uid)
                    _showBannerText = ShowBannerTextOnUnlock.GoalSong;
                ArchipelagoStatic.ArchLogger.Log(_showBannerText.ToString());

                if (TryUnlockMusic(newItem.NewMusic))
                    _showBannerText = ShowBannerTextOnUnlock.RefreshMusic;
            }

            ArchipelagoStatic.UnlockStagePanel.UnlockNewSong(data.Cast<IData>());

            _itemGiveDelay = default_give_delay;
        }

        public void HandleLock() {
            //Check if we need to override the banner text on an unlocking item

            if (_showBannerText == ShowBannerTextOnUnlock.None)
                return;

            if (!ArchipelagoStatic.UnlockStagePanel.gameObject.activeInHierarchy) {
                MusicTagManager.instance.RefreshStageDisplayMusics(-1);
                ArchipelagoStatic.SongSelectPanel?.RefreshMusicFSV();
                _showBannerText = ShowBannerTextOnUnlock.None;
                return;
            }

            if (!ArchipelagoStatic.UnlockStagePanel.unlockText.gameObject.activeSelf) {
                switch (_showBannerText) {
                    case ShowBannerTextOnUnlock.GoalSong:
                        PnlUnlockStagePatch.SetupLock(ArchipelagoStatic.UnlockStagePanel, "Its the Goal!", false);
                        break;
                    case ShowBannerTextOnUnlock.DuplicateSong:
                        PnlUnlockStagePatch.SetupLock(ArchipelagoStatic.UnlockStagePanel, "Its a duplicate...", false);
                        break;
                }
            }
        }

        bool TryUnlockMusic(MusicInfo musicInfo) {
            _unlockedSongs.Add(musicInfo.uid);
            if (!GlobalDataBase.dbMusicTag.ContainsHide(musicInfo))
                return false;

            //Remove the song from hidden, and also favourite it, so the favourite symbol can act as a "Has a check"
            GlobalDataBase.dbMusicTag.RemoveHide(musicInfo);
            GlobalDataBase.dbMusicTag.AddCollection(musicInfo);
            return true;
        }

        void HandleLocationChecked(string locationName) {
            var subSection = locationName.Substring(0, locationName.Length);
            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(subSection, out var singularInfo)) {
                if (GlobalDataBase.dbMusicTag.ContainsCollection(singularInfo))
                    GlobalDataBase.dbMusicTag.RemoveCollection(singularInfo); //Unfavourite the song
                _completedSongs.Add(singularInfo.uid);
            }
            else if (ArchipelagoStatic.AlbumDatabase.TryGetAlbum(subSection, out var album)) {
                foreach (var musicInfo in album) {
                    if (GlobalDataBase.dbMusicTag.ContainsCollection(musicInfo))
                        GlobalDataBase.dbMusicTag.RemoveCollection(musicInfo); //Unfavourite the song
                    _completedSongs.Add(musicInfo.uid);
                }
            }
            else
                ArchipelagoStatic.ArchLogger.Warning("HandleLocationChecked", $"Unknown Location: {locationName}");
        }

        public void CheckLocation(string uid, string locationName) {
            ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Checking location for: {locationName}");
            System.Threading.Tasks.Task.Run(async () => await CheckLocationsInner(uid, locationName));
        }

        async System.Threading.Tasks.Task CheckLocationsInner(string uid, string locationName) {
            try {
                var location1 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-0");
                var location2 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-1");
                _completedSongs.Add(uid);

                //Complete the location check, but also scout to ensure we get the items we are sending to other players.
                await _currentSession.Locations.CompleteLocationChecksAsync(location1, location2);
                var items = await _currentSession.Locations.ScoutLocationsAsync(false, location1, location2);

                ArchipelagoStatic.ArchLogger.Log("CheckLocations", "Received Items Packet.");
                HandleLocationChecked(locationName);
                foreach (var item in items.Locations) {
                    //The item should already be handled
                    if (item.Player == _slot)
                        continue;

                    EnqueueItem(item, true);
                }

                if (GoalSong != null && GoalSong.uid == uid) {
                    //Todo: Better victory stuff
                    ArchipelagoStatic.ArchLogger.Log("ItemHandler", "Victory achieved, enqueing visuals for next available time.");
                    _showWin = true;

                    var statusUpdatePacket = new StatusUpdatePacket {
                        Status = ArchipelagoClientState.ClientGoal
                    };

                    await _currentSession.Socket.SendPacketAsync(statusUpdatePacket);
                }
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Check Location", e);
            }
        }

        public bool IsSongUnlocked(string musicUid) {
            return _unlockedSongs.Contains(musicUid);
        }

        /// <summary>
        /// Unhide all songs. In case something breaks.
        /// </summary>
        public void FixMyGame() {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            foreach (var musicInfo in list)
                GlobalDataBase.dbMusicTag.RemoveHide(musicInfo);
        }

        public MusicInfo GetRandomUnfinishedSong() {
            //Not very efficient, but its a button clicked once.
            var unfinishedSongs = _unlockedSongs.Where(x => !_completedSongs.Contains(x)).ToList();

            if (unfinishedSongs.Count <= 0)
                return null;

            var selectedSong = unfinishedSongs[random.Next(unfinishedSongs.Count)];

            var ids = new Il2CppSystem.Collections.Generic.List<string>();
            ids.Add(selectedSong);
            var buffer = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetMusicInfosByUids(ids, buffer);

            foreach (var info in buffer)
                return info;
            throw new Exception("Failed to find random music info.");
        }

        private struct QueuedItem {
            public MusicInfo NewMusic;
            public string ItemName;
            public string PlayerName;

            public QueuedItem(MusicInfo music) {
                NewMusic = music;
                ItemName = null;
                PlayerName = null;
            }

            public QueuedItem(string itemName, string playerName) {
                NewMusic = null;
                ItemName = itemName;
                PlayerName = playerName;
            }
        }

        enum ShowBannerTextOnUnlock {
            None,
            RefreshMusic,
            DuplicateSong,
            GoalSong
        }
    }
}
