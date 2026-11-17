using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The DM and character chat commands used to come from two large definitions and now come from ten, one per command
/// family. The snapshot was taken from the two original classes (name, aliases, permissions, targeting, argument check
/// and description of every command); it fails if a command is lost, renamed, re-aliased or changes what it says.
/// </summary>
[TestFixture]
public class DMAndCharacterChatCommandSetTests
{
    private static IChatCommandListDefinition[] Definitions() => new IChatCommandListDefinition[]
    {
        new DMVariableChatCommand(),
        new DMWorldChatCommand(),
        new DMSpawnChatCommand(),
        new DMPlayerChatCommand(),
        new DMToolsChatCommand(),
        new DMShipChatCommand(),
        new DMCommunicationChatCommand(),
        new CharacterInfoChatCommand(),
        new CharacterSocialChatCommand(),
        new CharacterAppearanceChatCommand(),
        new CharacterCompanionChatCommand(),
    };

    [Test]
    public void CommandSet_MatchesTheSnapshotOfTheOriginalDefinitions()
    {
        var expected = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server.Tests", "Feature", "Snapshots",
            "DMAndCharacterChatCommands.txt")).Replace("\r\n", "\n");

        Dump().Should().Be(expected);
    }

    [Test]
    public void EveryCommandNameIsDefinedByExactlyOneDefinition()
    {
        var names = Definitions().SelectMany(definition => definition.BuildChatCommands().Keys).ToList();

        names.Should().OnlyHaveUniqueItems();
    }

    private static string Dump()
    {
        var details = new Dictionary<string, ChatCommandDetail>();
        foreach (var definition in Definitions())
        {
            foreach (var (key, value) in definition.BuildChatCommands())
            {
                details.Add(key, value);
            }
        }

        return Format(details);
    }

    internal static string Format(Dictionary<string, ChatCommandDetail> details)
    {
        var groups = details.Values.Distinct().OrderBy(detail => details.Where(p => p.Value == detail).Min(p => p.Key),
            StringComparer.Ordinal).ToList();
        var builder = new StringBuilder();
        foreach (var (key, detail) in details.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            builder.Append(key).Append('|')
                .Append("group=").Append(groups.IndexOf(detail)).Append('|')
                .Append(detail.Authorization).Append('|')
                .Append(detail.RequiresTarget).Append('|')
                .Append(detail.ValidTargetTypes).Append('|')
                .Append(detail.AllowsLocationTarget).Append('|')
                .Append(detail.AvailableToAllOnTestEnvironment).Append('|')
                .Append(detail.ValidateArguments != null).Append('|')
                .Append(detail.DoAction != null).Append('|')
                .Append(detail.Description.Replace("\r", "").Replace("\n", "\n")).Append('\n');
        }

        return builder.ToString();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
