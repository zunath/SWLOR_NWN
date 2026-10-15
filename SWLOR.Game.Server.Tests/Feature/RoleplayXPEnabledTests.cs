using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class RoleplayXPEnabledTests
{
    private const string EnabledVariable = "SWLOR_RPXP_ENABLED";
    private static readonly FieldInfo SettingsField = typeof(ApplicationSettings)
        .GetField("_settings", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo DetectionField = typeof(RoleplayXP)
        .GetField("_oocDetection", BindingFlags.NonPublic | BindingFlags.Static)!;
    private string _previousValue;
    private object _previousSettings;
    private object _previousDetection;

    [SetUp]
    public void SaveSettings()
    {
        _previousValue = Environment.GetEnvironmentVariable(EnabledVariable);
        _previousSettings = SettingsField.GetValue(null);
        _previousDetection = DetectionField.GetValue(null);
    }

    [TearDown]
    public void RestoreSettings()
    {
        Environment.SetEnvironmentVariable(EnabledVariable, _previousValue);
        SettingsField.SetValue(null, _previousSettings);
        DetectionField.SetValue(null, _previousDetection);
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("invalid", false)]
    [TestCase("false", false)]
    [TestCase("0", false)]
    [TestCase("no", false)]
    [TestCase("true", true)]
    [TestCase(" TRUE ", true)]
    [TestCase("1", true)]
    [TestCase("yes", true)]
    public void SettingsRequireExplicitEnablement(string value, bool expected)
    {
        Configure(value).RoleplayXPEnabled.Should().Be(expected);
    }

    private static IEnumerable<TestCaseData> DisabledHandlers()
    {
        foreach (var method in new[] { nameof(RoleplayXP.LoadOOCDetection), nameof(RoleplayXP.ProcessRPMessage), nameof(RoleplayXP.DistributeRoleplayXP) })
        foreach (var value in new string[] { null, "false" })
            yield return new TestCaseData(method, value);
    }

    [TestCaseSource(nameof(DisabledHandlers))]
    public void DisabledHandlersReturnBeforeEngineOrDatabaseWork(string method, string value)
    {
        Configure(value);
        var handler = typeof(RoleplayXP).GetMethod(method)!.CreateDelegate<Action>();

        handler.Should().NotThrow("disabled RP XP must not access NWN, player records, or the detection file");
        DetectionField.GetValue(null).Should().BeSameAs(_previousDetection);
    }

    [Test]
    public void ShippedContainerConfigurationDisablesRPXP()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var setting = File.ReadLines(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Docker", "swlor.env"))
            .Single(line => line.StartsWith(EnabledVariable + "=", StringComparison.Ordinal));

        Configure(setting[(EnabledVariable.Length + 1)..]).RoleplayXPEnabled.Should().BeFalse();
    }

    private static ApplicationSettings Configure(string value)
    {
        Environment.SetEnvironmentVariable(EnabledVariable, value);
        SettingsField.SetValue(null, null);
        return ApplicationSettings.Get();
    }
}
