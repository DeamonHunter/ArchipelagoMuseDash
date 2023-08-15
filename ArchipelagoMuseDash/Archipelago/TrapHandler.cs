using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago.Traps;
using ArchipelagoMuseDash.Helpers;
using Assets.Scripts.UI.Controls;
using GameLogic;

namespace ArchipelagoMuseDash.Archipelago
{

    public class TrapHandler
    {

        private int _lastHandledTrap;
        private ITrap _activatedTrap;

        private readonly List<ITrap> _knownTraps = new List<ITrap>();

        public TrapHandler()
        {
            _lastHandledTrap = ArchipelagoStatic.SessionHandler.DataStorageHandler.GetHandledTrapCount();
            _knownTraps.Clear();
        }

        public bool EnqueueIfTrap(NetworkItem item)
        {
            if (_knownTraps.Any(t => ArchipelagoHelpers.IsItemDuplicate(t.NetworkItem, item)))
                return true;

            ITrap trap;
            switch (item.Item)
            {
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
                default:
                    return false;
            }

            trap.NetworkItem = item;
            _knownTraps.Add(trap);
            return true;
        }

        public void ActivateNextTrap()
        {
            if (_lastHandledTrap >= _knownTraps.Count || _activatedTrap != null)
            {
                if (_activatedTrap != null)
                    ShowText.ShowInfo(_activatedTrap.TrapMessage);
                return;
            }

            _activatedTrap = _knownTraps[_lastHandledTrap];

            if (_activatedTrap.NetworkItem.Player == 0 && _activatedTrap.NetworkItem.Location < 0)
                _knownTraps.RemoveAt(_lastHandledTrap);
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

        public void PreGameSceneLoad() => _activatedTrap?.PreGameSceneLoad();
        public void LoadMusicDataByFilenameHook() => _activatedTrap?.LoadMusicDataByFilenameHook();

        public void SetRuntimeMusicDataHook(Il2CppSystem.Collections.Generic.List<MusicData> result)
        {
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
    }
}