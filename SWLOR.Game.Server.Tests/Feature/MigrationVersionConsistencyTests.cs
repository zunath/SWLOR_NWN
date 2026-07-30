using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Guards the numbered-migration naming convention: every migration file whose
/// name carries an _N_ prefix must declare that same N in its Version property,
/// and no two migrations of the same kind may share a version. The version is
/// duplicated between filename and code by convention; this test keeps the two
/// from drifting.
///
/// Unnumbered files in the migration folders (e.g. StoredItemDataMigration.cs)
/// are deliberately exempt: they are shared helper routines invoked from
/// numbered migrations, not IServerMigration/IPlayerMigration implementations.
/// </summary>
[TestFixture]
public class MigrationVersionConsistencyTests
{
    private static readonly Regex NumberedFilePattern = new(@"^_(\d+)_.+\.cs$", RegexOptions.Compiled);
    private static readonly Regex VersionPattern = new(@"int\s+Version\s*=>\s*(\d+)\s*;", RegexOptions.Compiled);

    private static IEnumerable<FileInfo> GetNumberedMigrationFiles(string subfolder)
    {
        var root = RepoPaths.FindRepositoryRoot();
        var folder = new DirectoryInfo(Path.Combine(root.FullName,
            "SWLOR.Game.Server", "Feature", "MigrationDefinition", subfolder));
        folder.Exists.Should().BeTrue($"migration folder {folder.FullName} should exist");

        return folder.GetFiles("*.cs").Where(f => NumberedFilePattern.IsMatch(f.Name));
    }

    [Test]
    [TestCase("ServerMigration")]
    [TestCase("PlayerMigration")]
    public void NumberedMigrationFilePrefixesMatchTheirVersionProperty(string subfolder)
    {
        var files = GetNumberedMigrationFiles(subfolder).ToList();
        files.Should().NotBeEmpty();

        foreach (var file in files)
        {
            var filePrefix = int.Parse(NumberedFilePattern.Match(file.Name).Groups[1].Value);
            var source = File.ReadAllText(file.FullName);

            var versionMatch = VersionPattern.Match(source);
            versionMatch.Success.Should().BeTrue(
                $"{subfolder}/{file.Name} is _N_-numbered and must declare 'int Version => N;'");

            var declaredVersion = int.Parse(versionMatch.Groups[1].Value);
            declaredVersion.Should().Be(filePrefix,
                $"{subfolder}/{file.Name}: the _N_ filename prefix and the Version property must agree");

            // The class name should carry the same prefix as the file.
            source.Should().MatchRegex($@"class\s+_{filePrefix}_",
                $"{subfolder}/{file.Name}: the class name should start with _{filePrefix}_ to match the file");
        }
    }

    [Test]
    [TestCase("ServerMigration")]
    [TestCase("PlayerMigration")]
    public void MigrationVersionsAreUniquePerKind(string subfolder)
    {
        var versions = GetNumberedMigrationFiles(subfolder)
            .Select(f => int.Parse(NumberedFilePattern.Match(f.Name).Groups[1].Value))
            .ToList();

        versions.Should().OnlyHaveUniqueItems(
            $"two {subfolder} migrations with the same version would race at boot");
    }
}
