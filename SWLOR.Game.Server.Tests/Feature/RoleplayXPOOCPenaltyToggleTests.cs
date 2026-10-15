using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class RoleplayXPOOCPenaltyToggleTests
{
    private const string EnabledVariable = "SWLOR_RPXP_OOC_PENALTY_ENABLED";
    private string _previousValue;

    [SetUp]
    public void SaveSettings()
    {
        _previousValue = Environment.GetEnvironmentVariable(EnabledVariable);
    }

    [TearDown]
    public void RestoreSettings()
    {
        Environment.SetEnvironmentVariable(EnabledVariable, _previousValue);
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
        Configure(value).RoleplayXPOOCPenaltyEnabled.Should().Be(expected);
    }

    [Test]
    public void ShippedContainerConfigurationDisablesOnlyOOCPenalties()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var setting = File.ReadLines(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Docker", "swlor.env"))
            .Single(line => line.StartsWith(EnabledVariable + "=", StringComparison.Ordinal));

        Configure(setting[(EnabledVariable.Length + 1)..]).RoleplayXPOOCPenaltyEnabled.Should().BeFalse();
    }

    private static ApplicationSettings Configure(string value)
    {
        Environment.SetEnvironmentVariable(EnabledVariable, value);
        return (ApplicationSettings)Activator.CreateInstance(typeof(ApplicationSettings), nonPublic: true);
    }
}
