using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using GameLogic;
using PeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps
{
    public class ShadowEdgeTrap : ITrap
    {
        public string TrapMessage => "★★ Trap Activated ★★\nShadow Edge!";
        public NetworkItem NetworkItem { get; set; }

        public void PreGameSceneLoad() { }

        public void LoadMusicDataByFilenameHook() { }

        public void SetRuntimeMusicDataHook(List<MusicData> data)
        {
            ArchipelagoStatic.ArchLogger.LogDebug("DBStageInfo", $"SetRuntimeMusicData {data.Count}");

            var shadowEdgeInNoteData = CreateShadowEdgeInNoteData();
            TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(shadowEdgeInNoteData.uid, shadowEdgeInNoteData));

            for (int i = data.Count - 1; i > 1; i--)
            {
                var bmsUid = data[i].noteData.bmsUid;
                if (bmsUid != BmsNodeUid.ShadowEdgeIn && bmsUid != BmsNodeUid.ShadowEdgeOut)
                    continue;
                TrapHelper.RemoveIndex(data, i);
            }

            //ChangeToBadApple(data);
            TrapHelper.FixIndexes(data);
        }

        public void OnEnd() { }

        private NoteConfigData CreateShadowEdgeInNoteData() => new NoteConfigData()
        {
            id = "85",
            ibms_id = "2E",
            uid = "000807",
            mirror_uid = "000807",
            scene = "0",
            des = "暗边",
            prefab_name = "000807",
            type = 25,
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
            m_BmsUid = BmsNodeUid.ShadowEdgeIn,
            sceneChangeNames = null
        };
    }
}