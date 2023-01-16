using System.Collections.Generic;
using ArchipelagoMuseDash.Archipelago;
using ArchipelagoMuseDash.Logging;
using Assets.Scripts.PeroTools.Platforms.Steam;
using Assets.Scripts.UI.Panels;
using UnityEngine;

namespace ArchipelagoMuseDash {
    /// <summary>
    /// Static Helper class which stores information to make it easier for patches to do things
    /// </summary>
    public static class ArchipelagoStatic {
        public static ArchLogger ArchLogger;

        public static AlbumDatabase AlbumDatabase = new AlbumDatabase();
        public static ArchipelagoLogin Login;
        public static SessionHandler SessionHandler;
        public static Texture2D ArchipelagoIcon;

        public static string CurrentScene;
        public static HashSet<string> ActivatedEnableDisableHookers = new HashSet<string>();

        public static GameObject MuseCharacter;
        public static PnlStage SongSelectPanel;
        public static PnlUnlockStage UnlockStagePanel;

        public static SteamSync SteamSync;
        public static string OriginalFolderName;
        public static string OriginalFilePath;
    }
}
