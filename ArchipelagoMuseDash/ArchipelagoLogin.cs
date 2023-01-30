using System;
using System.IO;
using System.Text;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Nice.Datas;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

namespace ArchipelagoMuseDash {
    public class ArchipelagoLogin {
        public bool HasBeenShown;

        string _ipAddress;
        string _username;
        string _password;

        string _error;
        readonly string _lastLoginPath;

        float _deltaTime;
        bool _fixSave;

        //Todo: Is there some trigger we can check to see if the loading screen has been removed? 
        const float archipelago_login_display_delay = 1f; //Delays displaying the login for archipelago so it doesn't show over the loading screen

        GUIStyle _buttonStyle;
        GUIStyle _labelStyle;
        GUIStyle _textFieldStyle;
        GUIStyle _toggleStyle;
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

            HasBeenShown = true;
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

            GUILayout.Label(_error ?? "", _labelStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
                GUILayout.Height(110f)
            }));

            if (GUILayout.Button("Log In", _buttonStyle, null))
                AttemptLogin();

            _fixSave = GUILayout.Toggle(_fixSave, "Reset all songs to be visible.", _toggleStyle, null);
            if (GUILayout.Button("Play Without Archipelago", _buttonStyle, null))
                AttemptPlayNormally(_fixSave);
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
            _toggleStyle = new GUIStyle(GUI.skin.toggle) {
                fontSize = 16
            };
        }

        void AttemptLogin() {
            try {
                if (!ArchipelagoStatic.SessionHandler.TryFreshLogin(_ipAddress, _username, _password, out var reason)) {
                    _error = reason;
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine(_ipAddress);
                sb.AppendLine(_username);
                File.WriteAllText(_lastLoginPath, sb.ToString());

                HideLoginOverlay();
            }
            catch (Exception e) {
                ArchipelagoStatic.ArchLogger.Error("Login", e);
                _error = e.Message;
            }
        }

        void AttemptPlayNormally(bool fixGame) {
#if DEBUG
            //Attach this to playing normally so that it can be easily triggered, once everything *should* be loaded
            ArchipelagoStatic.SongNameChanger.DumpSongsToTextFile(Path.Combine(Application.absoluteURL, "Output/SongDump.txt"));
#endif

            ArchipelagoStatic.SteamSync.m_FolderPath = ArchipelagoStatic.OriginalFolderName;
            ArchipelagoStatic.SteamSync.m_FilePath = ArchipelagoStatic.SteamSync.m_FolderPath + "/" + ArchipelagoStatic.SteamSync.m_FileName;

            DataManager.instance.Load();
            GlobalDataBase.dbMusicTag.InitDatabase();

            if (fixGame)
                FixMyGame();
            HideLoginOverlay();

            //Force collection update
            MusicTagManager.instance.RefreshStageDisplayMusics(-1);
            ArchipelagoStatic.SongSelectPanel?.RefreshMusicFSV();
        }

        /// <summary>
        /// Unhide all songs. In case something breaks.
        /// </summary>
        public void FixMyGame() {
            var list = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetAllMusicInfo(list);

            foreach (var musicInfo in list)
                GlobalDataBase.dbMusicTag.RemoveHide(musicInfo);

            DataManager.instance.Save();
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
    }
}
