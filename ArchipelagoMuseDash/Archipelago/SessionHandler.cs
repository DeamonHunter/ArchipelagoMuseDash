using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;

namespace ArchipelagoMuseDash.Archipelago;

/// <summary>
///     The main class which handles the creation and usage of <see cref="ArchipelagoSession" />.
/// </summary>
public class SessionHandler {
    public ItemHandler ItemHandler;
    public HintHandler HintHandler;
    public DeathLinkHandler DeathLinkHandler;
    public BattleHandler BattleHandler;
    public DataStorageHandler DataStorageHandler;

    public SongSelectAdditions SongSelectAdditions;

    private bool _sessionStarted;
    private ArchipelagoSession _currentSession;
    private int _slot;
    private int _team;
    private Dictionary<string, object> _slotData;

    public bool CanReleaseOnVictory { get; private set; }
    public bool CanCollectOnVictory { get; private set; }

    public bool IsLoggedIn => _currentSession != null && _sessionStarted;


    /// <summary>
    ///     Attempts to create a new <see cref="ArchipelagoSession" /> using the data provided.<br />
    ///     - If Successful, it will create a new <see cref="ArchipelagoSession" /> and create the session.<br />
    ///     - If Unsuccessful, will return the error code that was generated.
    /// </summary>
    /// <param name="ipAddress">
    ///     The IP Address (containing port) to connect to. Typically archipelago.gg:12345 or
    ///     localhost:38281
    /// </param>
    /// <param name="username">The name of the slot to connect to.</param>
    /// <param name="password">The password for this slot.</param>
    /// <param name="reason">The first error code that was generated if the attempt was unsuccessful.</param>
    /// <returns>Whether the session was successfully created.</returns>
    public async Task<string?>  TryFreshLogin(string ipAddress, string username, string password) {
        if (_currentSession != null)
            throw new Exception("Already Connected!");

        var session = ArchipelagoSessionFactory.CreateSession(ipAddress);
        session.Socket.ErrorReceived += (exception, message) => {
            ArchipelagoStatic.ArchLogger.Log("[ArchError]", $"{message} : {exception}");
        };

        LoginResult loginResult;
        try
        {
            await session.ConnectAsync();
            loginResult = await session.LoginAsync("Muse Dash", username, ItemsHandlingFlags.AllItems, password: password);
        }
        catch (TaskCanceledException e)
        {
            loginResult = new LoginFailure("Timed out. Please check connection info.");
            ArchipelagoStatic.ArchLogger.Error("Login", e);
        }

        if (!loginResult.Successful) {
            var failed = (LoginFailure)loginResult;

            //Todo: When does multiple errors show up? And should we handle that case here.
            return failed.Errors[0];
        }

        var successful = (LoginSuccessful)loginResult;

        _slot = successful.Slot;
        _team = successful.Team;
        _slotData = successful.SlotData;
        _currentSession = session;

        return null;
    }
    
    /// <summary>
    ///     Starts up all Archipelago related services. Should be started after loading save file to ensure nothing is broken.
    /// </summary>
    public void StartSession() {
        _sessionStarted = true;
        try {
            DataStorageHandler = new DataStorageHandler(_slot, _team, _currentSession.DataStorage);
            ItemHandler = new ItemHandler(_currentSession, _slot);
            HintHandler = new HintHandler(_currentSession, _slot);
            DeathLinkHandler = new DeathLinkHandler(_currentSession, _slot, _slotData);
            BattleHandler = new BattleHandler();
            SongSelectAdditions = new SongSelectAdditions();

            _currentSession.MessageLog.OnMessageReceived += ArchipelagoStatic.ArchLogger.LogMessage;

            CanReleaseOnVictory = (_currentSession.RoomState.ReleasePermissions & (Permissions.Enabled | Permissions.Goal)) != 0;
            CanCollectOnVictory = (_currentSession.RoomState.CollectPermissions & (Permissions.Enabled | Permissions.Goal)) != 0;

            var hasItems = _currentSession.RoomState.Version.Major > 0 || _currentSession.RoomState.Version.Minor > 4
                || (_currentSession.RoomState.Version.Minor == 4 && _currentSession.RoomState.Version.Build > 5);
            ArchipelagoStatic.ArchLogger.Log("SessionHandler", $"Joined a server with version: {_currentSession.RoomState.Version.Major}:{_currentSession.RoomState.Version.Minor}:{_currentSession.RoomState.Version.Build}");
            ArchipelagoStatic.ArchLogger.Log("SessionHandler", $"Has Items: {hasItems}");
            
            ArchipelagoStatic.AlbumDatabase.Setup();
            ItemHandler.Setup(_slotData, hasItems);
            HintHandler.Setup();
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("SessionHandler", e);
        }
    }
    
    public async Task Disconnect()
    {
        try
        {
            if (_currentSession == null)
                throw new Exception("Trying to disconnect from a non-existent connection?");

            await _currentSession.Socket.DisconnectAsync();
            
            DataStorageHandler = null;
            ItemHandler = null;
            HintHandler = null;
            DeathLinkHandler = null;
            BattleHandler = null;
            SongSelectAdditions = null;
            _currentSession = null;
            
            ArchipelagoStatic.Login.StopWaiting(null);
        }
        catch (Exception e)
        {
            ArchipelagoStatic.Login.StopWaiting(e);
        }
    }

    /// <summary>
    ///     Runs on Every Unity's OnUpdate(). Passes the update along to child handlers, if the session was started.
    /// </summary>
    public void OnUpdate() {
        if (!IsLoggedIn || ArchipelagoStatic.IsLoadingAP)
            return;

        try { 
            SongSelectAdditions.OnUpdate();

            ItemHandler.OnUpdate();
            ItemHandler.Unlocker.OnUpdate();
            HintHandler.OnUpdate();
            BattleHandler.OnUpdate();
            DeathLinkHandler.Update();
            
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("OnUpdate", e);
        }
    }

    /// <summary>
    ///     Runs on Every Unity's OnLateUpdate(). Passes the update along to child handlers, if the session was started.
    /// </summary>
    public void OnLateUpdate() {
        if (!IsLoggedIn || ArchipelagoStatic.IsLoadingAP)
            return;

        try { 
            ItemHandler.Unlocker.OnLateUpdate();
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("OnLateUpdate", e);
        }
    }

    /// <summary>
    ///     Runs on Unity's Scene has changed.
    /// </summary>
    public void SceneChanged(string sceneName) {
        try { 
            if (sceneName == "UISystem_PC")
                SongSelectAdditions?.MainSceneLoaded();
            else if (sceneName == "GameMain")
                SongSelectAdditions?.BattleSceneLoaded();
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("SceneChanged", e);
        }
    }

    public void CollectItems() {
        _currentSession.Socket.SendPacketAsync(new SayPacket { Text = "!collect" });
    }
    public void ReleaseItems() {
        _currentSession.Socket.SendPacketAsync(new SayPacket { Text = "!release" });
    }
}