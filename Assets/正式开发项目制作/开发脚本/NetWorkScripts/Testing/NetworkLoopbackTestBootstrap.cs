#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Aoyi.Mirror;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NetworkLoopbackOptions
{
    public string Role { get; private set; }
    public string RunId { get; private set; }
    public int Port { get; private set; }
    public string ArtifactsRoot { get; private set; }
    public int TimeoutSeconds { get; private set; }

    public static bool TryParse(string[] args, out NetworkLoopbackOptions options, out string error)
    {
        options = null;
        error = null;

        string role = GetArgument(args, "-networkTestRole");
        if (string.IsNullOrWhiteSpace(role))
            return false;

        role = role.Trim().ToLowerInvariant();
        if (role != "host" && role != "client")
        {
            error = $"Unsupported network test role: {role}";
            return false;
        }

        string runId = GetArgument(args, "-networkTestRunId");
        if (string.IsNullOrWhiteSpace(runId))
        {
            error = "-networkTestRunId is required.";
            return false;
        }

        string portValue = GetArgument(args, "-networkTestPort");
        if (!int.TryParse(portValue, out int port) || port < 1 || port > ushort.MaxValue)
        {
            error = $"Invalid network test UDP port: {portValue}";
            return false;
        }

        string artifactsRoot = GetArgument(args, "-networkTestArtifacts");
        if (string.IsNullOrWhiteSpace(artifactsRoot))
        {
            error = "-networkTestArtifacts is required.";
            return false;
        }

        int timeoutSeconds = 60;
        string timeoutValue = GetArgument(args, "-networkTestTimeout");
        if (!string.IsNullOrWhiteSpace(timeoutValue)
            && (!int.TryParse(timeoutValue, out timeoutSeconds) || timeoutSeconds < 5 || timeoutSeconds > 600))
        {
            error = $"Invalid network test timeout: {timeoutValue}";
            return false;
        }

        options = new NetworkLoopbackOptions
        {
            Role = role,
            RunId = runId.Trim(),
            Port = port,
            ArtifactsRoot = artifactsRoot,
            TimeoutSeconds = timeoutSeconds
        };
        return true;
    }

    private static string GetArgument(string[] args, string name)
    {
        if (args == null)
            return null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}

public sealed class NetworkLoopbackTestBootstrap : MonoBehaviour
{
    [Serializable]
    private sealed class CheckpointRecord
    {
        public string runId;
        public string role;
        public string checkpoint;
        public string scene;
        public int playerIndex;
        public int teamId;
        public string timestampUtc;
    }

    [Serializable]
    private sealed class ResultRecord
    {
        public string runId;
        public string role;
        public bool success;
        public string checkpoint;
        public string message;
        public string scene;
        public int playerIndex;
        public int teamId;
        public string timestampUtc;
    }

    private NetworkLoopbackOptions _options;
    private AoyiNetworkRoomManager _manager;
    private string _lastCheckpoint = "not-started";
    private bool _resultWritten;
    private bool _connectedWritten;
    private bool _playerReadyWritten;
    private bool _sceneWritten;
    private bool _battleOverSent;
    private bool _lobbySceneWritten;

    public static bool ShouldSendBattleOver(
        string role,
        bool localReachedBattle,
        bool peerReachedBattle,
        bool clientReady,
        bool battleOverSent)
    {
        return string.Equals(role, "host", StringComparison.OrdinalIgnoreCase)
            && localReachedBattle
            && peerReachedBattle
            && clientReady
            && !battleOverSent;
    }

    public static bool ShouldCompleteLobbyReturn(bool localReachedLobby, bool peerReachedLobby)
    {
        return localReachedLobby && peerReachedLobby;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        bool enabled = NetworkLoopbackOptions.TryParse(args, out NetworkLoopbackOptions options, out string error);
        if (!enabled)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[NetworkLoopbackTest] {error}");
                Application.Quit(2);
            }
            return;
        }

        GameObject bootstrapObject = new GameObject("NetworkLoopbackTestBootstrap");
        DontDestroyOnLoad(bootstrapObject);
        NetworkLoopbackTestBootstrap bootstrap = bootstrapObject.AddComponent<NetworkLoopbackTestBootstrap>();
        bootstrap._options = options;
        bootstrap.StartCoroutine(bootstrap.RunTest());
    }

    private IEnumerator RunTest()
    {
        Directory.CreateDirectory(_options.ArtifactsRoot);
        WriteCheckpoint("bootstrap");
        yield return PrepareRoomScene();

        try
        {
            _manager = CreateManager();
            if (_options.Role == "host")
                _manager.StartHost();
            else
                _manager.StartClient();
            WriteCheckpoint("transport-started");
        }
        catch (Exception ex)
        {
            Complete(false, $"Failed to start Mirror: {ex}");
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + _options.TimeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (!_connectedWritten && NetworkClient.active && NetworkClient.isConnected)
            {
                _connectedWritten = true;
                WriteCheckpoint("connected");
            }

            if (!_playerReadyWritten && _manager.GetLocalPlayerIndex() >= 0)
            {
                _playerReadyWritten = true;
                WriteCheckpoint("room-player-ready");
            }

            string sceneName = SceneManager.GetActiveScene().name;
            if (!_sceneWritten
                && _connectedWritten
                && _playerReadyWritten
                && _manager.IsAoyiBattleScene(sceneName))
            {
                _sceneWritten = true;
                WriteCheckpoint("battle-scene");
            }

            bool peerReachedBattle = PeerReachedCheckpoint("battle-scene");
            if (ShouldSendBattleOver(
                _options.Role,
                _sceneWritten,
                peerReachedBattle,
                NetworkClient.ready,
                _battleOverSent))
            {
                _battleOverSent = true;
                MirrorNetBridge.ClientSend(new MsgBattleOver
                {
                    roomId = _options.RunId,
                    userId = _manager.GetLocalPlayerIndex()
                });
                WriteCheckpoint("battle-over-sent");
            }

            if (_sceneWritten
                && !_lobbySceneWritten
                && string.Equals(sceneName, _manager.RoomScene, StringComparison.Ordinal))
            {
                _lobbySceneWritten = true;
                WriteCheckpoint("lobby-scene");
            }

            bool peerReachedLobby = _lobbySceneWritten && PeerReachedCheckpoint("lobby-scene");
            if (ShouldCompleteLobbyReturn(_lobbySceneWritten, peerReachedLobby))
            {
                Complete(true, "Both loopback players returned to the lobby after battle over.");
                yield break;
            }

            yield return null;
        }

        Complete(false, $"Timed out after {_options.TimeoutSeconds}s at checkpoint {_lastCheckpoint}.");
    }

    private IEnumerator PrepareRoomScene()
    {
        float startupDeadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name == "LoadScene"
            && Time.realtimeSinceStartup < startupDeadline)
        {
            yield return null;
        }

        if (SceneManager.GetActiveScene().name != "LobbyPanel")
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("LobbyPanel", LoadSceneMode.Single);
            if (load == null)
                throw new InvalidOperationException("Failed to start loading LobbyPanel for the loopback test.");
            while (!load.isDone)
                yield return null;
        }

        yield return null;
        WriteCheckpoint("room-scene-ready");
    }

    private AoyiNetworkRoomManager CreateManager()
    {
        if (NetworkManager.singleton != null)
            throw new InvalidOperationException($"A NetworkManager already exists: {NetworkManager.singleton.GetType().Name}");

        GameObject managerObject = new GameObject("NetworkLoopbackRoomManager");
        DontDestroyOnLoad(managerObject);
        kcp2k.KcpTransport transport = managerObject.AddComponent<kcp2k.KcpTransport>();
        transport.Port = (ushort)_options.Port;

        AoyiNetworkRoomManager manager = managerObject.AddComponent<AoyiNetworkRoomManager>();
        manager.transport = transport;
        manager.networkAddress = "127.0.0.1";
        manager.maxRoomPlayers = 2;
        manager.minPlayers = 2;
        manager.maxConnections = 2;
        manager.pendingBattleScene = "dantiao_map";
        manager.aoyiRoomScene = SceneManager.GetActiveScene().name;
        manager.RoomScene = manager.aoyiRoomScene;
        return manager;
    }

    private bool PeerReachedCheckpoint(string checkpoint)
    {
        string peerRole = _options.Role == "host" ? "client" : "host";
        string path = Path.Combine(_options.ArtifactsRoot, $"{peerRole}-checkpoints.ndjson");
        if (!File.Exists(path))
            return false;

        try
        {
            return File.ReadAllText(path).Contains($"\"checkpoint\":\"{checkpoint}\"");
        }
        catch (IOException)
        {
            return false;
        }
    }

    private void WriteCheckpoint(string checkpoint)
    {
        _lastCheckpoint = checkpoint;
        CheckpointRecord record = new CheckpointRecord
        {
            runId = _options.RunId,
            role = _options.Role,
            checkpoint = checkpoint,
            scene = SceneManager.GetActiveScene().name,
            playerIndex = _manager != null ? _manager.GetLocalPlayerIndex() : -1,
            teamId = _manager != null ? _manager.GetLocalPlayerTeamId() : 0,
            timestampUtc = DateTime.UtcNow.ToString("O")
        };

        string path = Path.Combine(_options.ArtifactsRoot, $"{_options.Role}-checkpoints.ndjson");
        File.AppendAllText(path, JsonUtility.ToJson(record) + Environment.NewLine);
        Debug.Log($"[NetworkLoopbackTest] {_options.Role}: {checkpoint}");
    }

    private void Complete(bool success, string message)
    {
        if (_resultWritten)
            return;
        _resultWritten = true;

        ResultRecord result = new ResultRecord
        {
            runId = _options.RunId,
            role = _options.Role,
            success = success,
            checkpoint = _lastCheckpoint,
            message = message,
            scene = SceneManager.GetActiveScene().name,
            playerIndex = _manager != null ? _manager.GetLocalPlayerIndex() : -1,
            teamId = _manager != null ? _manager.GetLocalPlayerTeamId() : 0,
            timestampUtc = DateTime.UtcNow.ToString("O")
        };

        Directory.CreateDirectory(_options.ArtifactsRoot);
        string path = Path.Combine(_options.ArtifactsRoot, $"{_options.Role}-result.json");
        string temporaryPath = path + $".{System.Diagnostics.Process.GetCurrentProcess().Id}.tmp";
        File.WriteAllText(temporaryPath, JsonUtility.ToJson(result, true));
        File.Move(temporaryPath, path);
        Debug.Log($"[NetworkLoopbackTest] {_options.Role}: success={success}, message={message}");

        if (NetworkServer.active && NetworkClient.active)
            _manager.StopHost();
        else if (NetworkClient.active)
            _manager.StopClient();
        else if (NetworkServer.active)
            _manager.StopServer();

        Application.Quit(success ? 0 : 1);
    }

    private void OnApplicationQuit()
    {
        if (_options != null && !_resultWritten)
            Complete(false, "Application quit before the loopback test completed.");
    }
}
#endif
