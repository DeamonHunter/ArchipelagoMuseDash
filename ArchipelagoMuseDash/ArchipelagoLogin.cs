using System;
using System.IO;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Il2CppSystem.Text;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

namespace ArchipelagoMuseDash {
    public class ArchipelagoLogin {
        string _ipAddress;
        string _username;
        string _password;

        string _error;
        string _lastLoginPath;

        float _deltaTime;

        //Todo: Is there some trigger we can check to see if the loading screen has been removed? 
        const float archipelago_login_display_delay = 1f; //Delays displaying the login for archipelago so it doesn't show over the loading screen

        GUIStyle _buttonStyle;
        GUIStyle _labelStyle;
        GUIStyle _textFieldStyle;
        GUIStyle _windowStyle;

        public ArchipelagoLogin() {
#if DEBUG
            _ipAddress = "localhost:38281";
#else
            _ipAddress = "archipelago.gg:38281";
#endif
            _lastLoginPath = Path.Combine(Application.absoluteURL, "UserData/ArchSaves/LastLogin.txt");
        }

        public void ShowLoginScreen() {
            if (File.Exists(_lastLoginPath)) {
                using (var file = File.OpenRead(_lastLoginPath)) {
                    using (var sr = new StreamReader(file)) {
                        _ipAddress = sr.ReadLine();
                        _username = sr.ReadLine();
                    }
                }
            }


            ArchipelagoStatic.LoggedInToGame = false;
            MelonEvents.OnGUI.Subscribe(DrawArchLogin);
        }

        void DrawArchLogin() {
            try {
                _deltaTime += Time.deltaTime;

                var museCharacter = ArchipelagoStatic.MuseCharacter;
                if (museCharacter == null)
                    return;

                //Traverse up the tree so that we disable all UI but the background
                var uiParent = museCharacter.transform.parent.parent.parent;
                uiParent.gameObject.SetActive(false);

                //Give some time for the actual background to show up
                if (_deltaTime < archipelago_login_display_delay)
                    return;

                if (_buttonStyle == null)
                    SetupStyles();

                GUI.ModalWindow(0, new Rect(Screen.width / 2.0f - 250, Screen.height / 2.0f - 190, 500, 380), (GUI.WindowFunction)DrawArchWindow, "Connect to an Archipelago Server", _windowStyle);
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("DrawArchLogin", e);
                MelonEvents.OnGUI.Unsubscribe(DrawArchLogin);
            }
        }

        void DrawArchWindow(int windowID) {
            GUILayout.Label("IP Address And Port:", _labelStyle, null);
            _ipAddress = GUILayout.TextField(_ipAddress, _textFieldStyle, null);

            GUILayout.Label("Username:", _labelStyle, null);
            _username = GUILayout.TextField(_username, _textFieldStyle, null);

            GUILayout.Label("Password:", _labelStyle, null);
            _password = GUILayout.TextField(_password, _textFieldStyle, null);

            GUILayout.Label(_error ?? "", new Il2CppReferenceArray<GUILayoutOption>(new GUILayoutOption[] {
                GUILayout.Height(120f)
            }));


            if (GUILayout.Button("Log In", _buttonStyle, null))
                AttemptLogin();

            if (GUILayout.Button("Play Without Archipelago", _buttonStyle, null))
                AttemptPlayNormally();
        }

        void SetupStyles() {
            _labelStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 16
            };
            _textFieldStyle = new GUIStyle(GUI.skin.textField) {
                fontSize = 16
            };
            _buttonStyle = new GUIStyle(GUI.skin.button) {
                fontSize = 16
            };
            _windowStyle = new GUIStyle(GUI.skin.window) {
                fontSize = 16
            };
        }

        void AttemptLogin() {
            try {
                var session = ArchipelagoSessionFactory.CreateSession(_ipAddress);

                var loginResult = session.TryConnectAndLogin("Muse Dash", _username, ItemsHandlingFlags.AllItems, password: _password);

                if (!loginResult.Successful) {
                    _error = "Failed to connect to a slot. Ensure you have typed your username correctly";
                    return;
                }

                ArchipelagoStatic.LoggedInToGame = true;

                var successful = (LoginSuccessful)loginResult;
                ArchipelagoStatic.SessionHandler.RegisterSession(session, successful.Slot, successful.SlotData);

                var sb = new StringBuilder();
                sb.AppendLine(_ipAddress);
                sb.AppendLine(_username);
                File.WriteAllText(_lastLoginPath, sb.ToString());
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Login", e);
                _error = e.Message;
                return;
            }

            HideLoginOverlay();
        }

        void AttemptPlayNormally() {
            ArchipelagoStatic.SteamSync.m_FolderPath = ArchipelagoStatic.OriginalFolderName;
            ArchipelagoStatic.SteamSync.m_FilePath = ArchipelagoStatic.SteamSync.m_FolderPath + "/" + ArchipelagoStatic.SteamSync.m_FileName;
            ArchipelagoStatic.SteamSync.LoadLocal();
            HideLoginOverlay();
        }

        void HideLoginOverlay() {
            var museCharacter = ArchipelagoStatic.MuseCharacter;
            if (museCharacter != null) {
                //Traverse up the tree so that we reenable all UI
                var uiParent = museCharacter.transform.parent.parent.parent;
                uiParent.gameObject.SetActive(true);
            }

            MelonEvents.OnGUI.Unsubscribe(DrawArchLogin);
        }


        public void LogOut() {
            //Todo: Actually use this
        }
    }
}
