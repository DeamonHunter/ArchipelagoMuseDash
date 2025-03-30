using Archipelago.MultiClient.Net.Models;
using Il2CppAssets.Scripts.Database;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class BeefcakeSFXTrap : ITrap {

    private int? _originalSFX;
    public string TrapName => "Beefcake SFX";
    public string TrapMessage => "★★ Trap Activated ★★\nGet pumped.";
    public ItemInfo NetworkItem { get; set; }

    public void PreGameSceneLoad() {
        ArchipelagoStatic.ArchLogger.LogDebug("BeefcakeSFXTrap", "PreGameSceneLoad");
        _originalSFX ??= GlobalDataBase.dbUISpecial.battleSfxType;
        GlobalDataBase.dbUISpecial.battleSfxType = BattleSfxType.muscleman;
    }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) { }

    public void OnEnd() {
        ArchipelagoStatic.ArchLogger.LogDebug("BeefcakeSFXTrap", "OnEnd");
        if (!_originalSFX.HasValue)
            return;
        GlobalDataBase.dbUISpecial.battleSfxType = _originalSFX.Value;
    }
}