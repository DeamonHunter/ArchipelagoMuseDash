using System;
using ArchipelagoMuseDash;
using ArchipelagoMuseDash.Logging;
using ArchipelagoMuseDash.Patches;
using Il2CppSystem.IO;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ArchipelagoMuseDashMod), "Archipelago Muse Dash", "0.1.0", "DeamonHunter")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace ArchipelagoMuseDash {
    /// <summary>
    /// The Archipelago mod. MelonLoader's entry point.
    /// </summary>
    public class ArchipelagoMuseDashMod : MelonMod {
        public override void OnInitializeMelon() {
            base.OnInitializeMelon();
            ArchipelagoStatic.ArchLogger = new ArchLogger();
            ArchipelagoStatic.Login = new ArchipelagoLogin();
            ArchipelagoStatic.SessionHandler = new ArchipelagoSessionHandler();
            LoadExternalAssets();
            WebAPIPatch.DoPatching(HarmonyInstance);
        }

        void LoadExternalAssets() {
            var path = Path.Combine(Application.absoluteURL, "Mods/ArchIcon.png");

            var bytes = File.ReadAllBytes(path);
            var archIconTexture = new Texture2D(1, 1);
            archIconTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            archIconTexture.wrapMode = TextureWrapMode.Clamp;

            ImageConversion.LoadImage(archIconTexture, bytes);
            ArchipelagoStatic.ArchipelagoIcon = archIconTexture;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            ArchipelagoStatic.ArchLogger.Log("Scene Load", $"{sceneName} was loaded.");

            ArchipelagoStatic.CurrentScene = sceneName;

            try {
                if (ArchipelagoStatic.HasShownLogin || sceneName != "UISystem_PC")
                    return;

                ArchipelagoStatic.HasShownLogin = true;

                ArchipelagoStatic.Login.ShowLoginScreen();
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Scene Load", e);
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (!ArchipelagoStatic.LoggedInToGame)
                return;

            ArchipelagoStatic.SessionHandler.CheckForNewItems(false);

            if (ArchipelagoStatic.CurrentScene == "UISystem_PC")
                ArchipelagoStatic.SessionHandler.HandleNewItems();
        }

        public override void OnLateUpdate() {
            base.OnLateUpdate();
            if (!ArchipelagoStatic.LoggedInToGame)
                return;

            if (ArchipelagoStatic.CurrentScene == "UISystem_PC")
                ArchipelagoStatic.SessionHandler.HandleLock();
        }
    }
}
