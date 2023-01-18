using System.Collections.Generic;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Assets.Scripts.Database;
using Assets.Scripts.UI.Controls;

namespace ArchipelagoMuseDash.Archipelago {
    public class HintHandler {
        public const string ArchipelagoDialogueTitle = "Archipelago Hint";

        ArchipelagoSession _currentSession;
        int _currentPlayerSlot;

        Dictionary<string, Hint> _locationHints = new Dictionary<string, Hint>();
        Dictionary<string, Hint> _itemsHints = new Dictionary<string, Hint>();

        MusicInfo _lastMusic;
        bool _forceUpdate;

        public HintHandler(ArchipelagoSession session, int playerSlot) {
            _currentSession = session;
            _currentPlayerSlot = playerSlot;
        }

        public void Setup() {
            _locationHints.Clear();
            _itemsHints.Clear();
            _currentSession.DataStorage.TrackHints(HandleHints);
        }

        public void OnUpdate() {
            if (ArchipelagoStatic.SessionHandler.SongSelectAdditions.HintButton == null) {
                _forceUpdate = true;
                return;
            }

            var currentlySelectedSong = GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo;
            if (currentlySelectedSong?.uid == _lastMusic?.uid && !_forceUpdate)
                return;

            var hintButton = ArchipelagoStatic.SessionHandler.SongSelectAdditions.HintButtonComp;

            var hintText = ArchipelagoStatic.SessionHandler.SongSelectAdditions.HintText;
            var hintTextComp = ArchipelagoStatic.SessionHandler.SongSelectAdditions.HintTextComp;

            _forceUpdate = false;
            _lastMusic = currentlySelectedSong;

            var isSongRandomSelect = currentlySelectedSong == null || currentlySelectedSong.uid == "?";

            if (!isSongRandomSelect && _currentSession.RoomState.HintCostPercentage <= 100) {
                var itemHandler = ArchipelagoStatic.SessionHandler.ItemHandler;
                if (itemHandler.SongsInLogic.Contains(currentlySelectedSong.uid) && !itemHandler.UnlockedSongUids.Contains(currentlySelectedSong.uid))
                    hintButton.interactable = true; //Todo: Check to see if all hints are exhausted
                else
                    hintButton.interactable = false;
            }
            else
                hintButton.interactable = false;

            if (!isSongRandomSelect && ArchipelagoStatic.SessionHandler.HintHandler.TryGetSongHints(currentlySelectedSong, out var hintStr)) {
                ArchipelagoStatic.ArchLogger.Log("Hinting", hintStr);
                hintText.SetActive(true);
                hintTextComp.text = hintStr;
            }
            else
                hintText.SetActive(false);
        }

        public void ShowHintPopup() {
            var song = GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo;
            if (song == null || song.uid == "?") {
                ShowText.ShowInfo("Cannot buy a hint for the random button.");
                return;
            }

            if (ArchipelagoStatic.SessionHandler.ItemHandler.UnlockedSongUids.Contains(song.uid)) {
                ShowText.ShowInfo("You have already unlocked this song.");
                return;
            }

            if (_currentSession.RoomState.HintCostPercentage > 100) {
                ShowText.ShowInfo("Hint buying has been disabled.");
                return;
            }

            var songName = ArchipelagoStatic.AlbumDatabase.GetLocalisedSongNameForMusicInfo(song);

            if (_currentSession.RoomState.HintCostPercentage <= 0) {
                //Todo: Save the original message somewhere.
                ArchipelagoStatic.HideSongDialogue.Show();
                ArchipelagoStatic.HideSongDialogue.m_Title.text = ArchipelagoDialogueTitle;
                ArchipelagoStatic.HideSongDialogue.m_Msg.text = $"Are you sure want to hint the song {songName}?";
            }
            else {
                //Todo: Fix up when HintCost is properly calculated at the start
                int neededHintPoints;
                if (_currentSession.RoomState.HintCost <= 0)
                    neededHintPoints = _currentSession.Locations.AllLocations.Count / _currentSession.RoomState.HintCostPercentage;
                else
                    neededHintPoints = _currentSession.RoomState.HintCost;

                var hintPoints = _currentSession.RoomState.HintPoints;
                if (hintPoints >= neededHintPoints) {
                    ArchipelagoStatic.HideSongDialogue.Show();
                    ArchipelagoStatic.HideSongDialogue.m_Title.text = ArchipelagoDialogueTitle;
                    ArchipelagoStatic.HideSongDialogue.m_Msg.text = $"Are you sure want to hint the song {songName}?\nYou have {hintPoints} hint points and {neededHintPoints} points for a hint.";
                }
                else
                    ShowText.ShowInfo($"You do not have enough points to hint this song. You have {hintPoints} and need {neededHintPoints}.");
            }
        }

        public void HintSong(MusicInfo song) {
            //There is no specific packet or function to send for costed hints. So send a chat message for the player
            _currentSession.Socket.SendPacketAsync(new SayPacket { Text = $"!hint {ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(song)}" });
        }

        public void HandleHints(Hint[] hints) {
            ArchipelagoStatic.ArchLogger.Log("Hinting", "Got new hints.");
            _forceUpdate = true;

            foreach (var hint in hints) {
                if (hint.FindingPlayer == _currentPlayerSlot) {
                    var locationName = _currentSession.Locations.GetLocationNameFromId(hint.LocationId);

                    ArchipelagoStatic.ArchLogger.Log("Hinting", $"Got Hint for location: {locationName}, Finding Player, {hint.Found}");
                    if (hint.Found)
                        _locationHints.Remove(locationName);
                    else
                        _locationHints[locationName] = hint;
                }

                if (hint.ReceivingPlayer == _currentPlayerSlot) {
                    var itemName = _currentSession.Items.GetItemName(hint.ItemId);

                    ArchipelagoStatic.ArchLogger.Log("Hinting", $"Got Hint for location: {itemName}, Recieving Player, {hint.Found}");

                    if (hint.Found)
                        _itemsHints.Remove(itemName);
                    else
                        _itemsHints[itemName] = hint;
                }
            }
        }

        public bool TryGetSongHints(MusicInfo info, out string hint) {
            var sb = new StringBuilder();

            var itemName = ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(info);
            if (_itemsHints.TryGetValue(itemName, out var locatedHint)) {
                //Item is local
                if (locatedHint.FindingPlayer == _currentPlayerSlot) {
                    var locationName = _currentSession.Locations.GetLocationNameFromId(locatedHint.LocationId);
                    sb.Append($"To be found at {locationName.Substring(0, locationName.Length - 2)}");
                }
                else
                    sb.Append($"To be found by {_currentSession.Players.GetPlayerAlias(locatedHint.FindingPlayer)} at {_currentSession.Locations.GetLocationNameFromId(locatedHint.LocationId)}");
            }

            bool addedItemHint = false;

            if (_locationHints.TryGetValue(itemName + "-0", out var itemHint1)) {
                var item = _currentSession.Items.GetItemName(itemHint1.ItemId);
                if (itemHint1.ReceivingPlayer != _currentPlayerSlot)
                    item += $"[{_currentSession.Players.GetPlayerAlias(itemHint1.ReceivingPlayer)}]";

                addedItemHint = true;
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append($"Has: {item}");
            }

            if (_locationHints.TryGetValue(itemName + "-1", out var itemHint2)) {
                var item = _currentSession.Items.GetItemName(itemHint2.ItemId);
                if (itemHint2.ReceivingPlayer != _currentPlayerSlot)
                    item += $"[{_currentSession.Players.GetPlayerAlias(itemHint2.ReceivingPlayer)}]";

                if (addedItemHint)
                    sb.Append($", {item}");
                else {
                    if (sb.Length > 0)
                        sb.AppendLine();
                    sb.Append($"Has: {item}");
                }
            }

            hint = sb.ToString();
            return hint.Length > 0;
        }
    }
}
