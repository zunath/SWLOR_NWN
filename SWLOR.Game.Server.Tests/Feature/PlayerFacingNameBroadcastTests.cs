using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerFacingNameBroadcastTests
{
    [Test]
    public void CombatAndSpaceBroadcasts_DoNotInterpolateRawPlayerNames()
    {
        var root = FindRepositoryRoot();
        var paths = new List<string>
        {
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Space.cs")
        };

        paths.AddRange(Directory.GetFiles(
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "ShipModuleDefinition"),
            "*.cs"));

        foreach (var path in paths)
        {
            var source = File.ReadAllText(path);
            var rawNameBroadcastInvocations = ExtractInvocations(source, "Messaging.SendMessageNearbyToPlayers")
                .Where(invocation => invocation.Text.Contains("GetName("))
                .Select(invocation => $"{Path.GetFileName(path)}:{GetLineNumber(source, invocation.StartIndex)}")
                .ToList();

            rawNameBroadcastInvocations.Should().BeEmpty($"{Path.GetFileName(path)} should render player-facing broadcast names per receiver");
        }
    }

    [Test]
    public void HoloComDirectory_UsesObserverSpecificDisplayNames()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition",
            "HoloComDialog.cs"));

        source.Should().NotContain("GetName(");
        source.Should().Contain("PlayerName.GetDisplayName(player, pc)");
        source.Should().Contain("PlayerName.GetDisplayName(player, callSender)");
        source.Should().Contain("PlayerName.GetDisplayName(player, callReceiver)");
        source.Should().Contain("PlayerName.GetDisplayName(sender, receiver)");
    }

    [Test]
    public void PublicPlayerFacingSurfaces_DoNotExposeCanonicalPlayerNames()
    {
        var root = FindRepositoryRoot();

        var holoNetSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "HoloNetViewModel.cs"));
        holoNetSource.Should().Contain("PlayerName.GetDisplayName(onlinePlayer, Player)");
        holoNetSource.Should().Contain("\"HoloNet Broadcast\"");
        holoNetSource.Should().NotContain("authorName + \" broadcasts a new HoloNet message");

        var communicationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Communication.cs"));
        communicationSource.Should().Contain("PlayerName.GetColoredDisplayName(receiver, speaker)");
        communicationSource.Should().Contain("var speaker = GetEffectiveChatSpeaker(sender);");
        communicationSource.Should().Contain("var finalChannel = channel;");
        communicationSource.Should().Contain("finalChannel = ChatChannel.DMTalk;");
        communicationSource.Should().NotContain("var finalChannel = ChatChannel.ServerMessage;");

        var statusEffectSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs"));
        statusEffectSource.Should().Contain("PlayerName.GetDisplayName(receiver, creature)");
        statusEffectSource.Should().Contain("PlayerName.GetDisplayName(receiver, source)");
        statusEffectSource.Should().NotContain("var name = GetName(creature);");

        var propertyPermissionsSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PropertyPermissionsViewModel.cs"));
        propertyPermissionsSource.Should().Contain("PlayerNameService.SearchKnownPlayerIdsByName(Player, SearchText, int.MaxValue)");
        propertyPermissionsSource.Should().Contain("PlayerNameService.GetDisplayNameByPlayerId");
        propertyPermissionsSource.Should().NotContain("nameof(Entity.Player.Name)");

        var electionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ElectionViewModel.cs"));
        electionSource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, candidate.Id, candidate.Name)");
        electionSource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, selectedCandidateId, dbCandidate.Name)");
        electionSource.Should().NotContain("Your vote for {dbCandidate.Name} has been cast.");

        var citySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageCityViewModel.cs"));
        citySource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, citizen.Id, citizen.Name)");

        var citizenshipSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageCitizenshipViewModel.cs"));
        citizenshipSource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, dbCity.OwnerPlayerId, dbMayorPlayer.Name)");
        citizenshipSource.Should().NotContain("Mayor: {dbMayorPlayer.Name}");

        var propertySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs"));
        propertySource.Should().NotContain("$\"{GetName(player)}'s Apartment\"");
        propertySource.Should().NotContain("$\"{GetName(player)}'s Starship\"");
        propertySource.Should().NotContain("$\"{GetName(player)}'s City\"");
        propertySource.Should().NotContain("GetPlayerName(");
        propertySource.Should().NotContain("**Mayor**:");
        propertySource.Should().NotContain("**New Mayor**:");
        propertySource.Should().NotContain("**Founding Mayor**:");
    }

    [Test]
    public void CopiedPlayerObjects_UseGenericDisplaySafeNames()
    {
        var root = FindRepositoryRoot();

        var holoComSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "HoloCom.cs"));
        holoComSource.Should().Contain("var holoSender = CopyObject(sender");
        holoComSource.Should().Contain("var holoReceiver = CopyObject(receiver");
        holoComSource.Should().Contain("SetName(holoSender, \"HoloCom Hologram\");");
        holoComSource.Should().Contain("SetName(holoReceiver, \"HoloCom Hologram\");");

        var spaceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Space.cs"));
        spaceSource.Should().Contain("var copy = CopyObject(player");
        spaceSource.Should().Contain("SetName(copy, \"Pilot\");");
    }

    private static List<(int StartIndex, string Text)> ExtractInvocations(string source, string methodName)
    {
        var invocations = new List<(int StartIndex, string Text)>();
        var searchIndex = 0;

        while (searchIndex < source.Length)
        {
            var methodIndex = source.IndexOf(methodName, searchIndex, StringComparison.Ordinal);
            if (methodIndex < 0)
                break;

            var openParenIndex = source.IndexOf('(', methodIndex + methodName.Length);
            if (openParenIndex < 0)
                break;

            var closeParenIndex = FindClosingParenthesis(source, openParenIndex);
            closeParenIndex.Should().BeGreaterThan(openParenIndex, $"the {methodName} invocation should be parseable");

            invocations.Add((methodIndex, source.Substring(methodIndex, closeParenIndex - methodIndex + 1)));
            searchIndex = closeParenIndex + 1;
        }

        return invocations;
    }

    private static int FindClosingParenthesis(string source, int openParenIndex)
    {
        // This lightweight parser intentionally does not support C# raw string literals.
        // If SendMessageNearbyToPlayers invocations start using """ raw strings, parsing may be incorrect.
        var depth = 0;
        var inString = false;
        var inVerbatimString = false;
        var inChar = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var index = openParenIndex; index < source.Length; index++)
        {
            var character = source[index];
            var nextCharacter = index + 1 < source.Length ? source[index + 1] : '\0';

            if (inLineComment)
            {
                if (character == '\n')
                    inLineComment = false;

                continue;
            }

            if (inBlockComment)
            {
                if (character == '*' && nextCharacter == '/')
                {
                    inBlockComment = false;
                    index++;
                }

                continue;
            }

            if (inString)
            {
                if (inVerbatimString)
                {
                    if (character == '"' && nextCharacter == '"')
                    {
                        index++;
                        continue;
                    }

                    if (character == '"')
                        inString = false;

                    continue;
                }

                if (character == '\\')
                {
                    index++;
                    continue;
                }

                if (character == '"')
                    inString = false;

                continue;
            }

            if (inChar)
            {
                if (character == '\\')
                {
                    index++;
                    continue;
                }

                if (character == '\'')
                    inChar = false;

                continue;
            }

            if (character == '/' && nextCharacter == '/')
            {
                inLineComment = true;
                index++;
                continue;
            }

            if (character == '/' && nextCharacter == '*')
            {
                inBlockComment = true;
                index++;
                continue;
            }

            if (character == '"')
            {
                var previousCharacter = index > 0 ? source[index - 1] : '\0';
                var twoCharactersBack = index > 1 ? source[index - 2] : '\0';

                inString = true;
                inVerbatimString =
                    previousCharacter == '@' ||
                    previousCharacter == '$' && twoCharactersBack == '@';
                continue;
            }

            if (character == '\'')
            {
                inChar = true;
                continue;
            }

            if (character == '(')
            {
                depth++;
                continue;
            }

            if (character != ')')
                continue;

            depth--;
            if (depth == 0)
                return index;
        }

        return -1;
    }

    private static int GetLineNumber(string source, int index)
    {
        return source.Take(index).Count(character => character == '\n') + 1;
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
