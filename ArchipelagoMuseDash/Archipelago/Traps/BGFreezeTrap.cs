using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class BGFreezeTrap : ITrap {
    public string TrapMessage => "★★ Trap Activated ★★\nBackground Frozen!";
    public NetworkItem NetworkItem { get; set; }

    public void PreGameSceneLoad() { }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) {
        ArchipelagoStatic.ArchLogger.LogDebug("DBStageInfo", $"SetRuntimeMusicData {data.Count}");

        var backgroundFreezeNoteData = CreateBackgroundFreezeNoteData();
        TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(backgroundFreezeNoteData.uid, backgroundFreezeNoteData));

        for (int i = data.Count - 1; i > 1; i--) {
            var bmsUid = data[i].noteData.bmsUid;
            if (bmsUid != BmsNodeUid.BgFreeze && bmsUid != BmsNodeUid.BgUnfreeze)
                continue;
            TrapHelper.RemoveIndex(data, i);
        }

        //ChangeToBadApple(data);
        TrapHelper.FixIndexes(data);
    }

    private NoteConfigData CreateBackgroundFreezeNoteData() => new NoteConfigData() {
        id = "112",
        ibms_id = "2N",
        uid = "000903",
        mirror_uid = "000903",
        scene = "0",
        des = "黑白滤镜开始",
        prefab_name = "000903",
        type = 32,
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
        m_BmsUid = BmsNodeUid.BgFreeze,
        sceneChangeNames = null
    };
}