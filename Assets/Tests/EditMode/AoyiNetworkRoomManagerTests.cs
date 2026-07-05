using NUnit.Framework;
using System.Reflection;

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
