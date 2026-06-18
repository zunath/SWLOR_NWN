using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PlayerGuideViewModel : GuiViewModelBase<PlayerGuideViewModel, GuiPayloadBase>
    {
        private static readonly IReadOnlyList<PlayerGuideTopic> Topics = BuildTopics();

        private readonly List<int> _filteredTopicIndexes = new();
        private readonly List<int> _relatedTopicIndexes = new();

        private int SelectedTopicIndex { get; set; }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> TopicButtonTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> TopicSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> TopicTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string SelectedTopicCategory
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedTopicName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedTopicSummary
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedArticleBody
        {
            get => Get<string>();
            set => Set(value);
        }

        public string QuestionSummaryText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> RelatedTopicTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> RelatedTopicTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            SelectedTopicIndex = -1;
            LoadTopics();
            SelectTopicByTopicIndex(0);

            WatchOnClient(model => model.SearchText);
        }

        private void LoadTopics()
        {
            var topicButtonTexts = new GuiBindingList<string>();
            var topicSelections = new GuiBindingList<bool>();
            var topicTooltips = new GuiBindingList<string>();
            var search = SearchText ?? string.Empty;

            _filteredTopicIndexes.Clear();

            for (var index = 0; index < Topics.Count; index++)
            {
                var topic = Topics[index];
                if (!MatchesSearch(topic, search))
                    continue;

                _filteredTopicIndexes.Add(index);
                topicButtonTexts.Add(topic.Name);
                topicSelections.Add(index == SelectedTopicIndex);
                topicTooltips.Add(topic.Summary);
            }

            TopicButtonTexts = topicButtonTexts;
            TopicSelections = topicSelections;
            TopicTooltips = topicTooltips;
        }

        private static bool MatchesSearch(PlayerGuideTopic topic, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return topic.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   topic.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   topic.Summary.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   topic.Blocks.Any(block =>
                       block.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                       block.Body.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                   topic.Questions.Any(question =>
                       question.Question.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                       question.Answer.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        private void SelectTopicByTopicIndex(int topicIndex)
        {
            if (topicIndex < 0 || topicIndex >= Topics.Count)
                topicIndex = 0;

            if (!_filteredTopicIndexes.Contains(topicIndex))
            {
                SearchText = string.Empty;
                LoadTopics();
            }

            var oldFilteredIndex = _filteredTopicIndexes.IndexOf(SelectedTopicIndex);
            if (oldFilteredIndex >= 0 && oldFilteredIndex < TopicSelections.Count)
            {
                TopicSelections[oldFilteredIndex] = false;
            }

            SelectedTopicIndex = topicIndex;
            var filteredIndex = _filteredTopicIndexes.IndexOf(topicIndex);
            if (filteredIndex >= 0 && filteredIndex < TopicSelections.Count)
            {
                TopicSelections[filteredIndex] = true;
            }

            var topic = Topics[topicIndex];
            SelectedTopicCategory = topic.Category.ToUpperInvariant();
            SelectedTopicName = topic.Name;
            SelectedTopicSummary = topic.Summary;
            SelectedArticleBody = BuildArticleBody(topic.Blocks);
            LoadQuestions(topic);
            LoadRelatedTopics(topic);
        }

        private void LoadQuestions(PlayerGuideTopic topic)
        {
            var sb = new StringBuilder();

            foreach (var question in topic.Questions)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine($"Q: {question.Question}");
                sb.AppendLine($"A: {question.Answer}");
            }

            QuestionSummaryText = sb.ToString().TrimEnd();
        }

        private void LoadRelatedTopics(PlayerGuideTopic topic)
        {
            var relatedTopicTexts = new GuiBindingList<string>();
            var relatedTopicTooltips = new GuiBindingList<string>();

            _relatedTopicIndexes.Clear();

            foreach (var relatedTopicName in topic.RelatedTopics)
            {
                var topicIndex = FindTopicIndex(relatedTopicName);
                if (topicIndex < 0)
                    continue;

                var relatedTopic = Topics[topicIndex];
                _relatedTopicIndexes.Add(topicIndex);
                relatedTopicTexts.Add(relatedTopic.Name);
                relatedTopicTooltips.Add(relatedTopic.Summary);
            }

            RelatedTopicTexts = relatedTopicTexts;
            RelatedTopicTooltips = relatedTopicTooltips;
        }

        private static int FindTopicIndex(string topicName)
        {
            for (var index = 0; index < Topics.Count; index++)
            {
                if (Topics[index].Name.Equals(topicName, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            return -1;
        }

        private static string BuildArticleBody(IReadOnlyList<ArticleBlock> blocks)
        {
            var sb = new StringBuilder();

            foreach (var block in blocks)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine(block.Title.ToUpperInvariant());
                sb.AppendLine(block.Body);
            }

            return sb.ToString().TrimEnd();
        }

        public Action OnClickSearch() => () =>
        {
            LoadTopics();

            if (_filteredTopicIndexes.Count > 0)
                SelectTopicByTopicIndex(_filteredTopicIndexes[0]);
            else
                ShowNoSearchResults();
        };

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            LoadTopics();
            SelectTopicByTopicIndex(0);
        };

        public Action OnClickTopic() => () =>
        {
            var filteredIndex = NuiGetEventArrayIndex();
            if (filteredIndex < 0 || filteredIndex >= _filteredTopicIndexes.Count)
                return;

            SelectTopicByTopicIndex(_filteredTopicIndexes[filteredIndex]);
        };

        public Action OnClickRelatedTopic() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _relatedTopicIndexes.Count)
                return;

            SelectTopicByTopicIndex(_relatedTopicIndexes[index]);
        };

        private void ShowNoSearchResults()
        {
            SelectedTopicIndex = -1;
            SelectedTopicCategory = "BROWSE";
            SelectedTopicName = "No Matching Topics";
            SelectedTopicSummary = "No guide topics matched your search. Clear the search to return to the full guide.";
            SelectedArticleBody = "Try searching for skills, perks, decay, refunds, abilities, attributes, or windows.";
            QuestionSummaryText = string.Empty;
            RelatedTopicTexts = new GuiBindingList<string>();
            RelatedTopicTooltips = new GuiBindingList<string>();
        }

        private static IReadOnlyList<PlayerGuideTopic> BuildTopics()
        {
            return new List<PlayerGuideTopic>
            {
                new(
                    "Common Questions",
                    "Core",
                    "Start here for skills, perks, decay, refunds, XP debt, and the windows players ask about most.",
                    "Skills, perks, decay, refunds",
                    new[]
                    {
                        new ArticleBlock("Skills",
                            "Skills gain XP individually. Ranks in skills that contribute to the skill cap create Skill Points for perks until you have earned 400 total SP. Every 10 total SP grants 1 Ability Point, up to 40 AP."),
                        new ArticleBlock("Perks",
                            "Perk ranks cost SP. A perk rank can have requirements, grant feats or stat bonuses, and run purchase, refund, equip, or unequip effects."),
                        new ArticleBlock("Skill Decay",
                            "Once you are at 400 ranks in cap-contributing skills, a new rank in one cap-contributing skill can randomly lower another eligible cap-contributing skill by 1 rank and reset that decayed skill's XP to 0."),
                        new ArticleBlock("Refunds and XP Debt",
                            "Manual perk refunds remove the selected perk completely, return all SP paid for its purchased ranks, consume 1 Perk Refund Token, and start a 1 hour real-time cooldown. Earned skill XP pays down XP debt before the remainder can apply to a skill.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do skills work?", "Skill XP raises individual skills; cap-contributing ranks create SP and every 10 total SP grants AP."),
                        new QuestionAnswer("What does skill decay mean?", "At 400 cap-contributing ranks, gaining another eligible rank can lower another unlocked eligible skill by 1 rank."),
                        new QuestionAnswer("How do perk refunds work?", "Manual refunds return all SP paid for the selected perk, consume a token, and start a 1 hour cooldown.")
                    },
                    new[] { "Skills", "Perks", "Skill Decay", "Perk Refunds" }),

                new(
                    "Skills",
                    "Core",
                    "Skill XP, SP, AP, XP debt, Social XP bonuses, and RP XP distribution.",
                    "Skill XP, SP, AP",
                    new[]
                    {
                        new ArticleBlock("Skills Window",
                            "The Skills window shows available XP, XP debt, each visible skill's rank, title, progress to the next rank, raw XP, description, decay lock state, and RP XP distribution button state."),
                        new ArticleBlock("Skill Cap",
                            "The character skill cap is 400 total ranks across skills that contribute to the skill cap."),
                        new ArticleBlock("SP and AP",
                            "When a cap-contributing skill ranks up, you gain 1 unallocated SP and 1 total SP until Total SP reaches 400. Every 10 total SP grants 1 unallocated AP, up to 40 AP."),
                        new ArticleBlock("XP Adjustments",
                            "Skill XP can be adjusted before it is applied. Positive Social adds 2.5 percent XP per point, XP percent stat adjustments and DM XP bonuses can add more, and a 30 percent henchman penalty can apply when that XP source uses the henchman penalty."),
                        new ArticleBlock("XP Debt and RP XP",
                            "XP debt is paid first. If earned XP is less than your debt, all of that XP removes debt and none goes to the skill. RP XP distribution is enabled only when you have unallocated XP and is blocked in the character migration area.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What is the skill cap?", "400 total ranks across skills that contribute to the skill cap."),
                        new QuestionAnswer("How do I get AP?", "Every 10 total SP grants 1 unallocated AP, up to 40."),
                        new QuestionAnswer("Why did XP go to debt?", "XP debt is paid down before skill XP is applied.")
                    },
                    new[] { "Skill Decay", "Perks", "Attributes", "Useful Windows" }),

                new(
                    "Skill Decay",
                    "Core",
                    "What can decay, what locking does, and why decay can affect perks.",
                    "Locks and cap behavior",
                    new[]
                    {
                        new ArticleBlock("When Decay Happens",
                            "Decay can happen when a cap-contributing skill gains a rank while your total cap-contributing ranks are at the 400 skill cap."),
                        new ArticleBlock("Eligible Skills",
                            "The decay pool contains other skills that contribute to the skill cap, are not locked, are not the skill currently gaining XP, and have rank greater than 0."),
                        new ArticleBlock("Lock States",
                            "LOCKED excludes a cap-contributing skill from the random decay pool. UNLOCKED means it can be selected if otherwise eligible. N/A means the skill does not contribute to the skill cap, so it cannot use the decay lock button."),
                        new ArticleBlock("No Available Decay",
                            "If you are already at the cap and no other eligible skill can decay, a cap-contributing skill cannot keep advancing through normal skill XP."),
                        new ArticleBlock("Perk Requirement Effects",
                            "After decay, the server checks perks tied to that skill. If you no longer meet a perk's requirements, the perk is reduced to your effective level and SP is returned for removed levels.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What can decay?", "Only other unlocked cap-contributing skills with rank greater than 0."),
                        new QuestionAnswer("What does LOCKED mean?", "The skill is excluded from the random decay pool."),
                        new QuestionAnswer("Why did a perk change?", "Decay can make you fail a skill requirement, causing an automatic perk level refund.")
                    },
                    new[] { "Skills", "Perk Refunds", "Perks", "Common Questions" }),

                new(
                    "Perks",
                    "Progression",
                    "Perk ranks, SP prices, requirements, colors, beast perks, and granted effects.",
                    "SP prices and requirements",
                    new[]
                    {
                        new ArticleBlock("Costs and Details",
                            "Each perk level has its own SP price. The Perks window shows the next upgrade price in the Buy Upgrade button and in the selected perk details."),
                        new ArticleBlock("Green and Red States",
                            "The list color is based on whether the next upgrade's requirements pass. The Buy button is enabled only when the next upgrade exists, requirements pass, and you have enough unallocated SP."),
                        new ArticleBlock("What Perks Can Do",
                            "Perk levels can grant feats and stat bonuses. Perks can also define requirements and purchase, refund, equip, or unequip triggers."),
                        new ArticleBlock("Requirements",
                            "Perk builder support includes skill, quest, character type, unlock, must-have perk, cannot-have perk, beast level, and beast role requirements."),
                        new ArticleBlock("Beast Perks",
                            "If you have an active beast, the Perks window can switch to Beast Perks. Beast mode uses the beast's unallocated SP and beast level instead of the player's SP total.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why is a perk red?", "Its next rank requirement check failed."),
                        new QuestionAnswer("When can I buy?", "When the next rank exists, requirements pass, and you have enough SP."),
                        new QuestionAnswer("What are beast perks?", "A Perks window mode that uses the active beast's SP and level.")
                    },
                    new[] { "Perk Refunds", "Skills", "Abilities", "Skill Decay" }),

                new(
                    "Perk Refunds",
                    "Progression",
                    "Manual refunds, token use, cooldowns, beast refunds, and automatic decay refunds.",
                    "Tokens and cooldowns",
                    new[]
                    {
                        new ArticleBlock("Manual Refund",
                            "A manual refund removes the selected perk from the player or active beast, returns the total SP paid for all purchased ranks of that perk, and removes granted feats from the target."),
                        new ArticleBlock("Token and Cooldown",
                            "You need at least 1 Perk Refund Token. A successful manual refund consumes 1 token and sets your next refund availability to 1 hour from the refund time."),
                        new ArticleBlock("Refund Checks",
                            "Individual perks can define a refund requirement check, and that check can stop the refund with a message."),
                        new ArticleBlock("After Refund",
                            "The server saves the player, publishes a perk refund refresh, removes the perk's granted feats and status effects, runs refund triggers, exports the character, and refreshes the Perks window."),
                        new ArticleBlock("Automatic Decay Refund",
                            "A decay refund happens only when skill decay drops your effective perk level. It returns SP for each removed level, removes those level feats, syncs remaining granted feats, removes related status effects, runs refund triggers, and exports the character. It does not use the manual token or cooldown path.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What does refund return?", "All SP paid for all purchased ranks of the selected perk."),
                        new QuestionAnswer("What does refund cost?", "A successful manual refund consumes 1 Perk Refund Token."),
                        new QuestionAnswer("Does decay use a token?", "No. Decay refunds use the automatic skill-requirement path.")
                    },
                    new[] { "Perks", "Skill Decay", "Skills", "Common Questions" }),

                new(
                    "Abilities",
                    "Combat",
                    "Ability descriptions, FP, STM, recast groups, cooldowns, and granted active feats.",
                    "FP, STM, recast",
                    new[]
                    {
                        new ArticleBlock("Active Ability Source",
                            "Perk levels can grant feats. When a perk purchase grants active ability feats, the Perks window syncs those feats and attempts to place newly available active ability feats on the hotbar."),
                        new ArticleBlock("Description Fields",
                            "Ability descriptions are overridden to show the ability name, FP requirement, STM requirement, recast seconds, and the perk level description."),
                        new ArticleBlock("Cooldown Checks",
                            "Before activation, the ability system checks the ability's recast group. If the recast time has not expired, it tells you how long to wait."),
                        new ArticleBlock("Cooldown Application",
                            "After ability use, the server applies recast delay to the ability's recast group. Player recast times are saved to the player record and the cooldown visual is applied."),
                        new ArticleBlock("Recast Groups",
                            "Ability definitions can assign a recast group and delay. Recast groups have cached player-facing short names.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Where do abilities come from?", "Perk levels can grant active ability feats."),
                        new QuestionAnswer("What is Recast?", "The cooldown seconds shown in the ability description."),
                        new QuestionAnswer("Why can't I use it yet?", "Its recast group still has time remaining.")
                    },
                    new[] { "Perks", "Attributes", "Useful Windows", "Common Questions" }),

                new(
                    "Attributes",
                    "Character",
                    "The current in-game attribute descriptions from the server TLK overrides.",
                    "AP choices and effects",
                    new[]
                    {
                        new ArticleBlock("Might",
                            "Improves damage dealt by melee weapons and increases carrying capacity. It is listed with Vibroblade, Heavy Vibroblade, Spear, Twin Blade, Katar, Staff, Smithery, and Gathering."),
                        new ArticleBlock("Perception",
                            "Improves damage dealt by ranged and finesse weapons and increases physical accuracy. It is listed with Vibroknife, Lightsaber, Saberstaff, Katar, Pistol, Rifle, Fabrication, and Devices."),
                        new ArticleBlock("Vitality",
                            "Improves maximum hit points and reduces damage received. It also improves physical defense, natural HP/FP/STM regeneration, and rest recovery."),
                        new ArticleBlock("Willpower",
                            "Improves force attack, force defense, maximum force points, and First Aid capabilities. It also improves Force abilities and ship combat modules."),
                        new ArticleBlock("Agility",
                            "Improves ranged, finesse, and throwing accuracy, evasion, maximum stamina, ship combat modules, and reduces critical hit chance against you."),
                        new ArticleBlock("Social",
                            "Improves XP gain and leadership capabilities. It also improves guild point acquisition, quest credit rewards, XP debt reduction on death, and reduces ship repair bills.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I get AP?", "Every 10 total SP grants 1 AP, up to 40."),
                        new QuestionAnswer("What helps XP gain?", "Social improves XP gain."),
                        new QuestionAnswer("What helps stamina?", "Agility increases maximum stamina.")
                    },
                    new[] { "Skills", "Abilities", "Perks", "Common Questions" }),

                new(
                    "Useful Windows",
                    "Interface",
                    "Quick references to commands already registered by the character command list.",
                    "Menu shortcuts",
                    new[]
                    {
                        new ArticleBlock("/skills",
                            "Toggles the Skills menu."),
                        new ArticleBlock("/perk or /perks",
                            "Toggles the Perks menu."),
                        new ArticleBlock("/recipe or /recipes",
                            "Toggles the Recipes menu."),
                        new ArticleBlock("/resetwindows",
                            "Closes open SWLOR windows, clears saved window geometry, restores default window geometry, and reports that positions and sizes were reset.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I open Skills?", "Use /skills."),
                        new QuestionAnswer("How do I open Perks?", "Use /perk or /perks."),
                        new QuestionAnswer("How do I reset windows?", "Use /resetwindows.")
                    },
                    new[] { "Skills", "Perks", "Common Questions", "Abilities" })
            };
        }

        private sealed class PlayerGuideTopic
        {
            public string Name { get; }
            public string Category { get; }
            public string Summary { get; }
            public string RailSummary { get; }
            public IReadOnlyList<ArticleBlock> Blocks { get; }
            public IReadOnlyList<QuestionAnswer> Questions { get; }
            public IReadOnlyList<string> RelatedTopics { get; }

            public PlayerGuideTopic(
                string name,
                string category,
                string summary,
                string railSummary,
                IReadOnlyList<ArticleBlock> blocks,
                IReadOnlyList<QuestionAnswer> questions,
                IReadOnlyList<string> relatedTopics)
            {
                Name = name;
                Category = category;
                Summary = summary;
                RailSummary = railSummary;
                Blocks = blocks;
                Questions = questions;
                RelatedTopics = relatedTopics;
            }
        }

        private sealed class ArticleBlock
        {
            public string Title { get; }
            public string Body { get; }

            public ArticleBlock(string title, string body)
            {
                Title = title;
                Body = body;
            }
        }

        private sealed class QuestionAnswer
        {
            public string Question { get; }
            public string Answer { get; }

            public QuestionAnswer(string question, string answer)
            {
                Question = question;
                Answer = answer;
            }
        }
    }
}
