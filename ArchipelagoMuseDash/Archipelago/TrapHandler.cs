using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMuseDash.Archipelago.Traps;
using ArchipelagoMuseDash.Helpers;
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
        if (_knownTraps.Any(trap => ArchipelagoHelpers.IsItemDuplicate(trap.NetworkItem, item)))
            return true;

        ITrap trap = item.Item switch {
            2900001 => new BadAppleTrap(),
            2900002 => new PixelateTrap(),
            2900003 => new RandomWaveTrap(),
            2900004 => new ShadowEdgeTrap(),
            2900005 => new ChromaticAberrationTrap(),
            2900006 => new BGFreezeTrap(),
            2900007 => new GrayScaleTrap(),
            2900008 => new NyaaTrap(),
            2900009 => new ErrorSFXTrap(),
            _ => null
        };

        if (trap == null)
            return false;

        trap.NetworkItem = item;
        _knownTraps.Add(trap);
        return true;
    }

    public void ActivateNextTrap() {
        if (_lastHandledTrap >= _knownTraps.Count || _activatedTrap != null) {
            ShowText.ShowInfo(_activatedTrap.TrapMessage);
            return;
        }

        _activatedTrap = _knownTraps[_lastHandledTrap];
        if (_activatedTrap.NetworkItem is { Player: 0, Location: < 0 })
            _knownTraps.RemoveAt(_lastHandledTrap);
        else
            _lastHandledTrap++;

        ShowText.ShowInfo(_activatedTrap.TrapMessage);
        ArchipelagoStatic.ArchLogger.Log("TrapHandler", $"Activated trap {_activatedTrap}");
    }

    public void SetTrapFinished() {
        if (_activatedTrap == null)
            return;

        _activatedTrap.OnEnd();

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