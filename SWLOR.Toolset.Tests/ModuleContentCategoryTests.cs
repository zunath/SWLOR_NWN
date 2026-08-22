using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>The checked-in Module Contents arrangement remains complete and agrees with placements.</summary>
    [TestFixture]
    [Category("Corpus")]
    public sealed class ModuleContentCategoryTests
    {
        private CategoryCatalog _catalog = null!;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            var path = Path.Combine(CorpusLocator.RepositoryRoot, "toolset", "categories.json");
            _catalog = CategoryCatalog.Load(path, out var warning);
            warning.Should().BeNull();
        }

        [TestCase(ResourceType.Area, "are", ".are.json")]
        [TestCase(ResourceType.Dlg, "dlg", ".dlg.json")]
        [TestCase(ResourceType.Nss, "nss", ".nss")]
        public void EveryModuleContentResourceIsFiledExactlyOnce(
            ResourceType type,
            string directory,
            string suffix)
        {
            var resources = Directory.EnumerateFiles(
                    Path.Combine(CorpusLocator.ModuleDirectory, directory),
                    "*" + suffix)
                .Select(path => Path.GetFileName(path)[..^suffix.Length])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var section = _catalog.Section(type);
            var memberships = section.AllFolders()
                .SelectMany(folder => folder.Members.Select(member => (Member: member, Folder: folder)))
                .GroupBy(pair => pair.Member, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(pair => pair.Folder).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            memberships.Keys.Should().BeEquivalentTo(resources,
                "Unsorted is useful while editing, but the checked-in module arrangement should be complete");
            memberships.Should().OnlyContain(pair => pair.Value.Count == 1,
                "an item shown in several folders has no single authoritative home in Module Contents");
        }

        [Test]
        public void LocationFiledDialogsAgreeWithTheAreasContainingTheirNpcs()
        {
            var areaSection = _catalog.Section(ResourceType.Area);
            var dialogSection = _catalog.Section(ResourceType.Dlg);
            var areaRoots = areaSection.Folders.Select(folder => folder.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var areaFolderByResRef = Memberships(areaSection);
            var dialogFolderByResRef = Memberships(dialogSection);
            var placementRoots = ConversationPlacementRoots(areaSection, areaFolderByResRef);

            foreach (var (dialog, roots) in placementRoots)
            {
                if (!dialogFolderByResRef.TryGetValue(dialog, out var folder))
                    continue;

                var dialogRoot = dialogSection.PathTo(folder).First();
                if (!areaRoots.Contains(dialogRoot))
                    continue; // Functional folders such as Contract quests and Shared are intentional.

                roots.Should().Equal(new[] { dialogRoot },
                    "'{0}' is in the location folder '{1}' but its placed NPCs are elsewhere",
                    dialog, dialogRoot);
            }
        }

        [Test]
        public void Cz220DialogsFollowTheActualStationNpcsRatherThanTheCzNamePrefix()
        {
            var areaSection = _catalog.Section(ResourceType.Area);
            var dialogSection = _catalog.Section(ResourceType.Dlg);
            var roots = ConversationPlacementRoots(areaSection, Memberships(areaSection));
            var czFolder = dialogSection.Find("CZ-220");
            czFolder.Should().NotBeNull();

            var stationOnlyDialogs = roots
                .Where(pair => pair.Value.SetEquals(new[] { "CZ-220" }) &&
                               !pair.Key.StartsWith("cq_", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key);

            czFolder!.Members.Should().Contain(stationOnlyDialogs,
                "dialogs used only by CZ-220 NPCs and objects belong with that station");
            czFolder.Members.Should().Contain("dariobeto",
                "Dario's currently unplaced conversation identifies him as Darryn Beto's CZ-220 counterpart");

            foreach (var dialog in czFolder.Members.Where(roots.ContainsKey))
            {
                roots[dialog].Should().Contain("CZ-220",
                    $"'{dialog}' must not land in CZ-220 merely because its ResRef starts with 'cz'");
            }
        }

        private static Dictionary<string, CategoryFolder> Memberships(CategorySection section) =>
            section.AllFolders()
                .SelectMany(folder => folder.Members.Select(member => (Member: member, Folder: folder)))
                .ToDictionary(pair => pair.Member, pair => pair.Folder, StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, HashSet<string>> ConversationPlacementRoots(
            CategorySection areaSection,
            IReadOnlyDictionary<string, CategoryFolder> areaFolderByResRef)
        {
            var blueprintConversations = new Dictionary<string, IReadOnlyDictionary<string, string>>(
                StringComparer.Ordinal)
            {
                ["Creature List"] = ReadBlueprintConversations("utc", ".utc.json"),
                ["Placeable List"] = ReadBlueprintConversations("utp", ".utp.json"),
                ["Door List"] = ReadBlueprintConversations("utd", ".utd.json")
            };
            var roots = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var gitDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "git");

            foreach (var path in Directory.EnumerateFiles(gitDirectory, "*.git.json"))
            {
                var areaResRef = Path.GetFileName(path)[..^".git.json".Length];
                if (!areaFolderByResRef.TryGetValue(areaResRef, out var areaFolder))
                    continue;

                var areaRoot = areaSection.PathTo(areaFolder).First();
                if (areaRoot is "Prefabs" or "System")
                    continue; // A reusable set piece or engine area does not define a live-world home.

                using var document = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var (listName, conversationsByTemplate) in blueprintConversations)
                {
                    if (!TryGetValue(document.RootElement, listName, out var list) ||
                        list.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var instance in list.EnumerateArray())
                    {
                        var conversation = ReadString(instance, "Conversation");
                        if (string.IsNullOrWhiteSpace(conversation))
                        {
                            var template = ReadString(instance, "TemplateResRef");
                            if (template != null)
                                conversationsByTemplate.TryGetValue(template, out conversation);
                        }

                        if (string.IsNullOrWhiteSpace(conversation))
                            continue;

                        if (!roots.TryGetValue(conversation, out var usedIn))
                        {
                            usedIn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            roots[conversation] = usedIn;
                        }
                        usedIn.Add(areaRoot);
                    }
                }
            }

            return roots;
        }

        private static Dictionary<string, string> ReadBlueprintConversations(
            string directoryName,
            string suffix)
        {
            var conversations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, directoryName);
            foreach (var path in Directory.EnumerateFiles(directory, "*" + suffix))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var conversation = ReadString(document.RootElement, "Conversation");
                if (!string.IsNullOrWhiteSpace(conversation))
                {
                    var resRef = Path.GetFileName(path)[..^suffix.Length];
                    conversations[resRef] = conversation;
                }
            }
            return conversations;
        }

        private static string? ReadString(JsonElement owner, string fieldName) =>
            TryGetValue(owner, fieldName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static bool TryGetValue(JsonElement owner, string fieldName, out JsonElement value)
        {
            value = default;
            return owner.TryGetProperty(fieldName, out var field) &&
                   field.ValueKind == JsonValueKind.Object &&
                   field.TryGetProperty("value", out value);
        }
    }
}
