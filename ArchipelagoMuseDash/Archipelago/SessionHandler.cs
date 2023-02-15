using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

namespace ArchipelagoMuseDash.Archipelago {
    /// <summary>
    /// The main class which handles the creation and usage of <see cref="ArchipelagoSession"/>.
    /// </summary>
    public class SessionHandler {
        public ItemHandler ItemHandler;
        public HintHandler HintHandler;
        public DeathLinkHandler DeathLinkHandler;

        public SongSelectAdditions SongSelectAdditions;

        public bool IsLoggedIn => _currentSession != null;

        ArchipelagoSession _currentSession;
        int _slot;
        Dictionary<string, object> _slotData;

        /// <summary>
        /// Attempts to create a new <see cref="ArchipelagoSession"/> using the data provided.<br/>
        /// - If Successful, it will create a new <see cref="ArchipelagoSession"/> and create the session.<br/>
        /// - If Unsuccessful, will return the error code that was generated.
        /// </summary>
        /// <param name="ipAddress">The IP Address (containing port) to connect to. Typically archipelago.gg:12345 or localhost:38281</param>
        /// <param name="username">The name of the slot to connect to.</param>
        /// <param name="password">The password for this slot.</param>
        /// <param name="reason">The first error code that was generated if the attempt was unsuccessful.</param>
        /// <returns>Whether the session was successfully created.</returns>
        public bool TryFreshLogin(string ipAddress, string username, string password, out string reason) {
            if (_currentSession != null)
                throw new NotImplementedException("Changing sessions is not implemented atm.");

            var session = ArchipelagoSessionFactory.CreateSession(ipAddress);

            var loginResult = session.TryConnectAndLogin("Muse Dash", username, ItemsHandlingFlags.AllItems, password: password);

            if (!loginResult.Successful) {
                var failed = (LoginFailure)loginResult;

                //Todo: When does multiple errors show up? And should we handle that case here.
                reason = failed.Errors[0];
                return false;
            }

            var successful = (LoginSuccessful)loginResult;

            _slot = successful.Slot;
            _slotData = successful.SlotData;
            _currentSession = session;

            reason = null;
            return true;
        }

        /// <summary>
        /// Starts up all Archipelago related services. Should be started after loading save file to ensure nothing is broken.
        /// </summary>
        public void StartSession() {
            try {
                ItemHandler = new ItemHandler(_currentSession, _slot);
                HintHandler = new HintHandler(_currentSession, _slot);
                DeathLinkHandler = new DeathLinkHandler(_currentSession, _slot, _slotData);
                SongSelectAdditions = new SongSelectAdditions();

                _currentSession.MessageLog.OnMessageReceived += ArchipelagoStatic.ArchLogger.LogMessage;

                ArchipelagoStatic.AlbumDatabase.Setup();
                ItemHandler.Setup(_slotData);
                HintHandler.Setup();
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("ItemHandler", e);
            }
        }

        /// <summary>
        /// Runs on Every Unity's OnUpdate(). Passes the update along to child handlers, if the session was started.
        /// </summary>
        public void OnUpdate() {
            if (!IsLoggedIn)
                return;

            SongSelectAdditions.OnUpdate();

            ItemHandler.OnUpdate();
            ItemHandler.Unlocker.OnUpdate();
            HintHandler.OnUpdate();
            DeathLinkHandler.Update();
        }

        /// <summary>
        /// Runs on Every Unity's OnLateUpdate(). Passes the update along to child handlers, if the session was started.
        /// </summary>
        public void OnLateUpdate() {
            if (!IsLoggedIn)
                return;

            ItemHandler.Unlocker.OnLateUpdate();
        }

        /// <summary>
        /// Runs on Unity's Scene has changed.
        /// </summary>
        public void SceneChanged(string sceneName) {
            if (sceneName != "UISystem_PC")
                return;

            SongSelectAdditions?.MainSceneLoaded();
        }
    }
}
