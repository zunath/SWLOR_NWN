using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerNameRecognitionTests
{
    [Test]
    public void PlayerNameOverrides_ObfuscateCommunityNameWhenEnabled()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var method = ExtractMethod(source, "private static void ApplyNameOverridesForPlayer(uint player)");

        method.Should().Contain("var shouldScrambleAccountName = Disguise.ShouldScrambleAccountName(player);");
        method.Should().Contain("if (!shouldScrambleAccountName)");
        method.Should().Contain("RenamePlugin.ClearPCNameOverride(player);");
        method.Should().Contain("if (shouldScrambleAccountName)");
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
        trueNameMethod.Should().Contain("BuildStaffDisplayName(target)");
        trueNameMethod.Should().NotContain("PlayerPlugin.SetCreatureNameOverride");
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

        var validationIndex = setMethod.IndexOf("ValidateKnownNameAssignment(observer, target, name);", StringComparison.Ordinal);
        var targetIdIndex = setMethod.IndexOf("var targetId = Disguise.GetIdentityKey(target);", StringComparison.Ordinal);

        validationIndex.Should().BeGreaterThanOrEqualTo(0);
        targetIdIndex.Should().BeGreaterThanOrEqualTo(0);
        validationIndex.Should().BeLessThan(targetIdIndex);

        assignmentValidationMethod.Should().Contain("var targetValidationError = ValidateKnownNameTarget(observer, target);");
        validationMethod.Should().Contain("!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target)");
        validationMethod.Should().Contain("target == observer");
        assignmentValidationMethod.Should().Contain("string.IsNullOrWhiteSpace(targetId)");
    }

    [Test]
    public void KnownNameStorage_RejectsColorTokensBeforeSanitizing()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var descriptorSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerDescriptor.cs"));
        var inputValidationMethod = ExtractMethod(source, "public static string ValidateKnownNameInput(string name)");
        var assignmentValidationMethod = ExtractMethod(source, "public static string ValidateKnownNameAssignment(uint observer, uint target, string name)");
        var setMethod = ExtractMethod(source, "public static void SetKnownName(uint observer, uint target, string name)");
        var unknownDisplayMethod = ExtractMethod(descriptorSource, "public static void SetUnknownDisplayName(uint player, string name)");

        source.Should().Contain("public const int MaxKnownNameLength = 64;");
        source.Should().Contain("if (name.Length > MaxKnownNameLength)");
        source.Should().Contain("Names may be no longer than {MaxKnownNameLength} characters.");
        inputValidationMethod.Should().Contain("ContainsColorToken(name)");
        inputValidationMethod.Should().Contain("\"Names may not contain color codes.\"");
        inputValidationMethod.Should().Contain("ValidateKnownName(SanitizeKnownName(name))");
        var colorTokenValidationIndex = inputValidationMethod.IndexOf("ContainsColorToken(name)", StringComparison.Ordinal);
        var sanitizeValidationIndex = inputValidationMethod.IndexOf("SanitizeKnownName(name)", StringComparison.Ordinal);
        colorTokenValidationIndex.Should().BeGreaterThanOrEqualTo(0);
        sanitizeValidationIndex.Should().BeGreaterThanOrEqualTo(0);
        colorTokenValidationIndex.Should().BeLessThan(sanitizeValidationIndex);
        source.Should().Contain("private static bool ContainsColorToken(string name)");
        source.Should().Contain("UtilPlugin.StripColors(name)");

        assignmentValidationMethod.Should().Contain("ValidateKnownNameInput(name)");
        setMethod.Should().Contain("ValidateKnownNameAssignment(observer, target, name);");
        unknownDisplayMethod.Should().Contain("PlayerName.ValidateKnownNameInput(name)");
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

        setMethod.Should().Contain("ValidateKnownNameAssignment(observer, target, name);");
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
        tellMethod.Should().Contain("ApplyChatNameOverride(target, sender);");
        tellMethod.Should().Contain("ApplyChatNameOverride(sender, target);");
        tellMethod.Should().Contain("DelayCommand(0.1f, () => RestoreNameOverride(target, sender));");
        tellMethod.Should().Contain("DelayCommand(0.1f, () => RestoreNameOverride(sender, target));");
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
        var descriptorSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerDescriptor.cs"));
        var playerSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Entity",
            "Player.cs"));

        var coloredDisplayMethod = ExtractMethod(source, "public static string GetColoredDisplayName(uint observer, uint target)");
        var coloredChatDisplayMethod = ExtractMethod(source, "public static string GetColoredChatDisplayName(uint observer, uint target)");
        var playerIdDisplayMethod = ExtractMethod(source, "public static string GetDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName)");
        coloredDisplayMethod.Should().Contain("ColorToken.Gray(displayName)");
        coloredDisplayMethod.Should().Contain("ShouldShowDescriptorForNamedPlayers(observer)");
        coloredDisplayMethod.Should().Contain("BuildColoredDisplayNameWithDescriptor(knownName, Disguise.GetDisplayDescriptor(target))");
        coloredDisplayMethod.Should().Contain("ColorToken.GetPCColor(knownName)");
        coloredChatDisplayMethod.Should().Contain("ResolveChatDisplayName(observer, target, out var isUnknown)");
        coloredChatDisplayMethod.Should().NotContain("ShouldShowDescriptorForNamedPlayers(observer)");
        coloredChatDisplayMethod.Should().NotContain("BuildColoredDisplayNameWithDescriptor");
        playerIdDisplayMethod.Should().Contain("ShouldShowDescriptorForNamedPlayers(observer)");
        playerIdDisplayMethod.Should().Contain("BuildDisplayNameWithDescriptor(fallbackDisplayName, PlayerDescriptor.GetUnknownDisplayNameByPlayerId(targetPlayerId))");
        playerIdDisplayMethod.Should().Contain(": knownName");

        playerSource.Should().Contain("public string UnknownDisplayName { get; set; }");
        playerSource.Should().Contain("public bool? ShowDescriptorsForNamedPlayers { get; set; }");
        playerSource.Should().Contain("public bool? ShowOwnDescriptor { get; set; }");
        playerSource.Should().Contain("public bool? ScrambleAccountName { get; set; }");
        playerSource.Should().Contain("ShowDescriptorsForNamedPlayers = true;");
        playerSource.Should().Contain("ShowOwnDescriptor = true;");
        playerSource.Should().Contain("ScrambleAccountName = true;");
        descriptorSource.Should().Contain("public static void SetUnknownDisplayName(uint player, string name)");
        source.Should().Contain("Disguise.GetDisplayDescriptor(target)");
        source.Should().Contain("public static void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage)");
        source.Should().Contain("private static string BuildDisplayNameWithDescriptor(string primaryName, string descriptor)");
        source.Should().Contain("private static string BuildColoredDisplayNameWithDescriptor(string primaryName, string descriptor)");
        source.Should().Contain("private static string ResolveChatDisplayName(uint observer, uint target, out bool isUnknown)");
        source.Should().Contain("private static bool ShouldShowDescriptorForNamedPlayers(uint observer)");
        source.Should().Contain("ShowDescriptorsForNamedPlayersByObserverId");
        source.Should().NotContain("SendWithChatNameOverride");
        source.Should().NotContain("SetCreatureNameOverride");
        source.Should().NotContain("BuildCreatureNameOverride");
        ExtractMethod(descriptorSource, "public static void SetUnknownDisplayName(uint player, string name)")
            .Should().Contain("dbPlayer.UnknownDisplayName = sanitizedName;");

        var nameOverrideMethod = ExtractMethod(source, "private static void ApplyNameOverride(uint observer, uint target)");
        nameOverrideMethod.Should().Contain("UnknownNamePrefix");
        nameOverrideMethod.Should().Contain("UnknownNameSuffix");
        nameOverrideMethod.Should().Contain("RenamePlugin.SetPCNameOverride(target, displayName, prefix, suffix, PlayerNameOverrideType.Default, observer);");
        nameOverrideMethod.Should().NotContain("SetCreatureNameOverride");

        var applyPlayerMethod = ExtractMethod(source, "private static void ApplyNameOverridesForPlayer(uint player)");
        applyPlayerMethod.Should().Contain("var ownDisplayName = ShouldShowOwnDescriptor(player)");
        applyPlayerMethod.Should().Contain("BuildDisplayNameWithDescriptor(GetName(player), unknownDisplayName)");
        applyPlayerMethod.Should().Contain("RenamePlugin.SetPCNameOverride(player, ownDisplayName, string.Empty, string.Empty, PlayerNameOverrideType.Default, player);");

        var resolveDisplayMethod = ExtractMethod(source, "private static string ResolveDisplayName(uint observer, uint target, out bool isUnknown)");
        resolveDisplayMethod.Should().Contain("ShouldShowDescriptorForNamedPlayers(observer)");
        resolveDisplayMethod.Should().Contain("BuildDisplayNameWithDescriptor(knownName, Disguise.GetDisplayDescriptor(target))");
        resolveDisplayMethod.Should().Contain(": knownName");

        var resolveChatDisplayMethod = ExtractMethod(source, "private static string ResolveChatDisplayName(uint observer, uint target, out bool isUnknown)");
        resolveChatDisplayMethod.Should().Contain("GetIsDM(observer)");
        resolveChatDisplayMethod.Should().Contain("return BuildStaffDisplayName(target);");
        resolveChatDisplayMethod.Should().Contain("TryGetKnownName(observer, target, out var knownName)");
        resolveChatDisplayMethod.Should().Contain("return knownName;");
        resolveChatDisplayMethod.Should().Contain("return Disguise.GetDisplayDescriptor(target);");
        resolveChatDisplayMethod.Should().NotContain("BuildDisplayNameWithDescriptor");
        resolveChatDisplayMethod.Should().NotContain("ShouldShowDescriptorForNamedPlayers(observer)");

        var staffDisplayMethod = ExtractMethod(source, "private static string BuildStaffDisplayName(uint target)");
        staffDisplayMethod.Should().Contain("GetName(target)");
        staffDisplayMethod.Should().Contain("Disguise.GetDisplayDescriptor(target)");
        staffDisplayMethod.Should().Contain("return $\"{trueName} [{ColorToken.Gray(unknownDisplayName)}]\";");
        staffDisplayMethod.Should().Contain("ColorToken.Gray(unknownDisplayName)");

        var displayWithDescriptorMethod = ExtractMethod(source, "private static string BuildDisplayNameWithDescriptor(string primaryName, string descriptor)");
        displayWithDescriptorMethod.Should().Contain("return $\"{primaryName} [{ColorToken.Gray(descriptor)}]\";");
        displayWithDescriptorMethod.Should().NotContain("\\n");

        var coloredDisplayWithDescriptorMethod = ExtractMethod(source, "private static string BuildColoredDisplayNameWithDescriptor(string primaryName, string descriptor)");
        coloredDisplayWithDescriptorMethod.Should().Contain("return $\"{ColorToken.GetPCColor(primaryName)} [{ColorToken.Gray(descriptor)}]\";");
        coloredDisplayWithDescriptorMethod.Should().NotContain("\\n");
    }

    [Test]
    public void UnknownNames_GenerateStableDescriptorsFromAppearanceAndBaseStats()
    {
        var root = FindRepositoryRoot();
        var playerNameSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerDescriptor.cs"));

        playerNameSource.Should().Contain("public const string UnknownName = PlayerDescriptor.DefaultUnknownDisplayName;");
        playerNameSource.Should().NotContain("private const int GenericDescriptorChancePercent");
        playerNameSource.Should().NotContain("public static string GenerateUnknownDisplayName(Player dbPlayer)");
        source.Should().Contain("private const int GenericDescriptorChancePercent = 25;");
        source.Should().Contain("private const string Appearance2DA = \"appearance\";");
        source.Should().Contain("private const string HumanoidSpeciesName = \"Humanoid\";");
        source.Should().Contain("private static readonly HashSet<AppearanceType> DescriptorSpeciesAppearanceTypes");
        source.Should().Contain("private static readonly Dictionary<AbilityType, string[]> StatDescriptorAdjectives");
        source.Should().Contain("private static readonly string[] GenericDescriptorAdjectives");
        source.Should().NotContain("GeneratedUnknownDisplayName");

        source.Should().Contain("public static bool EnsureUnknownDisplayName(uint player)");
        source.Should().NotContain("public static bool EnsureUnknownDisplayName(Player dbPlayer)");

        var generateMethod = ExtractMethod(source, "public static string GenerateUnknownDisplayName(Player dbPlayer)");
        generateMethod.Should().Contain("ResolveDescriptorAdjective(dbPlayer)");
        generateMethod.Should().Contain("ResolveSpeciesName(dbPlayer?.OriginalAppearanceType ?? AppearanceType.Invalid)");
        generateMethod.Should().Contain("PlayerName.SanitizeKnownName($\"{adjective} {species}\")");

        var abilityMethod = ExtractMethod(source, "private static bool TryResolveDescriptorAbility(Player dbPlayer, string seed, out AbilityType ability)");
        abilityMethod.Should().Contain("dbPlayer.BaseStats.TryGetValue(abilityType, out var value)");
        abilityMethod.Should().Contain("highestAbilities[GetStableIndex(seed, \"descriptor-ability\", highestAbilities.Count)]");

        var speciesMethod = ExtractMethod(source, "private static string ResolveSpeciesName(AppearanceType appearanceType)");
        speciesMethod.Should().Contain("!DescriptorSpeciesAppearanceTypes.Contains(appearanceType)");
        speciesMethod.Should().Contain("Get2DAString(Appearance2DA, AppearanceLabelColumn, (int)appearanceType)");
        speciesMethod.Should().Contain("DynamicAppearanceLabelPrefix");
        speciesMethod.Should().Contain("HumanoidSpeciesName");
    }

    [Test]
    public void UnknownNames_AreGeneratedDuringMigrationAndLoginInitialization()
    {
        var root = FindRepositoryRoot();
        var migrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_22_CombatSystemReplacement.cs"));
        var initializationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PlayerInitialization.cs"));
        var playerNameSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));
        var appearanceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "appearance.2da"));

        migrationSource.Should().Contain("EnsureUnknownDisplayName(dbPlayer);");
        migrationSource.Should().Contain("var hasOriginalAppearanceType = jObject[nameof(Player.OriginalAppearanceType)] != null;");
        migrationSource.Should().Contain("dbPlayer.OriginalAppearanceType = AppearanceType.Invalid;");
        var migrationEnsureMethod = ExtractMethod(migrationSource, "private static void EnsureUnknownDisplayName(Player dbPlayer)");
        migrationEnsureMethod.Should().Contain("PlayerName.SanitizeKnownName(dbPlayer.UnknownDisplayName)");
        migrationEnsureMethod.Should().Contain("PlayerDescriptor.GenerateUnknownDisplayName(dbPlayer)");
        migrationEnsureMethod.Should().Contain("dbPlayer.UnknownDisplayName = generatedDisplayName;");

        var initializationMethod = ExtractMethod(initializationSource, "public static void InitializePlayer()");
        initializationMethod.Should().Contain("if (PlayerDescriptor.EnsureUnknownDisplayName(player))");
        initializationMethod.Should().Contain("PlayerName.RefreshNameOverridesForPlayer(player);");
        var versionGateIndex = initializationMethod.IndexOf("if (dbPlayer.Version >= 1 || dbPlayer.Version == -1)", StringComparison.Ordinal);
        var firstDescriptorEnsureIndex = initializationMethod.IndexOf("if (PlayerDescriptor.EnsureUnknownDisplayName(player))", StringComparison.Ordinal);
        var firstRefreshIndex = initializationMethod.IndexOf("PlayerName.RefreshNameOverridesForPlayer(player);", firstDescriptorEnsureIndex, StringComparison.Ordinal);
        var firstReturnIndex = initializationMethod.IndexOf("return;", versionGateIndex, StringComparison.Ordinal);
        versionGateIndex.Should().BeGreaterThanOrEqualTo(0);
        firstDescriptorEnsureIndex.Should().BeGreaterThan(versionGateIndex);
        firstDescriptorEnsureIndex.Should().BeLessThan(firstReturnIndex);
        firstRefreshIndex.Should().BeLessThan(firstReturnIndex);

        var racialAppearanceMethod = ExtractMethod(initializationSource, "private static void AssignRacialAppearance(uint player, Player dbPlayer)");
        racialAppearanceMethod.Should().Contain("Race.GetDefaultAppearance(GetRacialType(player), GetGender(player))");
        racialAppearanceMethod.Should().Contain("dbPlayer.OriginalAppearanceType = raceAppearance.AppearanceType;");

        playerNameSource.Should().Contain("public static void RefreshNameOverridesForPlayer(uint player)");
        playerNameSource.Should().NotContain("public static bool EnsureUnknownDisplayName(uint player)");
        appearanceSource.Should().Contain("\"(Dynamic) Wookiee\"");
        appearanceSource.Should().NotContain("\"(Dynamic) Wookie\"");
    }

    [Test]
    public void Settings_ControlIdentityNameplateAndAccountPrivacyForPlayersOnly()
    {
        var root = FindRepositoryRoot();
        var definitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "SettingsDefinition.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "SettingsViewModel.cs"));
        var playerNameSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PlayerName.cs"));

        definitionSource.Should().Contain("Identity");
        definitionSource.Should().NotContain("Nameplates");
        definitionSource.Should().Contain("Show My Public Description");
        definitionSource.Should().Contain("BindIsChecked(model => model.ShowOwnDescriptor)");
        definitionSource.Should().Contain("Show Others' Public Descriptions");
        definitionSource.Should().Contain("BindIsChecked(model => model.ShowDescriptorsForNamedPlayers)");
        definitionSource.Should().NotContain(".SetText(\"Account\")");
        definitionSource.Should().Contain("Hide My Account Name");
        definitionSource.Should().Contain("BindIsChecked(model => model.ScrambleAccountName)");

        viewModelSource.Should().Contain("public bool ShowDescriptorsForNamedPlayers");
        viewModelSource.Should().Contain("public bool ShowOwnDescriptor");
        viewModelSource.Should().Contain("public bool ScrambleAccountName");
        viewModelSource.Should().Contain("public bool IsIdentitySelected");
        viewModelSource.Should().Contain("ShowDescriptorsForNamedPlayers = dbPlayer.Settings.ShowDescriptorsForNamedPlayers ?? true;");
        viewModelSource.Should().Contain("ShowOwnDescriptor = dbPlayer.Settings.ShowOwnDescriptor ?? true;");
        viewModelSource.Should().Contain("ScrambleAccountName = dbPlayer.Settings.ScrambleAccountName ?? true;");
        viewModelSource.Should().Contain("dbPlayer.Settings.ShowDescriptorsForNamedPlayers = ShowDescriptorsForNamedPlayers;");
        viewModelSource.Should().Contain("dbPlayer.Settings.ShowOwnDescriptor = ShowOwnDescriptor;");
        viewModelSource.Should().Contain("dbPlayer.Settings.ScrambleAccountName = ScrambleAccountName;");
        viewModelSource.Should().Contain("PlayerName.RefreshNameOverridesForObserver(Player);");
        viewModelSource.Should().Contain("PlayerName.RefreshNameOverridesForPlayer(Player);");

        playerNameSource.Should().Contain("public static void RefreshNameOverridesForObserver(uint observer)");
        playerNameSource.Should().Contain("ShowDescriptorsForNamedPlayersByObserverId.Remove(observerId);");
        playerNameSource.Should().Contain("private static bool ShouldShowOwnDescriptor(uint player)");
        playerNameSource.Should().Contain("Disguise.ShouldScrambleAccountName(player)");
        playerNameSource.Should().Contain("GetIsDM(observer) ||");
        playerNameSource.Should().Contain("GetIsDMPossessed(observer)");
    }

    [Test]
    public void NormalBiographyEditor_CannotOverwriteAnActiveDisguiseBiography()
    {
        var root = FindRepositoryRoot();
        var settingsSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "SettingsViewModel.cs"));
        var descriptionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ChangeDescriptionViewModel.cs"));

        var openEditorMethod = ExtractMethod(settingsSource, "public Action OnClickChangeDescription()");
        openEditorMethod.Should().Contain("Disguise.GetActiveDisguise(Player) != null");
        openEditorMethod.Should().Contain("Deactivate it to edit your normal biography.");
        openEditorMethod.Should().Contain("return;");

        var saveMethod = ExtractMethod(descriptionSource, "public Action OnClickSave()");
        saveMethod.Should().Contain("Disguise.GetActiveDisguise(Player) != null");
        saveMethod.Should().Contain("Edit your active disguise's biography from the Disguises window.");
        saveMethod.Should().Contain("return;");
    }

    [Test]
    public void DisguiseIdentityMutations_AreAuditedWithCanonicalIdentityAndRelevantState()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Disguise.cs"));

        var createMethod = ExtractMethod(source, "public static PlayerDisguise CreateDisguise(uint player)");
        createMethod.Should().Contain("Disguise created: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId}");
        createMethod.Should().Contain("LogGroup.PlayerName");
        createMethod.Should().Contain("GetName(player)");

        var saveMethod = ExtractMethod(source, "public static SaveDisguiseResult SaveDisguise(");
        saveMethod.Should().Contain("var previousPrivateName = disguise.PrivateName;");
        saveMethod.Should().Contain("var previousDescriptor = disguise.Descriptor;");
        saveMethod.Should().Contain("var previousBiography = disguise.Biography ?? string.Empty;");
        saveMethod.Should().Contain("Disguise saved: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId}");
        saveMethod.Should().Contain("PreviousPrivateName={PreviousPrivateName} PrivateName={PrivateName}");
        saveMethod.Should().Contain("PreviousDescriptor={PreviousDescriptor} Descriptor={Descriptor}");
        saveMethod.Should().Contain("PreviousBiographyLength={PreviousBiographyLength} BiographyLength={BiographyLength}");
        saveMethod.Should().Contain("PreviousPortraitInternalId={PreviousPortraitInternalId} PortraitInternalId={PortraitInternalId}");
        saveMethod.Should().Contain("PreviousSoundSetId={PreviousSoundSetId} SoundSetId={SoundSetId}");
        saveMethod.Should().Contain("PreviousScrambleAccountId={PreviousScrambleAccountId} ScrambleAccountId={ScrambleAccountId}");
        saveMethod.Should().Contain("LogGroup.PlayerName");
        saveMethod.Should().Contain("GetName(player)");

        var activateMethod = ExtractMethod(source, "public static ActivateDisguiseResult Activate(uint player, string disguiseId)");
        activateMethod.Should().Contain("var previousDisguiseId = dbPlayer.ActiveDisguiseId;");
        activateMethod.Should().Contain("Disguise activated: PlayerId={PlayerId} PlayerName={PlayerName} PreviousDisguiseId={PreviousDisguiseId} DisguiseId={DisguiseId}");
        activateMethod.Should().Contain("LogGroup.PlayerName");
        activateMethod.Should().Contain("GetName(player)");
        activateMethod.IndexOf("return ActivateDisguiseResult.Success();", StringComparison.Ordinal)
            .Should().BeLessThan(activateMethod.IndexOf("Disguise activated:", StringComparison.Ordinal));

        var deactivateMethod = ExtractMethod(source, "public static bool Deactivate(uint player)");
        deactivateMethod.Should().Contain("var activeDisguiseId = dbPlayer.ActiveDisguiseId;");
        deactivateMethod.Should().Contain("var activeDisguise = GetActiveDisguise(dbPlayer);");
        deactivateMethod.Should().Contain("Disguise deactivated: PlayerId={PlayerId} PlayerName={PlayerName} DisguiseId={DisguiseId}");
        deactivateMethod.Should().Contain("activeDisguise?.PrivateName ?? string.Empty");
        deactivateMethod.Should().Contain("activeDisguise?.Descriptor ?? string.Empty");
        deactivateMethod.Should().Contain("activeDisguise?.PortraitInternalId ?? -1");
        deactivateMethod.Should().Contain("activeDisguise?.SoundSetId ?? -1");
        deactivateMethod.Should().Contain("activeDisguise?.ScrambleAccountId ?? false");
        deactivateMethod.Should().Contain("LogGroup.PlayerName");
        deactivateMethod.Should().Contain("GetName(player)");
    }

    [Test]
    public void Disguises_UseDisguiseIdentityKeysAndHardDeleteRetiredIdentities()
    {
        var root = FindRepositoryRoot();
        var disguiseSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Disguise.cs"));
        var activateResultSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "DisguiseService",
            "ActivateDisguiseResult.cs"));
        var entitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Entity",
            "PlayerDisguise.cs"));
        var playerSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Entity",
            "Player.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "DisguiseViewModel.cs"));
        var cacheSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Cache.cs"));
        var dialogSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition",
            "IdentityBrokerDialog.cs"));
        var characterSheetDefinitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "CharacterSheetDefinition.cs"));
        var characterSheetViewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterSheetViewModel.cs"));
        var dmChatCommandSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "DMChatCommand.cs"));

        disguiseSource.Should().Contain("public const string IdentityKeyPrefix = \"disguise:\";");
        disguiseSource.Should().Contain("BuildIdentityKey(activeDisguise.Id)");
        disguiseSource.Should().Contain("PlayerName.DeleteKnownNameReferences(identityKey)");
        disguiseSource.Should().Contain("DB.Delete<PlayerDisguise>(disguise.Id)");
        disguiseSource.Should().Contain("public const int ActivationDelayMinutes = 30;");
        disguiseSource.Should().Contain("public const int MinimumActivationDelayMinutes = 5;");
        disguiseSource.Should().Contain("public static TimeSpan GetActivationDelay(uint player)");
        disguiseSource.Should().Contain("public static ActivateDisguiseResult Activate");
        disguiseSource.Should().Contain("ValidateActivationDelay(player, playerId)");
        disguiseSource.Should().Contain("GetLatestActivationDate(playerId)");
        disguiseSource.Should().Contain("There is a {delayMinutes}-minute delay between disguise activations.");
        disguiseSource.Should().Contain("Deactivation is available immediately.");
        disguiseSource.Should().Contain("DateLastActivated = DateTime.UtcNow");
        disguiseSource.Should().Contain("public static int ResetActivationCooldowns(uint player)");
        disguiseSource.Should().Contain("disguise.DateLastActivated = null;");
        disguiseSource.Should().Contain("public static bool Unretire");
        disguiseSource.Should().Contain("disguise.DateRetired = null;");
        disguiseSource.Should().Contain("TakeGoldFromCreature(amount, player, true)");
        disguiseSource.Should().Contain("dbPlayer.UnallocatedXP -= amount");
        disguiseSource.Should().Contain("new RPXPRefreshEvent()");
        disguiseSource.Should().Contain("new DisguiseChangedRefreshEvent()");
        dmChatCommandSource.Should().Contain(".Description(\"Resets a player's ability, disguise, and perk refund cooldowns.\")");
        dmChatCommandSource.Should().Contain("AbilityCooldownVisual.ClearAllRecastDelays(target);");
        dmChatCommandSource.Should().Contain("Disguise.ResetActivationCooldowns(target);");

        activateResultSource.Should().Contain("public bool IsSuccessful");
        activateResultSource.Should().Contain("public string ErrorMessage");
        activateResultSource.Should().Contain("public static ActivateDisguiseResult Success()");
        activateResultSource.Should().Contain("public static ActivateDisguiseResult Failure(string errorMessage)");

        entitySource.Should().Contain("[Indexed]");
        entitySource.Should().Contain("public string PlayerId { get; set; }");
        entitySource.Should().Contain("public bool IsRetired { get; set; }");
        entitySource.Should().Contain("public bool ScrambleAccountId { get; set; }");
        entitySource.Should().Contain("Biography = string.Empty;");
        entitySource.Should().Contain("public string Biography { get; set; }");
        playerSource.Should().Contain("public const int DefaultDisguiseSlotLimit = 1;");
        playerSource.Should().Contain("DisguiseSlotLimit = DefaultDisguiseSlotLimit;");
        playerSource.Should().Contain("UndisguisedPortraitResref = string.Empty;");
        playerSource.Should().Contain("public string UndisguisedPortraitResref { get; set; }");
        playerSource.Should().Contain("UndisguisedDescription = string.Empty;");
        playerSource.Should().Contain("public string UndisguisedDescription { get; set; }");
        playerSource.Should().Contain("public bool HasUndisguisedDescriptionSnapshot { get; set; }");

        viewModelSource.Should().Contain("public bool IsAvailableSelected");
        viewModelSource.Should().Contain("public bool IsRetiredSelected");
        viewModelSource.Should().Contain("public bool ShowEmptyState");
        viewModelSource.Should().Contain("public string EmptyStateTitle");
        viewModelSource.Should().Contain("public string EmptyStateText");
        viewModelSource.Should().Contain("public bool ScrambleAccountId");
        viewModelSource.Should().Contain("public string Biography");
        viewModelSource.Should().Contain("public bool ShowUnretireButton");
        viewModelSource.Should().NotContain("public bool ShowDetailActionRow");
        viewModelSource.Should().Contain("public const string ContentPartialElement");
        viewModelSource.Should().Contain("public const string ContentAvailablePartial");
        viewModelSource.Should().Contain("public const string ContentRetiredPartial");
        viewModelSource.Should().Contain("public const string ContentEditPartial");
        viewModelSource.Should().Contain("public const string ContentEmptyPartial");
        disguiseSource.Should().Contain("PrivateName = $\"Disguise #{usedSlots + 1}\"");
        disguiseSource.Should().Contain("public const int MaxPrivateNameLength = 32;");
        disguiseSource.Should().Contain("public const int MaxBiographyLength = 5000;");
        disguiseSource.Should().Contain("privateName.Length > MaxPrivateNameLength");
        disguiseSource.Should().Contain("biography.Length > MaxBiographyLength");
        disguiseSource.Should().Contain("disguise.Biography = biography;");
        disguiseSource.Should().Contain("Private disguise names may be no longer than {MaxPrivateNameLength} characters.");
        disguiseSource.Should().Contain("Unable to locate that disguise.");
        disguiseSource.Should().Contain("Retired disguises cannot be edited.");
        disguiseSource.Should().Contain("Unable to activate that disguise.");
        disguiseSource.Should().Contain("before activating another disguise.");
        disguiseSource.Should().Contain("dbPlayer.UndisguisedPortraitResref = GetPortraitResRef(player);");
        disguiseSource.Should().Contain("if (!string.IsNullOrWhiteSpace(dbPlayer.UndisguisedPortraitResref))");
        disguiseSource.Should().Contain("SetPortraitResRef(player, dbPlayer.UndisguisedPortraitResref);");
        disguiseSource.Should().Contain("dbPlayer.UndisguisedPortraitResref = string.Empty;");
        disguiseSource.Should().Contain("dbPlayer.UndisguisedDescription = GetDescription(player) ?? string.Empty;");
        disguiseSource.Should().Contain("dbPlayer.HasUndisguisedDescriptionSnapshot = true;");
        disguiseSource.Should().Contain("private const string BlankBiographyPlaceholder = \"No description is available.\";");
        disguiseSource.Should().Contain("string.IsNullOrWhiteSpace(disguise.Biography)");
        disguiseSource.Should().Contain("? BlankBiographyPlaceholder");
        disguiseSource.Should().Contain("SetDescription(player, biography);");
        disguiseSource.Should().NotContain("SetDescription(player, disguise.Biography ?? string.Empty);");
        disguiseSource.Should().Contain("SetDescription(player, dbPlayer.UndisguisedDescription ?? string.Empty);");
        disguiseSource.Should().Contain("dbPlayer.HasUndisguisedDescriptionSnapshot = false;");
        var ensureDescriptionSnapshotMethod = ExtractMethod(disguiseSource, "private static void EnsureUndisguisedDescriptionSnapshot(uint player)");
        ensureDescriptionSnapshotMethod.Should().Contain("dbPlayer.UndisguisedDescription = GetDescription(player) ?? string.Empty;");
        ensureDescriptionSnapshotMethod.Should().Contain("dbPlayer.HasUndisguisedDescriptionSnapshot = true;");
        ensureDescriptionSnapshotMethod.Should().Contain("DB.Set(dbPlayer);");
        ExtractMethod(disguiseSource, "private static void ApplyAppearance(uint player, PlayerDisguise disguise)")
            .Should().Contain("EnsureUndisguisedDescriptionSnapshot(player);");
        viewModelSource.Should().Contain("Creating a new disguise will consume one of your disguise slots. Retired disguises also occupy disguise slots until they are wiped. Are you sure?");
        viewModelSource.Should().Contain("ActivateButtonText = IsSelectedDisguiseActive() ? \"Deactivate\" : \"Activate\";");
        viewModelSource.Should().Contain("Disguise.Deactivate(Player)");
        viewModelSource.Should().Contain("Deactivating this disguise immediately restores your normal identity. Deactivation does not trigger the {delayMinutes}-minute delay between disguise activations. Are you sure?");
        viewModelSource.Should().Contain("Activating this disguise starts a {delayMinutes}-minute delay before you can activate another disguise. Deactivation has no delay. Are you sure?");
        viewModelSource.Should().Contain("var selectedDisguiseId = _selectedDisguiseId;");
        viewModelSource.Should().Contain("var result = Disguise.Activate(Player, selectedDisguiseId);");
        viewModelSource.Should().Contain("private void ReloadAvailableDisguise(string selectedDisguiseId)");
        viewModelSource.Should().Contain("LoadList(selectedDisguiseId);");
        viewModelSource.Should().Contain("if (!result.IsSuccessful)");
        viewModelSource.Should().Contain("FloatingTextStringOnCreature(result.ErrorMessage, Player, false);");
        viewModelSource.Should().Contain("Disguise.Unretire(Player, _selectedDisguiseId)");
        viewModelSource.Should().Contain("Restoring this disguise will move it back to your available disguises. Are you sure?");
        viewModelSource.Should().Contain("public Action OnClickPreviewSoundSet()");
        viewModelSource.Should().Contain("Cache.GetSoundSetPreviewSoundResref(_selectedSoundSetId)");
        viewModelSource.Should().Contain("PlayerPlugin.PlaySound(Player, previewSoundResref, OBJECT_INVALID)");
        viewModelSource.Should().NotContain("PlayVoiceChat(VoiceChat.Hello, Player)");
        viewModelSource.Should().Contain("public int SelectedSoundSetIndex");
        viewModelSource.Should().Contain("private readonly Dictionary<int, int> _soundSetIndexesById");
        viewModelSource.Should().Contain("private readonly List<GuiBindingList<GuiComboEntry>> _soundSetOptionPages");
        viewModelSource.Should().Contain("currentPage.Add(new GuiComboEntry(label, optionIndex));");
        viewModelSource.Should().Contain("private const int SoundSetPageSize");
        viewModelSource.Should().Contain("public GuiBindingList<GuiComboEntry> SoundSetPageNumbers");
        viewModelSource.Should().Contain("public int SelectedSoundSetPageIndex");
        viewModelSource.Should().Contain("private bool _suppressSoundSetPageChange");
        viewModelSource.Should().Contain("private int _selectedSoundSetId");
        viewModelSource.Should().Contain("SelectSoundSet(disguise.SoundSetId);");
        viewModelSource.Should().Contain("Biography = disguise.Biography ?? string.Empty;");
        viewModelSource.Should().Contain("WatchOnClient(model => model.Biography);");
        ExtractMethod(viewModelSource, "public Action OnClickSave()")
            .Should().Contain("Biography,");
        viewModelSource.Should().Contain("private void LoadSoundSetPageOptions");
        viewModelSource.Should().Contain("private void LoadSoundSetPageNumbers");
        viewModelSource.Should().Contain("private int GetSelectedSoundSetIndexOnCurrentPage");
        viewModelSource.Should().Contain("_soundSetPageIndex = absoluteIndex / SoundSetPageSize;");
        viewModelSource.Should().Contain("SoundSetOptions = _soundSetOptionPages[_soundSetPageIndex];");
        viewModelSource.Should().Contain("SoundSetPageNumbers = pageNumbers;");
        viewModelSource.Should().Contain("WatchOnClient(model => model.SelectedSoundSetPageIndex)");
        viewModelSource.Should().Contain("_soundSetIndexesById.TryGetValue");
        viewModelSource.Should().Contain("private void SetSelectedSoundSetFromPageIndex");
        viewModelSource.Should().Contain("public Action OnClickPreviousSoundSetPage()");
        viewModelSource.Should().Contain("public Action OnClickNextSoundSetPage()");
        viewModelSource.Should().Contain("private int SanitizeSoundSetIndex");
        viewModelSource.Should().Contain("if (index < 0)");
        viewModelSource.Should().Contain("LoadSoundSetPageOptions(GetSelectedSoundSetIndexOnCurrentPage(), true);");
        viewModelSource.Should().Contain("selectedPageIndex = GetCurrentSoundSetPageSize() > 0 ? 0 : -1;");
        viewModelSource.Should().Contain("private void RefreshSoundSetBindings");
        viewModelSource.Should().NotContain("SetSoundset(Player, _soundSetIds[SelectedSoundSetIndex])");
        viewModelSource.Should().Contain("DelayCommand(0.0f, () => PortraitInternalId = sanitizedValue)");
        viewModelSource.Should().Contain("PortraitInternalId = _activePortraitInternalId.ToString();");
        viewModelSource.Should().Contain("Disguise.GetDisguises(playerId, IsRetiredSelected)");
        viewModelSource.Should().Contain("ConfigureEmptyState(_disguiseIds.Count > 0);");
        viewModelSource.Should().Contain("EmptyStateTitle = \"No Disguise Selected\";");
        viewModelSource.Should().Contain("EmptyStateTitle = \"No Retired Disguises\";");
        viewModelSource.Should().Contain("EmptyStateTitle = \"No Available Disguises\";");
        viewModelSource.Should().Contain("SelectDisguiseAtIndex(0);");
        viewModelSource.Should().Contain("ChangePartialView(ContentPartialElement, GetContentPartialName())");
        viewModelSource.Should().NotContain("ChangePartialView(DetailPartialElement");
        viewModelSource.Should().NotContain("ChangePartialView(PortraitPartialElement");
        viewModelSource.Should().NotContain("ChangePartialView(ActionPartialElement");
        viewModelSource.Should().Contain("private Action WithLayoutRestore(Action action)");
        viewModelSource.Should().Contain("private void RestoreLayoutPartials()");
        viewModelSource.Should().Contain("ChangePartialView(\"_window_\", \"%%WINDOW_MAIN%%\");");
        viewModelSource.Should().Contain("DelayCommand(0.0f, ApplyLayoutPartials);");
        viewModelSource.Should().Contain("private string GetContentPartialName()");
        viewModelSource.Should().Contain("return ContentEditPartial;");
        viewModelSource.Should().Contain("return ContentRetiredPartial;");
        viewModelSource.Should().Contain("return IsAvailableSelected ? ContentAvailablePartial : ContentEmptyPartial;");

        cacheSource.Should().Contain("private static Dictionary<int, string> SoundSetPreviewSoundResrefs");
        cacheSource.Should().Contain("private static string ResolveSoundSetPreviewSoundResref(string soundSetResref)");
        cacheSource.Should().Contain("public static string GetSoundSetPreviewSoundResref(int soundSetId)");
        cacheSource.Should().Contain("[\"wookie\"] = \"p_zaalbar_bat1\"");
        cacheSource.Should().Contain("return trimmedResref.Length <= 11");

        var disguiseInitializeMethod = ExtractMethod(viewModelSource, "protected override void Initialize(GuiPayloadBase initialPayload)");
        var portraitDefaultIndex = disguiseInitializeMethod.IndexOf("PortraitInternalId = \"1\";", StringComparison.Ordinal);
        var portraitWatchIndex = disguiseInitializeMethod.IndexOf("WatchOnClient(model => model.PortraitInternalId);", StringComparison.Ordinal);
        portraitDefaultIndex.Should().BeGreaterThanOrEqualTo(0);
        portraitWatchIndex.Should().BeGreaterThanOrEqualTo(0);
        portraitDefaultIndex.Should().BeLessThan(portraitWatchIndex);
        ExtractMethod(viewModelSource, "private void ClearSelection()")
            .Should().Contain("PortraitInternalId = \"1\";");
        var newMethod = ExtractMethod(viewModelSource, "public Action OnClickNew()");
        newMethod.Should().Contain("ShowModal(\"Creating a new disguise will consume one of your disguise slots. Retired disguises also occupy disguise slots until they are wiped. Are you sure?\"");
        newMethod.Should().Contain("WithLayoutRestore(() =>");
        newMethod.Should().Contain("RestoreLayoutPartials");
        var activateOrDeactivateMethod = ExtractMethod(viewModelSource, "public Action OnClickActivateOrDeactivate()");
        activateOrDeactivateMethod.Should().Contain("ShowModal($\"Deactivating this disguise immediately restores your normal identity. Deactivation does not trigger the {delayMinutes}-minute delay between disguise activations. Are you sure?\"");
        activateOrDeactivateMethod.Should().Contain("ShowModal($\"Activating this disguise starts a {delayMinutes}-minute delay before you can activate another disguise. Deactivation has no delay. Are you sure?\"");
        activateOrDeactivateMethod.Should().Contain("var delayMinutes = GetActivationDelayMinutes();");
        activateOrDeactivateMethod.Should().Contain("WithLayoutRestore(() =>");
        activateOrDeactivateMethod.Should().Contain("RestoreLayoutPartials");
        activateOrDeactivateMethod.Should().Contain("var selectedDisguiseId = _selectedDisguiseId;");
        activateOrDeactivateMethod.Should().Contain("var result = Disguise.Activate(Player, selectedDisguiseId);");
        activateOrDeactivateMethod.Should().Contain("ReloadAvailableDisguise(selectedDisguiseId);");
        activateOrDeactivateMethod.Should().NotContain("LoadList(_selectedDisguiseId);");
        var retireMethod = ExtractMethod(viewModelSource, "public Action OnClickRetire()");
        retireMethod.Should().Contain("WithLayoutRestore(() =>");
        retireMethod.Should().Contain("RestoreLayoutPartials");
        var unretireMethod = ExtractMethod(viewModelSource, "public Action OnClickUnretire()");
        unretireMethod.Should().Contain("ShowModal(\"Restoring this disguise will move it back to your available disguises. Are you sure?\"");
        unretireMethod.Should().Contain("WithLayoutRestore(() =>");
        unretireMethod.Should().Contain("RestoreLayoutPartials");

        dialogSource.Should().Contain("Disguise.WipeCreditCost");
        dialogSource.Should().Contain("Disguise.WipeRoleplayXPCost");
        dialogSource.Should().Contain("DisguisePaymentMethod.Credits");
        dialogSource.Should().Contain("DisguisePaymentMethod.RoleplayXP");
        dialogSource.Should().Contain("Disguise.DeleteRetiredDisguise");
        dialogSource.Should().Contain("starport registries, transit manifests, broker ledgers, and public ID mirrors");
        dialogSource.Should().Contain("Authorize the Scrub");
        dialogSource.Should().Contain("The identity is gone. Anyone chasing that name will find static.");

        characterSheetDefinitionSource.Should().Contain("AddActionButton(actions, \"Disguises\", model => model.OnClickDisguises());");
        characterSheetViewModelSource.Should().Contain("public Action OnClickDisguises()");
        characterSheetViewModelSource.Should().Contain("Gui.TogglePlayerWindow(Player, GuiWindowType.Disguises)");
        characterSheetViewModelSource.Should().Contain("IGuiRefreshable<DisguiseChangedRefreshEvent>");
        var disguiseChangedRefreshMethod = ExtractMethod(characterSheetViewModelSource, "public void Refresh(DisguiseChangedRefreshEvent payload)");
        disguiseChangedRefreshMethod.Should().Contain("RefreshPortrait();");
        disguiseChangedRefreshMethod.Should().Contain("RefreshStats();");

        var disguiseDefinitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "DisguiseDefinition.cs"));
        // Redesigned master-detail layout: one left rail (tabs, slot meter, list, New button)
        // and a single content partial that swaps per state.
        disguiseDefinitionSource.Should().Contain("private const float RailWidth");
        disguiseDefinitionSource.Should().Contain(".SetWidth(RailWidth)");
        disguiseDefinitionSource.Should().Contain(".SetWidth(PortraitWidth)");
        disguiseDefinitionSource.Should().Contain(".SetWidth(ActionButtonWidth)");

        // The content pane fills the space left of the rail so the header and action bar
        // stretch to the window edges; form inputs keep explicit widths so labels never clip.
        disguiseDefinitionSource.Should().Contain("row.AddPartialView(DisguiseViewModel.ContentPartialElement);");
        disguiseDefinitionSource.Should().NotContain("private const float ContentWidth");
        disguiseDefinitionSource.Should().Contain(".SetWidth(FormFieldWidth)");

        // Field labels spell out who sees each value; lengths come from constants, never magic numbers.
        disguiseDefinitionSource.Should().Contain("AddTextField(col, \"Private Slot Label  (only you see this)\", model => model.PrivateName, Disguise.MaxPrivateNameLength);");
        disguiseDefinitionSource.Should().Contain("AddTextField(col, \"Public Description  (shown to others)\", model => model.Descriptor, PlayerName.MaxKnownNameLength);");
        disguiseDefinitionSource.Should().Contain(".SetText(\"Biography  (shown when examined)\")");
        disguiseDefinitionSource.Should().Contain(".BindValue(model => model.Biography)");
        disguiseDefinitionSource.Should().Contain(".SetMaxLength(Disguise.MaxBiographyLength)");
        disguiseDefinitionSource.Should().NotContain(", model => model.PrivateName, 32)");
        disguiseDefinitionSource.Should().NotContain(", model => model.Descriptor, 64)");
        disguiseDefinitionSource.Should().Contain("Hide Account Name");
        disguiseDefinitionSource.Should().NotContain("Account ID");

        // Slot usage renders as a colored meter, not a bare text line.
        disguiseDefinitionSource.Should().Contain("BindValue(model => model.SlotUsageProgress)");
        disguiseDefinitionSource.Should().Contain("BindColor(model => model.SlotUsageColor)");
        disguiseDefinitionSource.Should().Contain("BindText(model => model.SlotBarLabel)");

        // The active disguise is colored in the list and flagged by a status tag in the detail header.
        disguiseDefinitionSource.Should().Contain("BindColor(model => model.DisguiseColors)");
        disguiseDefinitionSource.Should().Contain("BindText(model => model.PrivateName)");
        disguiseDefinitionSource.Should().Contain("BindText(model => model.StatusText)");
        disguiseDefinitionSource.Should().Contain("BindColor(model => model.StatusColor)");

        // New Disguise button lives in the rail and only shows on the Available tab.
        disguiseDefinitionSource.Should().Contain(".SetText(\"New Disguise\")");
        disguiseDefinitionSource.Should().Contain(".BindIsVisible(model => model.IsAvailableSelected)");

        // A single consolidated content partial - no separate detail/portrait/action partials.
        disguiseDefinitionSource.Should().Contain(".DefinePartialView(DisguiseViewModel.ContentAvailablePartial, AddAvailableContentArea)");
        disguiseDefinitionSource.Should().Contain(".DefinePartialView(DisguiseViewModel.ContentRetiredPartial, AddRetiredContentArea)");
        disguiseDefinitionSource.Should().Contain(".DefinePartialView(DisguiseViewModel.ContentEditPartial, AddEditContentArea)");
        disguiseDefinitionSource.Should().Contain(".DefinePartialView(DisguiseViewModel.ContentEmptyPartial, AddEmptyContentArea)");
        disguiseDefinitionSource.Should().Contain("row.AddPartialView(DisguiseViewModel.ContentPartialElement)");
        disguiseDefinitionSource.Should().NotContain("DetailPartialElement");
        disguiseDefinitionSource.Should().NotContain("PortraitPartialElement");
        disguiseDefinitionSource.Should().NotContain("ActionPartialElement");
        disguiseDefinitionSource.Should().Contain("private static void AddAvailableContentArea");
        disguiseDefinitionSource.Should().Contain("private static void AddRetiredContentArea");
        disguiseDefinitionSource.Should().Contain("private static void AddEditContentArea");
        disguiseDefinitionSource.Should().Contain("private static void AddEmptyContentArea");
        disguiseDefinitionSource.Should().Contain("private static void AddSelectedContentArea");
        disguiseDefinitionSource.Should().Contain("private static void AddDetailHeader");
        disguiseDefinitionSource.Should().Contain("private static void AddPortraitRail");
        disguiseDefinitionSource.Should().Contain("private static void AddFieldsArea");
        disguiseDefinitionSource.Should().Contain("BindText(model => model.EmptyStateTitle)");
        disguiseDefinitionSource.Should().Contain("BindText(model => model.EmptyStateText)");

        // Visibility is driven by swapping partials, never per-button Show* flags.
        disguiseDefinitionSource.Should().NotContain("BindIsVisible(model => model.ShowActivateButton)");
        disguiseDefinitionSource.Should().NotContain("BindIsVisible(model => model.ShowEditButton)");
        disguiseDefinitionSource.Should().NotContain("BindIsVisible(model => model.ShowRetireButton)");
        disguiseDefinitionSource.Should().NotContain("BindIsVisible(model => model.ShowUnretireButton)");
        disguiseDefinitionSource.Should().NotContain("BindIsVisible(model => model.ShowEmptyState)");
        disguiseDefinitionSource.Should().NotContain("BindIsVisible(model => model.ShowDetailActionRow)");

        // Sound set paging.
        disguiseDefinitionSource.Should().Contain("BindOptions(model => model.SoundSetPageNumbers)");
        disguiseDefinitionSource.Should().Contain("BindSelectedIndex(model => model.SelectedSoundSetPageIndex)");
        disguiseDefinitionSource.Should().Contain("BindOnClicked(model => model.OnClickPreviousSoundSetPage())");
        disguiseDefinitionSource.Should().Contain("BindOnClicked(model => model.OnClickNextSoundSetPage())");
        disguiseDefinitionSource.Should().Contain("BindOnClicked(model => model.OnClickPreviewSoundSet())");

        // No leftover verbose button captions or fixed list row heights.
        disguiseDefinitionSource.Should().NotContain(".SetText(\"Portrait\")");
        disguiseDefinitionSource.Should().NotContain(".SetText(\"Previous\")");
        disguiseDefinitionSource.Should().NotContain(".SetText(\"Next\")");
        disguiseDefinitionSource.Should().NotContain(".SetRowHeight(");

        var buildWindowMethod = ExtractMethod(disguiseDefinitionSource, "public GuiConstructedWindow BuildWindow()");
        var railIndex = buildWindowMethod.IndexOf("row.AddColumn(AddRail)", StringComparison.Ordinal);
        var contentAreaIndex = buildWindowMethod.IndexOf("row.AddPartialView(DisguiseViewModel.ContentPartialElement)", StringComparison.Ordinal);
        railIndex.Should().BeGreaterThanOrEqualTo(0);
        contentAreaIndex.Should().BeGreaterThan(railIndex);

        // The rail holds the tab toggles, slot meter, disguise list and New button.
        var railMethod = ExtractMethod(disguiseDefinitionSource, "private static void AddRail");
        railMethod.Should().Contain("BindIsToggled(model => model.IsAvailableSelected)");
        railMethod.Should().Contain("BindIsToggled(model => model.IsRetiredSelected)");
        railMethod.Should().Contain("BindValue(model => model.SlotUsageProgress)");
        railMethod.Should().Contain("BindRowCount(model => model.DisguiseNames)");
        railMethod.Should().Contain(".SetText(\"New Disguise\")");

        // Each state's action band exposes exactly the right buttons.
        var availableActionBandMethod = ExtractMethod(disguiseDefinitionSource, "private static void AddAvailableActionBand");
        availableActionBandMethod.Should().Contain("ActivateButtonText");
        availableActionBandMethod.Should().Contain(".SetText(\"Edit\")");
        availableActionBandMethod.Should().Contain(".SetText(\"Retire\")");
        availableActionBandMethod.Should().NotContain(".SetText(\"Unretire\")");
        availableActionBandMethod.Should().NotContain(".SetText(\"Save\")");
        availableActionBandMethod.Should().NotContain(".SetText(\"Cancel\")");

        var retiredActionBandMethod = ExtractMethod(disguiseDefinitionSource, "private static void AddRetiredActionBand");
        retiredActionBandMethod.Should().Contain(".SetText(\"Unretire\")");
        retiredActionBandMethod.Should().NotContain("ActivateButtonText");
        retiredActionBandMethod.Should().NotContain(".SetText(\"Edit\")");
        retiredActionBandMethod.Should().NotContain(".SetText(\"Save\")");

        var editActionBandMethod = ExtractMethod(disguiseDefinitionSource, "private static void AddEditActionBand");
        editActionBandMethod.Should().Contain(".SetText(\"Save\")");
        editActionBandMethod.Should().Contain(".SetText(\"Cancel\")");
        editActionBandMethod.Should().NotContain("ActivateButtonText");
        editActionBandMethod.Should().NotContain(".SetText(\"Edit\")");
        editActionBandMethod.Should().NotContain(".SetText(\"Unretire\")");

        // Portrait image precedes the edit-only arrow stepper (< [id] >), margin-collapsed and edit-gated.
        var portraitRailMethod = ExtractMethod(disguiseDefinitionSource, "private static void AddPortraitRail");
        var portraitImageIndex = portraitRailMethod.IndexOf(".BindResref(model => model.PortraitResref)", StringComparison.Ordinal);
        var previousArrowIndex = portraitRailMethod.IndexOf(".SetText(\"<\")", StringComparison.Ordinal);
        var portraitIdIndex = portraitRailMethod.IndexOf(".BindValue(model => model.PortraitInternalId)", StringComparison.Ordinal);
        var nextArrowIndex = portraitRailMethod.IndexOf(".SetText(\">\")", StringComparison.Ordinal);
        portraitImageIndex.Should().BeGreaterThanOrEqualTo(0);
        previousArrowIndex.Should().BeGreaterThan(portraitImageIndex);
        portraitIdIndex.Should().BeGreaterThan(previousArrowIndex);
        nextArrowIndex.Should().BeGreaterThan(portraitIdIndex);
        portraitRailMethod.Should().Contain(".BindIsVisible(model => model.IsEditMode)");
        portraitRailMethod.Should().Contain(".SetMargin(0f)");
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
