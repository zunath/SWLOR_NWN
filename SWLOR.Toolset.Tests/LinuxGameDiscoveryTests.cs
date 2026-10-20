using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

public class LinuxGameDiscoveryTests
{
    [Test]
    public void SteamDiscoveryIncludesNativeLegacyAndFlatpakLibraries()
    {
        var home = Path.Combine(Path.GetTempPath(), "swlor_home");
        NwnInstallLocator.LinuxSteamRoots(home).Should().BeEquivalentTo(new[]
        {
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam")
        });
    }

    [Test]
    public void ExplicitGameInstallWorksOnEitherPlatform()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor_game_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "data"));
        try { NwnInstallLocator.Locate(root).Should().Be(root); }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test, NonParallelizable]
    public void UserProfileOverrideFindsCustomLinuxProfiles()
    {
        var previous = Environment.GetEnvironmentVariable("NWN_USER_DIRECTORY");
        var profile = Path.Combine(Path.GetTempPath(), "custom_nwn_profile");
        try
        {
            Environment.SetEnvironmentVariable("NWN_USER_DIRECTORY", profile);
            NwnIniProfile.DefaultIniPath.Should().Be(Path.Combine(profile, "nwn.ini"));
        }
        finally { Environment.SetEnvironmentVariable("NWN_USER_DIRECTORY", previous); }
    }
}
