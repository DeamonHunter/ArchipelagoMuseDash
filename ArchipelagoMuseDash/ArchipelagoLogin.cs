using System.Text;
using ArchipelagoMuseDash.Helpers;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace ArchipelagoMuseDash;

public class ArchipelagoLogin {
    private bool _showLoginButton;
    private bool _showLoginScreen;

    private string _ipAddress;
    private string _username;
    private string _password;

    private string _error;
    private readonly string _lastLoginPath;

    private float _deltaTime;

    private const float archipelago_login_display_delay = 0.25f;

    private Texture2D _yesTexture;
    private Texture2D _yesTextureHighlighted;
    private Texture2D _noTexture;
    private Texture2D _noTextureHighlighted;
    private Texture2D _backgroundTexture;

    private GUIStyle _windowStyle;
    private GUIStyle _buttonYesStyle;
    private GUIStyle _buttonNoStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _textFieldStyle;
    private string _versionNumber;

    private bool _hasCollected;
    private bool _hasReleased;

    public ArchipelagoLogin(string versionNumber) {
#if DEBUG
        _ipAddress = "localhost:38281";
#else
        _ipAddress = "archipelago.gg:38281";
#endif
        _lastLoginPath = Path.Combine(Application.absoluteURL, "UserData/ArchSaves/LastLogin.txt");

        _backgroundTexture = AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.LoginBackground.png");
        _yesTexture = AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.ButtonYes.png");
        _yesTextureHighlighted = AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.ButtonYesHighlighted.png");
        _noTexture = AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.ButtonNo.png");
        _noTextureHighlighted = AssetHelpers.LoadTexture("ArchipelagoMuseDash.Assets.ButtonNoHighlighted.png");

        _versionNumber = versionNumber;

        MelonEvents.OnGUI.Subscribe(DrawArchLogin);
    }

    public void OnUpdate() {
        _showLoginButton = !ArchipelagoStatic.SessionHandler.IsLoggedIn && ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlHome");
    }

    public void SceneSwitch() {
        _deltaTime = 0;
    }

    private void DrawArchLogin() {
        try {
            //Can only call this in GUI
            if (_windowStyle == null)
                SetupStyles(); //Todo: Add textures

            DrawForfeitRelease();

            if (!_showLoginScreen) {
                if (!_showLoginButton)
                    return;

                if (ArchipelagoStatic.LoadingSceneActive)
                    return;

                //Due to the way muse dash works, the home screen is enable for a frame or two after the load screen finishes.
                if (_deltaTime < archipelago_login_display_delay) {
                    _deltaTime += Time.deltaTime;
                    return;
                }

                if (!GUI.Button(new Rect(Screen.width - 320, Screen.height - 120, 300, 100), "Show Archipelago Login", _buttonNoStyle))
                    return;

                _showLoginScreen = true;

                if (File.Exists(_lastLoginPath)) {
                    using (var file = File.OpenRead(_lastLoginPath)) {
                        using (var sr = new StreamReader(file)) {
                            _ipAddress = sr.ReadLine();
                            _username = sr.ReadLine();
                        }
                    }
                }
            }

            var museCharacter = ArchipelagoStatic.MuseCharacter;
            if (museCharacter == null)
                return;

            //Traverse up the tree so that we disable all UI but the background
            var uiParent = museCharacter.transform.parent.parent.parent;
            uiParent.gameObject.SetActive(false);

            GUI.ModalWindow(0, new Rect(Screen.width / 2.0f - 250, Screen.height / 2.0f - 185, 500, 370), (GUI.WindowFunction)DrawArchWindow, "Connect to an Archipelago Server", _windowStyle);
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("DrawArchLogin", e);
            MelonEvents.OnGUI.Unsubscribe(DrawArchLogin);
        }
    }

    private void DrawArchWindow(int windowID) {
        GUILayout.Label("", _labelStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
            GUILayout.Height(40f)
        }));

        GUILayout.Label("IP Address And Port:", _labelStyle);
        _ipAddress = GUILayout.TextField(_ipAddress, _textFieldStyle);

        GUILayout.Label("Username:", _labelStyle);
        _username = GUILayout.TextField(_username, _textFieldStyle);

        GUILayout.Label("Password:", _labelStyle);
        _password = GUILayout.TextField(_password, _textFieldStyle);

        GUILayout.Label("Version: " + _versionNumber, _labelStyle);

        GUILayout.Label(_error ?? "", _labelStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
            GUILayout.Height(60f)
        }));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back Out", _buttonNoStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
                GUILayout.Height(40f)
            })))
            HideLoginOverlay();

        if (GUILayout.Button("Log In", _buttonYesStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
                GUILayout.Height(40f)
            })))
            AttemptLogin();

        GUILayout.EndHorizontal();
    }

    private void DrawForfeitRelease() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        if (!ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlHome"))
            return;

        if (!ArchipelagoStatic.SessionHandler.ItemHandler.VictoryAchieved)
            return;

        //Todo: Work out if this can be done.
        if (!_hasCollected && ArchipelagoStatic.SessionHandler.CanCollectOnVictory) {
            if (GUI.Button(new Rect(Screen.width - 220, Screen.height - 160, 200, 60), "Run !collect", _buttonNoStyle)) {
                _hasCollected = true;
                ArchipelagoStatic.SessionHandler.CollectItems();
                ShowText.ShowInfo("Collected items.");
            }
        }

        if (!_hasReleased && ArchipelagoStatic.SessionHandler.CanReleaseOnVictory) {
            if (GUI.Button(new Rect(Screen.width - 220, Screen.height - 80, 200, 60), "Run !release", _buttonNoStyle)) {
                _hasReleased = true;
                ArchipelagoStatic.SessionHandler.ReleaseItems();
                ShowText.ShowInfo("Released items.");
            }
        }
    }

    private void SetupStyles() {
        var mainState = new GUIStyleState() {
            background = _backgroundTexture,
            textColor = new Color(1, 1, 1, 1)
        };

        _windowStyle = new GUIStyle(GUI.skin.window) {
            fontSize = 24,
            normal = mainState,
            hover = mainState,
            active = mainState
        };

        var yesState = new GUIStyleState() {
            background = _yesTexture,
            textColor = Color.white
        };
        var yesStateHighlighted = new GUIStyleState() {
            background = _yesTextureHighlighted,
            textColor = Color.white
        };

        _buttonYesStyle = new GUIStyle(GUI.skin.button) {
            fontSize = 20,
            normal = yesState,
            hover = yesStateHighlighted,
            active = yesStateHighlighted
        };

        var noState = new GUIStyleState() {
            background = _noTexture,
            textColor = Color.white
        };
        var noStateHighlighted = new GUIStyleState() {
            background = _noTextureHighlighted,
            textColor = Color.white
        };

        _buttonNoStyle = new GUIStyle(GUI.skin.button) {
            fontSize = 20,
            normal = noState,
            hover = noStateHighlighted,
            active = noStateHighlighted
        };

        _labelStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 16
        };
        _textFieldStyle = new GUIStyle(GUI.skin.textField) {
            fontSize = 16
        };
    }

    private void AttemptLogin() {
        try {
            if (!ArchipelagoStatic.SessionHandler.TryFreshLogin(_ipAddress, _username, _password, out var reason)) {
                _error = reason;
                return;
            }

            var baseDirectory = Path.GetDirectoryName(_lastLoginPath);
            if (baseDirectory != null) {
                if (!Directory.Exists(baseDirectory))
                    Directory.CreateDirectory(baseDirectory);

                var sb = new StringBuilder();
                sb.AppendLine(_ipAddress);
                sb.AppendLine(_username);
                File.WriteAllText(_lastLoginPath, sb.ToString());
            }

            SwapToArchipelagoSave();
            ArchipelagoStatic.SessionHandler.StartSession();
#if DEBUG
            //Attach this to playing normally so that it can be easily triggered, once everything *should* be loaded
            ArchipelagoStatic.SongNameChanger.DumpSongsToTextFile(Path.Combine(Application.absoluteURL, "Output/SongDump.txt"));
#endif
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("Login", e);
            _error = e.Message;
        }
    }

    private void SwapToArchipelagoSave() {
        var path = Path.Combine(Application.absoluteURL, "UserData/ArchSaves");
        ArchipelagoStatic.SteamSync.m_FolderPath = path;
        ArchipelagoStatic.SteamSync.m_FilePath = ArchipelagoStatic.SteamSync.m_FolderPath + "/" + ArchipelagoStatic.SteamSync.m_FileName;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        //Copy over current data
        if (!File.Exists(ArchipelagoStatic.SteamSync.m_FilePath) && File.Exists(ArchipelagoStatic.OriginalFilePath))
            File.Copy(ArchipelagoStatic.OriginalFilePath, ArchipelagoStatic.SteamSync.m_FilePath);

        DataHelper.isUnlockAllMaster = true;

        //Force collection update
        MusicTagManager.instance.RefreshDBDisplayMusics();

        if (ArchipelagoStatic.SongSelectPanel)
            ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();

        HideLoginOverlay();
    }

    private void HideLoginOverlay() {
        var museCharacter = ArchipelagoStatic.MuseCharacter;
        if (museCharacter != null) {
            //Traverse up the tree so that we reenable all UI
            var uiParent = museCharacter.transform.parent.parent.parent;
            uiParent.gameObject.SetActive(true);
        }
        _showLoginScreen = false;
    }
}