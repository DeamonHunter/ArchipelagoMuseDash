using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public interface ITrap {
    string TrapMessage { get; }
    NetworkItem NetworkItem { get; set; }

    void PreGameSceneLoad();
    void LoadMusicDataByFilenameHook();
    void SetRuntimeMusicDataHook(List<MusicData> result);
    void OnEnd();
}