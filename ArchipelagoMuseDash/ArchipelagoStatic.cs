using System.Collections.Generic;
using ArchipelagoMuseDash.Logging;
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
        public static bool LoggedInToGame;
        public static bool HasShownLogin;
        public static string CurrentScene;

        public static ArchipelagoSessionHandler SessionHandler;

        public static GameObject MuseCharacter;
        public static PnlStage SongSelectPanel;
        public static PnlUnlockStage UnlockStagePanel;

        public static HashSet<string> ActivatedEnableDisableHookers = new HashSet<string>();

        public static Texture2D ArchipelagoIcon;
    }
}
