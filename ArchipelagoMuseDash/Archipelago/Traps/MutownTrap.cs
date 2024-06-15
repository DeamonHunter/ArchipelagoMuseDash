using Archipelago.MultiClient.Net.Models;
using Il2CppAssets.Scripts.Database;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

/// <summary>
///     This trap is not active due to the SFX not being officially released. Might be for an upcoming update.
/// </summary>
public class MutownTrap : ITrap {
    private int? _originalSFX;
    public string TrapName => "Mutown SFX";
    public string TrapMessage => "★★ Trap Activated ★★\nMutown!";
    public ItemInfo NetworkItem { get; set; }

    public void PreGameSceneLoad() {
        ArchipelagoStatic.ArchLogger.LogDebug("MutownTrap", "PreGameSceneLoad");
        _originalSFX ??= GlobalDataBase.dbUISpecial.battleSfxType;
        GlobalDataBase.dbUISpecial.battleSfxType = BattleSfxType.mutown;
    }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) { }

    public void OnEnd() {
        ArchipelagoStatic.ArchLogger.LogDebug("MutownTrap", "OnEnd");
        if (!_originalSFX.HasValue)
            return;
        GlobalDataBase.dbUISpecial.battleSfxType = _originalSFX.Value;
    }
}