using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    public void PlayerNameOverrides_PreserveTrueNamesForDMObservers()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var enterMethod = ExtractMethod(source, "public static void ApplyNameOverridesOnEnter()");
        var playerMethod = ExtractMethod(source, "private static void ApplyNameOverridesForPlayer(uint player)");
        var dmObserverMethod = ExtractMethod(source, "private static void ApplyNameOverridesForDMObserver(uint dm)");
        var trueNameMethod = ExtractMethod(source, "private static void ApplyTrueNameOverride(uint observer, uint target)");

        enterMethod.Should().Contain("if (GetIsDM(player))");
        enterMethod.Should().Contain("ApplyNameOverridesForDMObserver(player);");
        enterMethod.Should().Contain("DelayCommand(1.0f, () => ApplyNameOverridesForDMObserver(player));");

        playerMethod.Should().Contain("if (GetIsDM(otherPlayer))");
        playerMethod.Should().Contain("ApplyTrueNameOverride(otherPlayer, player);");
        playerMethod.Should().NotContain("!GetIsPC(otherPlayer) || GetIsDM(otherPlayer)");

        dmObserverMethod.Should().Contain("ApplyTrueNameOverride(dm, player);");
        trueNameMethod.Should().Contain("RenamePlugin.SetPCNameOverride(target, GetName(target), string.Empty, string.Empty, PlayerNameOverrideType.Default, observer);");
    }

    [Test]
    public void PlayerNameOverrides_SkipRedundantSelfIteration()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var playerMethod = ExtractMethod(source, "private static void ApplyNameOverridesForPlayer(uint player)");

        playerMethod.Should().MatchRegex(@"if\s*\(\s*otherPlayer\s*==\s*player\s*\)\s*continue;");
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
    public void KnownNameStorage_CachesObserverRecordsDuringScriptContext()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var staticConstructor = ExtractMethod(source, "static PlayerName()");
        var findMethod = ExtractMethod(source, "private static PlayerKnownName FindKnownNames(string observerId)");
        var getMethod = ExtractMethod(source, "private static PlayerKnownName GetKnownNames(uint observer, bool createIfMissing)");

        source.Should().Contain("private static readonly Dictionary<string, PlayerKnownName> KnownNamesByObserverId");
        staticConstructor.Should().Contain("ServerManager.OnScriptContextEnd += KnownNamesByObserverId.Clear;");
        findMethod.Should().Contain("KnownNamesByObserverId.TryGetValue(observerId, out var dbKnownNames)");
        findMethod.Should().Contain("DB.Search(new DBQuery<PlayerKnownName>()");
        findMethod.Should().Contain("KnownNamesByObserverId[observerId] = dbKnownNames;");
        getMethod.Should().Contain("KnownNamesByObserverId[observerId] = dbKnownNames;");
    }

    [Test]
    public void KnownNameStorage_ValidatesTargetsBeforePersisting()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var setMethod = ExtractMethod(source, "public static void SetKnownName(uint observer, uint target, string name)");
        var assignmentValidationMethod = ExtractMethod(source, "public static string ValidateKnownNameAssignment(uint observer, uint target, string name)");
        var validationMethod = ExtractMethod(source, "private static string ValidateKnownNameTarget(uint observer, uint target)");

        var validationIndex = setMethod.IndexOf("ValidateKnownNameAssignment(observer, target, sanitizedName);", StringComparison.Ordinal);
        var targetIdIndex = setMethod.IndexOf("var targetId = GetObjectUUID(target);", StringComparison.Ordinal);

        validationIndex.Should().BeGreaterThanOrEqualTo(0);
        targetIdIndex.Should().BeGreaterThanOrEqualTo(0);
        validationIndex.Should().BeLessThan(targetIdIndex);

        assignmentValidationMethod.Should().Contain("var targetValidationError = ValidateKnownNameTarget(observer, target);");
        validationMethod.Should().Contain("!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target)");
        validationMethod.Should().Contain("target == observer");
        assignmentValidationMethod.Should().Contain("string.IsNullOrWhiteSpace(targetId)");
    }

    [Test]
    public void KnownNameStorage_RejectsDuplicateAliasesPerObserver()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var setMethod = ExtractMethod(source, "public static void SetKnownName(uint observer, uint target, string name)");
        var assignmentValidationMethod = ExtractMethod(source, "public static string ValidateKnownNameAssignment(uint observer, uint target, string name)");
        var uniquenessMethod = ExtractMethod(source, "private static string ValidateKnownNameIsUnique(");

        setMethod.Should().Contain("ValidateKnownNameAssignment(observer, target, sanitizedName);");
        assignmentValidationMethod.Should().Contain("return ValidateKnownNameIsUnique(dbKnownNames, targetId, sanitizedName);");
        uniquenessMethod.Should().Contain("entry.Key != targetId");
        uniquenessMethod.Should().Contain("StringComparison.OrdinalIgnoreCase");
        uniquenessMethod.Should().Contain("You already use that name for another character.");
    }

    [Test]
    public void PlayerTells_BlockAmbiguousDisplayNames()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var tellMethod = ExtractMethod(source, "public static void PreventAmbiguousTellTargets()");
        var countMethod = ExtractMethod(source, "private static int CountDisplayNameMatches(uint observer, string displayName)");

        tellMethod.Should().Contain("ChatChannel.PlayerTell");
        tellMethod.Should().Contain("CountDisplayNameMatches(sender, displayName) <= 1");
        tellMethod.Should().Contain("ChatPlugin.SkipMessage();");
        tellMethod.Should().Contain("Use /name on the intended player before sending tells.");

        countMethod.Should().Contain("GetDisplayName(observer, player)");
        countMethod.Should().Contain("StringComparison.OrdinalIgnoreCase");
        countMethod.Should().Contain("player == observer");
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

        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var method = root
            .DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>()
            .FirstOrDefault(node => node.SpanStart <= start && node.Span.End > start);

        method.Should().NotBeNull($"method '{signature}' should be parsed by Roslyn");
        return method!.ToFullString();
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
