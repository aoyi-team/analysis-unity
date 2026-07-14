using System;
using System.Reflection;
using NUnit.Framework;

public class NetworkLoopbackOptionsTests
{
    [Test]
    public void NoRoleLeavesLoopbackTestDisabled()
    {
        Type optionsType = GetOptionsType();
        MethodInfo tryParse = GetTryParseMethod(optionsType);
        object[] arguments = { new[] { "Aoyi.exe" }, null, null };

        bool enabled = (bool)tryParse.Invoke(null, arguments);

        Assert.IsFalse(enabled);
        Assert.IsNull(arguments[1]);
        Assert.IsNull(arguments[2]);
    }

    [Test]
    public void ValidHostArgumentsAreParsed()
    {
        Type optionsType = GetOptionsType();
        MethodInfo tryParse = GetTryParseMethod(optionsType);
        object[] arguments =
        {
            new[]
            {
                "Aoyi.exe",
                "-networkTestRole", "host",
                "-networkTestRunId", "run-123",
                "-networkTestPort", "18888",
                "-networkTestArtifacts", "D:\\tmp\\run-123",
                "-networkTestTimeout", "45"
            },
            null,
            null
        };

        bool enabled = (bool)tryParse.Invoke(null, arguments);

        Assert.IsTrue(enabled, arguments[2] as string);
        object options = arguments[1];
        Assert.NotNull(options);
        Assert.AreEqual("host", GetProperty(optionsType, options, "Role"));
        Assert.AreEqual("run-123", GetProperty(optionsType, options, "RunId"));
        Assert.AreEqual(18888, GetProperty(optionsType, options, "Port"));
        Assert.AreEqual("D:\\tmp\\run-123", GetProperty(optionsType, options, "ArtifactsRoot"));
        Assert.AreEqual(45, GetProperty(optionsType, options, "TimeoutSeconds"));
    }

    [TestCase("observer", "18888")]
    [TestCase("client", "0")]
    [TestCase("client", "70000")]
    public void InvalidRoleOrPortIsRejected(string role, string port)
    {
        Type optionsType = GetOptionsType();
        MethodInfo tryParse = GetTryParseMethod(optionsType);
        object[] arguments =
        {
            new[]
            {
                "Aoyi.exe",
                "-networkTestRole", role,
                "-networkTestRunId", "run-123",
                "-networkTestPort", port,
                "-networkTestArtifacts", "D:\\tmp\\run-123"
            },
            null,
            null
        };

        bool enabled = (bool)tryParse.Invoke(null, arguments);

        Assert.IsFalse(enabled);
        Assert.IsNull(arguments[1]);
        Assert.IsNotEmpty(arguments[2] as string);
    }

    [Test]
    public void LoopbackHostSendsBattleOverOnlyAfterBothPlayersReachBattle()
    {
        Type bootstrapType = GetBootstrapType();
        MethodInfo method = bootstrapType.GetMethod(
            "ShouldSendBattleOver",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "NetworkLoopbackTestBootstrap.ShouldSendBattleOver should exist.");

        Assert.IsTrue((bool)method.Invoke(null, new object[] { "host", true, true, true, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { "client", true, true, true, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { "host", true, false, true, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { "host", true, true, false, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { "host", true, true, true, true }));
    }

    [Test]
    public void LoopbackCompletesOnlyAfterBothPlayersReturnToLobby()
    {
        Type bootstrapType = GetBootstrapType();
        MethodInfo method = bootstrapType.GetMethod(
            "ShouldCompleteLobbyReturn",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "NetworkLoopbackTestBootstrap.ShouldCompleteLobbyReturn should exist.");

        Assert.IsFalse((bool)method.Invoke(null, new object[] { true, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { false, true }));
        Assert.IsTrue((bool)method.Invoke(null, new object[] { true, true }));
    }

    private static Type GetOptionsType()
    {
        Type optionsType = Type.GetType("NetworkLoopbackOptions, Assembly-CSharp");
        Assert.NotNull(optionsType, "NetworkLoopbackOptions should exist in Assembly-CSharp.");
        return optionsType;
    }

    private static Type GetBootstrapType()
    {
        Type bootstrapType = Type.GetType("NetworkLoopbackTestBootstrap, Assembly-CSharp");
        Assert.NotNull(bootstrapType, "NetworkLoopbackTestBootstrap should exist in Assembly-CSharp.");
        return bootstrapType;
    }

    private static MethodInfo GetTryParseMethod(Type optionsType)
    {
        MethodInfo method = optionsType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method, "NetworkLoopbackOptions.TryParse should exist.");
        return method;
    }

    private static object GetProperty(Type optionsType, object instance, string name)
    {
        PropertyInfo property = optionsType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property, $"NetworkLoopbackOptions.{name} should exist.");
        return property.GetValue(instance);
    }
}
