using System.Net;
using System.Text;
using ArchipelagoMuseDash.Helpers;
using Il2Cpp;
using Il2CppAccount;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace ArchipelagoMuseDash;

public class ArchipelagoLogin {

    private const float archipelago_login_display_delay = 0.25f;
    private bool _showLoginButton;
    private bool _showLoginScreen;

    private string _ipAddress;
    private string _username;
    private string _password = "";
    private bool _showPassword;

    private string _error;
    private readonly string _lastLoginPath;

    private float _deltaTime;

    private readonly Texture2D _yesTexture;
    private readonly Texture2D _yesTextureHighlighted;
    private readonly Texture2D _noTexture;
    private readonly Texture2D _noTextureHighlighted;
    private readonly Texture2D _backgroundTexture;

    private GUIStyle _windowStyle;
    private GUIStyle _buttonYesStyle;
    private GUIStyle _buttonNoStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _labelCenterStyle;
    private GUIStyle _textFieldStyle;
    private GUIStyle _toggleStyle;
    private readonly string _versionNumber;
    private readonly string _trueVersion;

    private VersionCheckState _checkVersionState;
    private string _newVersionValue;

    private readonly Il2CppReferenceArray<GUILayoutOption> _defaultInputHeight = new(new[] {
        GUILayout.Height(25f)
    });

    private bool _hasCollected;
    private bool _hasReleased;
    private bool _backedUpFile;
    private bool _waiting;
    private bool _connectedOnce;

    public ArchipelagoLogin(string versionNumber, string trueVersion) {
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
        _trueVersion = trueVersion;

        MelonEvents.OnGUI.Subscribe(DrawArchipelagoScreen);
    }

    public void OnUpdate() {
        _showLoginButton = ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlHome");
    }

    public void SceneSwitch() {
        _deltaTime = 0;
    }

    private void DrawArchipelagoScreen() {
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

                if (!_backedUpFile)
                    BackupSaveData();

                var message = ArchipelagoStatic.SessionHandler.IsLoggedIn ? "Disconnect from Archipelago" : "Show Archipelago Login";
                if (!GUI.Button(new Rect(Screen.width - 320, Screen.height - 120, 300, 100), message, _buttonNoStyle))
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

            if (ArchipelagoStatic.SessionHandler.IsLoggedIn)
                GUI.ModalWindow(0, new Rect(Screen.width / 2.0f - 175, Screen.height / 2.0f - 90, 350, 180), (GUI.WindowFunction)DrawDisconnectWindow, "Disconnect from Archipelago", _windowStyle);
            else
                GUI.ModalWindow(0, new Rect(Screen.width / 2.0f - 250, Screen.height / 2.0f - 215, 500, 430), (GUI.WindowFunction)DrawMainWindow, "Connect to an Archipelago Server", _windowStyle);
            
            GUI.enabled = true;
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("DrawArchLogin", e);
            MelonEvents.OnGUI.Unsubscribe(DrawArchipelagoScreen);
        }
    }

    private void DrawMainWindow(int windowID) {
        if (_waiting)
            GUI.enabled = false;
        
        GUILayout.Label("", _labelStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
            GUILayout.Height(40f)
        }));

        GUILayout.Label("IP Address And Port:", _labelStyle);
        _ipAddress = GUILayout.TextField(_ipAddress, _textFieldStyle, _defaultInputHeight);

        GUILayout.Label("Username:", _labelStyle);
        _username = GUILayout.TextField(_username, _textFieldStyle, _defaultInputHeight);

        GUILayout.Label("Password:", _labelStyle);
        if (_showPassword)
            _password = GUILayout.TextField(_password, _textFieldStyle, _defaultInputHeight);
        else
            _password = GUILayout.PasswordField(_password, '*', _textFieldStyle, _defaultInputHeight);
        _showPassword = GUILayout.Toggle(_showPassword, "Show Password", _toggleStyle);

        GUILayout.Label("Version: " + _versionNumber, _labelStyle);

        GUILayout.Label(_error ?? "", _labelCenterStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
            GUILayout.Height(40f)
        }));

        GUILayout.BeginHorizontal();
        if (_connectedOnce) {
            if (GUILayout.Button("Exit Muse Dash", _buttonNoStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
                    GUILayout.Height(40f)
                })))
                QuitApplication();
        }
        else if (GUILayout.Button("Back Out", _buttonNoStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
                     GUILayout.Height(40f)
                 })))
            HideLoginOverlay();
        

        if (GUILayout.Button("Log In", _buttonYesStyle, new Il2CppReferenceArray<GUILayoutOption>(new[] {
                GUILayout.Height(40f)
            })))
            AttemptLogin();

        GUILayout.EndHorizontal();
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        switch (_checkVersionState) {
            case VersionCheckState.NotChecked: {
                if (GUILayout.Button("Check for new version", new Il2CppReferenceArray<GUILayoutOption>(new[] {
                        GUILayout.Height(30f),
                        GUILayout.Width(300f)
                    }))) {
                    _checkVersionState = VersionCheckState.Checking;
                    CheckForNewVersion().ConfigureAwait(false);
                }
                break;
            }
            case VersionCheckState.Checking:
                GUILayout.Label("Checking for update. Please wait...", _labelCenterStyle);
                break;
            case VersionCheckState.NewVersion:
                GUILayout.Label($"New Version Available: {_newVersionValue}", _labelCenterStyle);
                break;
            case VersionCheckState.UpToDate:
                GUILayout.Label("Up to date!", _labelCenterStyle);
                break;
            case VersionCheckState.Errored:
                GUILayout.Label($"An error has occured: {_newVersionValue}", _labelCenterStyle);
                break;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    private void DrawDisconnectWindow(int windowID)
    {
        if (_waiting)
            GUI.enabled = false;
            
        GUILayout.Label("", _labelStyle, GUILayout.Height(10f));
            
        GUILayout.Label("Are you sure you want to disconnect from Archipelago?", _labelCenterStyle, GUILayout.Height(40f));
        GUILayout.Label(!string.IsNullOrEmpty(_error) ? _error : "", _labelCenterStyle, GUILayout.Height(40f));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back Out", _buttonNoStyle, GUILayout.Height(40f)))
            HideLoginOverlay();

        if (GUILayout.Button("Disconnect", _buttonYesStyle, GUILayout.Height(40f)))
            Disconnect();

        GUILayout.EndHorizontal();
    }

    private void DrawForfeitRelease() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn || _showLoginScreen)
            return;

        if (!ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlHome"))
            return;

        if (!ArchipelagoStatic.SessionHandler.ItemHandler.VictoryAchieved)
            return;

        //Todo: Work out if this can be done.
        if (!_hasCollected && ArchipelagoStatic.SessionHandler.CanCollectOnVictory) {
            if (GUI.Button(new Rect(Screen.width - 220, Screen.height - 260, 200, 60), "Run !collect", _buttonNoStyle)) {
                _hasCollected = true;
                ArchipelagoStatic.SessionHandler.CollectItems();
                ShowText.ShowInfo("Collected items.");
            }
        }

        if (!_hasReleased && ArchipelagoStatic.SessionHandler.CanReleaseOnVictory) {
            if (GUI.Button(new Rect(Screen.width - 220, Screen.height - 200, 200, 60), "Run !release", _buttonNoStyle)) {
                _hasReleased = true;
                ArchipelagoStatic.SessionHandler.ReleaseItems();
                ShowText.ShowInfo("Released items.");
            }
        }
    }

    private void SetupStyles() {
        var mainState = new GUIStyleState {
            background = _backgroundTexture,
            textColor = new Color(1, 1, 1, 1)
        };

        _windowStyle = new GUIStyle(GUI.skin.window) {
            fontSize = 24,
            normal = mainState,
            hover = mainState,
            active = mainState
        };

        var yesState = new GUIStyleState {
            background = _yesTexture,
            textColor = Color.white
        };
        var yesStateHighlighted = new GUIStyleState {
            background = _yesTextureHighlighted,
            textColor = Color.white
        };

        _buttonYesStyle = new GUIStyle(GUI.skin.button) {
            fontSize = 20,
            normal = yesState,
            hover = yesStateHighlighted,
            active = yesStateHighlighted
        };

        var noState = new GUIStyleState {
            background = _noTexture,
            textColor = Color.white
        };
        var noStateHighlighted = new GUIStyleState {
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
        _labelCenterStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.UpperCenter
        };
        _textFieldStyle = new GUIStyle(GUI.skin.textField) {
            fontSize = 16
        };
        _toggleStyle = new GUIStyle(GUI.skin.toggle) {
            fontSize = 16
        };
    }

    private void AttemptLogin() {
        _waiting = true;
        AttemptLoginAsync().ConfigureAwait(false);
    }
    
    private async Task AttemptLoginAsync() {
        try {
            ArchipelagoStatic.IsLoadingAP = true;
            var ipAddress = _ipAddress.Trim();
            if (ipAddress.StartsWith("/connect"))
                ipAddress = ipAddress.Remove(0, 8).Trim();

            var reason = await ArchipelagoStatic.SessionHandler.TryFreshLogin(ipAddress, _username.Trim(), _password.Trim());
            if (reason != null) {
                _error = reason;
                _waiting = false;
                return;
            }
            
            var baseDirectory = Path.GetDirectoryName(_lastLoginPath);
            if (baseDirectory != null) {
                if (!Directory.Exists(baseDirectory))
                    Directory.CreateDirectory(baseDirectory);

                var sb = new StringBuilder();
                sb.AppendLine(_ipAddress);
                sb.AppendLine(_username);
                await File.WriteAllTextAsync(_lastLoginPath, sb.ToString());
            }

            MelonEvents.OnUpdate.Subscribe(() => {
                //Save first before doing anything
                if (!_connectedOnce) {
                    try {
                        GameAccountSystem.instance.Synchronize();
                    }
                    catch (Exception e) {
                        StopWaiting(e);
                    }
                    _connectedOnce = true;
                }

                ArchipelagoStatic.SessionHandler.StartSession();
                DataHelper.isUnlockAllMaster = true;

                HideLoginOverlay();
                ArchipelagoStatic.IsLoadingAP = false;
                RefreshSongs();
                DisableCustomAlbumsSaving();
#if DEBUG
            //Attach this to playing normally so that it can be easily triggered, once everything *should* be loaded
            ArchipelagoStatic.SongNameChanger.DumpSongsToTextFile(Path.Combine(Application.absoluteURL, "Output/MuseDashData"));
#endif
            }, unsubscribeOnFirstInvocation: true);
        }
        catch (Exception e) {
            StopWaiting(e);
            ArchipelagoStatic.ArchLogger.Error("Login", e);
            _error = e.Message;
        }
    }
    
    private void Disconnect()
    {
        _waiting = true;
        ArchipelagoStatic.SessionHandler.Disconnect().ConfigureAwait(false);
    }

    public void StopWaiting(Exception e)
    {
        if (e != null)
            ArchipelagoStatic.ArchLogger.Error("Login", e);
            
        _error = e?.Message;
        _waiting = false;
        ArchipelagoStatic.IsLoadingAP = false;
    }

    private void RefreshSongs() {
        //Force collection update
        MusicTagManager.instance.RefreshDBDisplayMusics();

        if (ArchipelagoStatic.SongSelectPanel)
            ArchipelagoStatic.SongSelectPanel.RefreshMusicFSV();
    }

    private void DisableCustomAlbumsSaving() {
        if (ArchipelagoStatic.CustomAlbumsSaveEntry == null || !ArchipelagoStatic.CustomAlbumsSaveEntry.Value)
            return;
        ArchipelagoStatic.CustomAlbumsSaveEntry.Value = false;
        ArchipelagoStatic.ArchipelagoOverridenCustomAlbums.Value = true;
    }

    private void BackupSaveData() {
        if (ArchipelagoStatic.SaveDataPath == null) {
            //This mod is possibly used by a non-steam client.
            _backedUpFile = true;
            return;
        }

        var backupPath = Path.Combine(Application.absoluteURL, "UserData", "ArchSaves");
        if (!Directory.Exists(backupPath))
            Directory.CreateDirectory(backupPath);

        var files = Directory.GetFiles(backupPath);
        if (files.Length >= 20) {
            var fileInfo = new DirectoryInfo(backupPath).GetFileSystemInfos().OrderBy(fi => fi.CreationTime).First();
            File.Delete(fileInfo.FullName);
        }

        if (File.Exists(ArchipelagoStatic.SaveDataPath))
            File.Copy(ArchipelagoStatic.SaveDataPath, Path.Combine(backupPath, DateTime.Now.ToString("yyyy.MM.dd_HHmmss") + ".sav"));

        _backedUpFile = true;
    }

    private void HideLoginOverlay() {
        var museCharacter = ArchipelagoStatic.MuseCharacter;
        if (museCharacter != null) {
            //Traverse up the tree so that we reenable all UI
            var uiParent = museCharacter.transform.parent.parent.parent;
            uiParent.gameObject.SetActive(true);
        }
        _showLoginScreen = false;
        _waiting = false;
    }

    private void QuitApplication() {
        Application.Quit();
    }

    private async Task CheckForNewVersion() {
        _waiting = true;
        try {
            //This is a very dumb, and likely to not work forever method to check updates.
            //Todo: Move to HTTPClient.
            var request = WebRequest.CreateHttp("https://github.com/DeamonHunter/ArchipelagoMuseDash/releases/latest");
            var response = await request.GetResponseAsync();

            var resolvedUrl = response.ResponseUri.ToString();
            response.Close();

            ArchipelagoStatic.ArchLogger.Log("GithubCheck", resolvedUrl);
            if (resolvedUrl.EndsWith("/"))
                resolvedUrl = resolvedUrl.Substring(0, resolvedUrl.Length - 1);

            var lastIndex = resolvedUrl.LastIndexOf('/');
            if (lastIndex > 0) {
                var version = resolvedUrl.Substring(lastIndex + 1);
                _checkVersionState = version == "v" + _trueVersion ? VersionCheckState.UpToDate : VersionCheckState.NewVersion;
                if (_checkVersionState == VersionCheckState.NewVersion)
                    _newVersionValue = version;
            }

            response.Close();
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("GithubCheck", e);
            _checkVersionState = VersionCheckState.Errored;
            _newVersionValue = e.Message;
        }
        finally {
            _waiting = false;
        }
    }

    private enum VersionCheckState {
        NotChecked,
        Checking,
        UpToDate,
        NewVersion,
        Errored
    }
}