using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Service;

public class ConversationArchitectureTests
{
    [Test]
    public void ConversationGraphs_LoadOnlyAfterSnippetRegistrationCompletes()
    {
        var snippetCacheEvents = typeof(Snippet)
            .GetMethod(nameof(Snippet.CacheData))!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script);
        var conversationCacheEvents = typeof(Conversation)
            .GetMethod(nameof(Conversation.CacheData))!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script);

        snippetCacheEvents.Should().BeEquivalentTo(new[] { ScriptName.OnModuleCacheBefore });
        conversationCacheEvents.Should().BeEquivalentTo(new[] { ScriptName.OnModuleCacheAfter },
            "conversation graphs validate snippet operations and must load after every before-cache handler has completed");
    }

    [Test]
    public void GeneratedDialogShellResources_DoNotExist()
    {
        var dialogDirectory = Path.Combine(FindRepositoryRoot().FullName, "Module", "dlg");
        var generatedShells = Directory
            .EnumerateFiles(dialogDirectory, "dialog*.dlg.json", SearchOption.TopDirectoryOnly)
            .Where(path => IsGeneratedShell(Path.GetFileName(path)))
            .Select(Path.GetFileName)
            .ToArray();

        generatedShells.Should().BeEmpty(
            "NUI conversations must not depend on the 255 generated DLG shell resources");
    }

    [Test]
    public void RetiredDialogServiceSources_DoNotExist()
    {
        var serverRoot = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server");

        File.Exists(Path.Combine(serverRoot, "Service", "Dialog.cs")).Should().BeFalse();
        var retiredDirectory = Path.Combine(serverRoot, "Service", "DialogService");
        var retiredSources = Directory.Exists(retiredDirectory)
            ? Directory.EnumerateFiles(retiredDirectory, "*.cs", SearchOption.AllDirectories)
            : Array.Empty<string>();
        retiredSources.Should().BeEmpty();
    }

    [Test]
    public void EveryCodeDrivenDialogDefinition_UsesTheNuiConversationMenuBase()
    {
        var definitions = typeof(ConversationMenuDefinition).Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                !type.IsNested &&
                type.Namespace == "SWLOR.Game.Server.Feature.DialogDefinition")
            .ToArray();

        definitions.Should().NotBeEmpty();
        definitions.Should().OnlyContain(type => typeof(ConversationMenuDefinition).IsAssignableFrom(type),
            "code-driven conversations must open directly in NUI without the retired Dialog service");
    }

    [Test]
    public void EveryAuthoredDialog_IsEitherMigratedOrAnExplicitLegacyException()
    {
        var root = FindRepositoryRoot().FullName;
        var graphDirectory = Path.Combine(root, "SWLOR.Game.Server", "ConversationData");
        var dialogDirectory = Path.Combine(root, "Module", "dlg");

        var graphIds = Directory.EnumerateFiles(graphDirectory, "*.conversation.json")
            .Select(path => Path.GetFileName(path)[..^".conversation.json".Length])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var exceptionIds = JArray.Parse(File.ReadAllText(
                Path.Combine(graphDirectory, "legacy-exceptions.json")))
            .Select(item => item.Value<string>("ConversationId"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var authoredIds = Directory.EnumerateFiles(dialogDirectory, "*.dlg.json")
            .Select(path => Path.GetFileName(path)[..^".dlg.json".Length])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        graphIds.Should().NotIntersectWith(exceptionIds,
            "a conversation cannot be both NUI-owned and delegated to native DLG");
        graphIds.Concat(exceptionIds).Should().BeEquivalentTo(authoredIds,
            "every authored conversation must have an explicit runtime path");
    }

    private static bool IsGeneratedShell(string fileName)
    {
        var match = Regex.Match(fileName, @"^dialog(?<number>\d+)\.dlg\.json$", RegexOptions.IgnoreCase);
        return match.Success &&
               int.TryParse(match.Groups["number"].Value, out var number) &&
               number is >= 1 and <= 255;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null &&
               !Directory.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
