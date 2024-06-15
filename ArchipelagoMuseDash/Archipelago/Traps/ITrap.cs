using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public interface ITrap {
    string TrapName { get; }
    string TrapMessage { get; }
    ItemInfo NetworkItem { get; set; }

    void PreGameSceneLoad();
    void LoadMusicDataByFilenameHook();
    void SetRuntimeMusicDataHook(List<MusicData> result);
    void OnEnd();
}