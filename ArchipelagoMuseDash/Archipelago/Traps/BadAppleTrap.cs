using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using Assets.Scripts.Database;
using Assets.Scripts.TouhouLogic;
using GameLogic;
using PeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps
{

    public class BadAppleTrap : ITrap
    {
        public string TrapMessage => "★★ Trap Activated ★★\nBad Apple!";
        public NetworkItem NetworkItem { get; set; }

        public void PreGameSceneLoad()
        {
            GlobalDataBase.dbTouhou.isBadApple = true;
            GlobalDataBase.s_DbOther.m_HpFx = TouhouLogic.ReplaceBadAppleString("fx_hp_ground");
            GlobalDataBase.s_DbOther.m_MusicFx = TouhouLogic.ReplaceBadAppleString("fx_score_ground");
            GlobalDataBase.s_DbOther.m_DustFx = TouhouLogic.ReplaceBadAppleString("dust_fx");
        }

        public void LoadMusicDataByFilenameHook()
        {
            //This is used to force the scene into the Touhou scene. The Touhou scene is the only one that can handle the bad apple look.
            GlobalDataBase.dbBattleStage.m_BeganScene = "scene_08";
            GlobalDataBase.dbBattleStage.m_BeganSceneIdx = 8;
        }

        public void SetRuntimeMusicDataHook(List<MusicData> data)
        {
            ArchipelagoStatic.ArchLogger.LogDebug("DBStageInfo", $"SetRuntimeMusicData {data.Count}");
            ChangeToBadApple(data);
            TrapHelper.FixIndexes(data);
        }

        public void OnEnd() { }

        private void ChangeToBadApple(List<MusicData> data)
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var md = data[i];

                var noteData = md.noteData;

                switch (noteData.bmsUid)
                {
                    case BmsNodeUid.ToggleScene1:
                    case BmsNodeUid.ToggleScene2:
                    case BmsNodeUid.ToggleScene3:
                    case BmsNodeUid.ToggleScene4:
                    case BmsNodeUid.ToggleScene5:
                    case BmsNodeUid.ToggleScene6:
                    case BmsNodeUid.ToggleScene7:
                    case BmsNodeUid.ToggleScene8:
                    case BmsNodeUid.ToggleScene9:
                    case BmsNodeUid.ToggleScene10:
                    {
                        TrapHelper.RemoveIndex(data, i);
                        continue;
                    }
                }

                //BmsNodeUid.Hp and BmsNodeUid.Music do not have stage specific versions, except for touhou. So these require special logic.
                if (noteData.m_BmsUid == BmsNodeUid.Hp || noteData.m_BmsUid == BmsNodeUid.Music)
                {
                    if (noteData.scene == "scene_08")
                        continue;

                    noteData.scene = "scene_08";
                    noteData.prefab_name = string.Concat("08", md.noteData.prefab_name);
                    md.noteData = noteData;
                    data[i] = md;
                    continue;
                }

                if (noteData.scene == null || noteData.scene.Length > 2)
                    continue;

                noteData.scene = "scene_08";
                if (int.TryParse(md.noteData.prefab_name.Substring(0, 2), out var value) && value != 8)
                {
                    switch (noteData.m_BmsUid)
                    {
                        case BmsNodeUid.Music:
                        case BmsNodeUid.Hp:
                            break;
                        default:
                            noteData.prefab_name = string.Concat("08", md.noteData.prefab_name.Substring(2));
                            break;
                    }
                }

                md.noteData = noteData;
                data[i] = md;
            }
        }
    }
}