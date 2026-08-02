using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards the toolset's dependency direction and the removal of its retired external format
    /// dependency.
    /// </summary>
    [TestFixture]
    public class ToolsetLicenseBoundaryTests
    {
        private static readonly string[] ToolsetProjects =
        {
            "SWLOR.Toolset",
            "SWLOR.Toolset.Domain",
            "SWLOR.Toolset.Tests"
        };

        private static readonly HashSet<string> ApprovedToolsetReferences = new(StringComparer.Ordinal)
        {
            "SWLOR.ConversationMigrator -> SWLOR.Toolset.Domain"
        };

        private static readonly string[] ExecutableSourceRoots =
        {
            "SWLOR.Toolset",
            "SWLOR.Toolset.Domain",
            "SWLOR.Toolset.Tests",
            Path.Combine("tools", "SWLOR.ConversationMigrator"),
            "SWLOR.NWN.Formats",
            "SWLOR.NWN.Formats.Tests",
            "SWLOR.NWN.Formats.Corpus.Tests"
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

        private static IEnumerable<string> FirstPartyProjects() =>
            Directory.EnumerateFiles(RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !HasPathSegment(path, "External"))
                .Where(path => !HasPathSegment(path, "bin"))
                .Where(path => !HasPathSegment(path, "obj"))
                // Sibling git worktrees live under .claude/worktrees and are whole second copies of
                // this repository at other commits. Scanning them asks this question of code that is
                // not the tree under test: a worktree still on a pre-Radoub-removal branch reported
                // "SWLOR.Toolset.Domain -> Radoub.Formats" against a checkout whose own projects
                // reference Radoub nowhere.
                .Where(path => !HasPathSegment(path, ".claude"));

        private static IEnumerable<string> FirstPartyExecutableSources()
        {
            foreach (var sourceRoot in ExecutableSourceRoots)
            {
                var root = Path.Combine(RepositoryRoot, sourceRoot);
                foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (HasPathSegment(path, "bin") || HasPathSegment(path, "obj"))
                        continue;

                    var extension = Path.GetExtension(path);
                    if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                        extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                        yield return path;
                }
            }
        }

        private static bool HasPathSegment(string path, string segment) =>
            path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains(segment, StringComparer.OrdinalIgnoreCase);

        private static string ProjectName(string path) => Path.GetFileNameWithoutExtension(path);

        [Test]
        public void OnlyApprovedApplicationLayersReferenceTheToolset()
        {
            var violations = new List<string>();

            foreach (var project in FirstPartyProjects())
            {
                if (ToolsetProjects.Contains(ProjectName(project)))
                    continue;

                foreach (Match reference in Regex.Matches(
                             File.ReadAllText(project),
                             @"<ProjectReference\s+Include=""([^""]+)""",
                             RegexOptions.IgnoreCase))
                {
                    var referenced = ProjectName(reference.Groups[1].Value);
                    if (!ToolsetProjects.Contains(referenced))
                        continue;

                    var dependency = $"{ProjectName(project)} -> {referenced}";
                    if (!ApprovedToolsetReferences.Contains(dependency))
                        violations.Add(dependency);
                }
            }

            violations.Should().BeEmpty(
                "only explicitly reviewed outer application layers may consume the headless toolset domain");
        }

        [Test]
        public void TheToolsetOnlyDependsOnItselfAndApprovedSharedProjects()
        {
            var allowed = new HashSet<string>(ToolsetProjects)
            {
                "SWLOR.Game.Server",
                "SWLOR.NWN.Formats"
            };
            var violations = new List<string>();

            foreach (var project in FirstPartyProjects().Where(p => ToolsetProjects.Contains(ProjectName(p))))
            {
                foreach (Match reference in Regex.Matches(
                             File.ReadAllText(project),
                             @"<ProjectReference\s+Include=""([^""]+)""",
                             RegexOptions.IgnoreCase))
                {
                    var referenced = ProjectName(reference.Groups[1].Value);
                    if (!allowed.Contains(referenced))
                        violations.Add($"{ProjectName(project)} -> {referenced}");
                }
            }

            violations.Should().BeEmpty(
                "new toolset project dependencies need an explicit architecture and license review");
        }

        [Test]
        public void FirstPartyExecutableSourcesHaveNoRetiredFormatDependency()
        {
            var dependencyName = string.Concat("Ra", "doub");
            var importPattern = new Regex(
                $@"^\s*(?:global\s+)?using\s+{Regex.Escape(dependencyName)}\.",
                RegexOptions.Multiline);
            var qualifiedTypePattern = new Regex(
                $@"\b{Regex.Escape(dependencyName)}\.[A-Za-z_]",
                RegexOptions.Multiline);
            var referencePattern = new Regex(
                @"<(?:ProjectReference|PackageReference|Reference)\s+Include=""([^""]+)""",
                RegexOptions.IgnoreCase);
            var violations = new List<string>();

            foreach (var path in FirstPartyExecutableSources())
            {
                var source = File.ReadAllText(path);
                if (Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Match reference in referencePattern.Matches(source))
                    {
                        if (reference.Groups[1].Value.Contains(
                                dependencyName,
                                StringComparison.OrdinalIgnoreCase))
                            violations.Add(Path.GetRelativePath(RepositoryRoot, path));
                    }

                    continue;
                }

                var uncommented = Regex.Replace(
                    source,
                    @"/\*.*?\*/|//[^\r\n]*",
                    string.Empty,
                    RegexOptions.Singleline);
                if (importPattern.IsMatch(uncommented) || qualifiedTypePattern.IsMatch(uncommented))
                    violations.Add(Path.GetRelativePath(RepositoryRoot, path));
            }

            violations.Should().BeEmpty(
                "the retired external format dependency must not return to executable first-party code");
        }

        [Test]
        public void BuiltFirstPartyAssembliesHaveNoRetiredAssemblyReference()
        {
            var dependencyName = string.Concat("Ra", "doub");
            var assemblies = new[]
            {
                typeof(SWLOR.Toolset.App).Assembly,
                typeof(SWLOR.Toolset.Domain.Workspace.ModuleWorkspace).Assembly,
                typeof(SWLOR.NWN.Formats.NwnFormatException).Assembly
            };

            var violations = assemblies
                .SelectMany(assembly => assembly.GetReferencedAssemblies()
                    .Where(reference => reference.Name?.Contains(
                        dependencyName,
                        StringComparison.OrdinalIgnoreCase) == true)
                    .Select(reference => $"{assembly.GetName().Name} -> {reference.Name}"))
                .ToArray();

            violations.Should().BeEmpty(
                "the compiled toolset, domain, and formats assemblies must not reference the retired dependency");
        }

        [Test]
        public void LicensedCorpusTestsAreInTheSolutionBehindTheAvailabilityGate()
        {
            // Both halves matter: the solution entry makes a full `dotnet test` discover the
            // corpus sweeps on machines that have the licensed assets, and the availability gate
            // is what keeps asset-less machines green (skipping, or failing loudly under
            // SWLOR_REQUIRE_LICENSED_CORPUS=1 so evidence runs cannot silently skip).
            var solution = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server.sln"));
            solution.Should().Contain(
                @"SWLOR.NWN.Formats.Corpus.Tests\SWLOR.NWN.Formats.Corpus.Tests.csproj",
                "a full solution test run must discover the corpus verification suite");

            var gate = Path.Combine(
                RepositoryRoot, "SWLOR.NWN.Formats.Corpus.Tests", "CorpusAvailabilityGate.cs");
            File.Exists(gate).Should().BeTrue(
                "the availability gate is what makes solution-level inclusion safe without the licensed assets");
        }

        [Test]
        public void TheRepositoryDeclaresItsMitLicense()
        {
            var license = Path.Combine(RepositoryRoot, "LICENSE.txt");

            File.Exists(license).Should().BeTrue("the root license grants use of first-party code");
            File.ReadAllText(license).Should().Contain("MIT License");
        }
    }
}
