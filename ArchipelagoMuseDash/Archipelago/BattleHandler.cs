using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago.Traps;
using ArchipelagoMuseDash.Helpers;
using Assets.Scripts.GameCore.Managers;
using Assets.Scripts.UI.Controls;
using GameLogic;

namespace ArchipelagoMuseDash.Archipelago
{
    public class BattleHandler
    {
        private int _lastHandledTrap;
        private ITrap _activatedTrap;

        private readonly List<ITrap> _knownTrapItems = new List<ITrap>();
        private readonly List<NetworkItem> _knownBattleItems = new List<NetworkItem>();

        private BattleItem _greatToPerfectCount;
        private BattleItem _missToGreatCount;
        private BattleItem _extraLifeCount;

        private int _extraLifesUsed;
        private bool _forceUpdate;

        public BattleHandler()
        {
            _knownTrapItems.Clear();
            _knownBattleItems.Clear();

            _lastHandledTrap = ArchipelagoStatic.SessionHandler.DataStorageHandler.GetHandledTrapCount();

            _greatToPerfectCount.CurrentCount -= ArchipelagoStatic.SessionHandler.DataStorageHandler.GetUsedGreatToPerfect();
            _missToGreatCount.CurrentCount -= ArchipelagoStatic.SessionHandler.DataStorageHandler.GetUsedMissToGreat();
            _extraLifeCount.CurrentCount -= ArchipelagoStatic.SessionHandler.DataStorageHandler.GetUsedExtraLifes();
        }

        public bool EnqueueIfBattleItem(NetworkItem item, out bool createFiller)
        {
            createFiller = false;
            if (EnqueueIfTrap(item))
                return true;

            if (item.Item < 2900030 || item.Item > 2900032)
                return false;

            if (_knownBattleItems.Any(n => ArchipelagoHelpers.IsItemDuplicate(n, item)))
                return true;

            createFiller = true;

            switch (item.Item)
            {
                case 2900030:
                {
                    _greatToPerfectCount.IncreaseCount(5);
                    break;
                }
                case 2900031:
                {
                    _missToGreatCount.IncreaseCount(5);
                    break;
                }
                case 2900032:
                {
                    _extraLifeCount.IncreaseCount(1);
                    break;
                }
            }

            _knownBattleItems.Add(item);
            _forceUpdate = true;
            return true;
        }

        private bool EnqueueIfTrap(NetworkItem item)
        {
            if (item.Item < 2900001 || item.Item > 2900009)
                return false;

            if (_knownTrapItems.Any(t => ArchipelagoHelpers.IsItemDuplicate(t.NetworkItem, item)))
                return true;

            ITrap trap;
            switch (item.Item)
            {
                default:
                    return false;

                case 2900001:
                    trap = new BadAppleTrap();
                    break;
                case 2900002:
                    trap = new PixelateTrap();
                    break;
                case 2900003:
                    trap = new RandomWaveTrap();
                    break;
                case 2900004:
                    trap = new ShadowEdgeTrap();
                    break;
                case 2900005:
                    trap = new ChromaticAberrationTrap();
                    break;
                case 2900006:
                    trap = new BGFreezeTrap();
                    break;
                case 2900007:
                    trap = new GrayScaleTrap();
                    break;
                case 2900008:
                    trap = new NyaaTrap();
                    break;
                case 2900009:
                    trap = new ErrorSFXTrap();
                    break;
            }

            trap.NetworkItem = item;
            _knownTrapItems.Add(trap);
            return true;
        }

        public void ActivateNextTrap()
        {
            if (_lastHandledTrap >= _knownTrapItems.Count || _activatedTrap != null)
            {
                if (_activatedTrap != null)
                    ShowText.ShowInfo(_activatedTrap.TrapMessage);
                return;
            }

            _activatedTrap = _knownTrapItems[_lastHandledTrap];

            if (_activatedTrap.NetworkItem.Player == 0 && _activatedTrap.NetworkItem.Location < 0)
                _knownTrapItems.RemoveAt(_lastHandledTrap);
            else
                _lastHandledTrap++;

            ShowText.ShowInfo(_activatedTrap.TrapMessage);
            ArchipelagoStatic.ArchLogger.Log("TrapHandler", $"Activated trap {_activatedTrap}");
        }

        public void SetTrapFinished()
        {
            if (_activatedTrap == null)
                return;

            _activatedTrap.OnEnd();

            //This will cause issues if multiple people are running the same game and don't go through all traps. But I'm fine with that.
            ArchipelagoStatic.SessionHandler.DataStorageHandler.SetHandledTrapCount(_lastHandledTrap);
            _activatedTrap = null;
        }

        public void PreGameSceneLoad()
        {
            _activatedTrap?.PreGameSceneLoad();
        }

        public void LoadMusicDataByFilenameHook()
        {
            _activatedTrap?.LoadMusicDataByFilenameHook();
        }

        public void SetRuntimeMusicDataHook(Il2CppSystem.Collections.Generic.List<MusicData> result)
        {
            ResetNewItemCount();

            var property = BattleProperty.instance;
            ArchipelagoStatic.ArchLogger.LogDebug("Battle Handler", $"Initial Counts: {property.greatToPerfect}, {property.missToGreat}");

            property.greatToPerfect += Math.Max(0, _greatToPerfectCount.CurrentCount);
            property.missToGreat += Math.Max(0, _missToGreatCount.CurrentCount);

            if (_activatedTrap == null)
                return;

            var list = new List<MusicData>(result.Count);
            foreach (var value in result)
                list.Add(value);

            _activatedTrap.SetRuntimeMusicDataHook(list);

            result.Clear();
            foreach (var value in list)
                result.Add(value);
        }

        public bool TryUseExtraLife()
        {
            if (_extraLifeCount.CurrentCount - _extraLifesUsed <= 0)
                return false;

            _extraLifesUsed++;
            return true;
        }

        public void OnBattleEnd()
        {
            var property = BattleProperty.instance;

            if (property.greatToPerfect < _greatToPerfectCount.CurrentCount)
            {
                var diff = _greatToPerfectCount.CurrentCount - property.greatToPerfect;
                ArchipelagoStatic.ArchLogger.LogDebug("Battle Handler", $"Greats Used {diff}");
                _greatToPerfectCount.CurrentCount -= diff;
                ArchipelagoStatic.SessionHandler.DataStorageHandler.SetUsedGreatToPerfect(_missToGreatCount.TotalCount - _missToGreatCount.CurrentCount);
            }

            if (property.missToGreat < _missToGreatCount.CurrentCount)
            {
                var diff = _missToGreatCount.CurrentCount - property.missToGreat;
                ArchipelagoStatic.ArchLogger.LogDebug("Battle Handler", $"Miss Used {diff}");
                _missToGreatCount.CurrentCount -= diff;
                ArchipelagoStatic.SessionHandler.DataStorageHandler.SetUsedMissToGreat(_missToGreatCount.TotalCount - _missToGreatCount.CurrentCount);
            }

            if (_extraLifesUsed > 0)
            {
                ArchipelagoStatic.ArchLogger.LogDebug("Battle Handler", $"Lives Used {_extraLifesUsed}");
                _missToGreatCount.CurrentCount -= _extraLifesUsed;
                ArchipelagoStatic.SessionHandler.DataStorageHandler.SetUsedExtraLifes(_extraLifeCount.TotalCount - _extraLifeCount.CurrentCount);
            }
        }

        public void ResetNewItemCount()
        {
            _forceUpdate = true;
            _greatToPerfectCount.NewCount = 0;
            _missToGreatCount.NewCount = 0;
            _extraLifeCount.NewCount = 0;
        }

        public void OnUpdate()
        {
            if (ArchipelagoStatic.SessionHandler.SongSelectAdditions.FillerTextComp == null)
            {
                _forceUpdate = true;
                return;
            }

            ArchipelagoStatic.SessionHandler.SongSelectAdditions.FillerTextComp.text = GetItemAmount();
        }

        public string GetItemAmount()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Single Use Items</b>");

            if (_greatToPerfectCount.CurrentCount > 0)
            {
                sb.AppendLine(_greatToPerfectCount.NewCount > 0
                    ? $"Great To Perfect: {_greatToPerfectCount.CurrentCount} ({_greatToPerfectCount.NewCount}↑)"
                    : $"Great To Perfect: {_greatToPerfectCount.CurrentCount}"
                );
            }

            if (_missToGreatCount.CurrentCount > 0)
            {
                sb.AppendLine(_missToGreatCount.NewCount > 0
                    ? $"Miss To Great: {_missToGreatCount.CurrentCount} ({_missToGreatCount.NewCount}↑)"
                    : $"Miss To Great: {_missToGreatCount.CurrentCount}"
                );
            }

            if (_extraLifeCount.CurrentCount > 0)
            {
                sb.AppendLine(_missToGreatCount.NewCount > 0
                    ? $"Extra Lives: {_missToGreatCount.CurrentCount} ({_missToGreatCount.NewCount}↑)"
                    : $"Extra Lives: {_missToGreatCount.CurrentCount}"
                );
            }

            if (_extraLifeCount.CurrentCount <= 0 && _missToGreatCount.CurrentCount <= 0 && _greatToPerfectCount.CurrentCount <= 0)
                sb.AppendLine("No items.");

            return sb.ToString().Trim();
        }

        private struct BattleItem
        {
            public int TotalCount;
            public int CurrentCount;
            public int NewCount;

            public void IncreaseCount(int amount)
            {
                TotalCount += amount;
                CurrentCount += amount;
                NewCount += amount;
            }
        }
    }
}