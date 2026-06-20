using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PropertyNameMigrationTests
{
    [Test]
    public void RemoveOwnerNamesMigration_RunsAfterPropertyLayoutCacheAndDoesNotMaskLayoutLookupFailures()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_35_RemoveOwnerNamesFromDefaultPropertyNames.cs"));

        source.Should().Contain("public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;");
        source.Should().NotContain("public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;");

        var getLayoutName = ExtractMethod(source, "private static string GetLayoutName(PropertyLayoutType layout)");
        getLayoutName.Should().Contain("return Property.GetLayoutByType(layout).Name;");
        getLayoutName.Should().NotContain("catch");
    }

    [Test]
    public void RemoveOwnerNamesMigration_MatchesDefaultSuffixWithoutCurrentOwnerName()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_35_RemoveOwnerNamesFromDefaultPropertyNames.cs"));
        var migration = ExtractMethod(source, "public void Migrate()");
        var suffixMatcher = ExtractMethod(source, "private static bool HasOwnerPrefixedDefaultName(string propertyName, string oldDefaultName)");

        migration.Should().NotContain("DB.Get<Player>(property.OwnerPlayerId)");
        migration.Should().NotContain("owner.Name");
        migration.Should().Contain("!HasOwnerPrefixedDefaultName(property.CustomName, oldDefaultName)");

        suffixMatcher.Should().Contain("\"'s {oldDefaultName}\"");
        suffixMatcher.Should().Contain("propertyName.EndsWith(defaultSuffix, StringComparison.Ordinal)");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var methodIndex = source.IndexOf(signature, StringComparison.Ordinal);
        methodIndex.Should().BeGreaterThanOrEqualTo(0, $"method '{signature}' should exist");

        var braceIndex = source.IndexOf('{', methodIndex);
        braceIndex.Should().BeGreaterThanOrEqualTo(0, $"method '{signature}' should have a body");

        var depth = 0;
        for (var index = braceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(methodIndex, index - methodIndex + 1);
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
