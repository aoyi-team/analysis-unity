# Match Session Architecture Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralize battle-scene resolution and match-session lifecycle so local, LAN, Relay host, Relay guest, and dedicated-server flows return to the lobby deterministically, close the remote room exactly once, and leave the player ready to match again.

**Architecture:** Keep Mirror and the legacy `MsgFramework` protocol intact. Add a pure scene catalog and match-session state object, then make one `MatchSessionCoordinator` own transitions from search through lobby return. Keep `AoyiNetworkRoomManager`, `BattleManager`, and `OnlineMatchManager` as adapters that report events to the coordinator rather than independently stopping transports, loading scenes, or closing Web rooms.

**Tech Stack:** Unity 2022.3.61f1c1, C#, Mirror, Edgegap Relay/KCP, Supabase matchmaking API, NUnit EditMode tests, PowerShell verification scripts

---

## Scope and boundaries

This plan includes:

- One authoritative mapping from `GameModes` to Unity scene names.
- A testable match-session phase model with generation ownership and idempotent ending.
- Host-authoritative lobby return for Mirror sessions.
- Exactly-once remote room closure, performed by the authoritative server/host.
- Relay and dedicated connection strategies with real `CancellationToken` propagation.
- Removal of matching orchestration and disabled legacy code from `ChooseHeroPanel.cs`.
- Extraction of battle-frame aggregation from `MirrorNetBridge` after lifecycle behavior is stable.

This plan does not include:

- Changing the legacy TCP/UDP message schema.
- Replacing Mirror, Edgegap, Supabase, or `MsgFramework`.
- Rewriting combat simulation, deterministic math, interpolation, or UI animation.
- Splitting authentication and room-directory providers. That independent migration remains in `docs/backend-abstraction-plan.md` and starts only after this plan passes loopback verification.
- Renaming Unity scenes, assets, or Chinese directories.

## Target file structure

### New focused files

- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/GameSceneCatalog.cs`
  - Owns scene-name constants and `GameModes` to battle-scene mapping.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionPhase.cs`
  - Defines lifecycle phases only.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionContext.cs`
  - Stores current mode, room, role, phase, and generation without Unity dependencies.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionCoordinator.cs`
  - Owns match entry, battle end, lobby return, transport shutdown, and state cleanup.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchRequestFactory.cs`
  - Builds legacy local-server match and exit messages without UI dependencies.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/HeroMatchStartResult.cs`
  - Defines the UI-facing result of routing a match start request.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/OnlineRoomLifecycleService.cs`
  - Closes the Supabase/Web match room and returns a success result.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/IOnlineConnectionStrategy.cs`
  - Defines the cancellable connection strategy contract.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/DedicatedServerConnectionStrategy.cs`
  - Connects a matched client to a dedicated KCP server.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/PlayerHostedRelayConnectionStrategy.cs`
  - Starts Relay host or retries Relay guest connection with cancellation.
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/OnlineRoomManagerFactory.cs`
  - Creates or reconfigures `AoyiNetworkRoomManager` and its transport.
- `Assets/正式开发项目制作/开发脚本/Mirror/MirrorBattleFrameCoordinator.cs`
  - Owns battle-ready aggregation, frame IDs, pending operations, and frame history.

Unity will generate a `.meta` file for each new asset. Include those generated `.meta` files in the same commit as their corresponding source files.

### Existing files to modify

- `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/SceneMgr.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/PlayerBasicInfoMgr.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineMatchManager.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs`
- `Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkRoomManager.cs`
- `Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkManager.cs`
- `Assets/正式开发项目制作/开发脚本/Mirror/MirrorNetBridge.cs`
- `Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs`
- `Assets/正式开发项目制作/开发脚本/CharactersChosePages/ChooseHeroPanel.cs`

---

### Task 1: Add one authoritative scene catalog

**Files:**
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/GameSceneCatalog.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/SceneMgr.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkRoomManager.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkManager.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [ ] **Step 1: Write failing scene-catalog tests**

Add these reflection-based tests to `AoyiNetworkRoomManagerTests.cs`:

```csharp
[Test]
public void GameSceneCatalogMapsSoloRankToPaiweiMap()
{
    System.Type catalogType = System.Type.GetType("GameSceneCatalog, Assembly-CSharp");
    Assert.NotNull(catalogType);

    System.Type gameModesType = System.Type.GetType("GameModes, Aoyi.Messages");
    object soloRank = System.Enum.Parse(gameModesType, "paiwei_solo");
    MethodInfo getBattleScene = catalogType.GetMethod("GetBattleScene", BindingFlags.Public | BindingFlags.Static);

    Assert.AreEqual("paiwei_map", getBattleScene.Invoke(null, new[] { soloRank }));
}

[Test]
public void GameSceneCatalogRecognizesOnlyEnabledBattleScenes()
{
    System.Type catalogType = System.Type.GetType("GameSceneCatalog, Assembly-CSharp");
    MethodInfo isBattleScene = catalogType.GetMethod("IsBattleScene", BindingFlags.Public | BindingFlags.Static);

    Assert.IsTrue((bool)isBattleScene.Invoke(null, new object[] { "dantiao_map" }));
    Assert.IsTrue((bool)isBattleScene.Invoke(null, new object[] { "paiwei_map" }));
    Assert.IsFalse((bool)isBattleScene.Invoke(null, new object[] { "paiwei_solo_map" }));
    Assert.IsFalse((bool)isBattleScene.Invoke(null, new object[] { "LobbyPanel" }));
}
```

- [ ] **Step 2: Run EditMode tests and verify RED**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.61f1c1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "D:\Desktop\game\aoyi team2" `
  -runTests -testPlatform EditMode `
  -testResults "D:\Desktop\game\aoyi team2\artifacts\match-session-task1-red.xml" `
  -logFile "D:\Desktop\game\aoyi team2\artifacts\match-session-task1-red.log"
```

Expected: the two new tests fail because `GameSceneCatalog` does not exist.

- [ ] **Step 3: Create the scene catalog**

Create `GameSceneCatalog.cs` with this complete implementation:

```csharp
public static class GameSceneCatalog
{
    public const string Login = "LoadScene";
    public const string Register = "RegiserScene";
    public const string Lobby = "LobbyPanel";
    public const string DantiaoBattle = "dantiao_map";
    public const string PaiweiBattle = "paiwei_map";

    public static string GetBattleScene(GameModes mode)
    {
        switch (mode)
        {
            case GameModes.paiwei:
            case GameModes.paiwei_solo:
                return PaiweiBattle;
            case GameModes.dantiao:
                return DantiaoBattle;
            default:
                return mode + "_map";
        }
    }

    public static bool IsBattleScene(string sceneName)
    {
        return string.Equals(sceneName, DantiaoBattle, System.StringComparison.Ordinal)
            || string.Equals(sceneName, PaiweiBattle, System.StringComparison.Ordinal);
    }
}
```

- [ ] **Step 4: Replace duplicated scene-name logic**

Make these exact migrations:

```csharp
// SceneMgr.LoadSceneByName
return SceneManager.LoadSceneAsync(GameSceneCatalog.GetBattleScene(mode));

// OnlineConnectionLauncher
nm.pendingBattleScene = GameSceneCatalog.GetBattleScene(mode);

// BattleManager lobby comparison/load
GameSceneCatalog.Lobby

// AoyiNetworkRoomManager defaults
public string aoyiRoomScene = GameSceneCatalog.Lobby;
public string[] aoyiBattleScenes = { GameSceneCatalog.DantiaoBattle, GameSceneCatalog.PaiweiBattle };

// AoyiNetworkManager defaults
public string loginScene = GameSceneCatalog.Login;
public string lobbyScene = GameSceneCatalog.Lobby;
```

Change `AoyiNetworkRoomManager.IsAoyiBattleScene` to delegate directly:

```csharp
public bool IsAoyiBattleScene(string sceneName)
{
    return GameSceneCatalog.IsBattleScene(sceneName);
}
```

- [ ] **Step 5: Run EditMode tests and verify GREEN**

Run the same Unity command with output names `match-session-task1-green.xml` and `match-session-task1-green.log`.

Expected: all EditMode tests pass, including both scene-catalog tests.

- [ ] **Step 6: Commit the scene catalog**

```powershell
git add -- "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/GameSceneCatalog.cs" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/GameSceneCatalog.cs.meta" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/SceneMgr.cs" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs" `
  "Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkRoomManager.cs" `
  "Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkManager.cs" `
  "Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs"
git commit -m "refactor: centralize game scene mapping"
```

---

### Task 2: Introduce a pure match-session state model

**Files:**
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionPhase.cs`
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionContext.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [ ] **Step 1: Write failing lifecycle-state tests**

Add tests that instantiate `MatchSessionContext` through reflection and require these behaviors:

```csharp
[Test]
public void MatchSessionRejectsStaleMatchedResultAndEndsOnlyOnce()
{
    System.Type contextType = System.Type.GetType("MatchSessionContext, Assembly-CSharp");
    Assert.NotNull(contextType);
    object context = System.Activator.CreateInstance(contextType);

    System.Type networkModeType = System.Type.GetType("NetworkMode, Assembly-CSharp");
    System.Type gameModesType = System.Type.GetType("GameModes, Aoyi.Messages");
    object onlineMode = System.Enum.Parse(networkModeType, "SupabaseOnline");
    object dantiao = System.Enum.Parse(gameModesType, "dantiao");

    int generation = (int)contextType.GetMethod("BeginSearch").Invoke(context, new[] { onlineMode, dantiao });
    Assert.IsFalse((bool)contextType.GetMethod("TryMarkMatched").Invoke(context, new object[] { generation - 1, "room-stale", "guest" }));
    Assert.IsTrue((bool)contextType.GetMethod("TryMarkMatched").Invoke(context, new object[] { generation, "room-a", "host" }));
    Assert.IsTrue((bool)contextType.GetMethod("TryBeginEnding").Invoke(context, null));
    Assert.IsFalse((bool)contextType.GetMethod("TryBeginEnding").Invoke(context, null));
}

[Test]
public void MatchSessionResetClearsRoomAndInvalidatesGeneration()
{
    System.Type contextType = System.Type.GetType("MatchSessionContext, Assembly-CSharp");
    object context = System.Activator.CreateInstance(contextType);
    System.Type networkModeType = System.Type.GetType("NetworkMode, Assembly-CSharp");
    System.Type gameModesType = System.Type.GetType("GameModes, Aoyi.Messages");

    int generation = (int)contextType.GetMethod("BeginSearch").Invoke(context, new[]
    {
        System.Enum.Parse(networkModeType, "SupabaseOnline"),
        System.Enum.Parse(gameModesType, "dantiao")
    });
    contextType.GetMethod("TryMarkMatched").Invoke(context, new object[] { generation, "room-a", "host" });
    contextType.GetMethod("Reset").Invoke(context, null);

    Assert.IsNull(contextType.GetProperty("RoomId").GetValue(context));
    Assert.Greater((int)contextType.GetProperty("Generation").GetValue(context), generation);
    Assert.AreEqual("Idle", contextType.GetProperty("Phase").GetValue(context).ToString());
}
```

- [ ] **Step 2: Run tests and verify RED**

Run the Task 1 Unity test command using `match-session-task2-red` artifact names.

Expected: both tests fail because the session types do not exist.

- [ ] **Step 3: Create `MatchSessionPhase`**

```csharp
public enum MatchSessionPhase
{
    Idle = 0,
    Searching = 1,
    Matched = 2,
    Connecting = 3,
    InRoom = 4,
    InBattle = 5,
    Ending = 6
}
```

- [ ] **Step 4: Create `MatchSessionContext`**

```csharp
public sealed class MatchSessionContext
{
    public MatchSessionPhase Phase { get; private set; } = MatchSessionPhase.Idle;
    public int Generation { get; private set; }
    public NetworkMode NetworkMode { get; private set; } = NetworkMode.LocalServer;
    public GameModes GameMode { get; private set; }
    public string RoomId { get; private set; }
    public string Role { get; private set; }

    public bool IsOnline => NetworkMode == NetworkMode.SupabaseOnline;

    public int BeginSearch(NetworkMode networkMode, GameModes gameMode)
    {
        Generation++;
        NetworkMode = networkMode;
        GameMode = gameMode;
        RoomId = null;
        Role = null;
        Phase = MatchSessionPhase.Searching;
        return Generation;
    }

    public bool TryMarkMatched(int generation, string roomId, string role)
    {
        if (generation != Generation || Phase != MatchSessionPhase.Searching || string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        RoomId = roomId;
        Role = role;
        Phase = MatchSessionPhase.Matched;
        return true;
    }

    public bool TryBeginConnecting()
    {
        if (Phase != MatchSessionPhase.Matched)
        {
            return false;
        }

        Phase = MatchSessionPhase.Connecting;
        return true;
    }

    public void MarkInRoom()
    {
        if (Phase == MatchSessionPhase.Connecting || Phase == MatchSessionPhase.Matched)
        {
            Phase = MatchSessionPhase.InRoom;
        }
    }

    public void MarkInBattle()
    {
        if (Phase != MatchSessionPhase.Idle && Phase != MatchSessionPhase.Ending)
        {
            Phase = MatchSessionPhase.InBattle;
        }
    }

    public bool TryBeginEnding()
    {
        if (Phase == MatchSessionPhase.Idle || Phase == MatchSessionPhase.Ending)
        {
            return false;
        }

        Phase = MatchSessionPhase.Ending;
        Generation++;
        return true;
    }

    public void Reset()
    {
        Generation++;
        Phase = MatchSessionPhase.Idle;
        RoomId = null;
        Role = null;
        GameMode = default;
    }
}
```

- [ ] **Step 5: Run tests and verify GREEN**

Run the Unity EditMode command using `match-session-task2-green` artifact names.

Expected: stale matched results are rejected, ending is idempotent, and reset clears room state.

- [ ] **Step 6: Commit the session state model**

```powershell
git add -- "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs"
git commit -m "refactor: add match session state model"
```

---

### Task 3: Centralize lobby return and remote-room closure

**Files:**
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionCoordinator.cs`
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/OnlineRoomLifecycleService.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/PlayerBasicInfoMgr.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkRoomManager.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [ ] **Step 1: Write failing lifecycle-policy tests**

Add:

```csharp
[Test]
public void OnlyOnlineAuthorityClosesRemoteRoom()
{
    System.Type coordinatorType = System.Type.GetType("MatchSessionCoordinator, Assembly-CSharp");
    MethodInfo method = coordinatorType.GetMethod("ShouldCloseRemoteRoom", BindingFlags.Public | BindingFlags.Static);
    System.Type networkModeType = System.Type.GetType("NetworkMode, Assembly-CSharp");

    object online = System.Enum.Parse(networkModeType, "SupabaseOnline");
    object lanClient = System.Enum.Parse(networkModeType, "LanClient");

    Assert.IsTrue((bool)method.Invoke(null, new object[] { online, true }));
    Assert.IsFalse((bool)method.Invoke(null, new object[] { online, false }));
    Assert.IsFalse((bool)method.Invoke(null, new object[] { lanClient, true }));
}

[Test]
public void EndingRequestIsIdempotent()
{
    System.Type contextType = System.Type.GetType("MatchSessionContext, Assembly-CSharp");
    object context = System.Activator.CreateInstance(contextType);
    System.Type networkModeType = System.Type.GetType("NetworkMode, Assembly-CSharp");
    System.Type gameModesType = System.Type.GetType("GameModes, Aoyi.Messages");
    contextType.GetMethod("BeginSearch").Invoke(context, new[]
    {
        System.Enum.Parse(networkModeType, "SupabaseOnline"),
        System.Enum.Parse(gameModesType, "dantiao")
    });

    Assert.IsTrue((bool)contextType.GetMethod("TryBeginEnding").Invoke(context, null));
    Assert.IsFalse((bool)contextType.GetMethod("TryBeginEnding").Invoke(context, null));
}
```

- [ ] **Step 2: Run tests and verify RED**

Run the Unity EditMode command with `match-session-task3-red` artifact names.

Expected: `OnlyOnlineAuthorityClosesRemoteRoom` fails because the coordinator does not exist.

- [ ] **Step 3: Create the remote-room lifecycle service**

```csharp
using System.Threading.Tasks;
using UnityEngine;

public static class OnlineRoomLifecycleService
{
    public static async Task<bool> CloseRoomAsync(string roomId)
    {
        string accessToken = SupabaseBackendProvider.GetSavedAccessToken();
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(accessToken))
        {
            Debug.LogWarning("[OnlineRoomLifecycleService] roomId 或 accessToken 为空，跳过关闭在线房间");
            return false;
        }

        OnlineMatchApiResult<OnlineMatchCloseResponse> result =
            await OnlineMatchApiClient.CloseMatchAsync(accessToken, roomId);

        bool closed = result != null && result.Success && result.Data != null && result.Data.Closed;
        if (!closed)
        {
            Debug.LogWarning($"[OnlineRoomLifecycleService] 关闭在线房间失败，roomId={roomId}, error={result?.ErrorMessage}");
        }
        return closed;
    }
}
```

- [ ] **Step 4: Create the session coordinator**

Create `MatchSessionCoordinator.cs` with these APIs and behavior:

```csharp
using System.Threading.Tasks;
using Aoyi.Mirror;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchSessionCoordinator : MonoBehaviour
{
    private static MatchSessionCoordinator instance;
    private Task finalizeTask;

    public static MatchSessionCoordinator Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MatchSessionCoordinator");
                instance = go.AddComponent<MatchSessionCoordinator>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public MatchSessionContext Context { get; } = new MatchSessionContext();

    public static bool ShouldCloseRemoteRoom(NetworkMode mode, bool serverActive)
    {
        return mode == NetworkMode.SupabaseOnline && serverActive;
    }

    public int BeginSearch(NetworkMode mode, GameModes gameMode)
    {
        return Context.BeginSearch(mode, gameMode);
    }

    public bool MarkMatched(int generation, string roomId, string role)
    {
        return Context.TryMarkMatched(generation, roomId, role);
    }

    public void MarkConnecting()
    {
        Context.TryBeginConnecting();
    }

    public void MarkInRoom()
    {
        Context.MarkInRoom();
    }

    public void MarkInBattle()
    {
        Context.MarkInBattle();
    }

    public bool RequestReturnToLobby()
    {
        if (!Context.TryBeginEnding())
        {
            return false;
        }

        if (NetworkServer.active && AoyiNetworkRoomManager.singleton != null)
        {
            AoyiNetworkRoomManager.singleton.ServerChangeScene(GameSceneCatalog.Lobby);
            return true;
        }

        if (!NetworkClient.active && SceneManager.GetActiveScene().name != GameSceneCatalog.Lobby)
        {
            SceneManager.LoadScene(GameSceneCatalog.Lobby);
        }

        return true;
    }

    public Task FinalizeLobbyReturnAsync()
    {
        if (finalizeTask == null || finalizeTask.IsCompleted)
        {
            finalizeTask = FinalizeLobbyReturnCoreAsync();
        }
        return finalizeTask;
    }

    private async Task FinalizeLobbyReturnCoreAsync()
    {
        bool serverActive = NetworkServer.active;
        bool clientActive = NetworkClient.active;
        string roomId = Context.RoomId ?? PlayerBasicInfoMgr.Instance?.RoomId;
        NetworkMode mode = Context.Phase == MatchSessionPhase.Idle
            ? PlayerBasicInfoMgr.Instance.CurrentNetworkMode
            : Context.NetworkMode;

        if (ShouldCloseRemoteRoom(mode, serverActive))
        {
            await OnlineRoomLifecycleService.CloseRoomAsync(roomId);
        }

        AoyiNetworkRoomManager manager = AoyiNetworkRoomManager.singleton;
        if (manager != null)
        {
            if (serverActive && clientActive)
            {
                manager.StopHost();
            }
            else if (serverActive)
            {
                manager.StopServer();
            }
            else if (clientActive)
            {
                manager.StopClient();
            }
        }

        PlayerBasicInfoMgr.Instance?.ClearMatchSession();
        Context.Reset();
    }
}
```

- [ ] **Step 5: Add focused player-session cleanup**

Add this method to `PlayerBasicInfoMgr` without clearing authentication or the selected `CurrentBackend`:

```csharp
public void ClearMatchSession()
{
    roomID = null;
    teamId = 0;
    TargetEndpoint = default;
    _battleAllPlayers = null;
    ClearBattleId();
}
```

- [ ] **Step 6: Route battle end through the coordinator**

Replace `BattleManager.ReturnToLobbyAfterBattleOver` with:

```csharp
private IEnumerator ReturnToLobbyAfterBattleOver()
{
    yield return null;
    yield return null;
    MatchSessionCoordinator.Instance.RequestReturnToLobby();
}
```

Remove direct `StopClient()` and direct `SceneManager.LoadScene("LobbyPanel")` calls from this method.

- [ ] **Step 7: Finalize transport state only after the lobby is loaded**

In both `AoyiNetworkRoomManager.OnRoomServerSceneChanged` and `OnRoomClientSceneChanged`, replace the duplicated stop coroutines with:

```csharp
if (string.Equals(sceneName, GameSceneCatalog.Lobby, System.StringComparison.Ordinal)
    && PlayerBasicInfoMgr.Instance != null
    && PlayerBasicInfoMgr.Instance.CurrentNetworkMode == NetworkMode.SupabaseOnline)
{
    _ = MatchSessionCoordinator.Instance.FinalizeLobbyReturnAsync();
    return;
}
```

Delete `StopOnlineHostAfterLobbyReturn`, `StopOnlineClientAfterLobbyReturn`, and `ShouldDisconnectOnlineSessionAfterLobbyReturn` after all callers and tests have migrated.

Keep `OnlineConnectionLauncher.NotifyMatchedRoomEnded` temporarily, but change it to a compatibility wrapper:

```csharp
public static void NotifyMatchedRoomEnded()
{
    _ = MatchSessionCoordinator.Instance.FinalizeLobbyReturnAsync();
}
```

- [ ] **Step 8: Run tests and verify GREEN**

Run the Unity EditMode command with `match-session-task3-green` artifact names.

Expected: all lifecycle policy tests pass and there are no missing-script or duplicate-type compiler errors.

- [ ] **Step 9: Commit centralized lifecycle handling**

```powershell
git add -- "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/PlayerBasicInfoMgr.cs" `
  "Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs" `
  "Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkRoomManager.cs" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs"
git commit -m "refactor: centralize match session lifecycle"
```

---

### Task 4: Bind matchmaking operations to the session coordinator

**Files:**
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchRequestFactory.cs`
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/HeroMatchStartResult.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineMatchManager.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/CharactersChosePages/ChooseHeroPanel.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [ ] **Step 1: Write failing request-factory and routing tests**

Move the existing local request assertions to `MatchRequestFactory` and add a scene-independent routing test:

```csharp
[Test]
public void MatchRequestFactoryBuildsSelectedHeroRequest()
{
    System.Type factoryType = System.Type.GetType("MatchRequestFactory, Assembly-CSharp");
    Assert.NotNull(factoryType);
    MethodInfo method = factoryType.GetMethod("BuildLocalMatchRequest", BindingFlags.Public | BindingFlags.Static);

    System.Type gameModesType = System.Type.GetType("GameModes, Aoyi.Messages");
    object dantiao = System.Enum.Parse(gameModesType, "dantiao");
    object request = method.Invoke(null, new object[] { dantiao, 101, "42" });

    System.Type requestType = System.Type.GetType("MsgMatchRequest, Aoyi.Messages");
    var pack = (System.Collections.IList)requestType.GetField("playerPack").GetValue(request);
    Assert.AreEqual(42, pack[0].GetType().GetField("userId").GetValue(pack[0]));
    Assert.AreEqual(101, pack[0].GetType().GetField("selectedHeroId").GetValue(pack[0]));
}
```

- [ ] **Step 2: Run tests and verify RED**

Run EditMode tests with `match-session-task4-red` artifact names.

Expected: the new factory test fails because `MatchRequestFactory` does not exist.

- [ ] **Step 3: Create the request factory**

```csharp
using System.Collections.Generic;

public static class MatchRequestFactory
{
    public static MsgMatchRequest BuildLocalMatchRequest(GameModes mode, int heroId, string userIdText)
    {
        return new MsgMatchRequest
        {
            GameModes = mode,
            playerPack = new List<PlayerChooseCache>
            {
                new PlayerChooseCache
                {
                    userId = ParseUserIdOrZero(userIdText),
                    selectedHeroId = heroId
                }
            }
        };
    }

    public static MsgExitRequest BuildExitRequest(GameModes mode, string userIdText)
    {
        return new MsgExitRequest
        {
            mode = mode,
            PlayerList = new List<int> { ParseUserIdOrZero(userIdText) }
        };
    }

    public static int ParseUserIdOrZero(string userIdText)
    {
        return int.TryParse(userIdText, out int userId) ? userId : 0;
    }
}
```

- [ ] **Step 4: Move match-mode routing into the coordinator**

Create `HeroMatchStartResult.cs`:

```csharp
public enum HeroMatchStartResult
{
    LocalServer,
    QuickMatch,
    OnlineMatch,
    UnsupportedNetworkMode
}
```

Add these methods to `MatchSessionCoordinator`:

```csharp
public HeroMatchStartResult StartMatch(GameModes gameMode, int heroId, int skinId)
{
    NetworkMode networkMode = PlayerBasicInfoMgr.Instance.CurrentNetworkMode;
    Context.BeginSearch(networkMode, gameMode);

    switch (networkMode)
    {
        case NetworkMode.LocalServer:
            NetWorkMgr.Send(MatchRequestFactory.BuildLocalMatchRequest(
                gameMode,
                heroId,
                PlayerBasicInfoMgr.Instance.GetID()));
            return HeroMatchStartResult.LocalServer;

        case NetworkMode.LanHost:
        case NetworkMode.LanClient:
            EnsureLanQuickMatchManager().StartQuickMatch(gameMode, heroId, skinId);
            return HeroMatchStartResult.QuickMatch;

        case NetworkMode.SupabaseOnline:
            EnsureOnlineMatchManager().StartQuickMatch(gameMode, heroId, skinId);
            return HeroMatchStartResult.OnlineMatch;

        default:
            Context.Reset();
            return HeroMatchStartResult.UnsupportedNetworkMode;
    }
}

public void CancelMatch(GameModes gameMode)
{
    switch (PlayerBasicInfoMgr.Instance.CurrentNetworkMode)
    {
        case NetworkMode.LocalServer:
            NetWorkMgr.Send(MatchRequestFactory.BuildExitRequest(
                gameMode,
                PlayerBasicInfoMgr.Instance.GetID()));
            Context.Reset();
            break;

        case NetworkMode.SupabaseOnline:
            Panels.OnlineMatchManager.Instance?.CancelMatch();
            break;

        case NetworkMode.LanHost:
        case NetworkMode.LanClient:
            LanQuickMatchManager.Instance?.CancelMatch();
            Context.Reset();
            break;
    }
}

private static LanQuickMatchManager EnsureLanQuickMatchManager()
{
    LanQuickMatchManager manager = FindObjectOfType<LanQuickMatchManager>();
    if (manager == null)
    {
        GameObject go = new GameObject("LanQuickMatchManager");
        manager = go.AddComponent<LanQuickMatchManager>();
        DontDestroyOnLoad(go);
    }
    return manager;
}

private static Panels.OnlineMatchManager EnsureOnlineMatchManager()
{
    Panels.OnlineMatchManager manager = FindObjectOfType<Panels.OnlineMatchManager>();
    if (manager == null)
    {
        GameObject go = new GameObject("OnlineMatchManager");
        manager = go.AddComponent<Panels.OnlineMatchManager>();
        DontDestroyOnLoad(go);
    }
    return manager;
}
```

- [ ] **Step 5: Report online state transitions to the coordinator**

In `OnlineMatchManager.StartQuickMatch`, reuse the generation created by `MatchSessionCoordinator.StartMatch`. Preserve compatibility for any legacy caller that invokes the manager directly:

```csharp
MatchSessionCoordinator coordinator = MatchSessionCoordinator.Instance;
int sessionGeneration = coordinator.Context.Phase == MatchSessionPhase.Searching
    && coordinator.Context.NetworkMode == NetworkMode.SupabaseOnline
    ? coordinator.Context.Generation
    : coordinator.BeginSearch(NetworkMode.SupabaseOnline, mode);
```

When the response becomes matched, require coordinator ownership before starting the connection:

```csharp
if (!MatchSessionCoordinator.Instance.MarkMatched(sessionGeneration, response.RoomId, response.Role))
{
    await CancelLateMatchTicketIfNeededAsync(currentAccessToken, new OnlineMatchApiResult<OnlineMatchResponse>
    {
        Success = true,
        Data = response
    });
    return;
}

MatchSessionCoordinator.Instance.MarkConnecting();
```

After `OnlineConnectionLauncher.StartMatchedRoomAsync` returns `true`, call:

```csharp
MatchSessionCoordinator.Instance.MarkInRoom();
```

On match cancellation or terminal failure, call `MatchSessionCoordinator.Instance.Context.Reset()` only when the session has not entered `Ending`.

- [ ] **Step 6: Mark the session as in battle when battle initialization begins**

At the start of `BattleManager.Init(BattleContext ctx)`, after storing the context, add:

```csharp
MatchSessionCoordinator.Instance.MarkInBattle();
```

This makes local, LAN, Relay, and dedicated flows enter the same lifecycle phase even when their connection setup paths differ.

- [ ] **Step 7: Remove UI-owned matching infrastructure**

Change `ChooseHeroPanel` so button handlers contain no network-mode switch:

```csharp
private void onStartMatchClick()
{
    HeroMatchStartResult result = MatchSessionCoordinator.Instance.StartMatch(GameMode, _curHeroId, _curSkinId);
    if (result == HeroMatchStartResult.UnsupportedNetworkMode)
    {
        Debug.LogWarning($"[ChooseHeroPanel] 不支持的网络模式：{PlayerBasicInfoMgr.Instance.CurrentNetworkMode}");
    }
}

private void onCancelBtnClick()
{
    MatchSessionCoordinator.Instance.CancelMatch(GameMode);
}
```

Delete `HeroSelectionMatchController`, the old in-file `HeroMatchStartResult`, its seven-delegate constructor, `StartLanQuickMatch`, `StartOnlineMatch`, and the entire disabled `#if false` legacy `OnlineMatchManager` block from `ChooseHeroPanel.cs`. The active `IOnlineMatchConnector` and `OnlineMatchManager` remain only in `NetWorkScripts/OnlineMatch/OnlineMatchManager.cs`.

- [ ] **Step 8: Run tests and verify GREEN**

Run EditMode tests with `match-session-task4-green` artifact names.

Expected: request construction tests pass, `Panels.OnlineMatchManager` has one active definition, and `ChooseHeroPanel.cs` contains no `#if false` block.

- [ ] **Step 9: Commit match-entry cleanup**

```powershell
git add -- "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchRequestFactory.cs" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchRequestFactory.cs.meta" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/HeroMatchStartResult.cs" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/HeroMatchStartResult.cs.meta" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/Session/MatchSessionCoordinator.cs" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineMatchManager.cs" `
  "Assets/正式开发项目制作/开发脚本/CharactersChosePages/ChooseHeroPanel.cs" `
  "Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs"
git commit -m "refactor: move match orchestration out of hero panel"
```

---

### Task 5: Split dedicated and Relay connection strategies

**Files:**
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/IOnlineConnectionStrategy.cs`
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/DedicatedServerConnectionStrategy.cs`
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/PlayerHostedRelayConnectionStrategy.cs`
- Create: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections/OnlineRoomManagerFactory.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [ ] **Step 1: Write failing strategy-resolution tests**

```csharp
[Test]
public void OnlineConnectionStrategyResolverSeparatesDedicatedAndRelay()
{
    System.Type resolverType = System.Type.GetType("OnlineConnectionStrategyResolver, Assembly-CSharp");
    Assert.NotNull(resolverType);
    MethodInfo resolve = resolverType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
    System.Type connectionModeType = System.Type.GetType("OnlineConnectionMode, Assembly-CSharp");

    object dedicated = System.Enum.Parse(connectionModeType, "DedicatedServer");
    object relay = System.Enum.Parse(connectionModeType, "PlayerHostedRelay");

    Assert.AreEqual("DedicatedServer", resolve.Invoke(null, new[] { dedicated }).ToString());
    Assert.AreEqual("PlayerHostedRelay", resolve.Invoke(null, new[] { relay }).ToString());
}

[Test]
public void OnlineConnectionRejectsPreCanceledToken()
{
    System.Type launcherType = System.Type.GetType("OnlineConnectionLauncher, Assembly-CSharp");
    System.Type responseType = System.Type.GetType("OnlineMatchResponse, Assembly-CSharp");
    System.Type gameModesType = System.Type.GetType("GameModes, Aoyi.Messages");
    MethodInfo method = launcherType.GetMethod(
        "StartMatchedRoomAsync",
        BindingFlags.Public | BindingFlags.Static,
        null,
        new[] { responseType, gameModesType, typeof(int), typeof(int), typeof(System.Threading.CancellationToken) },
        null);

    using (var cancellation = new System.Threading.CancellationTokenSource())
    {
        cancellation.Cancel();
        object dantiao = System.Enum.Parse(gameModesType, "dantiao");
        var task = (System.Threading.Tasks.Task<bool>)method.Invoke(
            null,
            new object[] { null, dantiao, 101, 1, cancellation.Token });

        Assert.ThrowsAsync<System.OperationCanceledException>(async () => await task);
    }
}
```

- [ ] **Step 2: Run tests and verify RED**

Run EditMode tests with `match-session-task5-red` artifact names.

Expected: the resolver type does not exist.

- [ ] **Step 3: Add the strategy contract and resolver**

```csharp
using System.Threading;
using System.Threading.Tasks;
using Aoyi.Mirror;

public interface IOnlineConnectionStrategy
{
    Task<bool> ConnectAsync(
        AoyiNetworkRoomManager manager,
        OnlineMatchResponse match,
        CancellationToken cancellationToken);
}

public enum OnlineConnectionStrategyKind
{
    DedicatedServer,
    PlayerHostedRelay
}

public static class OnlineConnectionStrategyResolver
{
    public static OnlineConnectionStrategyKind Resolve(OnlineConnectionMode mode)
    {
        switch (mode)
        {
            case OnlineConnectionMode.DedicatedServer:
                return OnlineConnectionStrategyKind.DedicatedServer;
            case OnlineConnectionMode.PlayerHostedRelay:
                return OnlineConnectionStrategyKind.PlayerHostedRelay;
            default:
                throw new System.NotSupportedException($"在线连接模式不受支持：{mode}");
        }
    }
}
```

- [ ] **Step 4: Move NetworkManager creation into `OnlineRoomManagerFactory`**

Expose one entry point:

```csharp
public static AoyiNetworkRoomManager Ensure(OnlineConnectionMode mode)
```

The method must:

- Reuse `AoyiNetworkRoomManager.singleton` when present.
- Stop active Mirror state before swapping transports.
- Attach `kcp2k.KcpTransport` for dedicated mode.
- Attach `EdgegapKcpTransport` for Relay mode.
- Set `manager.transport` and `Transport.active` to the selected component.
- Return `null` if another incompatible `NetworkManager` owns the singleton.

Move `EnsureDedicatedRoomManager`, `EnsurePlayerHostedRelayRoomManager`, `ConfigureDedicatedTransport`, and `CleanupMirrorState` out of `OnlineConnectionLauncher` into this file.

- [ ] **Step 5: Implement the dedicated strategy**

`DedicatedServerConnectionStrategy.ConnectAsync` must validate host and port, configure `KcpTransport`, check cancellation immediately before `StartClient`, and return `false` on configuration or startup failure:

```csharp
public Task<bool> ConnectAsync(
    AoyiNetworkRoomManager manager,
    OnlineMatchResponse match,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    string host = (SupabaseConfig.Instance.EdgegapDedicatedHost ?? string.Empty).Trim();
    int port = SupabaseConfig.Instance.EdgegapDedicatedUdpPort;
    kcp2k.KcpTransport transport = manager.transport as kcp2k.KcpTransport;

    if (transport == null || string.IsNullOrWhiteSpace(host) || port <= 0 || port > ushort.MaxValue)
    {
        return Task.FromResult(false);
    }

    manager.networkAddress = host;
    transport.Port = (ushort)port;
    Transport.active = transport;
    manager.StartClient();
    return Task.FromResult(true);
}
```

- [ ] **Step 6: Implement the cancellable Relay strategy**

Move Relay payload parsing, transport configuration, host startup, guest startup, and guest retry into `PlayerHostedRelayConnectionStrategy`.

Every delay and polling loop must accept the same token:

```csharp
await Task.Delay(GuestInitialConnectDelayMs, cancellationToken);

for (int attempt = 1; attempt <= GuestConnectAttempts; attempt++)
{
    cancellationToken.ThrowIfCancellationRequested();
    OnlineRoomManagerFactory.CleanupMirrorState(manager);
    ConfigureRelayTransport((EdgegapKcpTransport)manager.transport, connectionInfo);
    StartRelayClient(manager, match.RoomId, connectionInfo);

    float timeoutAt = Time.realtimeSinceStartup + GuestConnectAttemptTimeoutMs / 1000f;
    while (Time.realtimeSinceStartup < timeoutAt)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (NetworkClient.isConnected)
        {
            return true;
        }
        if (!NetworkClient.active)
        {
            break;
        }
        await Task.Delay(250, cancellationToken);
    }

    OnlineRoomManagerFactory.CleanupMirrorState(manager);
    await Task.Delay(GuestConnectRetryDelayMs, cancellationToken);
}
```

On `OperationCanceledException`, clean Mirror state and rethrow so `OnlineMatchManager` can distinguish cancellation from failure.

- [ ] **Step 7: Reduce `OnlineConnectionLauncher` to orchestration**

Its cancellation-aware overload becomes:

```csharp
public static async Task<bool> StartMatchedRoomAsync(
    OnlineMatchResponse match,
    GameModes mode,
    int heroId,
    int skinId,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (match == null
        || string.IsNullOrWhiteSpace(match.RoomId)
        || string.IsNullOrWhiteSpace(match.Role))
    {
        return false;
    }

    OnlineConnectionMode connectionMode = SupabaseConfig.Instance.OnlineConnectionMode;
    AoyiNetworkRoomManager manager = OnlineRoomManagerFactory.Ensure(connectionMode);
    if (manager == null)
    {
        return false;
    }

    manager.maxRoomPlayers = 2;
    manager.pendingBattleScene = GameSceneCatalog.GetBattleScene(mode);
    PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.SupabaseOnline;
    PlayerBasicInfoMgr.Instance.SetCurrentGamemode(mode);
    PlayerBasicInfoMgr.Instance.UpdateHeroCache(heroId, skinId);
    PlayerBasicInfoMgr.Instance.UpdateRoomID(match.RoomId);

    IOnlineConnectionStrategy strategy = connectionMode == OnlineConnectionMode.DedicatedServer
        ? (IOnlineConnectionStrategy)new DedicatedServerConnectionStrategy()
        : new PlayerHostedRelayConnectionStrategy();

    return await strategy.ConnectAsync(manager, match, cancellationToken);
}
```

Keep the no-token overload only as a compatibility wrapper that passes `CancellationToken.None`.

- [ ] **Step 8: Run tests and verify GREEN**

Run EditMode tests with `match-session-task5-green` artifact names.

Expected: strategy resolution passes, the pre-canceled connection test throws `OperationCanceledException`, and existing match ownership tests remain green.

- [ ] **Step 9: Commit connection strategies**

```powershell
git add -- "Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/Connections" `
  "Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs"
git commit -m "refactor: split online connection strategies"
```

---

### Task 6: Extract battle-frame aggregation from `MirrorNetBridge`

**Files:**
- Create: `Assets/正式开发项目制作/开发脚本/Mirror/MirrorBattleFrameCoordinator.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/Mirror/MirrorNetBridge.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [ ] **Step 1: Add direct coordinator regression tests**

Add tests requiring a constructible coordinator with these methods:

```csharp
Reset(string roomId, int expectedPlayers)
MsgBase Handle(MsgBase message)
bool IsBattleMessage(string protoName)
```

Reuse the existing `MsgBattleReady` and `MsgPlayerOp` test data. Expected assertions remain:

- First of two ready messages returns `null`.
- Second ready message returns a broadcast message.
- Each player operation returns exactly one incremental frame.
- Frame IDs advance from 1 to 2.
- `MsgBattleOver` and `MsgPlayerExit` pass through unchanged.

- [ ] **Step 2: Run tests and verify RED**

Run EditMode tests with `match-session-task6-red` artifact names.

Expected: tests fail because `MirrorBattleFrameCoordinator` does not exist.

- [ ] **Step 3: Create the coordinator and move battle state**

Move these fields out of `MirrorNetBridge` unchanged:

```csharp
private const int FrameHistoryLimit = 120;
private readonly Queue<MsgPlayerOp> pendingOps = new Queue<MsgPlayerOp>();
private readonly List<FrameData> frameHistory = new List<FrameData>();
private readonly object battleLock = new object();
private readonly HashSet<int> readyPlayers = new HashSet<int>();
private int frameId;
private string roomId = string.Empty;
private int randSeed;
private int expectedPlayers = 1;
private bool readyBroadcasted;
```

Move the existing behavior of `ResetBattleFrameState`, `HandleServerBattleMessage`, `IsServerAggregatedBattleMessage`, `BuildBattleReady`, and `BuildFramePack` into the new instance class. Preserve message names, frame contents, random seed behavior, history limit, and locking.

- [ ] **Step 4: Make `MirrorNetBridge` a transport adapter**

Add one field:

```csharp
private static readonly MirrorBattleFrameCoordinator BattleFrames = new MirrorBattleFrameCoordinator();
```

Replace the old methods with delegations:

```csharp
public static void ResetBattleFrameState(string roomId, int expectedPlayers)
{
    BattleFrames.Reset(roomId, expectedPlayers);
}

public static MsgBase HandleServerBattleMessage(MsgBase msg)
{
    return BattleFrames.Handle(msg);
}
```

In `ServerHandleRawMessage`, use `BattleFrames.IsBattleMessage(protoName)` before deciding whether the message may fall through to room/login handling.

- [ ] **Step 5: Run tests and verify GREEN**

Run EditMode tests with `match-session-task6-green` artifact names.

Expected: existing frame and ready tests pass without changing serialized messages or frame IDs.

- [ ] **Step 6: Commit the bridge extraction**

```powershell
git add -- "Assets/正式开发项目制作/开发脚本/Mirror/MirrorBattleFrameCoordinator.cs" `
  "Assets/正式开发项目制作/开发脚本/Mirror/MirrorBattleFrameCoordinator.cs.meta" `
  "Assets/正式开发项目制作/开发脚本/Mirror/MirrorNetBridge.cs" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs"
git commit -m "refactor: extract mirror battle frame coordinator"
```

---

### Task 7: Verify host, guest, rematch, and cancellation behavior

**Files:**
- Verify: all files changed by Tasks 1-6
- Verify: `scripts/Run-UnityCoverage.ps1`
- Verify: `scripts/Run-NetworkLoopback.ps1`
- Output: `artifacts/match-session-refactor/`

- [ ] **Step 1: Ensure no Unity Editor owns the project**

Run:

```powershell
Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
  Where-Object { $_.CommandLine -like '*D:\Desktop\game\aoyi team2*' } |
  Select-Object ProcessId, CommandLine
```

Expected: no process rows. If rows exist, close that editor before continuing.

- [ ] **Step 2: Run the full EditMode suite with coverage**

Run:

```powershell
& .\scripts\Run-UnityCoverage.ps1 `
  -ProjectRoot "D:\Desktop\game\aoyi team2" `
  -UnityExe "C:\Program Files\Unity\Hub\Editor\2022.3.61f1c1\Editor\Unity.exe" `
  -ArtifactsRoot "D:\Desktop\game\aoyi team2\artifacts\match-session-refactor\coverage"
```

Expected:

- NUnit failed count is `0`.
- Unity exit code is `0`.
- `artifacts/match-session-refactor/coverage/editmode-results.xml` exists.
- An HTML coverage report exists under `artifacts/match-session-refactor/coverage/coverage/`.

- [ ] **Step 3: Build the generated Unity solution**

```powershell
dotnet build "aoyi team2.sln" --no-restore --nologo
```

Expected: `0` errors. Existing `System.Net.Http` assembly-version warnings may remain unless separately fixed.

- [ ] **Step 4: Run the two-player loopback test repeatedly**

```powershell
& .\scripts\Run-NetworkLoopback.ps1 `
  -ProjectRoot "D:\Desktop\game\aoyi team2" `
  -UnityExe "C:\Program Files\Unity\Hub\Editor\2022.3.61f1c1\Editor\Unity.exe" `
  -Iterations 3 `
  -TimeoutSeconds 120 `
  -ArtifactsRoot "D:\Desktop\game\aoyi team2\artifacts\match-session-refactor\loopback"
```

Expected for all three runs:

- Host and guest enter the same battle.
- One death produces one battle-over transition.
- Host changes the network scene to `LobbyPanel`.
- Guest follows the host to `LobbyPanel` without loading it independently.
- Host closes the remote room once; guest does not issue a second close request.
- Host and guest stop Mirror after the lobby is loaded.
- A second matchmaking attempt can start without restarting either player.

- [ ] **Step 5: Run a cancellation-focused manual check**

Use two built players with Relay mode:

1. Start online matching.
2. Allow the match response to reach `matched` so Relay guest retry begins.
3. Cancel before the guest connects.
4. Wait longer than `GuestConnectAttemptTimeoutMs`.

Expected logs:

```text
[OnlineMatchManager] 已取消在线房间启动
```

Expected absence:

```text
Guest Relay 连接未完成，准备重试
```

after cancellation has been acknowledged.

- [ ] **Step 6: Inspect the final diff**

```powershell
git diff --check
git status --short
git diff --stat
```

Expected:

- No whitespace errors in files intentionally changed by this plan.
- No scene, Prefab, font, texture, Web, TCP server, or unrelated documentation changes are introduced by the refactor.
- Pre-existing unrelated worktree changes remain untouched.

- [ ] **Step 7: Commit verification-supported cleanup**

If verification caused only expected generated `.meta` additions or documentation updates:

```powershell
git add -- "Assets/正式开发项目制作/开发脚本" `
  "Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs" `
  "docs/superpowers/plans/2026-07-13-match-session-architecture-refactor.md"
git commit -m "refactor: complete match session architecture migration"
```

Do not use this broad `git add` command in a dirty worktree without first confirming every staged path belongs to this plan. Prefer the explicit file lists from Tasks 1-6 when unrelated changes are present.

---

## Completion criteria

The refactor is complete only when all conditions below are true:

- `paiwei_solo` resolves to `paiwei_map` in every local, LAN, Relay, and dedicated flow.
- `BattleManager` no longer directly stops Mirror or loads the lobby.
- Remote room closure is awaited and is initiated only by the authoritative online server/host.
- Host and guest both reach the lobby before Mirror is stopped.
- Match ending and lobby finalization are idempotent.
- Relay delays, connection polling, and retry delays observe the caller's cancellation token.
- `ChooseHeroPanel.cs` contains only UI/selection behavior and no disabled `OnlineMatchManager` implementation.
- `MirrorNetBridge` delegates battle-frame state to `MirrorBattleFrameCoordinator`.
- EditMode tests, generated solution build, and three loopback iterations pass.
- A rematch succeeds in the same process after a completed game.

## Follow-up boundary

After this plan is stable, execute the independent backend split described in `docs/backend-abstraction-plan.md`:

- `IAuthProvider`
- `IPlayerProfileProvider`
- `IRoomDirectoryProvider`
- `ITransportConnector`

Do not start that provider migration in the same branch as Tasks 1-6. Keeping the session-lifecycle refactor separate makes regressions in authentication, room-directory queries, and Mirror shutdown independently diagnosable.
