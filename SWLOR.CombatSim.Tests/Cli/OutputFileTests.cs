using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Cli;

namespace SWLOR.CombatSim.Tests.Cli
{
    [TestFixture]
    public class OutputFileTests
    {
        [Test]
        public void WriteAllText_ReplacesALinkedPath_WithoutTouchingTheFileItLinksTo()
        {
            var directory = Directory.CreateTempSubdirectory("combatsim-output-").FullName;
            try
            {
                var scenario = Path.Combine(directory, "scenario.json");
                File.WriteAllText(scenario, "{ }");
                var report = Path.Combine(directory, "report.md");
                if (!TryLink(report, scenario))
                    Assert.Ignore("This system cannot create a file link without extra privileges.");

                OutputFile.WriteAllText(report, "report");

                File.ReadAllText(scenario).Should().Be("{ }", "the linked input keeps its contents");
                File.ReadAllText(report).Should().Be("report");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void AreSameFile_FollowsADirectoryAliasInTheInputPath()
        {
            var directory = Directory.CreateTempSubdirectory("combatsim-output-").FullName;
            try
            {
                var real = Directory.CreateDirectory(Path.Combine(directory, "real")).FullName;
                var alias = Path.Combine(directory, "alias");
                File.WriteAllText(Path.Combine(real, "scenario.json"), "{ }");
                if (!TryAliasDirectory(alias, real))
                    Assert.Ignore("This system cannot create a directory alias without extra privileges.");

                CommandLineOptions.AreSameFile(Path.Combine(alias, "scenario.json"), Path.Combine(real, "scenario.json"))
                    .Should().BeTrue("an output naming the real file would overwrite the aliased input");
                CommandLineOptions.AreSameFile(Path.Combine(alias, "scenario.json"), Path.Combine(real, "report.md"))
                    .Should().BeFalse();
            }
            finally
            {
                var alias = Path.Combine(directory, "alias");
                if (Directory.Exists(alias))
                    Directory.Delete(alias);
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void AreSameFile_FollowsASymbolicLinkToAFile()
        {
            var directory = Directory.CreateTempSubdirectory("combatsim-output-").FullName;
            try
            {
                var scenario = Path.Combine(directory, "scenario.json");
                File.WriteAllText(scenario, "{ }");
                var link = Path.Combine(directory, "scenario-link.json");
                try
                {
                    File.CreateSymbolicLink(link, scenario);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Assert.Ignore("This system cannot create a symbolic link without extra privileges.");
                }

                CommandLineOptions.AreSameFile(link, scenario).Should().BeTrue();
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void WriteAllText_ReplacesAnExistingFile()
        {
            var directory = Directory.CreateTempSubdirectory("combatsim-output-").FullName;
            try
            {
                var report = Path.Combine(directory, "report.md");
                File.WriteAllText(report, "old");

                OutputFile.WriteAllText(report, "new");

                File.ReadAllText(report).Should().Be("new");
                Directory.GetFiles(directory).Should().ContainSingle("the temporary file is moved into place");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        // A symbolic link where allowed, otherwise a hard link, which Windows creates without extra privileges.
        private static bool TryLink(string link, string target)
        {
            try
            {
                File.CreateSymbolicLink(link, target);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }

            return OperatingSystem.IsWindows() && CreateHardLink(link, target, IntPtr.Zero);
        }

        // A directory symbolic link where allowed, otherwise a junction, which Windows creates without extra privileges.
        private static bool TryAliasDirectory(string alias, string target)
        {
            try
            {
                Directory.CreateSymbolicLink(alias, target);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }

            if (!OperatingSystem.IsWindows())
                return false;

            using var junction = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                ArgumentList = { "/c", "mklink", "/J", alias, target },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            junction!.WaitForExit();
            return junction.ExitCode == 0 && Directory.Exists(alias);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);
    }
}
