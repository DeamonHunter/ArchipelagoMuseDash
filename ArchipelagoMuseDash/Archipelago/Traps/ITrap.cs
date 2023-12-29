using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using GameLogic;

namespace ArchipelagoMuseDash.Archipelago.Traps
{
    public interface ITrap
    {
        string TrapName { get; }
        string TrapMessage { get; }
        NetworkItem NetworkItem { get; set; }

        void PreGameSceneLoad();
        void LoadMusicDataByFilenameHook();
        void SetRuntimeMusicDataHook(List<MusicData> result);
        void OnEnd();
    }
}