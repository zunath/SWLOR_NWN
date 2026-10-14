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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);
    }
}
