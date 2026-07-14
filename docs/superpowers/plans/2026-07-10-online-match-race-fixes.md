# Online Match Race Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make online matchmaking cancellation and restart deterministic, cancel Relay connection retries, and reject room payloads that do not contain the local user.

**Architecture:** Keep `OnlineMatchManager` as the Unity-facing coordinator, but bind every asynchronous result to the generation that created it. Bind matched-room startup to a `CancellationTokenSource`, and make room-role validation a fail-closed pure function that is covered by EditMode tests.

**Tech Stack:** Unity 2022.3, C#, Mirror, NUnit EditMode tests

---

### Task 1: Add regression tests for operation ownership

**Files:**
- Modify: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [x] **Step 1: Write failing generation and cancellation tests**

Add reflection-based tests that require these production APIs:

```csharp
Assert.IsFalse(InvokeShouldAcceptStartResult(1, 3, true, true, false));
Assert.IsTrue(InvokeShouldAcceptStartResult(3, 3, true, true, false));
Assert.IsTrue(InvokeCanCancelMatch(false, false, true));
```

- [x] **Step 2: Run the tests and verify RED**

Run the targeted EditMode test harness. Expected: FAIL because `ShouldAcceptStartResult` and `CanCancelMatch` do not exist.

- [x] **Step 3: Implement operation ownership**

In `OnlineMatchManager`, route all post-await acceptance through:

```csharp
public static bool ShouldAcceptStartResult(int generation, int currentGeneration, bool matching, bool waiting, bool canceled)
{
    return generation == currentGeneration && matching && waiting && !canceled;
}
```

Capture the access token per operation, cancel a ticket returned after local cancellation, and clear shared cancellation state before awaiting the cancel API.

- [x] **Step 4: Run targeted tests and verify GREEN**

Expected: generation and cancellation tests pass.

### Task 2: Add cancellable matched-room startup

**Files:**
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineMatchManager.cs`
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [x] **Step 1: Write a failing pre-canceled-token test**

Require a `StartMatchedRoomAsync(..., CancellationToken)` overload and assert that a pre-canceled token produces `OperationCanceledException` before any Mirror state is created.

- [x] **Step 2: Run the test and verify RED**

Expected: FAIL because the cancellation-aware overload does not exist.

- [x] **Step 3: Implement cancellation propagation**

Create one `CancellationTokenSource` per matched-room startup, pass its token through initial delay, retry delay, connection polling, and cleanup Mirror state on cancellation. Make `CancelMatch` accept `isStartingMatchedRoom` and cancel the active source.

- [x] **Step 4: Run targeted tests and verify GREEN**

Expected: the pre-canceled token test and match-cancel predicate test pass.

### Task 3: Fail closed on room membership mismatch

**Files:**
- Modify: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/OnlineMatch/OnlineRelayConnector.cs`
- Test: `Assets/Tests/EditMode/AoyiNetworkRoomManagerTests.cs`

- [x] **Step 1: Write failing role-resolution tests**

Require `TryResolveRoomRole(localUserId, hostUserId, guestUserId, out role)` and assert host/guest resolution succeeds while an unrelated user fails.

- [x] **Step 2: Run the tests and verify RED**

Expected: FAIL because `TryResolveRoomRole` does not exist.

- [x] **Step 3: Implement fail-closed role validation**

Use the pure resolver from the payload normalization step. When the room includes member IDs but the local user matches neither, return `false` from normalization and abort `StartMatchedRoomAsync`.

- [x] **Step 4: Run targeted tests and verify GREEN**

Expected: all role-resolution cases pass.

### Task 4: Full verification

**Files:**
- Verify all modified C# and test files

- [x] **Step 1: Build the Unity-generated solution**

Run: `dotnet build "aoyi team2.sln" --no-restore --nologo`

Expected: 0 compilation errors.

- [x] **Step 2: Run Web matchmaking tests**

Run from `Web`: `pnpm test`

Expected: 11 tests pass.

- [x] **Step 3: Inspect the final diff**

Run: `git diff --check` and `git status --short`.

Expected: only intended source, test, scene formatting, and plan changes are present; pre-existing unrelated changes remain untouched.
