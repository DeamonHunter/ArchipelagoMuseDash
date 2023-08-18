﻿using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using Assets.Scripts.Database;
using GameLogic;
using PeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps
{
    public class ErrorSFXTrap : ITrap
    {
        public string TrapMessage => "★★ Trap Activated ★★\nAn error has occured.";
        public NetworkItem NetworkItem { get; set; }

        private int? _originalSFX;

        public void PreGameSceneLoad()
        {
            if (!_originalSFX.HasValue)
                _originalSFX = GlobalDataBase.dbUISpecial.battleSfxType;
            GlobalDataBase.dbUISpecial.battleSfxType = BattleSfxType.error;
        }

        public void LoadMusicDataByFilenameHook() { }

        public void SetRuntimeMusicDataHook(List<MusicData> data) { }

        public void OnEnd()
        {
            if (!_originalSFX.HasValue)
                return;
            GlobalDataBase.dbUISpecial.battleSfxType = _originalSFX.Value;
        }
    }
}