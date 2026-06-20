using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerNameRecognitionTests
{
    [Test]
    public void PlayerNameOverrides_ObfuscateCommunityNameOnEnter()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var method = ExtractMethod(source, "private static void ApplyNameOverridesForPlayer(uint player)");

        method.Should().Contain("PlayerNameOverrideType.Obfuscate");
    }

    [Test]
    public void PlayerNameOverrides_ApplyImmediatelyOnEnterBeforeDelayedRetry()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var method = ExtractMethod(source, "public static void ApplyNameOverridesOnEnter()");

        var immediateCallIndex = method.IndexOf("ApplyNameOverridesForPlayer(player);", StringComparison.Ordinal);
        var delayedRetryIndex = method.IndexOf("DelayCommand(1.0f, () => ApplyNameOverridesForPlayer(player));", StringComparison.Ordinal);

        immediateCallIndex.Should().BeGreaterThanOrEqualTo(0);
        delayedRetryIndex.Should().BeGreaterThanOrEqualTo(0);
        immediateCallIndex.Should().BeLessThan(delayedRetryIndex);
    }

    [Test]
    public void KnownNameStorage_UsesIndexedObserverFieldAndGeneratedEntityId()
    {
        var root = FindRepositoryRoot();
        var serviceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var entitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Entity",
            "PlayerKnownName.cs"));
        var constructor = ExtractMethod(entitySource, "public PlayerKnownName(string observerPlayerId)");
        var findMethod = ExtractMethod(serviceSource, "private static PlayerKnownName FindKnownNames(string observerId)");

        entitySource.Should().Contain("[Indexed]");
        entitySource.Should().Contain("public string ObserverPlayerId");
        constructor.Should().Contain("ObserverPlayerId = observerPlayerId;");
        constructor.Should().NotContain("            Id = observerPlayerId;");
        constructor.Should().NotContain("            Id = id;");
        findMethod.Should().Contain("DB.Search(new DBQuery<PlayerKnownName>()");
        findMethod.Should().Contain("AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observerId, false)");
        serviceSource.Should().NotContain("BuildKnownNameId");
        serviceSource.Should().NotContain("private const string KnownNameIdPrefix");
    }

    [Test]
    public void KnownNameStorage_DoesNotRunLegacyKeyMigration()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));

        source.Should().NotContain("MigrateLegacyKnownNames");
        source.Should().NotContain("LegacyKnownNameIdPrefix");
        source.Should().NotContain("LegacyScopedKnownNameIdPrefix");
        source.Should().NotContain("DB.GetRawJson<PlayerKnownName>");
    }

    [Test]
    public void UnknownNames_UseGrayColorTokens()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));

        var coloredDisplayMethod = ExtractMethod(source, "public static string GetColoredDisplayName(uint observer, uint target)");
        coloredDisplayMethod.Should().Contain("ColorToken.Gray(displayName)");

        var nameOverrideMethod = ExtractMethod(source, "private static void ApplyNameOverride(uint observer, uint target)");
        nameOverrideMethod.Should().Contain("UnknownNamePrefix");
        nameOverrideMethod.Should().Contain("UnknownNameSuffix");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var brace = source.IndexOf('{', start);
        brace.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = brace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(start, index - start + 1);
        }

        throw new InvalidOperationException($"Could not extract method: {signature}");
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
