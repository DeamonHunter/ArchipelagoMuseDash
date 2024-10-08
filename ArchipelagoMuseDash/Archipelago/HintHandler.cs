﻿using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.UI.Controls;
using UnityEngine;

namespace ArchipelagoMuseDash.Archipelago;

public class HintHandler {
    public const string ARCHIPELAGO_DIALOGUE_TITLE = "Archipelago Hint";

    private readonly ArchipelagoSession _currentSession;
    private readonly int _currentPlayerSlot;

    private readonly Dictionary<long, Hint> _musicSheetHints = new();
    private readonly Dictionary<string, Hint> _locationHints = new();
    private readonly Dictionary<string, Hint> _itemsHints = new();

    private MusicInfo _lastMusic;
    private bool _forceUpdate;

    public HintHandler(ArchipelagoSession session, int playerSlot) {
        _currentSession = session;
        _currentPlayerSlot = playerSlot;
    }

    public void Setup() {
        _locationHints.Clear();
        _itemsHints.Clear();
        _musicSheetHints.Clear();
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
        var songTitleComp = ArchipelagoStatic.SessionHandler.SongSelectAdditions.SongTitleComp;

        _forceUpdate = false;
        _lastMusic = currentlySelectedSong;

        var isSongRandomSelect = currentlySelectedSong == null || currentlySelectedSong.uid == AlbumDatabase.RANDOM_PANEL_UID;

        songTitleComp.gameObject.SetActive(!isSongRandomSelect);
        songTitleComp.text = isSongRandomSelect ? "" : ArchipelagoStatic.SongNameChanger.GetSongName(currentlySelectedSong);
        ArchipelagoStatic.ArchLogger.LogDebug("Hint Handler", songTitleComp.text);

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
            ArchipelagoStatic.ArchLogger.LogDebug("Hinting", hintStr);
            hintText.SetActive(true);
            hintTextComp.text = hintStr;
        }
        else
            hintText.SetActive(false);
    }

    public void ShowHintPopup() {
        var song = GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo;
        if (song == null || song.uid == AlbumDatabase.RANDOM_PANEL_UID) {
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
            ArchipelagoStatic.HideSongDialogue.m_Title.text = ARCHIPELAGO_DIALOGUE_TITLE;
            ArchipelagoStatic.HideSongDialogue.m_Msg.text = $"Are you sure want to hint the song {songName}?";
        }
        else {
            //Todo: Fix up when HintCost is properly calculated at the start
            int neededHintPoints;
            if (_currentSession.RoomState.HintCost <= 0)
                neededHintPoints = Mathf.FloorToInt(_currentSession.Locations.AllLocations.Count * (_currentSession.RoomState.HintCostPercentage / 100f));
            else
                neededHintPoints = _currentSession.RoomState.HintCost;

            var hintPoints = _currentSession.RoomState.HintPoints;
            if (hintPoints >= neededHintPoints) {
                ArchipelagoStatic.HideSongDialogue.Show();
                ArchipelagoStatic.HideSongDialogue.m_Title.text = ARCHIPELAGO_DIALOGUE_TITLE;
                ArchipelagoStatic.HideSongDialogue.m_Msg.text = $"Are you sure want to hint the song {songName}?\nYou have {hintPoints} Hint Points and {neededHintPoints} points for a hint.\nHint Points may not be correct.";
            }
            else
                ShowText.ShowInfo($"You do not have enough points to hint this song. You have {hintPoints} and need {neededHintPoints}.");
        }
    }

    public void HintSong(MusicInfo song) {
        if (song == null)
            throw new ArgumentException("Tried to get hint on null MusicInfo.");

        //There is no specific packet or function to send for costed hints. So we need to send a chat message for the player
        if (song.uid == ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid)
            _currentSession.Socket.SendPacketAsync(new SayPacket { Text = "!hint Music Sheet" });
        else
            _currentSession.Socket.SendPacketAsync(new SayPacket { Text = $"!hint {ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(song)}" });
    }

    private void HandleHints(Hint[] hints) {
        ArchipelagoStatic.ArchLogger.Log("Hinting", "Got new hints.");
        _forceUpdate = true;

        foreach (var hint in hints) {
            var itemName = _currentSession.Items.GetItemName(hint.ItemId, _currentSession.Players.GetPlayerInfo(hint.ReceivingPlayer).Game);
            if (itemName == "Music Sheet" && hint.ReceivingPlayer == _currentPlayerSlot) {
                if (hint.Found)
                    _musicSheetHints.Remove(hint.LocationId);
                else
                    _musicSheetHints[hint.LocationId] = hint;
            }

            if (hint.FindingPlayer == _currentPlayerSlot) {
                var locationName = _currentSession.Locations.GetLocationNameFromId(hint.LocationId, _currentSession.Players.GetPlayerInfo(hint.FindingPlayer).Game);

                ArchipelagoStatic.ArchLogger.LogDebug("Hinting", $"Got Hint for location: {locationName}, Finding Player, {hint.Found}");
                if (hint.Found)
                    _locationHints.Remove(locationName);
                else
                    _locationHints[locationName] = hint;
            }

            if (hint.ReceivingPlayer != _currentPlayerSlot)
                continue;

            ArchipelagoStatic.ArchLogger.LogDebug("Hinting", $"Got Hint for location: {itemName}, Receiving Player, {hint.Found}");

            if (hint.Found)
                _itemsHints.Remove(itemName);
            else
                _itemsHints[itemName] = hint;
        }
    }

    public HashSet<string> GetHintedSongs() => _locationHints.Keys.ToHashSet();
    public bool HasLocationHint(string uid) {
        var itemName = ArchipelagoStatic.AlbumDatabase.GetItemNameFromUid(uid);
        return _locationHints.ContainsKey(itemName + "-0") || _locationHints.ContainsKey(itemName + "-1");
    }

    private bool TryGetSongHints(MusicInfo info, out string hint) {
        if (info == null)
            throw new ArgumentException("Tried to get hint on null MusicInfo.");

        if (ArchipelagoStatic.SessionHandler.ItemHandler.CompletedSongUids.Contains(info.uid)) {
            hint = null;
            return false;
        }

        var sb = new StringBuilder();

        if (info.uid == ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong?.uid) {
            if (_musicSheetHints.Count <= 0) {
                hint = null;
                return false;
            }

            sb.AppendLine($"{_musicSheetHints.Count} known locations for music sheets: ");
            foreach (var musicSheetHint in _musicSheetHints.Values) {
                if (musicSheetHint.Found)
                    continue;

                var locationName = _currentSession.Locations.GetLocationNameFromId(musicSheetHint.LocationId, _currentSession.Players.GetPlayerInfo(musicSheetHint.FindingPlayer).Game);
                GetHintFromLocationName(musicSheetHint, sb, locationName);
                if (locationName.Length > 2 && ArchipelagoStatic.AlbumDatabase.TryGetOldName(locationName[..^2], out var oldName))
                    GetHintFromLocationName(musicSheetHint, sb, oldName);
            }

            hint = sb.ToString();
            return true;
        }

        var itemName = ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(info);
        GetHintFromItemName(itemName, sb);
        if (itemName.Length > 2 && ArchipelagoStatic.AlbumDatabase.TryGetOldName(itemName[..^2], out var oldItemName))
            GetHintFromItemName(oldItemName, sb);

        hint = sb.ToString();
        return hint.Length > 0;
    }


    private void GetHintFromLocationName(Hint musicSheetHint, StringBuilder sb, string locationName) {
        if (musicSheetHint.FindingPlayer == _currentPlayerSlot) {
            sb.AppendLine($"{locationName.Substring(0, locationName.Length - 2)}");
            //Additional check, in case hints don't update
            if (ArchipelagoStatic.AlbumDatabase.TryGetMusicInfo(locationName, out var otherMusic)
                && ArchipelagoStatic.SessionHandler.ItemHandler.CompletedSongUids.Contains(otherMusic.uid))
                return;

            //Local Item
            sb.AppendLine(locationName);
        }
        else //Remote Item
            sb.AppendLine($"By {_currentSession.Players.GetPlayerAlias(musicSheetHint.FindingPlayer)} at {locationName}");
    }

    private void GetHintFromItemName(string itemName, StringBuilder sb) {

        if (_itemsHints.TryGetValue(itemName, out var locatedHint)) {
            var locationName = _currentSession.Locations.GetLocationNameFromId(locatedHint.LocationId, _currentSession.Players.GetPlayerInfo(locatedHint.FindingPlayer).Game);
            if (locatedHint.FindingPlayer == _currentPlayerSlot) //Local Item
                sb.Append($"To be found at {locationName[..^2]}");
            else //Remote Item
                sb.Append($"To be found by {_currentSession.Players.GetPlayerAlias(locatedHint.FindingPlayer)} at {locationName}");
        }

        var addedItemHint = false;

        if (_locationHints.TryGetValue(itemName + "-0", out var itemHint1)) {
            var item = _currentSession.Items.GetItemName(itemHint1.ItemId, _currentSession.Players.GetPlayerInfo(itemHint1.ReceivingPlayer).Game);
            if (itemHint1.ReceivingPlayer != _currentPlayerSlot)
                item += $"[{_currentSession.Players.GetPlayerAlias(itemHint1.ReceivingPlayer)}]";

            addedItemHint = true;
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append($"Has: {item}");
        }

        if (_locationHints.TryGetValue(itemName + "-1", out var itemHint2)) {
            var item = _currentSession.Items.GetItemName(itemHint2.ItemId, _currentSession.Players.GetPlayerInfo(itemHint2.ReceivingPlayer).Game);
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
    }
}