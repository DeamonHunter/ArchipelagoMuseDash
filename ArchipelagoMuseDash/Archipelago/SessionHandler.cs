using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Il2CppSystem.IO;
using UnityEngine;

// Due to how IL2CPP works, some things can't be invoked as an extension.
// ReSharper disable InvokeAsExtensionMethod

namespace ArchipelagoMuseDash.Archipelago {
    /// <summary>
    /// Handles the Archipelago session after we've logged in.
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

        public bool TryFreshLogin(string ipAddress, string username, string password, out string reason) {
            var session = ArchipelagoSessionFactory.CreateSession(ipAddress);

            var loginResult = session.TryConnectAndLogin("Muse Dash", username, ItemsHandlingFlags.AllItems, password: password);

            if (!loginResult.Successful) {
                var failed = (LoginFailure)loginResult;
                reason = failed.Errors[0];

                //_error = "Failed to connect to a slot. Ensure you have typed your username correctly";
                return false;
            }


            var successful = (LoginSuccessful)loginResult;
            StartSession(session, successful.Slot, successful.SlotData);
            reason = null;
            return true;
        }


        void StartSession(ArchipelagoSession session, int slot, Dictionary<string, object> slotData) {
            if (_currentSession != null)
                throw new NotImplementedException("Changing sessions is not implemented atm.");

            _slot = slot;
            _slotData = slotData;
            _currentSession = session;

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

        public void OnUpdate() {
            if (!IsLoggedIn)
                return;

            SongSelectAdditions.OnUpdate();

            ItemHandler.OnUpdate();
            ItemHandler.Unlocker.OnUpdate();
            HintHandler.OnUpdate();
        }

        public void OnLateUpdate() {
            if (!IsLoggedIn)
                return;

            ItemHandler.Unlocker.OnLateUpdate();
        }

        public void SceneChanged(string sceneName) {
            if (sceneName != "UISystem_PC")
                return;

            if (!ArchipelagoStatic.Login.HasBeenShown)
                ArchipelagoStatic.Login.ShowLoginScreen();

            SongSelectAdditions?.MainSceneLoaded();
        }
    }
}
