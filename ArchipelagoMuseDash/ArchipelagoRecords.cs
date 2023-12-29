using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore.HostComponent;
using Newtonsoft.Json;
using UnityEngine;

namespace ArchipelagoMuseDash {
    public class ArchipelagoRecords {
        private Dictionary<int, Difficulty> _records;

        private string RecordPath() => Path.Combine(Application.absoluteURL, "UserData", "ArchipelagoRecords.json");

        public void Load() {
            try {
                var path = RecordPath();
                if (!File.Exists(path)) {
                    _records = new Dictionary<int, Difficulty>();
                    return;
                }

                //Using Newtonsoft Json as it handles dictionaries easier.
                _records = JsonConvert.DeserializeObject<Dictionary<int, Difficulty>>(File.ReadAllText(path));
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Records", e);
                _records = new Dictionary<int, Difficulty>();
            }
        }

        private void Save() {
            //Using Newtonsoft Json as it handles dictionaries easier.
            try {
                File.WriteAllText(RecordPath(), JsonConvert.SerializeObject(_records));
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Records", e);
            }
        }

        public void RecordHighScore(string activeTrap) {
            var musicInfo = GlobalDataBase.dbBattleStage.selectedMusicInfo;
            var diff = GlobalDataBase.dbBattleStage.selectedDifficulty;
            var taskStage = TaskStageTarget.instance;

            var score = taskStage.GetScore();
            if (_records.TryGetValue(diff, out var difficulty) && difficulty.Records.TryGetValue(musicInfo.uid, out var prevRecord) && prevRecord.Score >= score)
                return;

            var newRecord = new Record();
            newRecord.Score = score;
            newRecord.Accuracy = taskStage.GetAccuracy();
            //This might break with FavGirl. But whatevs.
            newRecord.Character = GlobalDataBase.dbBattleStage.selectedRole;
            newRecord.Elfin = GlobalDataBase.dbBattleStage.selectedElfin;
            newRecord.Trap = activeTrap ?? "";

            if (!_records.ContainsKey(diff))
                _records.Add(diff, new Difficulty());

            _records[diff].Records[musicInfo.uid] = newRecord;

            //Todo: This probably creates a hitch when things gets large...
            Save();
        }

        public bool TryGetRecord(string uid, int diff, out Record record) {
            if (_records.TryGetValue(diff, out var difficulty) && difficulty.Records.TryGetValue(uid, out record))
                return true;
            record = default;
            return false;
        }

        [Serializable]
        private class Difficulty {
            public Dictionary<string, Record> Records = new();
        }

        public struct Record {
            public int Score;
            public float Accuracy;
            public int Character;
            public int Elfin;
            public string Trap;
        }
    }
}