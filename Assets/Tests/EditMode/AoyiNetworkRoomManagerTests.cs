using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AoyiNetworkRoomManagerTests
{
    [Test]
    public void HasEnoughPlayersToStartRequiresFullConfiguredRoom()
    {
        MethodInfo method = GetHasEnoughPlayersMethod();

        Assert.IsFalse((bool)method.Invoke(null, new object[] { 1, 2 }));
        Assert.IsTrue((bool)method.Invoke(null, new object[] { 2, 2 }));
    }

    [Test]
    public void ShouldStartBattleAfterReadyRequiresEnoughReadyRoomPlayers()
    {
        MethodInfo method = GetShouldStartBattleAfterReadyMethod();

        Assert.IsFalse((bool)method.Invoke(null, new object[] { 2, 1, 2 }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { 1, 1, 2 }));
        Assert.IsTrue((bool)method.Invoke(null, new object[] { 2, 2, 2 }));
    }

    [Test]
    public void MirrorBattleServerBuildsFramePackFromPlayerOps()
    {
        var bridgeType = System.Type.GetType("Aoyi.Mirror.MirrorNetBridge, Assembly-CSharp");
        Assert.NotNull(bridgeType, "MirrorNetBridge type should be available.");

        MethodInfo resetMethod = bridgeType.GetMethod(
            "ResetBattleFrameState",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null);
        Assert.NotNull(resetMethod, "MirrorNetBridge.ResetBattleFrameState should exist.");

        MethodInfo enqueueMethod = bridgeType.GetMethod(
            "HandleServerBattleMessage",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(enqueueMethod, "MirrorNetBridge.HandleServerBattleMessage should exist.");

        resetMethod.Invoke(null, new object[] { "room-a" });

        var opType = System.Type.GetType("MsgPlayerOp, Assembly-CSharp");
        Assert.NotNull(opType, "MsgPlayerOp type should be available.");
        object op = System.Activator.CreateInstance(opType);
        opType.GetField("roomId").SetValue(op, "room-a");
        opType.GetField("playerId").SetValue(op, 1);
        opType.GetField("teamId").SetValue(op, 2);
        opType.GetField("moveDirX").SetValue(op, 100);

        object framePack = enqueueMethod.Invoke(null, new object[] { op });
        Assert.NotNull(framePack, "Server should return a frame pack after receiving a player op.");

        var packType = framePack.GetType();
        Assert.AreEqual("room-a", packType.GetField("roomId").GetValue(framePack));
        Assert.AreEqual(1, packType.GetField("frameId").GetValue(framePack));

        var frames = (System.Collections.IList)packType.GetField("frames").GetValue(framePack);
        Assert.AreEqual(1, frames.Count);

        var frame = frames[0];
        var frameType = frame.GetType();
        var ops = (System.Collections.IList)frameType.GetField("allPlayerOps").GetValue(frame);
        Assert.AreEqual(1, ops.Count);
        Assert.AreEqual(1, opType.GetField("playerId").GetValue(ops[0]));

        object op2 = System.Activator.CreateInstance(opType);
        opType.GetField("roomId").SetValue(op2, "room-a");
        opType.GetField("playerId").SetValue(op2, 2);
        opType.GetField("teamId").SetValue(op2, 1);

        object framePack2 = enqueueMethod.Invoke(null, new object[] { op2 });
        Assert.NotNull(framePack2, "Server should return another frame pack after the second player op.");
        Assert.AreEqual(2, packType.GetField("frameId").GetValue(framePack2));

        var frames2 = (System.Collections.IList)packType.GetField("frames").GetValue(framePack2);
        Assert.AreEqual(2, frames2.Count, "Frame pack should include recent frame history so late clients can catch up.");
    }

    [Test]
    public void MirrorBattleReadyWaitsForAllExpectedPlayers()
    {
        var bridgeType = System.Type.GetType("Aoyi.Mirror.MirrorNetBridge, Assembly-CSharp");
        Assert.NotNull(bridgeType, "MirrorNetBridge type should be available.");

        MethodInfo resetMethod = bridgeType.GetMethod(
            "ResetBattleFrameState",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(int) },
            null);
        Assert.NotNull(resetMethod, "MirrorNetBridge.ResetBattleFrameState(roomId, expectedPlayers) should exist.");

        MethodInfo handleMethod = bridgeType.GetMethod(
            "HandleServerBattleMessage",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(handleMethod, "MirrorNetBridge.HandleServerBattleMessage should exist.");

        resetMethod.Invoke(null, new object[] { "room-ready", 2 });

        var readyType = System.Type.GetType("MsgBattleReady, Assembly-CSharp");
        Assert.NotNull(readyType, "MsgBattleReady type should be available.");

        object ready1 = System.Activator.CreateInstance(readyType);
        readyType.GetField("roomId").SetValue(ready1, "room-ready");
        readyType.GetField("userId").SetValue(ready1, 1);
        Assert.IsNull(handleMethod.Invoke(null, new object[] { ready1 }), "First ready should not start the battle yet.");

        object ready2 = System.Activator.CreateInstance(readyType);
        readyType.GetField("roomId").SetValue(ready2, "room-ready");
        readyType.GetField("userId").SetValue(ready2, 2);
        Assert.NotNull(handleMethod.Invoke(null, new object[] { ready2 }), "Battle should start after all expected players are ready.");
    }

    [Test]
    public void BattleInitRegistersBattleMessagesAfterPlayerManagerInit()
    {
        var battleManagerType = System.Type.GetType("BattleManager, Assembly-CSharp");
        Assert.NotNull(battleManagerType, "BattleManager type should be available.");

        MethodInfo method = battleManagerType.GetMethod(
            "GetBattleInitCommandNames",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "BattleManager.GetBattleInitCommandNames should exist.");

        var names = (string[])method.Invoke(null, null);
        int registerIndex = System.Array.IndexOf(names, "RegisterBattleMsg");
        int playerIndex = System.Array.IndexOf(names, "InitPlayerManager");

        Assert.GreaterOrEqual(registerIndex, 0, "RegisterBattleMsg should be in BattleInit.");
        Assert.GreaterOrEqual(playerIndex, 0, "InitPlayerManager should be in BattleInit.");
        Assert.Less(playerIndex, registerIndex, "Frame message listeners must not be registered before PlayerManager is initialized.");
    }

    [Test]
    public void PointAttackDamagesOnlyEnemiesInsideRadiusAndCanKill()
    {
        var resolverType = System.Type.GetType("CombatResolver, Assembly-CSharp");
        Assert.NotNull(resolverType, "CombatResolver type should be available.");

        var playerInfoType = System.Type.GetType("_playerInfo, Assembly-CSharp");
        Assert.NotNull(playerInfoType, "_playerInfo type should be available.");

        var fixed64Type = System.Type.GetType("FixMath.Fixed64, Aoyi.FixMath");
        Assert.NotNull(fixed64Type, "Fixed64 type should be available.");

        var fixedVector2Type = System.Type.GetType("FixMath.FixedVector2, Aoyi.FixMath");
        Assert.NotNull(fixedVector2Type, "FixedVector2 type should be available.");

        MethodInfo method = resolverType.GetMethod(
            "ApplyPointAttack",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { playerInfoType, typeof(IEnumerable<>).MakeGenericType(playerInfoType), typeof(Vector2), fixed64Type, fixed64Type },
            null);
        Assert.NotNull(method, "CombatResolver.ApplyPointAttack should exist.");

        object attacker = CreatePlayerInfo(playerInfoType, fixedVector2Type, "0", 101, 1, 0, 0);
        object enemyInRadius = CreatePlayerInfo(playerInfoType, fixedVector2Type, "1", 101, 2, 1, 0);
        object allyInRadius = CreatePlayerInfo(playerInfoType, fixedVector2Type, "2", 101, 1, 1, 0);
        object enemyOutsideRadius = CreatePlayerInfo(playerInfoType, fixedVector2Type, "3", 101, 2, 4, 0);

        var players = System.Array.CreateInstance(playerInfoType, 4);
        players.SetValue(attacker, 0);
        players.SetValue(enemyInRadius, 1);
        players.SetValue(allyInRadius, 2);
        players.SetValue(enemyOutsideRadius, 3);

        int hitCount = (int)method.Invoke(null, new[]
        {
            attacker,
            players,
            new Vector2(1, 0),
            System.Activator.CreateInstance(fixed64Type, 150),
            System.Activator.CreateInstance(fixed64Type, 0.5f)
        });

        Assert.AreEqual(1, hitCount);
        Assert.IsTrue((bool)playerInfoType.GetProperty("IsDead").GetValue(enemyInRadius), "Enemy in radius should die after lethal damage.");
        Assert.IsFalse((bool)playerInfoType.GetProperty("IsDead").GetValue(allyInRadius), "Ally in radius should not be damaged.");
        Assert.IsFalse((bool)playerInfoType.GetProperty("IsDead").GetValue(enemyOutsideRadius), "Enemy outside radius should not be damaged.");
    }



    [Test]
    public void BattleEndsWhenAnyPlayerDies()
    {
        var battleManagerType = System.Type.GetType("BattleManager, Assembly-CSharp");
        Assert.NotNull(battleManagerType, "BattleManager type should be available.");

        var playerInfoType = System.Type.GetType("_playerInfo, Assembly-CSharp");
        Assert.NotNull(playerInfoType, "_playerInfo type should be available.");

        var fixedVector2Type = System.Type.GetType("FixMath.FixedVector2, Aoyi.FixMath");
        Assert.NotNull(fixedVector2Type, "FixedVector2 type should be available.");

        MethodInfo method = battleManagerType.GetMethod(
            "ShouldEndBattleAfterDeath",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(IEnumerable<>).MakeGenericType(playerInfoType) },
            null);
        Assert.NotNull(method, "BattleManager.ShouldEndBattleAfterDeath should exist.");

        object alive = CreatePlayerInfo(playerInfoType, fixedVector2Type, "0", 101, 1, 0, 0);
        object dead = CreatePlayerInfo(playerInfoType, fixedVector2Type, "1", 101, 2, 1, 0);
        playerInfoType.GetMethod("SetDead").Invoke(dead, new object[] { true });

        var players = System.Array.CreateInstance(playerInfoType, 2);
        players.SetValue(alive, 0);
        players.SetValue(dead, 1);

        Assert.IsTrue((bool)method.Invoke(null, new object[] { players }));
    }

    [Test]
    public void HeroSelectionBuildsLocalMatchRequestWithSelectedHero()
    {
        System.Type controllerType = GetHeroSelectionMatchControllerType();
        MethodInfo method = controllerType.GetMethod("BuildLocalMatchRequest", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "HeroSelectionMatchController.BuildLocalMatchRequest should exist.");

        System.Type gameModesType = System.Type.GetType("GameModes, Assembly-CSharp");
        Assert.NotNull(gameModesType, "GameModes type should be available.");
        object dantiaoMode = System.Enum.Parse(gameModesType, "dantiao");

        object request = method.Invoke(null, new object[] { dantiaoMode, 101, "42" });
        Assert.NotNull(request);

        System.Type requestType = System.Type.GetType("MsgMatchRequest, Assembly-CSharp");
        Assert.NotNull(requestType, "MsgMatchRequest type should be available.");
        Assert.AreEqual(requestType, request.GetType());

        Assert.AreEqual(dantiaoMode, requestType.GetProperty("GameModes").GetValue(request));
        var playerPack = (System.Collections.IList)requestType.GetField("playerPack").GetValue(request);
        Assert.NotNull(playerPack);
        Assert.AreEqual(1, playerPack.Count);

        object playerChoose = playerPack[0];
        System.Type playerChooseType = playerChoose.GetType();
        Assert.AreEqual(42, playerChooseType.GetField("userId").GetValue(playerChoose));
        Assert.AreEqual(101, playerChooseType.GetField("selectedHeroId").GetValue(playerChoose));
    }

    [Test]
    public void HeroSelectionRoutesLanAndOnlineModesSeparately()
    {
        System.Type controllerType = GetHeroSelectionMatchControllerType();
        MethodInfo quickMatchMethod = controllerType.GetMethod("UsesQuickMatch", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(quickMatchMethod, "HeroSelectionMatchController.UsesQuickMatch should exist.");
        MethodInfo onlineMatchMethod = controllerType.GetMethod("UsesOnlineMatch", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(onlineMatchMethod, "HeroSelectionMatchController.UsesOnlineMatch should exist.");

        System.Type networkModeType = System.Type.GetType("NetworkMode, Assembly-CSharp");
        Assert.NotNull(networkModeType, "NetworkMode type should be available.");

        object localServer = System.Enum.Parse(networkModeType, "LocalServer");
        object lanHost = System.Enum.Parse(networkModeType, "LanHost");
        object lanClient = System.Enum.Parse(networkModeType, "LanClient");
        object supabaseOnline = System.Enum.Parse(networkModeType, "SupabaseOnline");

        Assert.IsFalse((bool)quickMatchMethod.Invoke(null, new object[] { localServer }));
        Assert.IsTrue((bool)quickMatchMethod.Invoke(null, new object[] { lanHost }));
        Assert.IsTrue((bool)quickMatchMethod.Invoke(null, new object[] { lanClient }));
        Assert.IsFalse((bool)quickMatchMethod.Invoke(null, new object[] { supabaseOnline }));

        Assert.IsFalse((bool)onlineMatchMethod.Invoke(null, new object[] { localServer }));
        Assert.IsFalse((bool)onlineMatchMethod.Invoke(null, new object[] { lanHost }));
        Assert.IsFalse((bool)onlineMatchMethod.Invoke(null, new object[] { lanClient }));
        Assert.IsTrue((bool)onlineMatchMethod.Invoke(null, new object[] { supabaseOnline }));
    }

    private static object CreatePlayerInfo(System.Type playerInfoType, System.Type fixedVector2Type, string userId, int heroId, int teamId, int x, int y)
    {
        object bornPoint = System.Activator.CreateInstance(fixedVector2Type, x, y);
        return System.Activator.CreateInstance(playerInfoType, userId, heroId, teamId, bornPoint);
    }

    private static System.Type GetHeroSelectionMatchControllerType()
    {
        System.Type controllerType = System.Type.GetType("Panels.HeroSelectionMatchController, Assembly-CSharp");
        Assert.NotNull(controllerType, "Panels.HeroSelectionMatchController should exist.");
        return controllerType;
    }

    private static MethodInfo GetHasEnoughPlayersMethod()
    {
        var type = System.Type.GetType("Aoyi.Mirror.AoyiNetworkRoomManager, Assembly-CSharp");
        Assert.NotNull(type, "AoyiNetworkRoomManager type should be available.");

        MethodInfo method = type.GetMethod(
            "HasEnoughPlayersToStart",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "AoyiNetworkRoomManager.HasEnoughPlayersToStart should exist.");
        return method;
    }

    private static MethodInfo GetShouldStartBattleAfterReadyMethod()
    {
        var type = System.Type.GetType("Aoyi.Mirror.AoyiNetworkRoomManager, Assembly-CSharp");
        Assert.NotNull(type, "AoyiNetworkRoomManager type should be available.");

        MethodInfo method = type.GetMethod(
            "ShouldStartBattleAfterReady",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "AoyiNetworkRoomManager.ShouldStartBattleAfterReady should exist.");
        return method;
    }
}
