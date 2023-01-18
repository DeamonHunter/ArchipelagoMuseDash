using System;
using System.Collections.Generic;
using ArchipelagoMuseDash.Archipelago.Items;
using ArchipelagoMuseDash.Patches;
using Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Nice.Interface;
using UnityEngine;

namespace ArchipelagoMuseDash.Archipelago {
    public class ItemUnlockHandler {
        ItemHandler _handler;

        readonly Queue<IMuseDashItem> _enqueuedItems = new Queue<IMuseDashItem>();

        IMuseDashItem _unlockingItem;
        bool _hasUnlockedItem;

        const float on_show_stage_select_delay = 0.75f;
        float _itemGiveDelay;

        public ItemUnlockHandler(ItemHandler handler) {
            _handler = handler;
        }

        public void AddItem(IMuseDashItem item) {
            lock (_enqueuedItems)
                _enqueuedItems.Enqueue(item);
        }

        public void UnlockAllItems() {
            lock (_enqueuedItems) {
                while (_enqueuedItems.Count > 0) {
                    var itemToUnlock = _enqueuedItems.Dequeue();
                    itemToUnlock.UnlockItem(_handler, true);
                }
            }

            //Force a refresh of the song select and relevant classes, so new songs should show.
            MusicTagManager.instance.RefreshStageDisplayMusics(-1);
            ArchipelagoStatic.SongSelectPanel?.RefreshMusicFSV();
        }

        void ShowItem(IMuseDashItem item) {
            if (_unlockingItem != null)
                throw new Exception("Tried to unlock an item while one was already unlocking.");

            _unlockingItem = item;
            _hasUnlockedItem = false;

            Data data = new Data();
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
                if (_enqueuedItems.Count <= 0)
                    return;

                var item = _enqueuedItems.Dequeue();
                ShowItem(item);
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
    }
}
