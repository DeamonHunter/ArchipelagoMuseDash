using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class ChromaticAberrationTrap : ITrap {
    public string TrapMessage => "★★ Trap Activated ★★\nChromatic Aberration!";
    public NetworkItem NetworkItem { get; set; }

    public void PreGameSceneLoad() { }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) {
        ArchipelagoStatic.ArchLogger.LogDebug("DBStageInfo", $"SetRuntimeMusicData {data.Count}");

        var chromaticAberrationNoteData = CreateChromaticAberrationNoteData();
        TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(chromaticAberrationNoteData.uid, chromaticAberrationNoteData));

        for (int i = data.Count - 1; i > 1; i--) {
            var bmsUid = data[i].noteData.bmsUid;
            if (bmsUid != BmsNodeUid.RgbSplit && bmsUid != BmsNodeUid.RgbSplitOver)
                continue;
            TrapHelper.RemoveIndex(data, i);
        }

        //ChangeToBadApple(data);
        TrapHelper.FixIndexes(data);
    }

    private NoteConfigData CreateChromaticAberrationNoteData() => new NoteConfigData() {
        id = "79",
        ibms_id = "2C",
        uid = "000805",
        mirror_uid = "000805",
        scene = "0",
        des = "RGB分离",
        prefab_name = "000805",
        type = 23,
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
        m_BmsUid = BmsNodeUid.RgbSplit,
        sceneChangeNames = null
    };
}