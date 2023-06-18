using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

/// <summary>
/// This trap works, but is very visually noisy. There may be a value somewhere that can be controlled to slow it down, but have not found it.
/// </summary>
public class CanvasScrollTrap : ITrap {
    public string TrapMessage => "★★ Trap Activated ★★\nCanvas Scroll!";
    public NetworkItem NetworkItem { get; set; }

    public void PreGameSceneLoad() { }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) {
        ArchipelagoStatic.ArchLogger.LogDebug("DBStageInfo", $"SetRuntimeMusicData {data.Count}");

        var scrollNoteData = UnityEngine.Random.Range(0, 2) == 0 ? CreateUpScrollNoteData() : CreateDownScrollNoteData();
        TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(scrollNoteData.uid, scrollNoteData));

        for (int i = data.Count - 1; i > 1; i--) {
            var bmsUid = data[i].noteData.bmsUid;
            if (bmsUid != BmsNodeUid.CanvasUpScroll && bmsUid != BmsNodeUid.CanvasDownScroll && bmsUid != BmsNodeUid.CanvasScrollOver)
                continue;
            TrapHelper.RemoveIndex(data, i);
        }

        //ChangeToBadApple(data);
        TrapHelper.FixIndexes(data);
    }

    public void OnEnd() { }

    private NoteConfigData CreateUpScrollNoteData() => new NoteConfigData() {
        id = "64",
        ibms_id = "27",
        uid = "000800",
        mirror_uid = "000800",
        scene = "0",
        des = "画面向上滚动",
        prefab_name = "000800",
        type = 18,
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
        m_BmsUid = BmsNodeUid.CanvasUpScroll,
        sceneChangeNames = null
    };

    private NoteConfigData CreateDownScrollNoteData() => new NoteConfigData() {
        id = "67",
        ibms_id = "28",
        uid = "000801",
        mirror_uid = "000801",
        scene = "0",
        des = "画面向下滚动",
        prefab_name = "000801",
        type = 19,
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
        m_BmsUid = BmsNodeUid.CanvasDownScroll,
        sceneChangeNames = null
    };
}