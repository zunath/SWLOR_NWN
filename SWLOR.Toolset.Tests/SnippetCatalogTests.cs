using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Covers the snippet registry the editor reads instead of hardcoding SWLOR's conversation
    /// logic. The corpus tests at the bottom are the ones that matter: every key and every argument
    /// count in the module has to be something the registry recognises, or the editor would be
    /// showing a writer something the game will not run.
    /// </summary>
    public class SnippetCatalogTests
    {
        private static readonly SnippetCatalog Catalog = SnippetCatalog.Build();

        [Test]
        public void TheRegistryHoldsEverySnippetTheGameDefines()
        {
            Catalog.All.Should().HaveCount(33);
            Catalog.Conditions.Should().HaveCount(15);
            Catalog.Actions.Should().HaveCount(18);
        }

        [Test]
        public void EverySnippetDeclaresADescriptionAndASentence()
        {
            foreach (var snippet in Catalog.All)
            {
                snippet.Description.Should().NotBeEmpty($"{snippet.Key} needs a tooltip");
                snippet.Phrase.Should().NotBeEmpty($"{snippet.Key} needs a sentence a writer can read");
            }
        }

        [Test]
        public void EveryPlaceholderNamesADeclaredArgument()
        {
            // A phrase referring to an argument that does not exist would render as a gap in the
            // sentence, which is worse than showing the key.
            foreach (var snippet in Catalog.All)
            {
                foreach (var phrase in new[] { snippet.Phrase, snippet.NegatedPhrase })
                {
                    foreach (var placeholder in Placeholders(phrase))
                    {
                        snippet.Arguments.Select(argument => argument.Name).Should().Contain(placeholder,
                            $"{snippet.Key} refers to {{{placeholder}}}");
                    }
                }
            }
        }

        [Test]
        public void EveryArgumentIsMentionedByItsSnippetsSentence()
        {
            foreach (var snippet in Catalog.All)
            {
                foreach (var argument in snippet.Arguments)
                {
                    if (argument.IsOptional)
                        continue;

                    Placeholders(snippet.Phrase).Should().Contain(argument.Name,
                        $"{snippet.Key} takes {argument.Name} but never mentions it");
                }
            }
        }

        [Test]
        public void EveryConditionOffersANegatedSentence()
        {
            // The corpus negates three keys today, but any condition can be negated with a '!' and
            // the fallback wording ("not: ...") reads badly enough to be worth avoiding everywhere.
            foreach (var snippet in Catalog.Conditions)
                snippet.NegatedPhrase.Should().NotBeEmpty($"{snippet.Key} can be written as !{snippet.Key}");
        }

        // ---------- sentences ----------

        [Test]
        public void AQuestStateConditionReadsAsASentence()
        {
            var snippet = Catalog.Find("condition-on-quest-state")!;

            snippet.ToSentence(new[] { "field_tinctures", "2" })
                .Should().Be("the player is on step 2 of field_tinctures");
        }

        [Test]
        public void ALookupTurnsIdsIntoNames()
        {
            var snippet = Catalog.Find("condition-on-quest-state")!;

            var sentence = snippet.ToSentence(
                new[] { "field_tinctures", "2" },
                negated: false,
                display: (argument, value) =>
                    argument.Type == SnippetArgumentType.QuestId && value == "field_tinctures"
                        ? "Field Tinctures"
                        : null);

            sentence.Should().Be("the player is on step 2 of Field Tinctures");
        }

        [Test]
        public void ANegatedConditionUsesItsOwnWording()
        {
            var snippet = Catalog.Find("!condition-has-quest")!;

            snippet.ToSentence(new[] { "field_tinctures" }, negated: true)
                .Should().Be("the player is not doing field_tinctures");
        }

        [Test]
        public void RepeatedArgumentsCollapseIntoOneReadableList()
        {
            var snippet = Catalog.Find("condition-completed-quest")!;

            snippet.ToSentence(new[] { "a", "b", "c" })
                .Should().Be("the player has finished a, b and c");
        }

        [TestCase(
            "condition-any-skill",
            false,
            "the player has Force at rank 10 or better or the player has Devices at rank 5 or better")]
        [TestCase(
            "condition-any-skill",
            true,
            "the player has no skill in Force at rank 10 and the player has no skill in Devices at rank 5")]
        [TestCase(
            "condition-all-skills",
            false,
            "the player has every one of Force at rank 10 or better and the player has every one of Devices at rank 5 or better")]
        [TestCase(
            "condition-all-skills",
            true,
            "the player is short of rank 10 in at least one of Force or the player is short of rank 5 in at least one of Devices")]
        public void RepeatedSkillRanksStayPaired(string key, bool negated, string expected)
        {
            var snippet = Catalog.Find(key)!;

            snippet.ToSentence(new[] { "Force", "10", "Devices", "5" }, negated)
                .Should().Be(expected);
        }

        [Test]
        public void AMissingArgumentIsMarkedRatherThanSilentlyDropped()
        {
            var snippet = Catalog.Find("condition-has-quest")!;

            snippet.ToSentence(Array.Empty<string>()).Should().Be("the player is doing ⟨questId⟩");
        }

        // ---------- argument shapes ----------

        [Test]
        public void ARepeatingArgumentKeepsItsTypePastTheDeclaredList()
        {
            var snippet = Catalog.Find("condition-any-skill")!;

            // Declared as a skill/rank pair that repeats, so index 2 is a skill again.
            snippet.ArgumentAt(0)!.Type.Should().Be(SnippetArgumentType.SkillId);
            snippet.ArgumentAt(1)!.Type.Should().Be(SnippetArgumentType.SkillRank);
            snippet.ArgumentAt(2)!.Type.Should().Be(SnippetArgumentType.SkillId);
            snippet.ArgumentAt(3)!.Type.Should().Be(SnippetArgumentType.SkillRank);
        }

        [Test]
        public void ThePairedSkillSnippetsRejectAnOddArgumentCount()
        {
            var snippet = Catalog.Find("condition-all-skills")!;

            snippet.IsValidArgumentCount(2).Should().BeTrue();
            snippet.IsValidArgumentCount(3).Should().BeFalse();
            snippet.IsValidArgumentCount(4).Should().BeTrue();
            snippet.IsValidArgumentCount(1).Should().BeFalse();
        }

        [Test]
        public void TheStoreSnippetWorksWithNoArgumentsAtAll()
        {
            var snippet = Catalog.Find("action-open-store")!;

            snippet.MinimumArgumentCount.Should().Be(0);
            snippet.IsValidArgumentCount(0).Should().BeTrue();
            snippet.IsValidArgumentCount(1).Should().BeTrue();
            snippet.IsValidArgumentCount(2).Should().BeFalse();
        }

        // ---------- the corpus ----------

        [Test]
        public void EverySnippetKeyUsedInTheModuleIsOneTheGameStillDefines()
        {
            var unknown = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (file, key, _) in ModuleSnippetUsages())
            {
                if (!Catalog.IsKnown(key))
                    unknown.Add($"{file}: {key}");
            }

            unknown.Should().BeEmpty();
        }

        [Test]
        public void NoSnippetUsageInTheModuleIsShortOfArguments()
        {
            // The gate the runtime now applies before invoking a snippet. It must reject nothing
            // that works today, so this has to stay empty: a name appearing here is a conversation
            // the change would break.
            var rejected = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (file, key, arguments) in ModuleSnippetUsages())
            {
                var snippet = Catalog.Find(key);
                if (snippet == null || snippet.Arguments.Count == 0)
                    continue;

                if (!snippet.HasEnoughArguments(arguments.Length))
                    rejected.Add($"{file}: {key} with {arguments.Length} argument(s)");
            }

            rejected.Should().BeEmpty();
        }

        /// <summary>
        /// The one place in the module that passes a snippet more arguments than it reads.
        /// <c>condition-has-quest</c> takes only a quest id, so the trailing "1" is ignored and the
        /// guard passes on any state rather than state 1 — most likely a
        /// <c>condition-on-quest-state</c> that was mistyped. It runs, so the runtime leaves it
        /// alone; the editor should point at it.
        /// </summary>
        private static readonly string[] KnownSurplusArguments =
        {
            "rorrska_buvvien.dlg.json: condition-has-quest with 2 argument(s)"
        };

        [Test]
        public void SurplusArgumentsAreVisibleToTheEditorWithoutBreakingTheGame()
        {
            var surplus = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (file, key, arguments) in ModuleSnippetUsages())
            {
                var snippet = Catalog.Find(key);
                if (snippet == null || snippet.Arguments.Count == 0)
                    continue;

                if (snippet.HasEnoughArguments(arguments.Length) && !snippet.IsValidArgumentCount(arguments.Length))
                    surplus.Add($"{file}: {key} with {arguments.Length} argument(s)");
            }

            surplus.Should().BeEquivalentTo(KnownSurplusArguments);
        }

        [Test]
        public void EverySnippetUsageInTheModuleRendersAsASentence()
        {
            foreach (var (file, key, arguments) in ModuleSnippetUsages())
            {
                var snippet = Catalog.Find(key)!;
                var sentence = snippet.ToSentence(arguments, key.StartsWith('!'));

                sentence.Should().NotBeNullOrWhiteSpace($"{file}: {key}");
                sentence.Should().NotContain("⟨", $"{file}: {key} left a placeholder unfilled");
            }
        }

        private static IEnumerable<(string File, string Key, string[] Arguments)> ModuleSnippetUsages()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "dlg");
            foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
            {
                var document = DlgDocument.Load(path);
                var file = Path.GetFileName(path);

                foreach (var link in document.AllLinks())
                {
                    foreach (var condition in link.Conditions)
                        yield return (file, condition.Key, condition.Arguments);
                }

                foreach (var node in document.Entries.Concat(document.Replies))
                {
                    foreach (var action in node.Actions)
                    {
                        if (action.IsOncePerPlayerMarker)
                            continue;

                        yield return (file, action.Key, action.Arguments);
                    }
                }
            }
        }

        private static IReadOnlyList<string> Placeholders(string phrase)
        {
            var names = new List<string>();
            var index = 0;
            while (index < phrase.Length)
            {
                var open = phrase.IndexOf('{', index);
                if (open < 0)
                    break;

                var close = phrase.IndexOf('}', open);
                if (close < 0)
                    break;

                names.Add(phrase[(open + 1)..close]);
                index = close + 1;
            }

            return names;
        }
    }
}
