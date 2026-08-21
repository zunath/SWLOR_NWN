using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.NWN.API.NWScript.Enum;

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
    public void EveryDirectlyColoredCodeDrivenHeader_WrapsItsEntireTextInOneColor()
    {
        var root = FindRepositoryRoot().FullName;
        var dialogDirectory = Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition");
        var errors = new List<string>();
        var assignmentPattern = new Regex(
            @"page\.Header\s*\+?=\s*(?<expression>.*?);",
            RegexOptions.Singleline);

        foreach (var path in Directory.EnumerateFiles(dialogDirectory, "*.cs"))
        {
            var source = File.ReadAllText(path);
            foreach (Match assignment in assignmentPattern.Matches(source))
            {
                var expression = assignment.Groups["expression"].Value.Trim();
                var colorRunCount = Regex.Matches(expression, @"ColorToken\.\w+\(").Count;
                if (colorRunCount == 0)
                    continue;

                if (colorRunCount != 1 ||
                    !expression.StartsWith("ColorToken.", StringComparison.Ordinal) ||
                    expression.Contains('+'))
                {
                    errors.Add($"{Path.GetFileName(path)}: {expression}");
                }
            }
        }

        errors.Should().BeEmpty(
            "mixed inline header colors must be corrected in the conversation definition instead of flattened by shared rendering code");

        var markupSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "ConversationService",
            "ConversationMarkup.cs"));
        markupSource.Should().NotContain("CollapseForHeader");
    }

    [Test]
    public void EveryAuthoredDialog_IsMigratedOrUsesTheApprovedDmfiNativePath()
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

        exceptionIds.Should().BeEquivalentTo(new[] { "dmfi_universal" },
            "DMFI intentionally keeps its native wand-driven conversation path");
        graphIds.Union(exceptionIds).Should().BeEquivalentTo(authoredIds,
            "every authored conversation must have either a NUI graph or the approved DMFI native path");
    }

    [Test]
    public void EveryModuleReferenceToAMigratedConversation_RoutesDirectlyToNui()
    {
        var root = FindRepositoryRoot().FullName;
        var graphDirectory = Path.Combine(root, "SWLOR.Game.Server", "ConversationData");
        var graphIds = Directory.EnumerateFiles(graphDirectory, "*.conversation.json")
            .Select(path => Path.GetFileName(path)[..^".conversation.json".Length])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var directoryName in new[] { "git", "utc", "utp", "utd" })
        {
            var directory = Path.Combine(root, "Module", directoryName);
            foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var document = JObject.Parse(File.ReadAllText(path));
                foreach (var resource in document.DescendantsAndSelf().OfType<JObject>())
                {
                    var conversationId = resource["Conversation"]?["value"]?.Value<string>();
                    if (string.IsNullOrWhiteSpace(conversationId) || !graphIds.Contains(conversationId))
                        continue;

                    var routes = new[] { "ScriptDialogue", "OnUsed", "OnFailToOpen" }
                        .Select(field => (Field: field, Script: resource[field]?["value"]?.Value<string>()))
                        .Where(route => route.Script != null)
                        .ToArray();
                    if (routes.Length != 1 ||
                        !routes[0].Script!.Equals("dialog_start", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"{Path.GetRelativePath(root, path)}: '{conversationId}' is routed by " +
                                   (routes.Length == 0
                                       ? "no supported interaction event"
                                       : string.Join(", ", routes.Select(route => $"{route.Field}='{route.Script}'"))));
                    }
                }
            }
        }

        errors.Should().BeEmpty(
            "migrated conversations must bypass NWN's native dialogue window at the module-resource boundary");
    }

    [Test]
    public void NativeDialogFallbacks_CaptureTheOwnerBeforeAssigningThePlayerCommand()
    {
        var serviceDirectory = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Service");
        foreach (var fileName in new[] { "ConversationMenu.cs", "AI.cs" })
        {
            var source = File.ReadAllText(Path.Combine(serviceDirectory, fileName));
            source.Should().NotContain("ActionStartConversation(OBJECT_SELF",
                $"{fileName} runs the fallback as the player, where OBJECT_SELF would resolve to that player");
            source.Should().Contain("ActionStartConversation(owner",
                $"{fileName} must capture the event owner before queuing the fallback command");
        }
    }

    [Test]
    public void ConversationWindow_UsesCompactNpcTextRowsAndAStableTitle()
    {
        var root = FindRepositoryRoot().FullName;
        var definitionSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ConversationWindowDefinition.cs"));

        definitionSource.Should().Contain(".SetTitle(\"Conversation\")");
        definitionSource.Should().NotContain(".BindTitle(model => model.WindowTitle)");

        var textPanelStart = definitionSource.IndexOf(".BindText(model => model.LineTexts)", StringComparison.Ordinal);
        var textPanelEnd = definitionSource.IndexOf("root.AddRow(row =>", textPanelStart, StringComparison.Ordinal);
        textPanelStart.Should().BeGreaterThanOrEqualTo(0);
        textPanelEnd.Should().BeGreaterThan(textPanelStart);

        var textPanelSource = definitionSource[textPanelStart..textPanelEnd];
        textPanelSource.Should().Contain(".SetScrollbars(NuiScrollbars.None)",
            "individual dialogue lines must not add a nested scrollbar");
        textPanelSource.Should().Contain(".SetScrollbars(NuiScrollbars.Y)",
            "the containing dialogue list owns the panel's only scrollbar");
        textPanelSource.Should().NotContain(".SetScrollbars(NuiScrollbars.Auto)");
        textPanelSource.Should().Contain(".SetHeight(24f)",
            "each text widget should fit one display line rather than wrapping inside a fixed-height row");
        textPanelSource.Should().Contain(".SetRowHeight(28f)",
            "successive dialogue lines should flow without multi-line row spacing");
        textPanelSource.Should().NotContain(".SetHeight(48f)");
        textPanelSource.Should().NotContain(".SetRowHeight(56f)");
        textPanelSource.Should().NotContain(".SetHeight(196f)");
        textPanelSource.Should().NotContain(".SetRowHeight(208f)");

        var viewModelSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ConversationViewModel.cs"));
        viewModelSource.Should().Contain("RefreshConversation(false)",
            "resizing should reflow the text for the available width");
        viewModelSource.Should().Contain("segmentCharacterLimit == _lastDialogueSegmentLimit",
            "position, height, and equivalent-width geometry events should not rebuild conversation bindings");
        viewModelSource.Should().NotContain("MinimumWindowWidth");
        viewModelSource.Should().NotContain("EnsureReadableWindowGeometry");
    }

    [Test]
    public void OversizedConversationText_IsSplitIntoScrollableRowsWithoutCuttingWords()
    {
        var method = typeof(ConversationViewModel).GetMethod(
            "SplitDialogueText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        var source = string.Join(" ", Enumerable.Repeat("dialogue", 100));
        var segments = ((IEnumerable<string>)method!.Invoke(null, new object[] { source, 80 })!).ToArray();

        segments.Should().HaveCountGreaterThan(1);
        segments.Should().OnlyContain(segment => segment.Length <= 80);
        string.Join(" ", segments).Should().Be(source);
    }

    [Test]
    public void ConversationWindow_UsesTheAvailableWidthWhenSplittingRows()
    {
        var limitMethod = typeof(ConversationViewModel).GetMethod(
            "CalculateDialogueSegmentLimit",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        limitMethod.Should().NotBeNull();

        ((int)limitMethod!.Invoke(null, new object[] { 650f })!).Should().Be(57);
        ((int)limitMethod.Invoke(null, new object[] { 400f })!).Should().Be(26);
        ((int)limitMethod.Invoke(null, new object[] { 300f })!).Should().Be(13);
        ((int)limitMethod.Invoke(null, new object[] { 200f })!).Should().Be(4);

        var splitMethod = typeof(ConversationViewModel).GetMethod(
            "SplitDialogueText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var source = string.Join(" ", Enumerable.Repeat("responsive", 30));
        var segmentLimit = (int)limitMethod.Invoke(null, new object[] { 400f })!;
        var segments = ((IEnumerable<string>)splitMethod!.Invoke(
            null,
            new object[] { source, segmentLimit })!).ToArray();

        segments.Should().OnlyContain(segment => segment.Length <= segmentLimit);
        string.Join(" ", segments).Should().Be(source);
    }

    [Test]
    public void DefaultWidthConversationText_FlowsThroughSingleLineRows()
    {
        var limitMethod = typeof(ConversationViewModel).GetMethod(
            "CalculateDialogueSegmentLimit",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var splitMethod = typeof(ConversationViewModel).GetMethod(
            "SplitDialogueText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        limitMethod.Should().NotBeNull();
        splitMethod.Should().NotBeNull();

        const string source =
            "We will be landing onto CZ-220 shortly. Please make sure you have all your belongings before exiting the ship.";
        var segmentLimit = (int)limitMethod!.Invoke(null, new object[] { 650f })!;
        var segments = ((IEnumerable<string>)splitMethod!.Invoke(
            null,
            new object[] { source, segmentLimit })!).ToArray();

        segments.Should().Equal(
            "We will be landing onto CZ-220 shortly. Please make sure",
            "you have all your belongings before exiting the ship.");
        segments.Should().OnlyContain(segment => segment.Length <= segmentLimit);
    }

    [Test]
    public void NarrowConversationRows_PreserveAvailableWordBoundaries()
    {
        var method = typeof(ConversationViewModel).GetMethod(
            "SplitDialogueText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        const string source = "A real wipe can end it";
        var segments = ((IEnumerable<string>)method!.Invoke(null, new object[] { source, 4 })!).ToArray();

        segments.Should().Equal("A", "real", "wipe", "can", "end", "it");
        string.Join(" ", segments).Should().Be(source);
    }

    [Test]
    public void ExplicitConversationLines_AreSplitIntoSeparateCompactRows()
    {
        var method = typeof(ConversationViewModel).GetMethod(
            "SplitDialogueText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        const string source = "Destination: Dantooine\r\nArriving in: 5 minutes\n\nReady.";
        var segments = ((IEnumerable<string>)method!.Invoke(null, new object[] { source, 80 })!).ToArray();

        segments.Should().Equal(
            "Destination: Dantooine",
            "Arriving in: 5 minutes",
            "Ready.");
    }

    [Test]
    public void EveryAuthoredConversationBlock_FitsCompactRows()
    {
        var splitMethod = typeof(ConversationViewModel).GetMethod(
            "SplitDialogueText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var limitMethod = typeof(ConversationViewModel).GetMethod(
            "CalculateDialogueSegmentLimit",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        splitMethod.Should().NotBeNull();
        limitMethod.Should().NotBeNull();
        var defaultSegmentLimit = (int)limitMethod!.Invoke(null, new object[] { 650f })!;

        var conversationDirectory = Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "ConversationData");
        var errors = new List<string>();

        foreach (var path in Directory.EnumerateFiles(conversationDirectory, "*.conversation.json"))
        {
            var document = JObject.Parse(File.ReadAllText(path));
            foreach (var node in document["Nodes"]?.Children<JProperty>() ?? [])
            {
                foreach (var block in node.Value["Text"]?.Children<JObject>() ?? [])
                {
                    var source = block["Text"]?.Value<string>() ?? string.Empty;
                    var segments = ((IEnumerable<string>)splitMethod!.Invoke(
                        null,
                        new object[] { source, defaultSegmentLimit })!).ToArray();
                    if (!string.IsNullOrWhiteSpace(source) && segments.Length == 0)
                        errors.Add($"{Path.GetFileName(path)}:{node.Name} produced no display rows");
                    if (segments.Any(segment =>
                            segment.Length > defaultSegmentLimit ||
                            segment.Contains('\n') ||
                            segment.Contains('\r')))
                        errors.Add($"{Path.GetFileName(path)}:{node.Name} produced an oversized display row");
                }
            }
        }

        errors.Should().BeEmpty(
            "all authored dialogue must pass through the compact, word-safe row layout");
    }

    [Test]
    public void AutomaticConversationPortraits_AreLimitedToCreatures()
    {
        var method = typeof(ConversationViewModel).GetMethod(
            "SupportsAutomaticPortrait",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        ((bool)method!.Invoke(null, new object[] { ObjectType.Creature })!).Should().BeTrue();
        ((bool)method.Invoke(null, new object[] { ObjectType.Placeable })!).Should().BeFalse(
            "placeables such as shuttle status consoles do not have renderable creature portraits");
        ((bool)method.Invoke(null, new object[] { ObjectType.Door })!).Should().BeFalse();
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
