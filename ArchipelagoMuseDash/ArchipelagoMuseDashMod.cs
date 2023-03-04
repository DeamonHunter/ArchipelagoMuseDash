using System;
using ArchipelagoMuseDash;
using ArchipelagoMuseDash.Archipelago;
using ArchipelagoMuseDash.Helpers;
using ArchipelagoMuseDash.Logging;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ArchipelagoMuseDashMod), "Archipelago Muse Dash", "0.7.0", "DeamonHunter")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace ArchipelagoMuseDash {
    /// <summary>
    /// The Archipelago mod. MelonLoader's entry point.
    /// </summary>
    public class ArchipelagoMuseDashMod : MelonMod {
        public override void OnInitializeMelon() {
            base.OnInitializeMelon();
            AssetHelpers.Assembly = MelonAssembly;

            ArchipelagoStatic.ArchLogger = new ArchLogger();
            ArchipelagoStatic.Login = new ArchipelagoLogin(Info.Version);
            ArchipelagoStatic.SessionHandler = new SessionHandler();

            ArchipelagoStatic.ArchipelagoIcons = new Texture2D[] {
                AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APIcon.png"),
                AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APUseful.png"),
                AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APTrash.png"),
                AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.APTrap.png"),
            };

            using (var stream = MelonAssembly.Assembly.GetManifestResourceStream("ArchipelagoMuseDash.Assets.SongNameReplacements.json"))
                ArchipelagoStatic.SongNameChanger = new SongNameChanger(stream);
        }


        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            try {
                ArchipelagoStatic.ArchLogger.LogDebug("Scene Load", $"{sceneName} was loaded.");

                ArchipelagoStatic.CurrentScene = sceneName;
                ArchipelagoStatic.Login.SceneSwitch();
                ArchipelagoStatic.SessionHandler.SceneChanged(sceneName);
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Scene Load", e);
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();

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
}
