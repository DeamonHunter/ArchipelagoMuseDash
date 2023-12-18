using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using GameLogic;
using PeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps
{
    public class RandomWaveTrap : ITrap
    {
        public string TrapMessage => "★★ Trap Activated ★★\nRipple!";
        public NetworkItem NetworkItem { get; set; }

        public void PreGameSceneLoad() { }

        public void LoadMusicDataByFilenameHook() { }

        public void SetRuntimeMusicDataHook(List<MusicData> data)
        {
            ArchipelagoStatic.ArchLogger.LogDebug("RandomWaveTrap", "SetRuntimeMusicDataHook");

            var randomWaveNote = CreateRandomWaveNoteData();
            TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(randomWaveNote.uid, randomWaveNote));

            for (var i = data.Count - 1; i > 1; i--)
            {
                var bmsUid = data[i].noteData.bmsUid;
                if (bmsUid != BmsNodeUid.RandomWave && bmsUid != BmsNodeUid.RandomWaveOver)
                    continue;
                TrapHelper.RemoveIndex(data, i);
            }

            //ChangeToBadApple(data);
            TrapHelper.FixIndexes(data);
        }

        public void OnEnd() { }

        private NoteConfigData CreateRandomWaveNoteData() => new NoteConfigData()
        {
            id = "73",
            ibms_id = "2A",
            uid = "000803",
            mirror_uid = "000803",
            scene = "0",
            des = "RGB分离",
            prefab_name = "000803",
            type = 21,
            effect = "0",
            key_audio = "0",
            boss_action = "0",
            left_perfect_range = 0,
            left_great_range = 0,
            right_perfect_range = 0,
            right_great_range = 0,
            damage = 0,
            pathway = 0,
            speed = 1,
            score = 0,
            fever = 0,
            missCombo = false,
            addCombo = false,
            jumpNote = false,
            isShowPlayEffect = false,
            m_BmsUid = BmsNodeUid.RandomWave,
            sceneChangeNames = null
        };
    }
}