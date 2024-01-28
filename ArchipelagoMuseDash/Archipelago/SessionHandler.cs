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
        _team = successful.Team;
        _slotData = successful.SlotData;
        _currentSession = session;

        reason = null;
        return true;
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

            ArchipelagoStatic.AlbumDatabase.Setup();
            ItemHandler.Setup(_slotData);
            HintHandler.Setup();
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("ItemHandler", e);
        }
    }

    /// <summary>
    ///     Runs on Every Unity's OnUpdate(). Passes the update along to child handlers, if the session was started.
    /// </summary>
    public void OnUpdate() {
        if (!IsLoggedIn)
            return;

        SongSelectAdditions.OnUpdate();

        ItemHandler.OnUpdate();
        ItemHandler.Unlocker.OnUpdate();
        HintHandler.OnUpdate();
        BattleHandler.OnUpdate();
        DeathLinkHandler.Update();
    }

    /// <summary>
    ///     Runs on Every Unity's OnLateUpdate(). Passes the update along to child handlers, if the session was started.
    /// </summary>
    public void OnLateUpdate() {
        if (!IsLoggedIn)
            return;

        ItemHandler.Unlocker.OnLateUpdate();
    }

    /// <summary>
    ///     Runs on Unity's Scene has changed.
    /// </summary>
    public void SceneChanged(string sceneName) {
        if (sceneName == "UISystem_PC")
            SongSelectAdditions?.MainSceneLoaded();
        else if (sceneName == "GameMain")
            SongSelectAdditions?.BattleSceneLoaded();

    }

    public void CollectItems() {
        _currentSession.Socket.SendPacketAsync(new SayPacket { Text = "!collect" });
    }
    public void ReleaseItems() {
        _currentSession.Socket.SendPacketAsync(new SayPacket { Text = "!release" });
    }
}