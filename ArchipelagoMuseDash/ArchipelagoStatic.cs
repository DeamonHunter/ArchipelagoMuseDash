﻿using ArchipelagoMuseDash.Archipelago;
using ArchipelagoMuseDash.Helpers;
using ArchipelagoMuseDash.Logging;
using Il2Cpp;
using Il2CppAssets.Scripts.UI.Panels;
using Il2CppFormulaBase;
using MelonLoader;
using UnityEngine;

namespace ArchipelagoMuseDash;

/// <summary>
///     Static Helper class which stores information to make it easier for patches to do things
/// </summary>
public static class ArchipelagoStatic {
    public static ArchLogger ArchLogger;

    //Loaded Assets
    public static SongNameChanger SongNameChanger;
    public static Texture2D[] ArchipelagoIcons;

    //Archipelago Controllers
    public static readonly AlbumDatabase AlbumDatabase = new();
    public static ArchipelagoLogin Login;
    public static SessionHandler SessionHandler;
    public static ArchipelagoRecords Records;

    //Needed MuseDash components
    public static bool LoadingSceneActive;
    public static string CurrentScene;
    public static StageBattleComponent BattleComponent;
    public static readonly HashSet<string> ActivatedEnableDisableHookers = new();
    public static GameObject MuseCharacter;
    public static PnlStage SongSelectPanel;
    public static PnlPreparation PreparationPanel;
    public static PnlUnlockStage UnlockStagePanel;
    public static SongHideAskMsg HideSongDialogue;

    public static string SaveDataPath;

    public static int ExtraLifesUsed;
    public static GameObject PlaceholderElfin;

    //this state is kept track of to make sure the application of AP song order
    //doesn't run uselessly over and over for each added song
    //without being visible
    public static bool IsLoadingAP = false;

    //Custom Albums 4 Workaround
    public static MelonPreferences_Entry<bool> CustomAlbumsSaveEntry;
    public static MelonPreferences_Entry<bool> ArchipelagoOverridenCustomAlbums;
}