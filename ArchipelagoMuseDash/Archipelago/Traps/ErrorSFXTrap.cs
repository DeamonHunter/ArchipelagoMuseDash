using Archipelago.MultiClient.Net.Models;
using Il2CppAssets.Scripts.Database;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class ErrorSFXTrap : ITrap {

    private int? _originalSFX;
    public string TrapName => "Error SFX";
    public string TrapMessage => "★★ Trap Activated ★★\nAn error has occured.";
    public ItemInfo NetworkItem { get; set; }

    public void PreGameSceneLoad() {
        ArchipelagoStatic.ArchLogger.LogDebug("ErrorSFXTrap", "PreGameSceneLoad");
        _originalSFX ??= GlobalDataBase.dbUISpecial.battleSfxType;
        GlobalDataBase.dbUISpecial.battleSfxType = BattleSfxType.error;
    }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) { }

    public void OnEnd() {
        ArchipelagoStatic.ArchLogger.LogDebug("ErrorSFXTrap", "OnEnd");
        if (!_originalSFX.HasValue)
            return;
        GlobalDataBase.dbUISpecial.battleSfxType = _originalSFX.Value;
    }
}