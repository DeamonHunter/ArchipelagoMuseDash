using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;
using Random = UnityEngine.Random;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class FocusLineTrap : ITrap {
    public string TrapName => "Focus";
    public string TrapMessage => "★★ Trap Activated ★★\nLet's focus!";
    public ItemInfo NetworkItem { get; set; }

    public void PreGameSceneLoad() { }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) {
        ArchipelagoStatic.ArchLogger.LogDebug("FocusLineTrap", "SetRuntimeMusicDataHook");

        var focusData = Random.RandomRange(0, 2) == 0 ? CreateFocusBlack() : CreateFocusWhite();
        TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(focusData.uid, focusData));

        for (var i = data.Count - 1; i > 1; i--) {
            var bmsUid = data[i].noteData.bmsUid;
            if (bmsUid != (BmsNodeUid)101 && bmsUid != (BmsNodeUid)102 && bmsUid != (BmsNodeUid)103)
                continue;
            TrapHelper.RemoveIndex(data, i);
        }

        TrapHelper.FixIndexes(data);
    }

    public void OnEnd() { }

    private NoteConfigData CreateFocusBlack() {
        return new NoteConfigData {
            id = "126",
            ibms_id = "2T",
            uid = "001005",
            mirror_uid = "001005",
            scene = "0",
            des = "像素化开始",
            prefab_name = "001005",
            type = 37,
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
            m_BmsUid = (BmsNodeUid)101,
            sceneChangeNames = null
        };
    }

    private NoteConfigData CreateFocusWhite() {
        return new NoteConfigData {
            id = "127",
            ibms_id = "2U",
            uid = "001006",
            mirror_uid = "001006",
            scene = "0",
            des = "像素化开始",
            prefab_name = "001006",
            type = 38,
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
            m_BmsUid = (BmsNodeUid)102,
            sceneChangeNames = null
        };
    }
}