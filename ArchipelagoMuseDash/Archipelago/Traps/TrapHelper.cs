using System.Text;
using Il2CppGameLogic;

namespace ArchipelagoMuseDash.Archipelago.Traps;

public static class TrapHelper {
    public static void FixIndexes(List<MusicData> list) {
        for (short i = 0; i < list.Count; i++) {
            var md = list[i];
            md.objId = i;
            list[i] = md;
        }
    }

    public static void RemoveIndex(List<MusicData> list, int index) {
        list.RemoveAt(index);
        for (var i = 0; i < list.Count; i++) {
            var note = list[i];

            if (!note.isDouble || note.doubleIdx < index)
                continue;

            note.doubleIdx--;
            list[i] = note;
        }
    }

    public static void InsertAtStart(List<MusicData> list, MusicData data) {
        data.objId = 1;
        data.configData.id = 0;

        var zeroTick = list[0];
        zeroTick.tick = 0;
        zeroTick.showTick = 0;
        zeroTick.configData.time = 0;


        data.tick = 0;
        data.showTick = 0;
        data.configData.time = 0;
        list.Insert(1, data);

        for (var i = 2; i < list.Count; i++) {
            var note = list[i];

            if (!note.isDouble || note.doubleIdx < 1)
                continue;

            note.doubleIdx++;
            list[i] = note;
        }
    }

    public static MusicData CreateDefaultMusicData(string noteUid, NoteConfigData data) {
        return new MusicData() {
            objId = 0,
            tick = 0,
            isLongPressing = false,
            doubleIdx = -1,
            isDouble = false,
            isLongPressEnd = false,
            longPressPTick = 0,
            endIndex = 0,
            dt = 0,
            longPressNum = 0,
            showTick = 0,
            configData = new MusicConfigData() {
                blood = false,
                pathway = 0,
                id = 0,
                length = 0,
                note_uid = noteUid,
                time = 0
            },
            noteData = data
        };
    }

    public static void OutputNote(MusicData data) {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("==Note==");
        stringBuilder.AppendLine($"objId: {data.objId}");
        stringBuilder.AppendLine($"tick: {data.tick.ToString()}");
        stringBuilder.AppendLine($"isLongPressing: {data.isLongPressing}");
        stringBuilder.AppendLine($"isLongPressStart: {data.isLongPressStart}");
        stringBuilder.AppendLine($"isLongPressEnd: {data.isLongPressEnd}");
        stringBuilder.AppendLine($"isLongPressType: {data.isLongPressType}");
        stringBuilder.AppendLine($"longPressNum: {data.longPressNum}");
        stringBuilder.AppendLine($"longPressCount: {data.longPressCount}");
        stringBuilder.AppendLine($"longPressPTick: {data.longPressPTick.ToString()}");
        stringBuilder.AppendLine($"doubleIdx: {data.doubleIdx}");
        stringBuilder.AppendLine($"isDouble: {data.isDouble}");
        stringBuilder.AppendLine($"endIndex: {data.endIndex}");
        stringBuilder.AppendLine($"dt: {data.dt.ToString()}");
        stringBuilder.AppendLine($"showTick: {data.showTick.ToString()}");

        stringBuilder.AppendLine($"MC: id: {data.configData.id}");
        stringBuilder.AppendLine($"MC: time: {data.configData.time.ToString()}");
        stringBuilder.AppendLine($"MC: note_uid: {data.configData.note_uid}");
        stringBuilder.AppendLine($"MC: length: {data.configData.length.ToString()}");
        stringBuilder.AppendLine($"MC: blood: {data.configData.blood}");
        stringBuilder.AppendLine($"MC: pathway: {data.configData.pathway}");

        stringBuilder.AppendLine($"NC: id: {data.noteData.id}");
        stringBuilder.AppendLine($"NC: ibms_id: {data.noteData.ibms_id}");
        stringBuilder.AppendLine($"NC: uid: {data.noteData.uid}");
        stringBuilder.AppendLine($"NC: mirror_uid: {data.noteData.mirror_uid}");
        stringBuilder.AppendLine($"NC: noteUid: {data.noteData.noteUid}");
        stringBuilder.AppendLine($"NC: scene: {data.noteData.scene}");
        stringBuilder.AppendLine($"NC: des: {data.noteData.des}");
        stringBuilder.AppendLine($"NC: prefab_name: {data.noteData.prefab_name}");
        stringBuilder.AppendLine($"NC: type: {data.noteData.type}");
        stringBuilder.AppendLine($"NC: effect: {data.noteData.effect}");
        stringBuilder.AppendLine($"NC: key_audio: {data.noteData.key_audio}");
        stringBuilder.AppendLine($"NC: boss_action: {data.noteData.boss_action}");
        stringBuilder.AppendLine($"NC: left_perfect_range: {data.noteData.left_perfect_range.ToString()}");
        stringBuilder.AppendLine($"NC: left_great_range: {data.noteData.left_great_range.ToString()}");
        stringBuilder.AppendLine($"NC: right_perfect_range: {data.noteData.right_perfect_range.ToString()}");
        stringBuilder.AppendLine($"NC: right_great_range: {data.noteData.right_great_range.ToString()}");
        stringBuilder.AppendLine($"NC: damage: {data.noteData.damage}");
        stringBuilder.AppendLine($"NC: pathway: {data.noteData.pathway}");
        stringBuilder.AppendLine($"NC: speed: {data.noteData.speed}");
        stringBuilder.AppendLine($"NC: score: {data.noteData.score}");
        stringBuilder.AppendLine($"NC: fever: {data.noteData.fever}");
        stringBuilder.AppendLine($"NC: missCombo: {data.noteData.missCombo}");
        stringBuilder.AppendLine($"NC: addCombo: {data.noteData.addCombo}");
        stringBuilder.AppendLine($"NC: jumpNote: {data.noteData.jumpNote}");
        stringBuilder.AppendLine($"NC: isShowPlayEffect: {data.noteData.isShowPlayEffect}");
        stringBuilder.AppendLine($"NC: m_BmsUid: {data.noteData.m_BmsUid}");

        if (data.noteData.sceneChangeNames != null)
            stringBuilder.AppendLine($"NC: sceneChangeNames: {string.Join(",", data.noteData.sceneChangeNames)}");
        else
            stringBuilder.AppendLine($"NC: sceneChangeNames: null");
        stringBuilder.AppendLine("==End Note==");

        ArchipelagoStatic.ArchLogger.Log("Output", stringBuilder.ToString());
    }
}