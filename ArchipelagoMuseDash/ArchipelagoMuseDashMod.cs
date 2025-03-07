using ArchipelagoMuseDash;
using ArchipelagoMuseDash.Archipelago;
using ArchipelagoMuseDash.Helpers;
using ArchipelagoMuseDash.Logging;
using MelonLoader;

[assembly: MelonInfo(typeof(ArchipelagoMuseDashMod), "Archipelago Muse Dash", "1.5.12", "DeamonHunter")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]
[assembly: MelonPriority(100)]

namespace ArchipelagoMuseDash;

public class ArchipelagoMuseDashMod : MelonMod {
    private bool _attemptedPreferencesLoad;

    public override void OnInitializeMelon() {
        base.OnInitializeMelon();
        AssetHelpers.Assembly = MelonAssembly;

        ArchipelagoStatic.ArchLogger = new ArchLogger();
        ArchipelagoStatic.Login = new ArchipelagoLogin(Info.Version, Info.Version);
        ArchipelagoStatic.SessionHandler = new SessionHandler();
        ArchipelagoStatic.Records = new ArchipelagoRecords();

        ArchipelagoStatic.ArchipelagoIcons = new[] {
            AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APProgression.png"),
            AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APUseful.png"),
            AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APTrash.png"),
            AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APTrap.png")
        };

        using (var stream = MelonAssembly.Assembly.GetManifestResourceStream("ArchipelagoMuseDash.Assets.SongNameReplacements.json"))
            ArchipelagoStatic.SongNameChanger = new SongNameChanger(stream);

        using (var stream = MelonAssembly.Assembly.GetManifestResourceStream("ArchipelagoMuseDash.Assets.MuseDashData.txt"))
            ArchipelagoStatic.AlbumDatabase.LoadMusicList(stream);

        LoadPreferences();
        ArchipelagoStatic.Records.Load();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
        base.OnSceneWasLoaded(buildIndex, sceneName);

        try {
            ArchipelagoStatic.ArchLogger.LogDebug("Scene Load", $"{sceneName} was loaded.");

            if (sceneName == "Welcome" && !_attemptedPreferencesLoad) {
                _attemptedPreferencesLoad = true;
                LoadPreferences();
            }

            ArchipelagoStatic.CurrentScene = sceneName;
            ArchipelagoStatic.Login.SceneSwitch();
            ArchipelagoStatic.SessionHandler.SceneChanged(sceneName);
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("Scene Load", e);
        }
    }

    private void LoadPreferences() {
        ArchipelagoStatic.ArchipelagoOverridenCustomAlbums ??= MelonPreferences.CreateEntry("ArchipelagoMuseDash", "OverridenCustomAlbums", false);
        ArchipelagoStatic.CustomAlbumsSaveEntry ??= MelonPreferences.GetEntry<bool>("CustomAlbums", "SavingEnabled");

        //Make sure that it got reset properly
        if (ArchipelagoStatic.CustomAlbumsSaveEntry == null || !ArchipelagoStatic.ArchipelagoOverridenCustomAlbums.Value)
            return;

        ArchipelagoStatic.CustomAlbumsSaveEntry.Value = true;
        ArchipelagoStatic.ArchipelagoOverridenCustomAlbums.Value = false;
    }

    public override void OnApplicationQuit() {
        base.OnApplicationQuit();

        if (!ArchipelagoStatic.ArchipelagoOverridenCustomAlbums.Value)
            return;
        ArchipelagoStatic.CustomAlbumsSaveEntry.Value = true;
        ArchipelagoStatic.ArchipelagoOverridenCustomAlbums.Value = false;
        MelonPreferences.Save();
    }

    public override void OnUpdate() {
        base.OnUpdate();

        //if (Input.GetKeyDown(KeyCode.F5))
        //    AssetHelpers.PrintoutAllGameObjects();

        ArchipelagoStatic.Login.OnUpdate();

        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.SessionHandler.OnUpdate();
    }

    public override void OnLateUpdate() {
        base.OnLateUpdate();
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        ArchipelagoStatic.SessionHandler.OnLateUpdate();
    }
}