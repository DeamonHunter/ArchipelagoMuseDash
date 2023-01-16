using Assets.Scripts.Database;

namespace ArchipelagoMuseDash.Archipelago {
    public class MuseDashItem {
        public MusicInfo NewMusic;
        public string ItemName;
        public string PlayerName;

        public MuseDashItem(MusicInfo music) {
            NewMusic = music;
        }

        public MuseDashItem(string itemName, string playerName) {
            ItemName = itemName;
            PlayerName = playerName;
        }
    }
}
