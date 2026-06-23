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
            SelectedArticleBody = "Try searching for skills, perks, decay, death, combat, crafting, beasts, quests, housing, ships, chat, or windows.";
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
                    "Start here for skills, AP, perks, decay, refunds, XP debt, death, combat, crafting, names, and windows.",
                    "Skills, AP, death",
                    new[]
                    {
                        new ArticleBlock("Skills",
                            "Skills gain XP individually. Skill ranks create Skill Points for perks until you have earned 400 SP from skill ranks. Languages do not count toward this 400-rank limit."),
                        new ArticleBlock("Ability Points (AP)",
                            "Every 10 earned SP grants 1 Ability Point, up to 40 AP. Spend AP from the Character Sheet to improve attributes such as Might, Perception, Vitality, Willpower, Agility, and Social."),
                        new ArticleBlock("Perks",
                            "Perk ranks cost SP. Perks can require certain progress before you buy the next rank, and they can grant active abilities, passive bonuses, or other effects."),
                        new ArticleBlock("Skill Decay",
                            "Once you are at 400 skill ranks, a new rank can randomly lower another eligible unlocked skill by 1 rank and reset that skill's XP to 0. Languages do not count toward this limit."),
                        new ArticleBlock("Perk Refunds",
                            "Manual perk refunds remove the selected perk completely, return all SP paid for its purchased ranks, consume 1 Perk Refund Token, and start a 1 hour real-time wait before another manual refund."),
                        new ArticleBlock("XP Debt",
                            "Earned skill XP pays down XP debt before the remainder can apply to a skill. If your debt uses all of the XP, that skill gains no XP from that reward."),
                        new ArticleBlock("Death and Resting",
                            "If you die, you can wait for another player to revive you or respawn to your registered medical facility. Resting restores HP, FP, and STM over time, but cannot be used in combat, near enemies, or outside dungeon safe-rest areas."),
                        new ArticleBlock("Crafting and Training",
                            "Recipes, crafting devices, research terminals, refineries, the Training Store, and the Character Sheet windows are the main places to turn earned progress into gear, tokens, and character choices."),
                        new ArticleBlock("Recognizing Players",
                            "Other player characters may appear with a gray descriptor until you remember them. Type /name <name>, then click the player character when the target cursor appears. Use /forgetname and click the character to clear that personal name. To change how unnamed players see you, use /name <description> on yourself. Names and descriptions are limited to 64 characters and cannot include color codes.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do skills work?", "Skill XP raises individual skills. Skill ranks create earned SP until 400; languages do not count toward that limit."),
                        new QuestionAnswer("How do I use AP?", "Earn 1 AP every 10 earned SP, then spend AP from the Character Sheet to improve attributes."),
                        new QuestionAnswer("What does skill decay mean?", "At 400 skill ranks, gaining another eligible rank can lower another unlocked eligible skill by 1 rank."),
                        new QuestionAnswer("How do perk refunds work?", "Manual refunds return all SP paid for the selected perk, consume a token, and start a 1 hour wait."),
                        new QuestionAnswer("Why did I get no skill XP?", "XP debt may have used the entire XP reward, or you may be at the 400-rank limit with no available skill that can decay."),
                        new QuestionAnswer("Where do I go after death?", "Respawning returns you to your registered medical facility and adds XP debt."),
                        new QuestionAnswer("What should I open first?", "Press B for this guide and C for the Character Sheet."),
                        new QuestionAnswer("How do I use /name?", "Type /name <name>, then click the player character when the target cursor appears. Target yourself to change the gray description shown to players who have not named you. Names and descriptions are limited to 64 characters and cannot include color codes. Name the intended player before sending tells if several characters share the same descriptor.")
                    },
                    new[] { "Communication", "Skills", "Attributes", "Perks", "Skill Decay", "Perk Refunds", "XP Debt", "Death & Recovery", "Combat Basics", "Crafting", "Training Store", "Useful Windows" }),

                new(
                    "Communication",
                    "Social",
                    "Talk ranges, comms, disabled Shout, HoloNet broadcasts, notes, settings, descriptions, names, emotes, and languages.",
                    "Chat and tools",
                    new[]
                    {
                        new ArticleBlock("Talk, Whisper, and Comms",
                            "Normal talk reaches players within 20 meters. Whisper reaches 4 meters. Party chat works as comms for party members and can also be heard by nearby players within 20 meters."),
                        new ArticleBlock("Disabled Shout Channel",
                            "The Shout chat channel is disabled for players. DMs can still use Shout for server-wide messages."),
                        new ArticleBlock("HoloNet Broadcast Window",
                            "The HoloNet broadcast window sends a longer broadcast, costs 2500 credits, and is limited to 600 characters."),
                        new ArticleBlock("Settings",
                            "Settings control achievement notifications, subdual mode, reset reminders, chat colors, emote colors, and language colors. Settings also links to your character description."),
                        new ArticleBlock("Notes",
                            "Notes are private player notes. You can keep up to 25 notes, and each note can hold up to 1000 characters."),
                        new ArticleBlock("Names",
                            "Unrecognized player characters appear with a gray descriptor until you personally set a known name. Type /name <name>, then click the player character when the target cursor appears. Use /forgetname and click the character to forget that personal name. Target yourself with /name <description> to set the gray text others see before they name you. Names and descriptions are limited to 64 characters and cannot include color codes."),
                        new ArticleBlock("Emotes and Languages",
                            "Speech can include emote text, and your emote style can be toggled between regular and novel formatting. Speaking or hearing non-Basic languages uses language skill; hearing a non-Basic language you do not fully know can grant language XP over time.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How far does talk carry?", "Talk reaches 20 meters. Whisper reaches 4 meters."),
                        new QuestionAnswer("What is party chat here?", "Party chat acts as comms for your party and can be overheard nearby."),
                        new QuestionAnswer("Why can't I use Shout?", "The player Shout channel is disabled. Use Comms for in-character radio-style communication."),
                        new QuestionAnswer("How do I name another player?", "Type /name <name>, then click that player character when the target cursor appears. Use /forgetname and click them again to clear it. Target yourself with /name to set your unnamed description. Names and descriptions are limited to 64 characters and cannot include color codes."),
                        new QuestionAnswer("How do languages improve?", "Listening to partially understood non-Basic speech can grant language XP over time.")
                    },
                    new[] { "Common Questions", "Useful Windows", "Skills", "Quests & Key Items", "Death & Recovery" }),

                new(
                    "Skills",
                    "Core",
                    "Skill XP, SP, AP, XP debt, Social XP bonuses, and the 400-rank limit.",
                    "Skill XP, SP, AP",
                    new[]
                    {
                        new ArticleBlock("Skills Window",
                            "The Skills window shows available XP, XP debt, each visible skill's rank, title, progress to the next rank, raw XP, description, decay lock state, and whether RP XP can be distributed to that skill."),
                        new ArticleBlock("Skill Limit",
                            "A character can earn up to 400 total ranks from skills that count toward the skill limit. Languages do not count toward this limit."),
                        new ArticleBlock("SP and AP",
                            "When a skill that counts toward the limit ranks up, you gain 1 unallocated SP and 1 earned SP until earned SP reaches 400. Every 10 earned SP grants 1 unallocated AP, up to 40 AP. Your displayed total SP also includes the 10 starting SP."),
                        new ArticleBlock("XP Adjustments",
                            "Skill XP can be adjusted before it is applied. Your Social attribute adds 2.5 percent skill XP per Social point. Some effects, event rewards, or active companions such as beasts and droids can also change the final amount."),
                        new ArticleBlock("XP Debt",
                            "XP debt is paid first. If earned XP is less than your debt, all of that XP removes debt and none goes to the skill.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What is the skill limit?", "400 total ranks from skills that count toward the limit. Languages do not count."),
                        new QuestionAnswer("How do I get AP?", "Every 10 earned SP grants 1 unallocated AP, up to 40."),
                        new QuestionAnswer("Why did XP go to debt?", "XP debt is paid down before skill XP is applied.")
                    },
                    new[] { "Attributes", "XP Debt", "Skill Decay", "Perks", "Training Store", "Crafting", "Useful Windows" }),

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
                            "A manual refund removes the selected perk from the player or active beast, returns the total SP paid for all purchased ranks of that perk, and removes abilities granted by that perk."),
                        new ArticleBlock("Token and Wait Time",
                            "You need at least 1 Perk Refund Token. A successful manual refund consumes 1 token and makes you wait 1 real-time hour before another manual perk refund."),
                        new ArticleBlock("Refund Checks",
                            "Some perks may block a manual refund and show a message explaining why."),
                        new ArticleBlock("After Refund",
                            "After a successful refund, abilities and effects from that perk are removed, hotbar entries for those abilities are cleared, and the Perks window updates."),
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
                    new[] { "Perks", "Skill Decay", "Skills", "XP Debt", "Training Store", "Common Questions" }),

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
                            "Social above 10 helps reduce XP debt from death.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why did my skill not move?", "Your XP reward may have been fully spent reducing XP debt."),
                        new QuestionAnswer("Where can I see debt?", "Open the Skills window and check XP Debt."),
                        new QuestionAnswer("What helps with debt?", "Social helps reduce XP debt from death.")
                    },
                    new[] { "Skills", "Attributes", "Death & Recovery", "Useful Windows", "Common Questions" }),

                new(
                    "Abilities",
                    "Combat",
                    "Ability descriptions, FP, STM, cooldowns, and perk-granted active abilities.",
                    "FP, STM, recast",
                    new[]
                    {
                        new ArticleBlock("Where Abilities Come From",
                            "Some perk ranks grant active abilities. When you buy one of those ranks, the new ability can appear on your character and may be placed on your hotbar."),
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
                    new[] { "Perks", "Attributes", "Combat Basics", "Useful Windows", "Common Questions" }),

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
                        new QuestionAnswer("How do I get AP?", "Every 10 earned SP grants 1 AP, up to 40."),
                        new QuestionAnswer("What helps XP gain?", "Social improves XP gain."),
                        new QuestionAnswer("What helps stamina?", "Agility increases maximum stamina.")
                    },
                    new[] { "Skills", "Abilities", "Perks", "Rebuilds", "Combat Basics", "Death & Recovery", "Common Questions" }),

                new(
                    "Rebuilds",
                    "Character",
                    "Full rebuilds, AP rebuilds, tokens, and what each option changes.",
                    "Full and AP rebuilds",
                    new[]
                    {
                        new ArticleBlock("Full Rebuild",
                            "A full character rebuild refunds your skills, stats, and perks so you can redistribute your starting attributes and skills. Using a rebuild terminal requires 1 Rebuild Token, while some server-required rebuilds can send you to the rebuild area without a token."),
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
                        new QuestionAnswer("What is a full rebuild?", "A full reset of skills, stats, and perks. Rebuild terminals consume 1 Rebuild Token; server-required rebuilds may not."),
                        new QuestionAnswer("What is an AP rebuild?", "A stat-only rebuild that consumes 1 Stat Refund Token and returns earned AP to spend again."),
                        new QuestionAnswer("What happens to partial skill XP?", "Partial XP toward the next skill rank is lost during a full rebuild reset.")
                    },
                    new[] { "Attributes", "Skills", "Perks", "Perk Refunds", "Training Store", "Useful Windows" }),

                new(
                    "Death & Recovery",
                    "Core",
                    "Death, respawn, medical registration, XP debt, subdual, and resting.",
                    "Respawn and rest",
                    new[]
                    {
                        new ArticleBlock("When You Die",
                            "Normal death lets you wait for another player to revive you or respawn to your registered medical facility. Death also clears active status effects."),
                        new ArticleBlock("Respawning",
                            "Respawning brings you back, restores half of your maximum HP, moves you to your registered medical facility, and adds XP debt."),
                        new ArticleBlock("Medical Registration",
                            "Registering at a medical facility chooses where you return after respawning. If you have no registered facility, you return to the default respawn point."),
                        new ArticleBlock("XP Debt From Death",
                            "Death debt scales with your earned SP. Medical Center upgrades and Social above 10 reduce that debt, up to an 80 percent total reduction."),
                        new ArticleBlock("Subdual",
                            "If another player defeats you while using subdual, you are subdued instead of going through normal respawn. Subdual briefly knocks you down and applies a short subdual penalty."),
                        new ArticleBlock("Resting",
                            "Resting restores HP, FP, and STM every few seconds. You cannot rest in combat, near enemies, or while nearby party members are in combat. Dungeon resting requires a safe rest location.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Where do I respawn?", "At your registered medical facility, or at the default respawn point if you have not registered one."),
                        new QuestionAnswer("Why did I get XP debt?", "Respawning after death adds XP debt. Higher Social and Medical Center upgrades reduce the amount."),
                        new QuestionAnswer("Why can't I rest?", "You may be in combat, too close to enemies, near a party member in combat, or outside a dungeon safe-rest location."),
                        new QuestionAnswer("What is subdual?", "A PvP defeat setting that subdues you instead of sending you through normal respawn.")
                    },
                    new[] { "XP Debt", "Skills", "Attributes", "Combat Basics", "Useful Windows" }),

                new(
                    "Combat Basics",
                    "Combat",
                    "HP, FP, STM, hit chance, damage, defenses, resistances, deflection, guard, and character-sheet stats.",
                    "Damage and defenses",
                    new[]
                    {
                        new ArticleBlock("Core Resources",
                            "HP is health. FP fuels Force abilities. STM fuels non-Force abilities. The Character Sheet shows your current and maximum values."),
                        new ArticleBlock("Hits and Misses",
                            "Accuracy is compared against Evasion to decide whether an attack lands. Hit chance has a floor and ceiling, so very high or very low stats still leave some uncertainty."),
                        new ArticleBlock("Damage and Defense",
                            "Physical attacks use Physical DEF. Force attacks use Force DEF. Fire, poison, electrical, and ice damage use matching resistances. Lower damage-taken values are better."),
                        new ArticleBlock("Critical Hits",
                            "Critical Rate raises your chance to critically hit, while Critical Damage makes critical hits stronger. The target's Vitality can affect the final critical chance."),
                        new ArticleBlock("Deflection and Guard",
                            "Attack Deflection works while wielding a weapon without a shield. Shield Deflection works while using a shield. Guard is separate and can reduce incoming physical damage while increasing enmity."),
                        new ArticleBlock("Combat Readiness",
                            "Combat Readiness increases activated ability damage and healing. It does not reduce cooldowns.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why did I miss?", "Your Accuracy was checked against the target's Evasion, with a minimum and maximum hit chance."),
                        new QuestionAnswer("What defense matters?", "Physical DEF helps against physical attacks, Force DEF helps against Force attacks, and resistances help against matching elemental or status types."),
                        new QuestionAnswer("Are deflection and guard the same?", "No. Deflection can stop an attack from landing, while Guard reduces physical damage after the hit."),
                        new QuestionAnswer("Does readiness lower cooldowns?", "No. Combat Readiness improves activated ability damage and healing.")
                    },
                    new[] { "Abilities", "Attributes", "Skills", "Death & Recovery", "Useful Windows" }),

                new(
                    "Crafting",
                    "Activities",
                    "Recipes, crafting devices, CP, progress, quality, durability, enhancements, research, and refining.",
                    "Recipes and refining",
                    new[]
                    {
                        new ArticleBlock("Recipes",
                            "The Recipes window lets you search recipes, filter to craftable recipes, and review level, quantity, enhancement slots, required components, and requirements."),
                        new ArticleBlock("Starting A Craft",
                            "Use the proper crafting device, choose a recipe or blueprint, and supply the required inventory components. Blueprints can cost credits and use one licensed run when crafted."),
                        new ArticleBlock("CP, Progress, Quality, Durability",
                            "Crafting spends CP on actions. Progress must reach the goal before Durability reaches 0. Quality improves rewards and enhancement transfer chances; for food, it also improves duration or charges."),
                        new ArticleBlock("Enhancements",
                            "Enhancements must be in your inventory, match the recipe's enhancement slot type, and be within 5 levels of the recipe. Some enhancements make the craft harder."),
                        new ArticleBlock("Success and Failure",
                            "A successful craft creates the item, can transfer enhancements, grants crafting skill XP, and marks first-time recipe completion. Failure grants reduced XP and can lose selected materials."),
                        new ArticleBlock("Research",
                            "Research terminals require Research I. Research jobs cost credits and time, work with weapon, armor, and food recipes, and can improve blueprints up to level 10. Cancelling a research job forfeits credits and progress."),
                        new ArticleBlock("Refining",
                            "Refineries turn raw materials into refined materials. Refining requires the Refining perk for the material, consumes Power Cores, takes a few seconds, and grants Gathering XP.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What ends a craft?", "Success happens when Progress reaches the goal. Failure happens if Durability reaches 0 first."),
                        new QuestionAnswer("What does Quality do?", "Quality improves crafting rewards and enhancement transfer chances; food also benefits in duration or charges."),
                        new QuestionAnswer("Why can't I use an enhancement?", "It may not match the slot type, may be too far from the recipe level, or may not be in your inventory."),
                        new QuestionAnswer("What does research do?", "Research spends credits and time to create or improve blueprints for weapon, armor, and food recipes.")
                    },
                    new[] { "Skills", "Perks", "Training Store", "Housing & Markets", "Useful Windows" }),

                new(
                    "Training Store",
                    "Progression",
                    "Available XP, refund tomes, token currencies, cantina discounts, and RP XP distribution.",
                    "XP store and tokens",
                    new[]
                    {
                        new ArticleBlock("Available XP",
                            "Available XP is separate from skill XP. You can spend it in the Training Store or distribute it into a selected skill from the Skills window."),
                        new ArticleBlock("Training Store Items",
                            "The Training Store sells Perk Refund Tomes and Stat Refund Tomes for XP. A citizen property's Cantina upgrades can reduce these XP prices."),
                        new ArticleBlock("Perk Refund Tome",
                            "Using a Perk Refund Tome adds 1 Perk Refund Token and consumes the tome. Perk Refund Tokens cap at 99."),
                        new ArticleBlock("Stat Refund Tome",
                            "Using a Stat Refund Tome adds 1 Stat Refund Token and consumes the tome. Stat Refund Tokens cap at 99."),
                        new ArticleBlock("Currencies Window",
                            "The Currencies window shows Rebuild Tokens, Perk Refund Tokens, and Stat Refund Tokens.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I get perk refund tokens?", "Buy a Perk Refund Tome from the Training Store and use it."),
                        new QuestionAnswer("How do I get stat refund tokens?", "Buy a Stat Refund Tome from the Training Store and use it."),
                        new QuestionAnswer("What lowers tome prices?", "Cantina upgrades from your citizen property can reduce Training Store XP prices."),
                        new QuestionAnswer("Where do I see tokens?", "Open Currencies from the Character Sheet.")
                    },
                    new[] { "Perk Refunds", "Rebuilds", "Skills", "Useful Windows" }),

                new(
                    "Beasts & Stables",
                    "Activities",
                    "Taming, active beasts, stable capacity, beast XP, beast perks, pet food, revival, eggs, and DNA.",
                    "Taming and pets",
                    new[]
                    {
                        new ArticleBlock("Taming",
                            "Tame requires the Tame perk, no active beast, a valid creature target, and open stable capacity. Your Tame rank controls the highest creature level you can tame, and Beast Mastery plus Social improve your chance."),
                        new ArticleBlock("Stable Capacity",
                            "You can keep 1 beast plus 1 more for each Stabling perk rank. The Stables window shows your count, marks the active beast in green, and lets you make a beast active or inactive."),
                        new ArticleBlock("Beast XP and SP",
                            "Beasts gain XP, level up, and earn their own unallocated SP. Tame rank limits how far a beast can level, and beast level cannot exceed 50."),
                        new ArticleBlock("Beast Perks",
                            "With an active beast, the Perks window can switch to Beast Perks. Beast perks spend the beast's SP and use the beast's level and role requirements."),
                        new ArticleBlock("Pet Food",
                            "Pet food requires an active beast. It grants a 30 minute beast XP bonus, with extra benefit for the beast's favorite food and a smaller bonus for hated food. A beast that already has the food effect is not hungry yet."),
                        new ArticleBlock("Reviving Beasts",
                            "When your active beast is unconscious, Revive Beast can bring it back if no other companion is active. Higher ranks revive with more HP, and Social improves the HP returned by higher ranks."),
                        new ArticleBlock("Eggs and DNA",
                            "Using a beast egg requires Tame I, no active beast, and open stable capacity. Releasing a beast is permanent, but gives DNA based on the beast's level. Incubators use DNA and enzymes over timed stages to create improved beast eggs.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why can't I tame this creature?", "You may already have an active beast, lack stable capacity, lack Tame rank, or the creature may not be tameable."),
                        new QuestionAnswer("How many beasts can I keep?", "One beast plus one additional beast for each Stabling perk rank."),
                        new QuestionAnswer("How do beast perks work?", "They spend the beast's SP, not yours, and use the beast's level and role."),
                        new QuestionAnswer("How do I revive my beast?", "Use Revive Beast when your active beast is unconscious and no other companion is active.")
                    },
                    new[] { "Perks", "Skills", "Crafting", "Useful Windows" }),

                new(
                    "Quests & Key Items",
                    "Activities",
                    "Active quests, objectives, abandoning quests, key items, achievements, and rewards.",
                    "Quests and unlocks",
                    new[]
                    {
                        new ArticleBlock("Quest Window",
                            "The Quest window lists active quests, supports search, and shows the selected quest's journal text and objectives. Completed quests are not shown in the active quest list."),
                        new ArticleBlock("Objectives",
                            "Quest objectives can track items, kills, conversations, and other progress. When objectives are complete, return to the quest giver or the location described by the journal text."),
                        new ArticleBlock("Abandoning Quests",
                            "You can abandon active quests from the Quest window. Abandoning removes the active quest progress unless the quest keeps completed history."),
                        new ArticleBlock("Rewards",
                            "Quests can grant credits, skill XP, items, key items, guild points, faction standing, or faction points."),
                        new ArticleBlock("Key Items",
                            "Key Items are permanent records of important unlocks, permissions, passes, maps, receipts, and other progress. Open Key Items from the Character Sheet."),
                        new ArticleBlock("Achievements",
                            "Achievements show long-term account progress. The Achievements window marks acquired achievements in green, locked achievements in red, and shows the acquired date when available.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Where are my quest objectives?", "Open Quests and select the quest. Objectives appear under the journal text."),
                        new QuestionAnswer("Why don't I see completed quests?", "The Quest window focuses on active quests, so completed quests are hidden there."),
                        new QuestionAnswer("What are key items?", "Permanent progress records such as passes, maps, receipts, unlocks, and permissions."),
                        new QuestionAnswer("Where are achievements?", "Open Achievements from the Character Sheet.")
                    },
                    new[] { "Useful Windows", "Skills", "Training Store", "Housing & Markets" }),

                new(
                    "Housing & Markets",
                    "Activities",
                    "Apartments, leases, permissions, storage, banks, markets, listings, taxes, and tills.",
                    "Homes and trading",
                    new[]
                    {
                        new ArticleBlock("Apartments",
                            "Apartment terminals let you preview layouts and rent one apartment. Renting pays the initial price, starts a 7 day lease, and lets you extend the lease up to 30 days ahead."),
                        new ArticleBlock("Managing A Lease",
                            "The Manage Apartment window lets permitted players enter, rename, change the description, extend the lease, manage permissions, or cancel the lease. Cancelling a lease permanently loses the apartment and everything inside without a refund."),
                        new ArticleBlock("Structures and Permissions",
                            "Properties have structure limits and permissions. Depending on the property, permissions can allow entering, editing structures, retrieving structures, renaming, changing descriptions, accessing storage, or managing special property features."),
                        new ArticleBlock("Bank Storage",
                            "Banks store inventory items up to the bank's item limit. Deposit targets an item in your inventory, and Withdraw returns the selected stored item to you."),
                        new ArticleBlock("Buying From Markets",
                            "Market buy windows let you search, filter by category, sort by price, examine items, and buy listings you can afford. You cannot buy listings from your own account."),
                        new ArticleBlock("Selling On Markets",
                            "Market listing windows let you add inventory items, set prices, list or unlist them, and remove listings back to your inventory. Listings older than two weeks are automatically unlisted."),
                        new ArticleBlock("Market Till",
                            "When another player buys your listing, the market tax is removed and the remaining credits go to your market till. Use the listing window to collect the till.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Can I rent more than one apartment?", "No. You can have one active apartment lease."),
                        new QuestionAnswer("How far can I extend a lease?", "Up to 30 days ahead."),
                        new QuestionAnswer("Where do sold-item credits go?", "To your market till after market tax."),
                        new QuestionAnswer("Why was my listing unlisted?", "Listings older than two weeks are automatically unlisted.")
                    },
                    new[] { "Crafting", "Training Store", "Ships & Space", "Useful Windows" }),

                new(
                    "Ships & Space",
                    "Activities",
                    "Ship deeds, registration, boarding, piloting, permissions, modules, targeting, repairs, shields, hull, and capacitor.",
                    "Ships and modules",
                    new[]
                    {
                        new ArticleBlock("Registering Ships",
                            "Ship Management can register ship deeds from your inventory. You can have up to 10 registered ships. Registering creates the ship and consumes the deed."),
                        new ArticleBlock("Boarding and Piloting",
                            "You can board a ship when you are at its current location. Inside the ship, use the ship computer to pilot if you have Pilot Ship permission and meet the ship and module requirements."),
                        new ArticleBlock("Other Players' Ships",
                            "Ship Management can show ships where you have permissions. Owners can grant permissions such as entering, piloting, refitting, renaming, or managing permissions."),
                        new ArticleBlock("Modules",
                            "Ships have high power, low power, and configuration module slots. Installing a module requires the right slot type, the right perk requirements, and the right ship class. Capital ships use capital-class modules."),
                        new ArticleBlock("Flying and Combat",
                            "While piloting, your ship uses shields, hull, and capacitor. Modules can require targets, range, capacitor, and cooldown time. Shields and capacitor recover over time while flying."),
                        new ArticleBlock("Repairs",
                            "Ship Management can repair missing shields and hull when the ship is at your location and you have enough credits. Starport upgrades and Social can reduce the repair bill."),
                        new ArticleBlock("Unregistering",
                            "Unregistering returns the ship deed, but the ship must be fully repaired and high or low power modules must be removed first. Structures inside the ship are permanently lost.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How many ships can I register?", "Up to 10."),
                        new QuestionAnswer("Why can't I pilot?", "You may lack Pilot Ship permission, the required ship or module perks, or the ship controls may already be in use."),
                        new QuestionAnswer("Why won't a module install?", "It may be the wrong slot type, require a perk, or require a different ship class."),
                        new QuestionAnswer("What lowers repair cost?", "Starport upgrades and Social can reduce the bill.")
                    },
                    new[] { "Housing & Markets", "Perks", "Combat Basics", "Useful Windows" }),

                new(
                    "Useful Windows",
                    "Interface",
                    "The main windows and shortcuts new players commonly need.",
                    "Guide and sheet",
                    new[]
                    {
                        new ArticleBlock("Player Guide",
                            "Press B to open this Player Guide."),
                        new ArticleBlock("Character Sheet",
                            "Press C to open the Character Sheet. It shows your SP, AP, attributes, combat values, and character actions."),
                        new ArticleBlock("Guide Button",
                            "The Character Sheet also has a Guide button, so you can return here while reviewing your character."),
                        new ArticleBlock("Character Sheet Actions",
                            "The Character Sheet action list opens Skills, Perks, Recipes, Quests, Currencies, Achievements, Notes, Settings, HoloCom, Key Items, and this Guide."),
                        new ArticleBlock("Skills and Perks",
                            "Skills shows ranks, XP, XP debt, decay locks, and RP XP distribution. Perks shows player perks and, when you have an active beast, beast perks."),
                        new ArticleBlock("Quests and Progress",
                            "Quests shows active quest objectives. Key Items shows permanent unlocks and progress records. Achievements shows long-term account progress."),
                        new ArticleBlock("Social Windows",
                            "Notes stores private notes. Settings controls notifications, subdual mode, reset reminders, chat colors, and description access. HoloCom opens your HoloCom options when available.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I open the guide?", "Press B."),
                        new QuestionAnswer("How do I open my character sheet?", "Press C."),
                        new QuestionAnswer("Where is the guide button?", "Open the Character Sheet and use the Guide button near the lower part of the action list."),
                        new QuestionAnswer("Where are currencies?", "Open the Character Sheet and choose Currencies."),
                        new QuestionAnswer("Where are quests and key items?", "Open the Character Sheet and choose Quests or Key Items.")
                    },
                    new[] { "Common Questions", "Skills", "Perks", "Training Store", "Quests & Key Items", "Communication" })
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
