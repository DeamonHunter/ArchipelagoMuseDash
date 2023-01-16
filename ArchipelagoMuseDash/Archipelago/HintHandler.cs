using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using Assets.Scripts.Database;

namespace ArchipelagoMuseDash.Archipelago {
    public class HintHandler {
        ArchipelagoSession _currentSession;
        int _currentPlayerSlot;

        Dictionary<string, KnownHint> _locationHints = new Dictionary<string, KnownHint>();
        Dictionary<string, KnownHint> _itemsHints = new Dictionary<string, KnownHint>();

        public HintHandler(ArchipelagoSession session, int playerSlot) {
            _currentSession = session;
            _currentPlayerSlot = playerSlot;
        }

        public void Setup() {
            var hints = _currentSession.DataStorage.GetHints(_currentPlayerSlot);

            _locationHints.Clear();
            _itemsHints.Clear();

            foreach (var hint in hints) {
                if (hint.Found)
                    continue;

                //Is the hint relevant to us?
                if (hint.ReceivingPlayer != _currentPlayerSlot && hint.FindingPlayer != _currentPlayerSlot)
                    continue;

                if (hint.FindingPlayer == _currentPlayerSlot) {
                    var newHint = new KnownHint {
                        ItemID = hint.ItemId,
                        LocationID = hint.LocationId,
                        PlayerSlot = hint.ReceivingPlayer
                    };

                    _locationHints.Add(_currentSession.Locations.GetLocationNameFromId(hint.LocationId), newHint);
                }

                if (hint.ReceivingPlayer == _currentPlayerSlot) {
                    var newHint = new KnownHint {
                        ItemID = hint.ItemId,
                        LocationID = hint.LocationId,
                        PlayerSlot = hint.FindingPlayer
                    };

                    _locationHints.Add(_currentSession.Items.GetItemName(hint.ItemId), newHint);
                }
            }
        }

        public void HintSong(MusicInfo song) {
            _currentSession.Socket.SendPacketAsync(new SayPacket { Text = $"!hint {ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(song)}" });
        }

        public void HandleHintMessage(HintItemSendLogMessage hintMessage) {
            if (hintMessage.ReceivingPlayerSlot != _currentPlayerSlot && hintMessage.SendingPlayerSlot != _currentPlayerSlot)
                return;

            if (hintMessage.SendingPlayerSlot == _currentPlayerSlot) {
                if (!hintMessage.IsFound) {
                    var newHint = new KnownHint {
                        ItemID = hintMessage.Item.Item,
                        LocationID = hintMessage.Item.Location,
                        PlayerSlot = hintMessage.ReceivingPlayerSlot
                    };

                    //Todo: Do we want to handle showing multiple locations
                    var locationName = _currentSession.Locations.GetLocationNameFromId(hintMessage.Item.Location);
                    if (!_locationHints.ContainsKey(locationName))
                        _locationHints.Add(locationName, newHint);
                }
                else
                    _locationHints.Remove(_currentSession.Locations.GetLocationNameFromId(hintMessage.Item.Location));
            }

            if (hintMessage.ReceivingPlayerSlot == _currentPlayerSlot) {
                if (!hintMessage.IsFound) {
                    var newHint = new KnownHint {
                        ItemID = hintMessage.Item.Item,
                        LocationID = hintMessage.Item.Location,
                        PlayerSlot = hintMessage.SendingPlayerSlot
                    };

                    var itemName = _currentSession.Items.GetItemName(hintMessage.Item.Item);
                    if (!_itemsHints.ContainsKey(itemName))
                        _itemsHints.Add(itemName, newHint);
                }
                else
                    _itemsHints.Remove(_currentSession.Items.GetItemName(hintMessage.Item.Item));
            }
        }

        public bool TryGetSongHints(MusicInfo info, out string locationStr, out string item1Str, out string item2Str) {
            var itemName = ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(info);
            if (_itemsHints.TryGetValue(itemName, out var locatedHint)) {
                //Item is local
                if (locatedHint.PlayerSlot == _currentPlayerSlot) {
                    var locationName = _currentSession.Locations.GetLocationNameFromId(locatedHint.LocationID);
                    locationStr = $"This song is unlocked by playing the song: {locationName.Substring(0, locationName.Length - 2)}";
                }
                else
                    locationStr = $"This song is on {_currentSession.Players.GetPlayerAlias(locatedHint.PlayerSlot)} at {_currentSession.Locations.GetLocationNameFromId(locatedHint.LocationID)}";
            }
            else
                locationStr = null;

            if (_locationHints.TryGetValue(itemName + "-0", out var itemHint1)) {
                item1Str = _currentSession.Items.GetItemName(itemHint1.ItemID);
                if (itemHint1.PlayerSlot != _currentPlayerSlot)
                    item1Str += $"[{_currentSession.Players.GetPlayerAlias(itemHint1.PlayerSlot)}]";
            }
            else
                item1Str = null;

            if (_locationHints.TryGetValue(itemName + "-1", out var itemHint2)) {
                item2Str = _currentSession.Items.GetItemName(itemHint2.ItemID);
                if (itemHint2.PlayerSlot != _currentPlayerSlot)
                    item2Str += $"[{_currentSession.Players.GetPlayerAlias(itemHint2.PlayerSlot)}]";
            }
            else
                item2Str = null;

            return locationStr != null || item1Str != null || item2Str != null;
        }


        private struct KnownHint {
            public long ItemID;
            public long LocationID;
            public int PlayerSlot;
        }
    }
}
