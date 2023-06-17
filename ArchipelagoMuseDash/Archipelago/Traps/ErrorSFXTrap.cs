using Archipelago.MultiClient.Net.Models;
using Il2CppAssets.Scripts.Database;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class ErrorSFXTrap : ITrap {
    public string TrapMessage => "★★ Trap Activated ★★\nAn error has occured.";
    public NetworkItem NetworkItem { get; set; }

    private int? _originalSFX;

    public void PreGameSceneLoad() {
        _originalSFX = GlobalDataBase.dbUISpecial.battleSfxType;
        GlobalDataBase.dbUISpecial.battleSfxType = BattleSfxType.error;
    }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) { }

    public void OnEnd() {
        GlobalDataBase.dbUISpecial.battleSfxType = _originalSFX.Value;
    }
}