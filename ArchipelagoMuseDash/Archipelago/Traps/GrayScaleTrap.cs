using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class GrayScaleTrap : ITrap {
    public string TrapMessage => "★★ Trap Activated ★★\nGray Scale!";
    public NetworkItem NetworkItem { get; set; }

    public void PreGameSceneLoad() { }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) {
        ArchipelagoStatic.ArchLogger.LogDebug("DBStageInfo", $"SetRuntimeMusicData {data.Count}");

        var greyScaleNoteData = CreateGreyScaleNoteData();
        TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(greyScaleNoteData.uid, greyScaleNoteData));

        for (int i = data.Count - 1; i > 1; i--) {
            var bmsUid = data[i].noteData.bmsUid;
            if (bmsUid != BmsNodeUid.GrayScaleStart && bmsUid != BmsNodeUid.GrayScaleEnd)
                continue;
            TrapHelper.RemoveIndex(data, i);
        }

        //ChangeToBadApple(data);
        TrapHelper.FixIndexes(data);
    }

    public void OnEnd() { }

    private NoteConfigData CreateGreyScaleNoteData() => new NoteConfigData() {
        id = "124",
        ibms_id = "2R",
        uid = "001003",
        mirror_uid = "001003",
        scene = "0",
        des = "黑白滤镜开始",
        prefab_name = "001003",
        type = 35,
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
        m_BmsUid = BmsNodeUid.GrayScaleStart,
        sceneChangeNames = null
    };
}