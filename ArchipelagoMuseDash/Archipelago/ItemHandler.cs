using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMuseDash.Archipelago.Items;
using ArchipelagoMuseDash.Helpers;
using Assets.Scripts.Database;
using Assets.Scripts.GameCore.Managers;
using Assets.Scripts.UI.Controls;
using UnityEngine.EventSystems;
using Task = System.Threading.Tasks.Task;

namespace ArchipelagoMuseDash.Archipelago
{

    public class ItemHandler
    {

        private const string showing_all_songs_text = "Showing: All";
        private const string showing_hinted_songs_text = "Showing: Hinted";
        private const string showing_unlocked_songs_text = "Showing: Unlocked";
        private const string showing_unplayed_songs_text = "Showing: Unplayed";
        private const string music_sheet_item_name = "Music Sheet";

        public readonly HashSet<string> SongsInLogic = new HashSet<string>();
        public readonly HashSet<string> UnlockedSongUids = new HashSet<string>();
        public readonly HashSet<string> CompletedSongUids = new HashSet<string>();
        public readonly HashSet<string> StarterSongUIDs = new HashSet<string>();

        private readonly ArchipelagoSession _currentSession;
        private readonly int _currentPlayerSlot;
        private int _triggeredFeverFillerCount;
        private bool _triggerFeverFiller;

        private readonly Random _random = new Random();

        public ItemHandler(ArchipelagoSession session, int playerSlot)
        {
            _currentSession = session;
            _currentPlayerSlot = playerSlot;

            _currentSession.Locations.CheckedLocationsUpdated += NewLocationChecked;

            Unlocker = new ItemUnlockHandler(this);
        }
        public ItemUnlockHandler Unlocker { get; }

        public ShownSongMode HiddenSongMode { get; private set; } = ShownSongMode.Unlocks;
        public GradeOption GradeNeeded { get; private set; }
        public MusicInfo GoalSong { get; private set; }
        public int NumberOfMusicSheetsToWin { get; private set; }
        public int CurrentNumberOfMusicSheets { get; private set; }
        public bool VictoryAchieved { get; set; }
        public bool ShowFillerItems { get; set; }

        public void Setup(Dictionary<string, object> slotData)
        {
            ArchipelagoStatic.ArchLogger.Log("ItemHandler", "Setup Called.");

            SongsInLogic.Clear();
            UnlockedSongUids.Clear();
            CompletedSongUids.Clear();
            StarterSongUIDs.Clear();

            //Todo: Handle these being missing
            if (slotData.TryGetValue("victoryLocation", out var victoryLocation))
            {
                ArchipelagoStatic.ArchLogger.Log("Goal Song", victoryLocation.ToString());
                try
                {
                    GoalSong = ArchipelagoStatic.AlbumDatabase.GetMusicInfo((string)victoryLocation);
                    GlobalDataBase.dbMusicTag.RemoveHide(GoalSong);
                    GlobalDataBase.dbMusicTag.AddCollection(GoalSong);

                    SongsInLogic.Add(GoalSong.uid);
                }
                catch
                {
                    ArchipelagoStatic.ArchLogger.Warning("ItemHandler", "Catastrophic failure occured: Goal song doesn't exist?\nPlease report this!");
                    ShowText.ShowInfo("Catastrophic failure occured: Goal song doesn't exist?\nPlease report this!");
                }
            }

            if (slotData.TryGetValue("musicSheetWinCount", out var tokenWinCount))
            {
                ArchipelagoStatic.ArchLogger.Log("Music Sheets to Win", ((long)tokenWinCount).ToString());
                NumberOfMusicSheetsToWin = (int)(long)tokenWinCount;
            }

            if (slotData.TryGetValue("gradeNeeded", out var gradeNeeded))
            {
                var grade = (GradeOption)(long)gradeNeeded;
                ArchipelagoStatic.ArchLogger.Log("Grade Needed to win", grade.ToString());
                GradeNeeded = grade;
            }
            else
                GradeNeeded = GradeOption.Any;

            if (slotData.TryGetValue("hasFiller", out var hasFiller))
                ShowFillerItems = (bool)hasFiller;
            else
                ShowFillerItems = false;

            foreach (var location in _currentSession.Locations.AllLocations)
            {
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                name = name.Substring(0, name.Length - 2);

                if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(name, out var info))
                    SongsInLogic.Add(info.uid);
                else
                    ArchipelagoStatic.ArchLogger.Warning("ItemHandler", $"Unknown location: {name}");
            }

            CurrentNumberOfMusicSheets = 0;

            CheckForNewItems();
            Unlocker.UnlockAllItems();
            ArchipelagoStatic.SessionHandler.BattleHandler.ResetNewItemCount();

            var prevFillerCount = ArchipelagoStatic.SessionHandler.DataStorageHandler.GetHandledFeverCount();
            if (prevFillerCount < _triggeredFeverFillerCount)
                _triggerFeverFiller = true;

            foreach (var location in _currentSession.Locations.AllLocationsChecked)
            {
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                CheckRemoteLocation(name.Substring(0, name.Length - 2), false);
            }

            ArchipelagoHelpers.SetBackToDefaultFilter();
            SetVisibilityOfAllSongs(ShownSongMode.Unlocks);
        }

        public void OnUpdate()
        {
            if (ArchipelagoStatic.CurrentScene == "UISystem_PC")
                CheckForNewItems();

            if (_triggerFeverFiller)
            {
                var battleStage = ArchipelagoStatic.BattleComponent;
                if (battleStage != null && !battleStage.isDead && !battleStage.isPause && !battleStage.isSucceed)
                {
                    GlobalManagers.feverManager.AddFever(9999);
                    _triggerFeverFiller = false;
                    ArchipelagoStatic.SessionHandler.DataStorageHandler.SetHandledFeverCount(_triggeredFeverFillerCount);
                }
            }

            if (ArchipelagoStatic.SessionHandler.SongSelectAdditions.ToggleSongsButton == null)
                return;

            var hideSongText = ArchipelagoStatic.SessionHandler.SongSelectAdditions.ToggleSongsText;

            switch (HiddenSongMode)
            {
                case ShownSongMode.AllInLogic:
                    hideSongText.text = showing_all_songs_text;
                    break;
                case ShownSongMode.Unplayed:
                    hideSongText.text = showing_unplayed_songs_text;
                    break;
                case ShownSongMode.Unlocks:
                    hideSongText.text = showing_unlocked_songs_text;
                    break;
                case ShownSongMode.Hinted:
                    hideSongText.text = showing_hinted_songs_text;
                    break;
                default:
                    hideSongText.text = hideSongText.text;
                    break;
            }
        }

        private void CheckForNewItems()
        {
            while (_currentSession.Items.Any())
            {
                var networkItem = _currentSession.Items.DequeueItem();
                //These items should always be for the local player.
                var item = GetItemFromNetworkItem(networkItem, false, false);
                if (item != null)
                    Unlocker.AddItem(item);
            }
        }

        private void NewLocationChecked(IReadOnlyCollection<long> locations)
        {
            foreach (var location in locations)
            {
                ArchipelagoStatic.ArchLogger.LogDebug("NewLocationCheck", $"New Location: {location}");
                var name = _currentSession.Locations.GetLocationNameFromId(location);
                CheckRemoteLocation(name.Substring(0, name.Length - 2), false);
            }
        }

        private IMuseDashItem GetItemFromNetworkItem(NetworkItem item, bool otherPlayersItem, bool locallyObtained)
        {
            var name = _currentSession.Items.GetItemName(item.Item);

            ArchipelagoStatic.ArchLogger.LogDebug("ItemHandler", $"Got Item: {name}({item.Item}). Player {item.Player}, Location {item.Location}, Flags {item.Flags}.");

            if (otherPlayersItem)
            {
                var playerName = _currentSession.Players.GetPlayerAlias(item.Player);
                if (string.IsNullOrEmpty(playerName))
                    playerName = "Unknown Player"; //Catch all for certain cases, like cheated items

                name = name ?? $"Unknown Item: {item.Item}";
                ArchipelagoStatic.ArchLogger.LogDebug("ItemHandler", $"External Item: {playerName}, {name}");
                return new ExternalItem(item.Item, name, playerName) { Item = item };
            }

            if (ArchipelagoStatic.SessionHandler.BattleHandler.EnqueueIfBattleItem(item, out var createFiller))
            {
                if (!createFiller || !locallyObtained)
                    return null;
                return new FillerItem() { Item = item };
            }

            if (name == music_sheet_item_name)
                return new MusicSheetItem() { Item = item };

            //Try to match by item id first
            if (ArchipelagoStatic.AlbumDatabase.TryGetSongFromItemId(item.Item, out var itemInfo))
            {
                ArchipelagoStatic.ArchLogger.LogDebug("ItemHandler", "Matched item id");

                if (item.Location == -2)
                    StarterSongUIDs.Add(itemInfo.uid);

                return new SongItem(itemInfo) { Item = item };
            }

            //Then match by name
            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(name, out var singularInfo))
                return new SongItem(singularInfo) { Item = item };

            if (ArchipelagoStatic.AlbumDatabase.TryGetAlbum(name, out var album))
                return new AlbumItem(name, album) { Item = item };

            if (name != "Nothing" && name != "Victory")
                ArchipelagoStatic.ArchLogger.Warning("ItemHandler", $"Unknown Item was given: {name}");

            return null;
        }

        public void AddMusicSheet(int number = 1)
        {
            CurrentNumberOfMusicSheets += number;

            if (CurrentNumberOfMusicSheets < NumberOfMusicSheetsToWin || UnlockedSongUids.Contains(GoalSong.uid))
            {
                MusicTagManager.instance.RefreshDBDisplayMusics();
                if (ArchipelagoStatic.SongSelectPanel)
                    ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
                return;
            }

            ArchipelagoStatic.ArchLogger.LogDebug("ItemHandler", "Force unlocking the goal song as we reached the goal.");

            UnlockSong(GoalSong);

            MusicTagManager.instance.RefreshDBDisplayMusics();
            if (ArchipelagoStatic.SongSelectPanel)
                ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
        }

        public MusicInfo GetRandomUnfinishedSong()
        {
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

        public void PickNextSongShownMode()
        {
            //Try to fix jank button selection logic
            EventSystem.current.SetSelectedGameObject(null);

            ArchipelagoStatic.ArchLogger.LogDebug("ItemHandler", "Choosing next song shown mode.");
            var nextMode = (ShownSongMode)(((int)HiddenSongMode + 1) % ((int)ShownSongMode.AllInLogic + 1));
            SetVisibilityOfAllSongs(nextMode);
        }

        private void SetVisibilityOfAllSongs(ShownSongMode mode)
        {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            ArchipelagoStatic.ArchLogger.Log("ItemHandler", $"Visibility being set to {mode}");
            HiddenSongMode = mode;
            ArchipelagoHelpers.SelectNextAvailableSong();

            var hintedSongs = mode == ShownSongMode.Hinted ? ArchipelagoStatic.SessionHandler.HintHandler.GetHintedSongs() : new HashSet<string>();

            foreach (var song in list)
            {
                if (song.uid == AlbumDatabase.RANDOM_PANEL_UID)
                    continue;

                if (!SongsInLogic.Contains(song.uid))
                {
                    AddHide(song, false);
                    GlobalDataBase.dbMusicTag.RemoveCollection(song);
                    continue;
                }

                //Goal should always be visible
                if (GoalSong?.uid == song.uid)
                {
                    GlobalDataBase.dbMusicTag.RemoveHide(song);
                    GlobalDataBase.dbMusicTag.AddCollection(song);
                    continue;
                }

                switch (mode)
                {
                    case ShownSongMode.AllInLogic:
                        if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                            GlobalDataBase.dbMusicTag.RemoveHide(song);

                        if (UnlockedSongUids.Contains(song.uid) && !CompletedSongUids.Contains(song.uid) && !GlobalDataBase.dbMusicTag.ContainsCollection(song))
                            GlobalDataBase.dbMusicTag.AddCollection(song);
                        break;

                    case ShownSongMode.Unlocks:
                        if (!UnlockedSongUids.Contains(song.uid))
                        {
                            AddHide(song, false);

                            if (GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.RemoveCollection(song);
                        }
                        else
                        {
                            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                                GlobalDataBase.dbMusicTag.RemoveHide(song);

                            if (!CompletedSongUids.Contains(song.uid) && !GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.AddCollection(song);
                        }
                        break;

                    case ShownSongMode.Unplayed:
                        if (!UnlockedSongUids.Contains(song.uid) || CompletedSongUids.Contains(song.uid))
                        {
                            AddHide(song, false);

                            if (GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.RemoveCollection(song);
                        }
                        else
                        {
                            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                                GlobalDataBase.dbMusicTag.RemoveHide(song);

                            if (!GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.AddCollection(song);
                        }
                        break;

                    case ShownSongMode.Hinted:
                        var name = ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(song);
                        if (!hintedSongs.Contains(name + "-0") && !hintedSongs.Contains(name + "-1") || CompletedSongUids.Contains(song.uid))
                        {
                            AddHide(song, false);

                            if (GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.RemoveCollection(song);
                        }
                        else
                        {
                            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                                GlobalDataBase.dbMusicTag.RemoveHide(song);

                            if (!GlobalDataBase.dbMusicTag.ContainsCollection(song))
                                GlobalDataBase.dbMusicTag.AddCollection(song);
                        }
                        break;
                }
            }

            MusicTagManager.instance.RefreshDBDisplayMusics();
            if (ArchipelagoStatic.SongSelectPanel)
                ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
        }

        private void AddHide(MusicInfo song, bool outsideSongSelect)
        {
            if (GlobalDataBase.dbMusicTag.ContainsHide(song))
                return;

            if (outsideSongSelect)
                GlobalDataBase.dbMusicTag.m_HideList.Add(song.uid);
            else
                GlobalDataBase.dbMusicTag.AddHide(song);
            GlobalDataBase.dbMusicTag.RemoveShowMusicUid(song);
        }

        public void UnlockSong(MusicInfo song)
        {
            UnlockedSongUids.Add(song.uid);
            if (HiddenSongMode == ShownSongMode.AllInLogic || !SongsInLogic.Contains(song.uid))
                return;

            if (HiddenSongMode == ShownSongMode.Unlocks || !CompletedSongUids.Contains(song.uid))
                GlobalDataBase.dbMusicTag.RemoveHide(song);

            if (!CompletedSongUids.Contains(song.uid))
                GlobalDataBase.dbMusicTag.AddCollection(song);
        }

#region Locations

        public void CheckLocation(string uid, string locationName)
        {
            if (CompletedSongUids.Contains(uid))
            {
                ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Location already checked for: {locationName}");
                return;
            }

            if (!SongsInLogic.Contains(uid) && GoalSong.uid != uid)
            {
                ArchipelagoStatic.ArchLogger.Warning("CheckLocations", $"Tried to check location that wasn't in logic: {locationName}");
                return;
            }

            ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Checking location for: {locationName}");
            Task.Run(async () => await CheckLocationsInner(uid, locationName));
        }

        private async Task CheckLocationsInner(string uid, string locationName)
        {
            try
            {
                CompletedSongUids.Add(uid);

                if (GoalSong != null && GoalSong.uid == uid)
                {
                    ArchipelagoStatic.ArchLogger.Log("ItemHandler", "Victory achieved, enqueueing visuals for next available time.");

                    //Todo: This maybe should be priority?
                    Unlocker.AddItem(new VictoryItem(_currentSession.Players.GetPlayerAlias(_currentPlayerSlot), uid));
                    Unlocker.PrioritiseItems(new[] { new NetworkItem() });

                    var statusUpdatePacket = new StatusUpdatePacket
                    {
                        Status = ArchipelagoClientState.ClientGoal
                    };

                    await _currentSession.Socket.SendPacketAsync(statusUpdatePacket);
                    return;
                }

                var locationsToCheck = new List<long>();

                var location1 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-0");
                if (location1 != -1 && _currentSession.Locations.AllLocations.Contains(location1))
                {
                    if (!_currentSession.Locations.AllLocationsChecked.Contains(location1))
                        locationsToCheck.Add(location1);
                }

                var location2 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", locationName + "-1");
                if (location2 != -1 && _currentSession.Locations.AllLocations.Contains(location2))
                {
                    if (!_currentSession.Locations.AllLocationsChecked.Contains(location2))
                        locationsToCheck.Add(location2);
                }

                if (locationsToCheck.Count <= 0)
                {
                    ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Failed to find any checks for {locationName}.");
                    //This is a workaround for older generated worlds
                    if (!ArchipelagoStatic.AlbumDatabase.TryGetOldName(locationName, out var oldName))
                        return;

                    ArchipelagoStatic.ArchLogger.Log("CheckLocations", $"Location had an older name checking for {oldName}");
                    await CheckLocationsInner(uid, oldName);
                    return;
                }

                var locationsArray = locationsToCheck.ToArray();

                //Complete the location check, but also scout to ensure we get the items we are sending to other players.
                await _currentSession.Locations.CompleteLocationChecksAsync(locationsArray);
                var items = await _currentSession.Locations.ScoutLocationsAsync(false, locationsArray);

                ArchipelagoStatic.ArchLogger.LogDebug("CheckLocations", "Received Items Packet.");
                CheckRemoteLocation(locationName, true);
                foreach (var networkItem in items.Locations)
                {
                    var item = GetItemFromNetworkItem(networkItem, networkItem.Player != _currentPlayerSlot, true);
                    if (item != null)
                        Unlocker.AddItem(item);
                }

                Unlocker.PrioritiseItems(items.Locations);
            }
            catch (Exception e)
            {
                ArchipelagoStatic.ArchLogger.Error("Check Location", e);
            }
        }

        private void CheckRemoteLocation(string locationName, bool force)
        {
            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(locationName, out var singularInfo))
            {
                //If a person collects 1 of 2 locations, we don't want to check it.
                if (!force && !IsSongFullyCompleted(locationName))
                    return;

                //Check to see if the song is favourited, and remove if it is
                if (GlobalDataBase.dbMusicTag.ContainsCollection(singularInfo))
                    GlobalDataBase.dbMusicTag.m_CollectionList.Remove(singularInfo.uid); //Due to internal changes, the normal method will crash outside of song select.

                if (HiddenSongMode == ShownSongMode.Unplayed)
                    AddHide(singularInfo, true);

                CompletedSongUids.Add(singularInfo.uid);
                return;
            }

            ArchipelagoStatic.ArchLogger.Warning("HandleLocationChecked", $"Unknown Location: {locationName}");
        }

        private bool IsSongFullyCompleted(string songKey)
        {
            var location1 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", songKey + "-0");
            if (location1 != -1 && _currentSession.Locations.AllLocations.Contains(location1))
            {
                if (!_currentSession.Locations.AllLocationsChecked.Contains(location1))
                    return false;
            }

            var location2 = _currentSession.Locations.GetLocationIdFromName("Muse Dash", songKey + "-1");
            if (location2 != -1 && _currentSession.Locations.AllLocations.Contains(location2))
            {
                if (!_currentSession.Locations.AllLocationsChecked.Contains(location2))
                    return false;
            }

            return true;
        }

#endregion
    }
}