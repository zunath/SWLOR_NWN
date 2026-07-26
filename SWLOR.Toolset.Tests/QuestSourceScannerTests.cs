using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Reads the real quest definitions out of the game-server source. Everything the conversation
    /// editor says about a quest — its name, how many steps it has, what gates it — comes from here,
    /// so these assert against quests that actually ship rather than fixtures.
    /// </summary>
    public class QuestSourceScannerTests
    {
        private static readonly IGameCodeIndex Index = new GameCodeIndex(GameServerSourceRoot);

        private static string GameServerSourceRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                    if (Directory.Exists(Path.Combine(candidate, "Feature", "QuestDefinition")))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the SWLOR.Game.Server source tree.");
            }
        }

        [Test]
        public void TheSourceScanIsAvailable()
        {
            Index.IsSourceScanAvailable.Should().BeTrue();
            Index.Quests.Should().NotBeEmpty();
        }

        [Test]
        public void AQuestReportsItsNameStepsAndPrerequisite()
        {
            var quest = Index.FindQuest("field_tinctures");

            quest.Should().NotBeNull();
            quest!.Name.Should().Be("Field Tinctures");
            quest.StateCount.Should().Be(2);
            quest.IsRepeatable.Should().BeFalse();
            quest.PrerequisiteQuestIds.Should().Equal("harvest_herbs");
            quest.SourceFile.Should().Be("DantooineQuestDefinition.cs");
        }

        [Test]
        public void ARepeatableQuestSaysSo()
        {
            var quest = Index.FindQuest("harvest_herbs");

            quest.Should().NotBeNull();
            quest!.Name.Should().Be("Harvesting Herbs");
            quest.IsRepeatable.Should().BeTrue();
            quest.StateCount.Should().Be(2);
            quest.PrerequisiteQuestIds.Should().BeEmpty();
        }

        [Test]
        public void JournalTextIsAttachedToTheStepItBelongsTo()
        {
            var quest = Index.FindQuest("field_tinctures")!;

            quest.JournalTextByState.Should().ContainKey(1);
            quest.JournalTextByState[1].Should().Contain("Collect three Wild Innards");
            quest.JournalTextByState.Should().ContainKey(2);
            quest.JournalTextByState[2].Should().Be(
                "Deliver the Wild Innards and Thune Blood to Healer Elara in the Dantooine Colony.");
        }

        [Test]
        public void EveryScannedQuestHasANameAndAtLeastOneStep()
        {
            var broken = Index.Quests.Values
                .Where(quest => string.IsNullOrWhiteSpace(quest.Name) || quest.StateCount == 0)
                .Select(quest => quest.Id)
                .ToList();

            broken.Should().BeEmpty();
        }

        [Test]
        public void EveryPrerequisiteQuestNamesAQuestThatExists()
        {
            var dangling = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var quest in Index.Quests.Values)
            {
                foreach (var prerequisite in quest.PrerequisiteQuestIds)
                {
                    if (!Index.IsValidQuestId(prerequisite))
                        dangling.Add($"{quest.Id} requires {prerequisite}");
                }
            }

            dangling.Should().BeEmpty();
        }

        /// <summary>
        /// The one conversation naming a quest the game does not declare. <c>suppress_rogues</c>
        /// appears nowhere in the game code, and <c>trooperquest</c> is itself among the
        /// conversations no blueprint or placed instance references — so this is dead content
        /// pointing at a quest that was never implemented, not a blind spot in the scan.
        /// </summary>
        private static readonly string[] KnownMissingQuests =
        {
            "trooperquest.dlg.json: suppress_rogues"
        };

        [Test]
        public void EveryQuestReferencedByAConversationIsOneTheGameDeclares()
        {
            // The check that makes a quest-id dropdown trustworthy: a conversation naming a quest
            // the scan cannot find means either the quest is gone or the scan has a blind spot, and
            // both are worth knowing before an editor offers the list as authoritative.
            var unknown = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (file, questId) in QuestIdsUsedInConversations())
            {
                if (!Index.IsValidQuestId(questId))
                    unknown.Add($"{file}: {questId}");
            }

            unknown.Should().BeEquivalentTo(KnownMissingQuests);
        }

        [Test]
        public void EveryQuestStateReferencedByAConversationExists()
        {
            // A guard on step 3 of a two-step quest can never fire. Reported per usage so the
            // editor can point at the exact condition.
            var impossible = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (file, questId, state) in QuestStatesUsedInConversations())
            {
                var quest = Index.FindQuest(questId);
                if (quest == null)
                    continue;

                if (state < 1 || state > quest.StateCount)
                    impossible.Add($"{file}: {questId} step {state}, but it has {quest.StateCount}");
            }

            impossible.Should().BeEmpty();
        }

        [Test]
        public void FactionsAndSkillsResolveToNames()
        {
            Index.Factions[7].Should().Be("Czerka Corporation");
            Index.Skills[13].Should().Be("Beast Mastery");
            Index.SkillEnumNames[13].Should().Be("BeastMastery");
        }

        private static IEnumerable<(string File, string QuestId)> QuestIdsUsedInConversations()
        {
            foreach (var (file, key, arguments) in DialogSnippetUsages())
            {
                if (arguments.Length == 0)
                    continue;

                var bare = key.StartsWith('!') ? key[1..] : key;
                if (bare.Contains("quest", StringComparison.Ordinal))
                    yield return (file, arguments[0]);
            }
        }

        private static IEnumerable<(string File, string QuestId, int State)> QuestStatesUsedInConversations()
        {
            foreach (var (file, key, arguments) in DialogSnippetUsages())
            {
                var bare = key.StartsWith('!') ? key[1..] : key;
                if (bare != "condition-on-quest-state" || arguments.Length < 2)
                    continue;

                for (var i = 1; i < arguments.Length; i++)
                {
                    if (int.TryParse(arguments[i], out var state))
                        yield return (file, arguments[0], state);
                }
            }
        }

        private static IEnumerable<(string File, string Key, string[] Arguments)> DialogSnippetUsages()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "dlg");
            foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
            {
                var document = Domain.Documents.DlgDocument.Load(path);
                var file = Path.GetFileName(path);

                foreach (var link in document.AllLinks())
                {
                    foreach (var condition in link.Conditions)
                        yield return (file, condition.Key, condition.Arguments);
                }

                foreach (var node in document.Entries.Concat(document.Replies))
                {
                    foreach (var action in node.Actions)
                        yield return (file, action.Key, action.Arguments);
                }
            }
        }
    }
}
