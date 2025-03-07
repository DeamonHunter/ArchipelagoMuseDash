using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago.Items;
using ArchipelagoMuseDash.Helpers;
using ArchipelagoMuseDash.Patches;
using Il2Cpp;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using UnityEngine;

namespace ArchipelagoMuseDash.Archipelago;

/// <summary>
///     Handles Item Unlocks
/// </summary>
public class ItemUnlockHandler {
    private const float on_show_stage_select_delay = 0.75f;
    private readonly ItemHandler _handler;
    private readonly Queue<IMuseDashItem> _enqueuedItems = new();
    //private readonly HashSet<NetworkItem> _knownItems = new HashSet<NetworkItem>();
    private readonly HashSet<(long, long)> _knownReceivedLocations = new();
    private readonly HashSet<long> _knownStartingSongs = new();

    private IMuseDashItem _unlockingItem;
    private bool _hasUnlockedItem;
    private float _itemGiveDelay;
    private int _currentItemCount;

    public ItemUnlockHandler(ItemHandler handler) {
        _handler = handler;
    }

    public void AddItem(IMuseDashItem item) {
        lock (_enqueuedItems) {
            //Enqueued items don't have a network item.
            if (item is VictoryItem) {
                _enqueuedItems.Enqueue(item);
                return;
            }
            
            if (_knownStartingSongs.Contains(item.Item.ItemId) || LocationIsKnown(item.Item)) {
                ArchipelagoStatic.ArchLogger.LogDebug("Item Unlock", $"Known Duplicate: {item.Item.ItemId}/{item.Item.LocationId}");
                return;
            }

            if (item.Item.LocationId == -2 && item is not MusicSheetItem)
                _knownStartingSongs.Add(item.Item.ItemId);
            if (item.Item.ItemId >= 0)
                _knownReceivedLocations.Add((item.Item.Player, item.Item.LocationId));
            _enqueuedItems.Enqueue(item);
        }
    }

    public void PrioritiseItems(Dictionary<long, ScoutedItemInfo> items) {
        lock (_enqueuedItems) {
            var itemsWithoutDuplicates = new List<IMuseDashItem>();
            var itemsWithDuplicates = new List<IMuseDashItem>();

            while (_enqueuedItems.Count > 0) {
                var enqueuedItem = _enqueuedItems.Dequeue();
                var hasDuplicate = items?.Any(item => ArchipelagoHelpers.IsItemDuplicate(item.Value, enqueuedItem.Item)) ?? false;
                if (hasDuplicate)
                    itemsWithDuplicates.Add(enqueuedItem);
                else
                    itemsWithoutDuplicates.Add(enqueuedItem);
            }

            //This reorders duplicate items such that they show first.
            //Duplicate items happens in one (normal) case, which is items a player has gotten
            foreach (var item in itemsWithDuplicates)
                _enqueuedItems.Enqueue(item);

            foreach (var item in itemsWithoutDuplicates)
                _enqueuedItems.Enqueue(item);
        }
    }

    public void UnlockAllItems() {
        lock (_enqueuedItems) {
            while (_enqueuedItems.Count > 0) {
                var itemToUnlock = _enqueuedItems.Dequeue();
                itemToUnlock.UnlockItem(_handler, true);
            }
        }

        //Force a refresh of the song select and relevant classes, so new songs should show.
        MusicTagManager.instance.RefreshDBDisplayMusics();
        if (ArchipelagoStatic.SongSelectPanel)
            ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
    }

    private void ShowItem(IMuseDashItem item) {
        if (_unlockingItem != null)
            throw new Exception("Tried to unlock an item while one was already unlocking.");

        if (item == null) {
            ArchipelagoStatic.ArchLogger.Log("ItemHandler", "GOT A NULL ITEM. PLEASE REPORT THIS.");
            return;
        }

        if (item is MusicSheetItem) {
            var handler = ArchipelagoStatic.SessionHandler.ItemHandler;

            if (handler.CurrentNumberOfMusicSheets + 1 >= handler.NumberOfMusicSheetsToWin
                && !handler.UnlockedSongUids.Contains(handler.GoalSong.uid)) {
                handler.AddMusicSheet();
                item = new SongItem(handler.GoalSong);
            }
        }

        _unlockingItem = item;
        _hasUnlockedItem = false;

        var data = new Data();
        // ReSharper disable once InvokeAsExtensionMethod
        VariableUtils.SetResult(data["uid"], _unlockingItem.UnlockSongUid);
        ArchipelagoStatic.UnlockStagePanel.UnlockNewSong(data.Cast<IData>());
    }

    private void ShowCompressedItem() {
        if (_unlockingItem != null)
            throw new Exception("Tried to unlock an item while one was already unlocking.");

        ArchipelagoStatic.ArchLogger.Log("ItemUnlocks", "Too many items. Compressing.");

        var itemList = new List<IMuseDashItem>();
        lock (_enqueuedItems) {
            while (_enqueuedItems.TryDequeue(out var item))
                itemList.Add(item);
        }

        _unlockingItem = new CompressedItems(itemList);
        _hasUnlockedItem = false;

        var data = new Data();
        // ReSharper disable once InvokeAsExtensionMethod
        VariableUtils.SetResult(data["uid"], _unlockingItem.UnlockSongUid);
        ArchipelagoStatic.UnlockStagePanel.UnlockNewSong(data.Cast<IData>());
    }

    public void OnUpdate() {
        //We only want to show items when Stage Select is the enabled display. TODO: Does options disable the stage.
        if (ArchipelagoStatic.CurrentScene != "UISystem_PC" || !ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlStage")) {
            _itemGiveDelay = on_show_stage_select_delay;
            return;
        }

        if (ArchipelagoStatic.UnlockStagePanel == null || ArchipelagoStatic.UnlockStagePanel.gameObject.activeInHierarchy || _unlockingItem != null)
            return;

        _itemGiveDelay -= Time.unscaledDeltaTime;
        if (_itemGiveDelay > 0)
            return;

        lock (_enqueuedItems) {
            if (_enqueuedItems.Count <= 0) {
                _currentItemCount = 0;
                return;
            }

            if (_currentItemCount >= 3 && _enqueuedItems.Count > 1) {
                ShowCompressedItem();
                _currentItemCount = 0;
                return;
            }

            var item = _enqueuedItems.Dequeue();
            ShowItem(item);
            _currentItemCount++;
        }
    }

    public void OnLateUpdate() {
        if (_unlockingItem == null)
            return;

        if (!ArchipelagoStatic.UnlockStagePanel.gameObject.activeSelf) {
            if (!_hasUnlockedItem)
                return; //Todo: Is this possible to trigger?
            _unlockingItem = null;
            return;
        }

        //Showing Pre Banner stuff
        if (ArchipelagoStatic.UnlockStagePanel.unlockText.gameObject.activeSelf)
            return;

        if (!_hasUnlockedItem) {
            _unlockingItem.UnlockItem(_handler, false);
            _hasUnlockedItem = true;
        }

        PnlUnlockStagePatch.ShowPostBanner(ArchipelagoStatic.UnlockStagePanel, _unlockingItem);
    }

    public IMuseDashItem GetCurrentItem() {
        return _unlockingItem;
    }

    private bool LocationIsKnown(ItemInfo item) {
        return item.LocationId >= 0 && _knownReceivedLocations.Contains((item.Player, item.LocationId));
    }
}