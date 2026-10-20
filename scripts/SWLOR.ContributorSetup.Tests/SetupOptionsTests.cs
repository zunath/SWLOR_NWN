using NUnit.Framework;

namespace SWLOR.ContributorSetup.Tests;

public class SetupOptionsTests
{
    [TestCase("toolset", "Server", "Toolset")]
    [TestCase("server", "Toolset", "Server")]
    [TestCase("setup", "server", "Server")]
    [TestCase("CHECK", "Toolset", "Toolset")]
    public void LaunchModeSelectsTheNamedApplication(string mode, string profile, string expected)
    {
        var options = SetupOptions.Parse(new[] { "--mode", mode, "--profile", profile });
        Assert.That(options.Profile, Is.EqualTo(expected));
    }

    [TestCase("--mode")]
    [TestCase("--profile", "Other")]
    [TestCase("--mode", "Other")]
    [TestCase("--unknown", "Server")]
    public void InvalidArgumentsFailBeforeSetup(params string[] args) =>
        Assert.Throws<ArgumentException>(() => SetupOptions.Parse(args));
}
