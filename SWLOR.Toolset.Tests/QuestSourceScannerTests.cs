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

        /// <summary>
        /// The guild definitions build every task through a private helper that takes the quest id
        /// as a parameter, so nothing about them appears in a literal Create() call. Reading only
        /// literals lost all four guilds - 651 quests that GameCodeIndex.Quests did not have while
        /// IsSourceScanAvailable still said yes, so the quest dropdown could not select them and
        /// conversation analysis reported real quests as nonexistent.
        /// </summary>
        [Test]
        public void HelperBuiltGuildTasksAreIndexed()
        {
            Index.FindQuest("eng_tsk_001").Should().NotBeNull();
            Index.FindQuest("eng_tsk_815").Should().NotBeNull();

            Index.Quests
                .Count(quest => quest.Key.StartsWith("eng_tsk_", StringComparison.Ordinal))
                .Should().Be(80, "that is every BuildItemTask call in the Engineering definition");

            // The other three guilds use the same shape, so none of them should be empty either.
            foreach (var prefix in new[] { "smth_tsk_", "fab_tsk_", "agr_tsk_", "hun_tsk_" })
            {
                Index.Quests
                    .Count(quest => quest.Key.StartsWith(prefix, StringComparison.Ordinal))
                    .Should().BeGreaterThan(0, $"{prefix} tasks are built through the same helper");
            }
        }

        /// <summary>
        /// A helper-built quest still reports the chain the helper declares - one state, repeatable -
        /// so the coverage strip has cells to draw.
        /// </summary>
        [Test]
        public void AHelperBuiltQuestCarriesTheHelpersChain()
        {
            var quest = Index.FindQuest("eng_tsk_001")!;

            quest.StateCount.Should().BeGreaterThan(0);
            quest.IsRepeatable.Should().BeTrue();
        }

        [Test]
        public void HelpersSharingAQuestIdParameterKeepTheirOwnObjectiveChains()
        {
            var killTask = Index.FindQuest("hun_tsk_001")!;
            var itemTask = Index.FindQuest("hun_tsk_004")!;

            killTask.StateCount.Should().Be(2);
            killTask.CollectItemObjectiveStates.Should().BeEmpty();
            itemTask.StateCount.Should().Be(1);
            itemTask.CollectItemObjectiveStates.Should().Equal(1);
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
            quest.CollectItemObjectiveStates.Should().Equal(1);
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
            quest.CollectItemObjectiveStates.Should().Equal(1);
            quest.PrerequisiteQuestIds.Should().BeEmpty();
        }

        [Test]
        public void NonCollectionObjectivesAreNotReportedAsItemHandIns()
        {
            var quest = Index.FindQuest("voritor_lizard_threat");

            quest.Should().NotBeNull();
            quest!.CollectItemObjectiveStates.Should().BeEmpty();
        }

        [Test]
        public void HelperBuiltGuildQuestsAreIndexed()
        {
            var agriculture = Index.FindQuest("agr_tsk_001");

            agriculture.Should().NotBeNull();
            agriculture!.StateCount.Should().Be(1);
            agriculture.IsRepeatable.Should().BeTrue();
            agriculture.SourceFile.Should().Be("AgricultureGuildQuestDefinition.cs");

            Index.FindQuest("eng_tsk_001").Should().NotBeNull();
            Index.FindQuest("fab_tsk_001").Should().NotBeNull();
            Index.FindQuest("smth_tsk_001").Should().NotBeNull();
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

        /// <summary>
        /// the_manda_leader (ViscaraQuestDefinition.TheMandalorianLeader) calls .HasRewardSelection()
        /// on its builder chain. ReachabilityEvaluator reads this flag to avoid marking the quest
        /// completed the moment its final action-advance-quest runs, since QuestDetail.Advance instead
        /// opens QuestRewardSelectionDialog and leaves it on its final state until the player actually
        /// picks a reward.
        /// </summary>
        [Test]
        public void AQuestWithRewardSelectionReportsSo()
        {
            var quest = Index.FindQuest("the_manda_leader");

            quest.Should().NotBeNull();
            quest!.HasRewardSelection.Should().BeTrue();
        }

        [Test]
        public void AQuestWithoutRewardSelectionReportsSo()
        {
            var quest = Index.FindQuest("field_tinctures");

            quest.Should().NotBeNull();
            quest!.HasRewardSelection.Should().BeFalse();
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
