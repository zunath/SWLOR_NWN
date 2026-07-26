using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards the license boundary described in <c>SWLOR.Toolset/LICENSE-NOTICE.md</c>.
    /// </summary>
    /// <remarks>
    /// SWLOR is MIT; the toolset links Radoub, which is GPL-3.0, so a built toolset binary is a
    /// combined GPL work. That stays contained only while dependencies flow one way -
    /// <c>SWLOR.Toolset → SWLOR.Toolset.Domain → { Radoub.Formats, SWLOR.Game.Server }</c> - and
    /// nothing MIT ever references back into the toolset. A rule that lives only in a markdown file
    /// is one careless "add project reference" away from quietly pulling the game server into the
    /// GPL, and nothing about the build would complain. Hence a test.
    /// </remarks>
    [TestFixture]
    public class ToolsetLicenseBoundaryTests
    {
        /// <summary>The projects that are allowed to reference the toolset: the toolset itself.</summary>
        private static readonly string[] ToolsetProjects =
        {
            "SWLOR.Toolset",
            "SWLOR.Toolset.Domain",
            "SWLOR.Toolset.Tests"
        };

        private static string RepositoryRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server.sln")))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root from the test context.");
            }
        }

        /// <summary>Every first-party project file. Radoub's own projects are not ours to police.</summary>
        private static IEnumerable<string> FirstPartyProjects() =>
            Directory.EnumerateFiles(RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}External{Path.DirectorySeparatorChar}"));

        private static string ProjectName(string path) => Path.GetFileNameWithoutExtension(path);

        [Test]
        public void NoMitProjectReferencesTheToolset()
        {
            var violations = new List<string>();

            foreach (var project in FirstPartyProjects())
            {
                if (ToolsetProjects.Contains(ProjectName(project)))
                    continue;

                var references = Regex.Matches(
                    File.ReadAllText(project),
                    @"<ProjectReference\s+Include=""([^""]+)""",
                    RegexOptions.IgnoreCase);

                foreach (Match reference in references)
                {
                    var referenced = ProjectName(reference.Groups[1].Value);
                    if (ToolsetProjects.Contains(referenced))
                        violations.Add($"{ProjectName(project)} -> {referenced}");
                }
            }

            violations.Should().BeEmpty(
                "the toolset is a combined GPL-3.0 work (it links Radoub); an MIT project referencing " +
                "it would pull that project into the GPL. See SWLOR.Toolset/LICENSE-NOTICE.md.");
        }

        [Test]
        public void TheToolsetOnlyDependsOnItselfAndTheAllowedProjects()
        {
            // The other direction: the toolset consuming MIT code is fine and expected, but it must not
            // grow a dependency on something that would drag more of the solution across the boundary.
            var allowed = new HashSet<string>(ToolsetProjects) { "SWLOR.Game.Server", "Radoub.Formats", "Radoub.UI" };
            var violations = new List<string>();

            foreach (var project in FirstPartyProjects().Where(p => ToolsetProjects.Contains(ProjectName(p))))
            {
                var references = Regex.Matches(
                    File.ReadAllText(project),
                    @"<ProjectReference\s+Include=""([^""]+)""",
                    RegexOptions.IgnoreCase);

                foreach (Match reference in references)
                {
                    var referenced = ProjectName(reference.Groups[1].Value);
                    if (!allowed.Contains(referenced))
                        violations.Add($"{ProjectName(project)} -> {referenced}");
                }
            }

            violations.Should().BeEmpty(
                "a new toolset dependency needs a deliberate license decision, not an implicit one");
        }

        [Test]
        public void TheRepositoryStillDeclaresItsMitLicense()
        {
            // Deleted as collateral in 2020's "Convert from master to dotnet core 3.1" and missing for
            // years afterwards, which left every fork without a grant and would have made a GPL source
            // offer impossible to satisfy.
            var license = Path.Combine(RepositoryRoot, "LICENSE.txt");

            File.Exists(license).Should().BeTrue("the root MIT license is what every other project relies on");
            File.ReadAllText(license).Should().Contain("MIT License");
        }

        [Test]
        public void TheToolsetShipsTheGplTextForTheCombinedWork()
        {
            var gpl = Path.Combine(RepositoryRoot, "SWLOR.Toolset", "LICENSE.GPL-3.0");

            File.Exists(gpl).Should().BeTrue("a conveyed toolset binary has to carry the license it is under");
            File.ReadAllText(gpl).Should().Contain("GNU GENERAL PUBLIC LICENSE");
        }

        [Test]
        public void EveryRadoubDerivedFileSaysItIsGpl()
        {
            // These are derivative works of Radoub, so they are GPL no matter how the rest of the
            // toolset's own source is licensed - and unlike a project reference, that survives Radoub
            // being dropped. Without a header the next reader has no way to know.
            var derived = new[]
            {
                Path.Combine("SWLOR.Toolset.Domain", "Render", "TextureLoader.cs"),
                Path.Combine("SWLOR.Toolset.Domain", "Render", "MdlMeshBuilder.cs"),
                Path.Combine("SWLOR.Toolset.Domain", "Render", "MdlGeometryFlattener.cs")
            };

            foreach (var relative in derived)
            {
                var path = Path.Combine(RepositoryRoot, relative);
                File.Exists(path).Should().BeTrue($"{relative} is listed in LICENSE-NOTICE.md as Radoub-derived");
                File.ReadAllText(path).Should().Contain(
                    "SPDX-License-Identifier: GPL-3.0",
                    $"{relative} is a derivative work of GPL-3.0 code and has to say so");
            }
        }
    }
}
