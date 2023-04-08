using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago.Traps;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppGameLogic;

namespace ArchipelagoMuseDash.Archipelago;

public class TrapHandler {
    private readonly DataStorageHelper _dataStorageHelper;

    private int _lastHandledTrap;
    private ITrap _activatedTrap;

    private readonly List<ITrap> _knownTraps = new();

    public TrapHandler(DataStorageHelper dataStorageHelper) {
        _dataStorageHelper = dataStorageHelper;
        _lastHandledTrap = dataStorageHelper["lastTrap"];
        _knownTraps.Clear();
    }

    public bool EnqueueIfTrap(NetworkItem item) {
        foreach (var trap in _knownTraps) {
            if (trap.NetworkItem.Item == item.Item && trap.NetworkItem.Location == item.Location)
                return true; //Already known
        }

        switch (item.Item) {
            case 2900001: {
                _knownTraps.Add(new BadAppleTrap() { NetworkItem = item });
                return true;
            }
            case 2900002: {
                _knownTraps.Add(new PixelateTrap() { NetworkItem = item });
                return true;
            }
            case 2900003: {
                _knownTraps.Add(new RandomWaveTrap() { NetworkItem = item });
                return true;
            }
            case 2900004: {
                _knownTraps.Add(new ShadowEdgeTrap() { NetworkItem = item });
                return true;
            }
            case 2900005: {
                _knownTraps.Add(new ChromaticAberrationTrap() { NetworkItem = item });
                return true;
            }
            case 2900006: {
                _knownTraps.Add(new BGFreezeTrap() { NetworkItem = item });
                return true;
            }
            case 2900007: {
                _knownTraps.Add(new GrayScaleTrap() { NetworkItem = item });
                return true;
            }
            default:
                return false;
        }
    }

    public void ActivateNextTrap() {
        if (_lastHandledTrap >= _knownTraps.Count || _activatedTrap != null) {
            ShowText.ShowInfo(_activatedTrap.TrapMessage);
            return;
        }

        _activatedTrap = _knownTraps[_lastHandledTrap];
        _lastHandledTrap++;
        ShowText.ShowInfo(_activatedTrap.TrapMessage);
        ArchipelagoStatic.ArchLogger.Log("TrapHandler", $"Activated trap {_activatedTrap}");
    }

    public void SetTrapFinished() {
        if (_activatedTrap == null)
            return;

        //This will cause issues if multiple people are running the same game and don't go through all traps. But I'm fine with that.
        _dataStorageHelper["lastTrap"] = _lastHandledTrap;
        _activatedTrap = null;
    }

    public void PreGameSceneLoad() => _activatedTrap?.PreGameSceneLoad();
    public void LoadMusicDataByFilenameHook() => _activatedTrap?.LoadMusicDataByFilenameHook();

    public void SetRuntimeMusicDataHook(Il2CppSystem.Collections.Generic.List<MusicData> result) {
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