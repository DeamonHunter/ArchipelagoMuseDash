using Archipelago.MultiClient.Net.Models;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public class PixelateTrap : ITrap {
    public string TrapName => "Pixelate";
    public string TrapMessage => "★★ Trap Activated ★★\nPixelation!";
    public ItemInfo NetworkItem { get; set; }

    public void PreGameSceneLoad() { }

    public void LoadMusicDataByFilenameHook() { }

    public void SetRuntimeMusicDataHook(List<MusicData> data) {
        ArchipelagoStatic.ArchLogger.LogDebug("PixelateTrap", "SetRuntimeMusicDataHook");

        var pixelateNoteData = CreatePixelateNoteData();
        TrapHelper.InsertAtStart(data, TrapHelper.CreateDefaultMusicData(pixelateNoteData.uid, pixelateNoteData));

        for (var i = data.Count - 1; i > 1; i--) {
            var bmsUid = data[i].noteData.bmsUid;
            if (bmsUid != BmsNodeUid.PixelStart && bmsUid != BmsNodeUid.PixelEnd)
                continue;
            TrapHelper.RemoveIndex(data, i);
        }

        //ChangeToBadApple(data);
        TrapHelper.FixIndexes(data);
    }

    public void OnEnd() { }

    private NoteConfigData CreatePixelateNoteData() {
        return new NoteConfigData() {
            id = "118",
            ibms_id = "2P",
            uid = "001001",
            mirror_uid = "001001",
            scene = "0",
            des = "像素化开始",
            prefab_name = "001001",
            type = 33,
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
            m_BmsUid = BmsNodeUid.PixelStart,
            sceneChangeNames = null
        };
    }
}