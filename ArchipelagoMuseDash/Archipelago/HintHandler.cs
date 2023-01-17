using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoMuseDash.Helpers;
using Assets.Scripts.Database;
using Assets.Scripts.UI.Controls;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ArchipelagoMuseDash.Archipelago {
    public class HintHandler {
        public const string ArchipelagoDialogueTitle = "Archipelago Hint";

        ArchipelagoSession _currentSession;
        int _currentPlayerSlot;

        Dictionary<string, Hint> _locationHints = new Dictionary<string, Hint>();
        Dictionary<string, Hint> _itemsHints = new Dictionary<string, Hint>();

        GameObject _hintHandler;
        Button _hintHandlerButton;
        GameObject _knownHintText;
        Text _uglyHintText;
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
            if (!ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlStage"))
                return;

            if (_hintHandler == null) {
                AddHintButton();
                AddHintBox();
                _forceUpdate = true;
            }

            var currentlySelectedSong = GlobalDataBase.dbMusicTag.m_CurSelectedMusicInfo;
            if (currentlySelectedSong?.uid == _lastMusic?.uid && !_forceUpdate)
                return;

            _forceUpdate = false;
            _lastMusic = currentlySelectedSong;

            var isSongRandomSelect = currentlySelectedSong == null || currentlySelectedSong.uid == "?";

            if (!isSongRandomSelect && _currentSession.RoomState.HintCostPercentage <= 100) {
                var itemHandler = ArchipelagoStatic.SessionHandler.ItemHandler;
                if (itemHandler.SongsInLogic.Contains(currentlySelectedSong.uid) && !itemHandler.UnlockedSongUids.Contains(currentlySelectedSong.uid))
                    _hintHandlerButton.interactable = true; //Todo: Check to see if all hints are exhausted
                else
                    _hintHandlerButton.interactable = false;
            }
            else
                _hintHandlerButton.interactable = false;

            if (!isSongRandomSelect && ArchipelagoStatic.SessionHandler.HintHandler.TryGetSongHints(currentlySelectedSong, out var hintStr)) {
                ArchipelagoStatic.ArchLogger.Log("Hinting", hintStr);
                _knownHintText.SetActive(true);
                _uglyHintText.text = hintStr;
            }
            else
                _knownHintText.SetActive(false);
        }

        public void MainSceneLoaded() {
            _hintHandler = null;
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

        public void AddHintButton() {
            //Todo: This needs a bit of cleaning up. Maybe split into other methods to make it easier to follow.

            var pnlStage = ArchipelagoStatic.SongSelectPanel;
            var likeButton = pnlStage.gameObject.GetComponentInChildren<StageLikeToggle>();

            //The HideSongDialogue has the button we want, and it should be available at this time.
            var yesButton = ArchipelagoStatic.HideSongDialogue.m_YesButton;
            var yesButtonImage = yesButton.GetComponent<Image>();
            var yesText = yesButton.transform.GetChild(0);
            var yesTextComp = yesText.GetComponent<Text>();

            _hintHandler = new GameObject("ArchipelagoHintButton");
            _hintHandler.transform.SetParent(likeButton.transform.parent, false);

            _hintHandlerButton = _hintHandler.AddComponent<Button>();
            _hintHandlerButton.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(new Action(ShowHintPopup)));
            _hintHandlerButton.transition = yesButton.transition;

            var colours = yesButton.colors;
            colours.disabledColor = new UnityEngine.Color(colours.disabledColor.r * 0.8f, colours.disabledColor.g * 0.8f, colours.disabledColor.b * 0.8f, 1);
            _hintHandlerButton.colors = colours;
            _hintHandlerButton.interactable = false;

            var image = _hintHandler.AddComponent<Image>();
            image.sprite = yesButtonImage.sprite;
            image.type = yesButtonImage.type;
            _hintHandlerButton.targetGraphic = image;

            var imageTransform = _hintHandler.GetComponent<RectTransform>();
            var yesButtonTransform = yesButton.gameObject.GetComponent<RectTransform>();

            imageTransform.sizeDelta = yesButtonTransform.sizeDelta;
            imageTransform.anchorMax = imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageTransform.anchoredPosition = new Vector2(220, 65);
            imageTransform.pivot = new Vector2(0, 0.5f);

            var hintButtonText = new GameObject("Text");
            hintButtonText.transform.SetParent(_hintHandler.transform, false);
            var hintTextComp = hintButtonText.AddComponent<Text>();

            AssetHelpers.CopyTextVariables(yesTextComp, hintTextComp);
            hintTextComp.text = "Get Hint";

            var rectTransfrom = hintButtonText.GetComponent<RectTransform>();
            var yesRectTransform = yesText.GetComponent<RectTransform>();

            rectTransfrom.anchorMin = yesRectTransform.anchorMin;
            rectTransfrom.anchorMax = yesRectTransform.anchorMax;
            rectTransfrom.pivot = yesRectTransform.pivot;
            rectTransfrom.sizeDelta = yesRectTransform.sizeDelta;
        }

        public void AddHintBox() {
            //Todo: This needs a bit of cleaning up. Maybe split into other methods to make it easier to follow.

            var pnlStage = ArchipelagoStatic.SongSelectPanel;
            var likeButton = pnlStage.gameObject.GetComponentInChildren<StageLikeToggle>();

            //The HideSongDialogue has the button we want, and it should be available at this time.
            var noButton = ArchipelagoStatic.HideSongDialogue.m_NoButton;
            var noButtonImage = noButton.GetComponent<Image>();
            var noText = noButton.transform.GetChild(0);
            var noTextComp = noText.GetComponent<Text>();

            _knownHintText = new GameObject("Known Hint Text");
            _knownHintText.transform.SetParent(likeButton.transform.parent, false);

            var hintBackgroundImage = _knownHintText.AddComponent<Image>();
            hintBackgroundImage.sprite = noButtonImage.sprite;
            hintBackgroundImage.type = noButtonImage.type;

            var hintTransform = _knownHintText.GetComponent<RectTransform>();

            hintTransform.anchorMax = hintTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hintTransform.anchoredPosition = new Vector2(400, 160);
            hintTransform.pivot = new Vector2(0, 0.5f);
            hintTransform.sizeDelta = new Vector2(500, 100);

            var hintText = new GameObject();
            hintText.transform.SetParent(_knownHintText.transform, false);

            _uglyHintText = hintText.AddComponent<Text>();
            AssetHelpers.CopyTextVariables(noTextComp, _uglyHintText);
            _uglyHintText.fontSize = 22;
            _uglyHintText.resizeTextForBestFit = true;
            _uglyHintText.resizeTextMaxSize = 22;
            _uglyHintText.resizeTextMinSize = 12;
            _uglyHintText.verticalOverflow = VerticalWrapMode.Truncate;

            var hintTextRect = hintText.GetComponent<RectTransform>();
            hintTextRect.anchorMin = new Vector2(0.1f, 0.1f);
            hintTextRect.anchorMax = new Vector2(0.9f, 0.9f);
            hintTextRect.sizeDelta = Vector2.zero; //Resets the size back to the anchors
        }
    }
}
