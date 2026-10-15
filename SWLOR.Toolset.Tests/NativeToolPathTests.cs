using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Tests;

public class NativeToolPathTests
{
    [TestCase("nwn_erf", true, "nwn_erf.exe")]
    [TestCase("nwn_erf.exe", true, "nwn_erf.exe")]
    [TestCase("nwn_erf", false, "nwn_erf")]
    [TestCase("nwn_erf.exe", false, "nwn_erf")]
    public void NamesMatchTheHost(string name, bool windows, string expected) =>
        NativeToolPath.ExecutableName(name, windows).Should().Be(expected);

    [Test]
    public void DirectoryResolutionUsesTheHostNativeExecutable()
    {
        var directory = Path.Combine(Path.GetTempPath(), "swlor_native_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "nwn_erf.exe"), "Windows utility");
            if (OperatingSystem.IsLinux())
            {
                NativeToolPath.FindInDirectory(directory, "nwn_erf").Should().BeNull(
                    "a PE executable is unusable on Linux");
                File.WriteAllText(Path.Combine(directory, "nwn_erf"), "Linux utility");
            }
            NativeToolPath.FindInDirectory(directory, "nwn_erf").Should()
                .Be(Path.Combine(directory, NativeToolPath.ExecutableName("nwn_erf")));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Test]
    public void LinuxDeploymentSymlinksRemainSupported()
    {
        if (!OperatingSystem.IsLinux()) Assert.Ignore("Linux deployment compatibility");
        var directory = Path.Combine(Path.GetTempPath(), "swlor_symlink_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var target = Path.Combine(directory, "native");
            File.WriteAllText(target, "native utility");
            var link = Path.Combine(directory, "nwn_erf.exe");
            File.CreateSymbolicLink(link, target);
            NativeToolPath.FindInDirectory(directory, "nwn_erf").Should().Be(link);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Test]
    public void BareExecutableUsesItsPathDirectoryForLinuxLibraries()
    {
        if (!OperatingSystem.IsLinux()) Assert.Ignore("Linux shared libraries");
        var directory = Path.Combine(Path.GetTempPath(), "swlor_path_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var executable = Path.Combine(directory, "nwn_erf");
            File.WriteAllText(executable, "native utility");
            var start = new ProcessStartInfo("nwn_erf");
            start.Environment["PATH"] = directory;
            start.Environment["LD_LIBRARY_PATH"] = "/existing";
            NativeToolPath.ConfigureLibraryPath(start);
            start.FileName.Should().Be(executable);
            start.Environment["LD_LIBRARY_PATH"].Should().Be(directory + ":/existing");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Test]
    public void UnknownExecutableDoesNotInventALinuxLibraryDirectory()
    {
        var start = new ProcessStartInfo("missing_" + Guid.NewGuid().ToString("N"));
        start.Environment["PATH"] = Path.GetTempPath();
        start.Environment["LD_LIBRARY_PATH"] = "/existing";
        NativeToolPath.ConfigureLibraryPath(start);
        start.Environment["LD_LIBRARY_PATH"].Should().Be("/existing");
    }

    [Test]
    public void LinuxNativeLibrariesAreScopedToTheChildProcess()
    {
        var start = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "tools", "nwn_erf"));
        start.Environment["LD_LIBRARY_PATH"] = "/existing";
        NativeToolPath.ConfigureLibraryPath(start);
        start.Environment["LD_LIBRARY_PATH"].Should().Be(OperatingSystem.IsLinux()
            ? Path.Combine(Path.GetTempPath(), "tools") + ":/existing"
            : "/existing");
    }
}
