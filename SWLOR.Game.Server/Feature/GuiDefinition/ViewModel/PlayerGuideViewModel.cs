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
                    "Start here for skills, AP, perks, decay, refunds, XP debt, rebuilds, and useful windows.",
                    "Skills, AP, perks",
                    new[]
                    {
                        new ArticleBlock("Skills",
                            "Skills gain XP individually. Skill ranks create Skill Points for perks until you have earned 400 total SP. Languages do not count toward this 400-rank limit."),
                        new ArticleBlock("Ability Points (AP)",
                            "Every 10 total SP grants 1 Ability Point, up to 40 AP. Spend AP from the Character Sheet to improve attributes such as Might, Perception, Vitality, Willpower, Agility, and Social."),
                        new ArticleBlock("Perks",
                            "Perk ranks cost SP. Perks can require certain progress before you buy the next rank, and they can grant active abilities, passive bonuses, or other effects."),
                        new ArticleBlock("Skill Decay",
                            "Once you are at 400 skill ranks, a new rank can randomly lower another eligible unlocked skill by 1 rank and reset that skill's XP to 0. Languages do not count toward this limit."),
                        new ArticleBlock("Perk Refunds",
                            "Manual perk refunds remove the selected perk completely, return all SP paid for its purchased ranks, consume 1 Perk Refund Token, and start a 1 hour real-time wait before another manual refund."),
                        new ArticleBlock("XP Debt",
                            "Earned skill XP pays down XP debt before the remainder can apply to a skill. If your debt uses all of the XP, that skill gains no XP from that reward.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do skills work?", "Skill XP raises individual skills. Skill ranks create SP until 400 total SP; languages do not count toward that limit."),
                        new QuestionAnswer("How do I use AP?", "Earn 1 AP every 10 total SP, then spend AP from the Character Sheet to improve attributes."),
                        new QuestionAnswer("What does skill decay mean?", "At 400 skill ranks, gaining another eligible rank can lower another unlocked eligible skill by 1 rank."),
                        new QuestionAnswer("How do perk refunds work?", "Manual refunds return all SP paid for the selected perk, consume a token, and start a 1 hour wait.")
                    },
                    new[] { "Skills", "Attributes", "Perks", "Skill Decay", "Perk Refunds", "XP Debt", "Rebuilds" }),

                new(
                    "Skills",
                    "Core",
                    "Skill XP, SP, AP, XP debt, Social XP bonuses, and the 400-rank limit.",
                    "Skill XP, SP, AP",
                    new[]
                    {
                        new ArticleBlock("Skills Window",
                            "The Skills window shows available XP, XP debt, each visible skill's rank, title, progress to the next rank, raw XP, description, decay lock state, and RP XP distribution button state."),
                        new ArticleBlock("Skill Limit",
                            "A character can earn up to 400 total ranks from skills that count toward the skill limit. Languages do not count toward this limit."),
                        new ArticleBlock("SP and AP",
                            "When a skill that counts toward the limit ranks up, you gain 1 unallocated SP and 1 total SP until Total SP reaches 400. Every 10 total SP grants 1 unallocated AP, up to 40 AP."),
                        new ArticleBlock("XP Adjustments",
                            "Skill XP can be adjusted before it is applied. Positive Social adds 2.5 percent XP per point, XP percent stat adjustments and DM XP bonuses can add more, and a 30 percent henchman penalty can apply when that XP source uses the henchman penalty."),
                        new ArticleBlock("XP Debt",
                            "XP debt is paid first. If earned XP is less than your debt, all of that XP removes debt and none goes to the skill.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What is the skill limit?", "400 total ranks from skills that count toward the limit. Languages do not count."),
                        new QuestionAnswer("How do I get AP?", "Every 10 total SP grants 1 unallocated AP, up to 40."),
                        new QuestionAnswer("Why did XP go to debt?", "XP debt is paid down before skill XP is applied.")
                    },
                    new[] { "Attributes", "XP Debt", "Skill Decay", "Perks", "Useful Windows" }),

                new(
                    "Skill Decay",
                    "Core",
                    "What can decay, what locking does, and why decay can affect perks.",
                    "Locks and 400-rank limit",
                    new[]
                    {
                        new ArticleBlock("When Decay Happens",
                            "Decay can happen when a skill gains a rank while your total ranks from skills are at the 400 skill limit. Languages do not count toward this limit."),
                        new ArticleBlock("Eligible Skills",
                            "The decay pool contains other skills that count toward the 400-rank limit, are not locked, are not the skill currently gaining XP, and have rank greater than 0."),
                        new ArticleBlock("Lock States",
                            "LOCKED excludes a skill from the random decay pool. UNLOCKED means it can be selected if otherwise eligible. N/A means the skill does not count toward the 400-rank limit, so it cannot use the decay lock button."),
                        new ArticleBlock("No Available Decay",
                            "If you are already at the limit and no other eligible skill can decay, that skill cannot gain XP from normal skill XP rewards in this circumstance."),
                        new ArticleBlock("Perk Requirement Effects",
                            "After decay, perks tied to that skill are checked. If you no longer meet a perk's requirements, the perk is reduced to your effective level and SP is returned for removed levels.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What can decay?", "Only other unlocked skills that count toward the 400-rank limit and have rank greater than 0."),
                        new QuestionAnswer("What does LOCKED mean?", "The skill is excluded from the random decay pool."),
                        new QuestionAnswer("Why did a perk change?", "Decay can make you fail a skill requirement, causing an automatic perk level refund.")
                    },
                    new[] { "Skills", "XP Debt", "Perk Refunds", "Perks", "Common Questions" }),

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
                            "Perk levels can grant active abilities, passive bonuses, and other effects."),
                        new ArticleBlock("Requirements",
                            "A perk may require a skill rank, quest completion, character type, unlock, another perk, not having a conflicting perk, or beast level or role progress before the next rank can be purchased."),
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
                    "Manual refunds, token use, wait time, beast refunds, and automatic decay refunds.",
                    "Tokens and wait time",
                    new[]
                    {
                        new ArticleBlock("Getting Tokens",
                            "Buy a Perk Refund Tome from the Training Store, then use the tome to add 1 Perk Refund Token to your collection. The tome is consumed when used, and your token collection caps at 99."),
                        new ArticleBlock("Manual Refund",
                            "A manual refund removes the selected perk from the player or active beast, returns the total SP paid for all purchased ranks of that perk, and removes granted feats from the target."),
                        new ArticleBlock("Token and Wait Time",
                            "You need at least 1 Perk Refund Token. A successful manual refund consumes 1 token and makes you wait 1 real-time hour before another manual perk refund."),
                        new ArticleBlock("Refund Checks",
                            "Some perks may block a manual refund and show a message explaining why."),
                        new ArticleBlock("After Refund",
                            "After a successful refund, granted feats and effects from that perk are removed, any hotbar entries for those feats are cleared, your character is saved, and the Perks window updates."),
                        new ArticleBlock("Automatic Decay Refund",
                            "A decay refund happens only when skill decay drops your effective perk level. It returns SP for removed ranks and does not use a manual refund token or the 1 hour manual refund wait.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What does refund return?", "All SP paid for all purchased ranks of the selected perk."),
                        new QuestionAnswer("How do I get tokens?", "Buy a Perk Refund Tome from the Training Store and use it."),
                        new QuestionAnswer("What does refund cost?", "A successful manual refund consumes 1 Perk Refund Token and starts a 1 hour wait."),
                        new QuestionAnswer("Does decay use a token?", "No. Decay refunds are automatic and do not use a manual refund token.")
                    },
                    new[] { "Perks", "Skill Decay", "Skills", "XP Debt", "Common Questions" }),

                new(
                    "XP Debt",
                    "Core",
                    "Why earned skill XP may pay debt before raising a skill.",
                    "Why XP may not apply",
                    new[]
                    {
                        new ArticleBlock("What XP Debt Does",
                            "When you have XP debt, earned skill XP pays down that debt before any remaining XP can apply to the skill."),
                        new ArticleBlock("When No Skill XP Is Gained",
                            "If the XP reward is less than or equal to your current debt, the reward only lowers debt and the skill gains no XP from that reward."),
                        new ArticleBlock("Where To See It",
                            "The Skills window shows your current XP Debt value."),
                        new ArticleBlock("Reducing Death Debt",
                            "Social improves XP gain and also helps reduce XP debt from death.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why did my skill not move?", "Your XP reward may have been fully spent reducing XP debt."),
                        new QuestionAnswer("Where can I see debt?", "Open the Skills window and check XP Debt."),
                        new QuestionAnswer("What helps with debt?", "Social helps reduce XP debt from death.")
                    },
                    new[] { "Skills", "Attributes", "Useful Windows", "Common Questions" }),

                new(
                    "Abilities",
                    "Combat",
                    "Ability descriptions, FP, STM, cooldowns, and perk-granted active abilities.",
                    "FP, STM, recast",
                    new[]
                    {
                        new ArticleBlock("Where Abilities Come From",
                            "Some perk ranks grant active abilities. When you buy one of those ranks, the new ability can appear as a feat and may be placed on your hotbar."),
                        new ArticleBlock("Ability Details",
                            "Ability descriptions show the FP cost, STM cost, recast time, and what the ability does."),
                        new ArticleBlock("Cooldowns",
                            "If an ability is still cooling down, the game tells you how long to wait before you can use it again."),
                        new ArticleBlock("Recast Groups",
                            "Some abilities share a cooldown group. Using one ability in that group can make the other abilities in the same group wait too.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Where do abilities come from?", "Some perk ranks grant active abilities."),
                        new QuestionAnswer("What is Recast?", "The cooldown seconds shown in the ability description."),
                        new QuestionAnswer("Why can't I use it yet?", "Its cooldown or shared cooldown group still has time remaining.")
                    },
                    new[] { "Perks", "Attributes", "Useful Windows", "Common Questions" }),

                new(
                    "Attributes",
                    "Character",
                    "The current in-game attribute descriptions.",
                    "AP choices and effects",
                    new[]
                    {
                        new ArticleBlock("Might",
                            "Improves damage dealt by melee weapons and increases carrying capacity. Especially useful for characters focusing on Vibroblade, Heavy Vibroblade, Spear, Twin Blade, Katar, Staff, Smithery, or Gathering."),
                        new ArticleBlock("Perception",
                            "Improves damage dealt by ranged and finesse weapons and increases physical accuracy. Especially useful for characters focusing on Vibroknife, Lightsaber, Saberstaff, Katar, Pistol, Rifle, Fabrication, or Devices."),
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
                    new[] { "Skills", "Abilities", "Perks", "Rebuilds", "Common Questions" }),

                new(
                    "Rebuilds",
                    "Character",
                    "Full rebuilds, AP rebuilds, tokens, and what each option changes.",
                    "Full and AP rebuilds",
                    new[]
                    {
                        new ArticleBlock("Full Rebuild",
                            "A full character rebuild refunds your skills, stats, and perks so you can redistribute your starting attributes and skills. It costs 1 Rebuild Token, and rebuild tokens are granted by DMs only when staff decide they are needed."),
                        new ArticleBlock("Full Rebuild Reset",
                            "The reset returns perk SP, resets skills that count toward the 400-rank limit to rank 0 and XP 0, clears skill locks, resets attributes to 10, returns your earned AP, and refunds your racial bonus stat. Partial XP toward the next skill rank is lost."),
                        new ArticleBlock("Finishing A Full Rebuild",
                            "You must distribute all required ability points and skill points before leaving the rebuild flow. Standard characters cannot take Force ranks, Force characters cannot take Devices ranks, and droids cannot become Force Sensitive."),
                        new ArticleBlock("AP Rebuild",
                            "An AP rebuild is the stat-only rebuild option. It requires 1 Stat Refund Token, lets you rebuild your starting attributes, clears spent AP upgrades, returns your earned AP to spend again, consumes the token when saved, and starts a 14-day wait before another AP rebuild."),
                        new ArticleBlock("Getting Stat Refund Tokens",
                            "Buy a Stat Refund Tome from the Training Store, then use the tome to add 1 Stat Refund Token to your collection. The tome is consumed when used, and your token collection caps at 99.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What is a full rebuild?", "A full reset of skills, stats, and perks that costs 1 DM-granted Rebuild Token."),
                        new QuestionAnswer("What is an AP rebuild?", "A stat-only rebuild that consumes 1 Stat Refund Token and returns earned AP to spend again."),
                        new QuestionAnswer("What happens to partial skill XP?", "Partial XP toward the next skill rank is lost during a full rebuild reset.")
                    },
                    new[] { "Attributes", "Skills", "Perks", "Perk Refunds", "Useful Windows" }),

                new(
                    "Useful Windows",
                    "Interface",
                    "The main windows new players commonly need.",
                    "Guide and character sheet",
                    new[]
                    {
                        new ArticleBlock("Player Guide",
                            "Press B to open this Player Guide."),
                        new ArticleBlock("Character Sheet",
                            "Press C to open the Character Sheet. It shows your SP, AP, attributes, combat values, and character actions."),
                        new ArticleBlock("Guide Button",
                            "The Character Sheet also has a Guide button, so you can return here while reviewing your character."),
                        new ArticleBlock("Skills, Perks, and Recipes",
                            "Use the main menu, radial options, or available window buttons to open Skills, Perks, and Recipes when you need more detail.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I open the guide?", "Press B."),
                        new QuestionAnswer("How do I open my character sheet?", "Press C."),
                        new QuestionAnswer("Where is the guide button?", "Open the Character Sheet and use the Guide button near the lower part of the action list.")
                    },
                    new[] { "Common Questions", "Skills", "Attributes", "Perks", "Rebuilds" })
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
