using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
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
            SelectedArticleBody = "Try searching for skills, perks, techniques, espionage, death, combat, crafting, gathering, research, beasts, droids, quests, contracts, travel, housing, markets, ships, guilds, citizenship, disguises, chat, or windows.";
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
                    "Start here for progression, recovery, combat, crafting, travel, companions, quests, communication, and essential windows.",
                    "Start here",
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
                            "If you die, you can wait for another player to revive you or respawn to your registered medical facility. Resting restores HP, FP, and STM over time, but combat, nearby enemies, nearby party combat, movement, attacks, or damage can prevent or interrupt it. Dungeons require a safe-rest area."),
                        new ArticleBlock("Crafting",
                            "Crafting devices use recipes, materials, and optional blueprints or enhancements to make gear, food, and other items. During a craft, spend CP on actions; Progress must reach the goal before Durability reaches 0. Quality improves crafting XP, vendor value, and enhancement transfer chances. Research terminals improve blueprints, and refineries turn raw materials into refined materials."),
                        new ArticleBlock("Travel and Companions",
                            "Complete the CZ-220 orientation to earn the shuttle pass used for interplanetary travel. Taxi terminals handle registered local routes. You may have only one active henchman companion at a time, either a beast or a droid."),
                        new ArticleBlock("Recognizing Players",
                            "Names above other player characters are private memory labels, not global character names. An unrecognized character appears as a gray public description. Use /name <label> on another player to save a label only your character can see for that presented identity; this does not rename them for anyone else. Use /forgetname to remove only your private label. Use /name <description> on yourself to set the gray public description seen by players who have not labeled your current identity. Labels and descriptions are limited to 64 characters and cannot include color codes.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do skills work?", "Skill XP raises individual skills. Skill ranks create earned SP until 400; languages do not count toward that limit."),
                        new QuestionAnswer("How do I use AP?", "Earn 1 AP every 10 earned SP, then spend AP from the Character Sheet to improve attributes."),
                        new QuestionAnswer("What does skill decay mean?", "At 400 skill ranks, gaining another eligible rank can lower another unlocked eligible skill by 1 rank."),
                        new QuestionAnswer("How do perk refunds work?", "Manual refunds return all SP paid for the selected perk, consume a token, and start a 1 hour wait."),
                        new QuestionAnswer("Why did I get no skill XP?", "XP debt may have used the entire XP reward, or you may be at the 400-rank limit with no available skill that can decay."),
                        new QuestionAnswer("Where do I go after death?", "Respawning returns you to your registered medical facility and adds XP debt."),
                        new QuestionAnswer("How do I leave CZ-220?", "Complete the orientation quest to earn the CZ-220 Shuttle Pass, then use a starport flights terminal."),
                        new QuestionAnswer("What should I open first?", "Press B for this guide, C for the Character Sheet, and J for active Quests."),
                        new QuestionAnswer("How do I use /name?", "Use /name <label> on another player to save a private label only you can see. Use it on yourself to set your gray public description. Neither action changes another player's character name. Labels and descriptions are limited to 64 characters and cannot include color codes. Label the intended player before sending tells if several characters share the same description.")
                    },
                    new[] { "Communication", "Skills", "Attributes", "Perks", "Skill Decay", "Perk Refunds", "XP Debt", "Death & Recovery", "Combat Basics", "Crafting", "Travel & Navigation", "Beasts & Stables", "Droids", "Quests & Key Items", "Useful Windows" }),

                new(
                    "Communication",
                    "Social",
                    "Talk ranges, scoped comms, HoloCom calls, disabled Shout, HoloNet broadcasts, names, emotes, languages, notes, and settings.",
                    "Chat and tools",
                    new[]
                    {
                        new ArticleBlock("Talk, Whisper, and Comms",
                            "Normal Talk reaches players within 20 meters. Whisper reaches 4 meters. Party chat acts as Comms: it reaches party members in the same ship, space region, supported event area, or planet, and nearby non-party listeners within 20 meters can overhear it when they share that scope. By default, you receive a warning when party members are out of range; this warning can be disabled in General Settings."),
                        new ArticleBlock("Disabled Shout Channel",
                            "The Shout chat channel is disabled for players. DMs can still use Shout for server-wide messages."),
                        new ArticleBlock("HoloNet Broadcast Window",
                            "The HoloNet broadcast window sends a longer broadcast, costs 2500 credits, and is limited to 600 characters."),
                        new ArticleBlock("HoloCom Calls",
                            "Open HoloCom from the Character Sheet to call another available online player. HoloCom cannot be used in space, and both participants are immobilized while a connected call is active. Use HoloCom or /endcall to end it; unanswered call attempts time out."),
                        new ArticleBlock("Settings",
                            "Settings controls achievement notifications, subdual mode, reset reminders, Mini-Vitals, chat and emote colors, language colors, character-description access, and identity/privacy options for descriptors and account names."),
                        new ArticleBlock("Notes",
                            "Notes are private player notes. You can keep up to 100 notes, and each note can hold up to 1000 characters. Notes can be searched, sorted into up to 25 categories you create yourself, and filtered by category."),
                        new ArticleBlock("Remembering Other Characters",
                            "Using /name <label> on another player saves a private label only your character can see. It does not rename the other character, and no other player sees the label you entered. The label can be a name your character was told, a nickname, or what your character believes that presented identity is called; it does not have to be the truth. Use /forgetname on the character to remove only your private label."),
                        new ArticleBlock("Your Public Description",
                            "An unrecognized player character appears as a gray public description. Use /name <description> on yourself to set the gray text shown to players who have not saved a label for your current identity. This does not change your real character name. Labels and public descriptions are limited to 64 characters and cannot include color codes."),
                        new ArticleBlock("Example",
                            "A masked character uses the public description 'Tall Armored Human.' Mira saves the private label 'Red Coat,' so only Mira sees Red Coat. Jax has not saved a label, so Jax still sees Tall Armored Human in gray. Neither player renamed the masked character. If the mask is a disguise, labels saved for the character's normal identity remain separate."),
                        new ArticleBlock("Emotes and Languages",
                            "Speech can include emote text, and /emotestyle toggles between regular and novel formatting. Use /emotes to list emotes or /emotegui to browse them in a window. Use /language help to list language aliases and /language <alias> to switch. Hearing a non-Basic language you do not fully know can grant language XP over time. Wookiees always speak Shyriiwook."),
                        new ArticleBlock("OOC and Speech Restrictions",
                            "Text after // or (( is treated as out-of-character and is not translated through the language system. Dead characters cannot speak, and the Shout channel remains staff-only."),
                        new ArticleBlock("Useful Chat Commands",
                            "Use /help to browse commands, /dice to open the dice bag, /bug to report a problem, /resetwindows to restore window positions, /save for a manual character save, and /stuck only as an emergency escape when trapped on a map. /stuck has a 30 minute cooldown.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How far does talk carry?", "Talk reaches 20 meters. Whisper reaches 4 meters."),
                        new QuestionAnswer("What is party chat here?", "Party chat acts as scoped Comms for your party and can be overheard by nearby players in the same scope."),
                        new QuestionAnswer("Why can't I use Shout?", "The player Shout channel is disabled. Use Comms for in-character radio-style communication."),
                        new QuestionAnswer("Can another player rename my character?", "No. They can only save a private label visible to their own character."),
                        new QuestionAnswer("Can two players see different names for the same character?", "Yes. Each character keeps their own private labels."),
                        new QuestionAnswer("Does /name reveal someone's real identity?", "No. It records what your character believes or calls the currently presented identity."),
                        new QuestionAnswer("How do I label another player?", "Type /name <label>, then click that player character. Use /forgetname and click them again to remove only your private label. Target yourself with /name to set your gray public description."),
                        new QuestionAnswer("How do I change languages?", "Use /language help, then /language <alias>. Wookiees remain in Shyriiwook."),
                        new QuestionAnswer("How do languages improve?", "Listening to partially understood non-Basic speech can grant language XP over time."),
                        new QuestionAnswer("How do I report a bug?", "Use /bug and include what happened, where it happened, and how to reproduce it.")
                    },
                    new[] { "Common Questions", "Useful Windows", "Skills", "Quests & Key Items", "Disguises", "Death & Recovery" }),

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
                            "XP debt is paid first. If earned XP is less than your debt, all of that XP removes debt and none goes to the skill."),
                        new ArticleBlock("Roleplay XP",
                            "Qualifying in-character Talk, Whisper, and Comms around other players builds toward automatic RP XP awards. OOC text does not count and can reset the current buildup. Social and citizen Cantina upgrades improve the award. Available RP XP can be distributed to eligible skills from the Skills window or spent in the Training Store.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What is the skill limit?", "400 total ranks from skills that count toward the limit. Languages do not count."),
                        new QuestionAnswer("How do I get AP?", "Every 10 earned SP grants 1 unallocated AP, up to 40."),
                        new QuestionAnswer("Why did XP go to debt?", "XP debt is paid down before skill XP is applied.")
                    },
                    new[] { "Attributes", "XP Debt", "Skill Decay", "Perks", "Training Store", "Crafting", "Communication", "Useful Windows" }),

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
                    "Perk ranks, SP prices, requirements, colors, Force Affinity, beast perks, and granted effects.",
                    "SP prices and requirements",
                    new[]
                    {
                        new ArticleBlock("Costs and Details",
                            "Each perk level has its own SP price. The Perks window shows the next upgrade price in the Buy Upgrade button and in the selected perk details."),
                        new ArticleBlock("Green and Red States",
                            "The list color is based on whether the next upgrade's requirements pass. The Buy button is enabled only when the next upgrade exists, requirements pass, and you have enough unallocated SP."),
                        new ArticleBlock("What Perks Can Do",
                            "Perk levels can grant active abilities, passive bonuses, and other effects."),
                        new ArticleBlock("Force Affinity",
                            "Force-sensitive characters see their current Force Affinity near the top of the Perks window. Selecting a Force perk also shows whether it is Light, Dark, or Universal and how the current affinity changes that power. See the Force Affinity guide topic for the full rules."),
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
                    new[] { "Force Affinity", "Perk Refunds", "Skills", "Abilities", "Skill Decay" }),

                new(
                    "Force Affinity",
                    "Combat",
                    "How Light, Dark, and Universal Force powers change—and are changed by—your affinity.",
                    "Light, Dark, and Universal powers",
                    new[]
                    {
                        new ArticleBlock("Reading Your Affinity",
                            "Force Affinity ranges from -10 Dark to +10 Light. A value of 0 is neutral. Your current value is displayed prominently in the Perks window and in the Character Sheet's detailed statistics."),
                        new ArticleBlock("How Affinity Changes",
                            "Owning any rank of a Light-aligned perk contributes +1 Light affinity. Owning any rank of a Dark-aligned perk contributes -1 Dark affinity. Additional ranks of the same perk do not contribute additional affinity. Refunding the perk removes its contribution. Universal Force perks do not change affinity."),
                        new ArticleBlock("Magnitude",
                            "Each point toward a power's side increases that power's damage, healing, shields, regeneration, or drain magnitude by 5 percent. Each point toward the opposing side reduces it by 5 percent. The multiplier is limited to 50 percent at full opposition and 150 percent at full alignment."),
                        new ArticleBlock("Hit Chance",
                            "Affinity also changes the hit chance of detrimental Light and Dark Force powers. At +10 Light, Light powers gain +5% hit chance and Dark powers suffer -5%. At -10 Dark, Dark powers gain +5% and Light powers suffer -5%. The final chance shown in the combat log already includes this adjustment."),
                        new ArticleBlock("Universal Powers and Durations",
                            "Universal Force powers use their normal Willpower scaling but neither gain nor lose magnitude or hit chance from affinity. Force Affinity does not change effect duration. Status resistance and explicit duration modifiers can still change a duration."),
                        new ArticleBlock("Example: +6 Light",
                            "At +6 Light, a Light power uses 130% magnitude and gains +3% hit chance. A Dark power uses 70% magnitude and suffers -3% hit chance. A Universal power remains at 100% magnitude with no affinity hit adjustment.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Do extra ranks of one power add more affinity?", "No. Owning any rank contributes one point for that perk; higher ranks of the same perk do not add more."),
                        new QuestionAnswer("Why is my Dark power weaker?", "Positive Light affinity weakens Dark power magnitude and hit chance. Negative Dark affinity does the same to Light powers."),
                        new QuestionAnswer("Do Universal powers move affinity?", "No. They neither change affinity nor receive its magnitude or hit-chance modifiers."),
                        new QuestionAnswer("Does affinity change duration?", "No. Affinity changes magnitude and hit chance, while resistance and duration modifiers handle duration."),
                        new QuestionAnswer("Where can I see the exact effect?", "The Perks window shows your current affinity and the current magnitude and hit-chance effect on a selected Force perk.")
                    },
                    new[] { "Perks", "Abilities", "Attributes", "Combat Basics" }),

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
                            "Each Social point above 10 reduces newly added death debt by 3 percent. Each effective citizen Medical Center level reduces it by another 5 percent, up to an 80 percent combined reduction.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why did my skill not move?", "Your XP reward may have been fully spent reducing XP debt."),
                        new QuestionAnswer("Where can I see debt?", "Open the Skills window and check XP Debt."),
                        new QuestionAnswer("What helps with debt?", "Social above 10 and citizen Medical Center upgrades reduce newly added death debt.")
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
                            "Some perk ranks and equipped Mimicry techniques grant active abilities. A newly granted active ability appears on your character and may be placed on your hotbar."),
                        new ArticleBlock("Ability Details",
                            "Ability descriptions show the FP cost, STM cost, recast time, and what the ability does."),
                        new ArticleBlock("Cooldowns",
                            "If an ability is still cooling down, the game tells you how long to wait before you can use it again."),
                        new ArticleBlock("Recast Groups",
                            "Some abilities share a cooldown group. Using one ability in that group can make the other abilities in the same group wait too.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Where do abilities come from?", "Some perk ranks and equipped Mimicry techniques grant active abilities."),
                        new QuestionAnswer("What is Recast?", "The cooldown seconds shown in the ability description."),
                        new QuestionAnswer("Why can't I use it yet?", "Its cooldown or shared cooldown group still has time remaining.")
                    },
                    new[] { "Perks", "Attributes", "Combat Basics", "Mimicry & Techniques", "Useful Windows", "Common Questions" }),

                new(
                    "Mimicry & Techniques",
                    "Combat",
                    "Learning creature techniques, individual rank requirements, learning chance, analyzer upgrades, and technique slots.",
                    "Learn enemy techniques",
                    new[]
                    {
                        new ArticleBlock("Combat Analyzer",
                            "Combat Analyzer I unlocks technique learning and the Techniques window and provides 2 technique slots. Every technique has its own Mimicry rank requirement based on when its source enemy appears. Higher Combat Analyzer ranks improve equipped technique potency."),
                        new ArticleBlock("Analyzing Creatures",
                            "To analyze a technique, you must be alive, have Combat Analyzer I, be within 15 meters when an eligible creature uses it, and remain in the same area and within 40 meters when that creature dies. Analysis progress is shown in combat messages."),
                        new ArticleBlock("Learning Chance",
                            "Your chance to learn depends on your Mimicry rank above the technique's minimum, Pattern Recognition, and Perception, and is capped at 75 percent. A successful recording permanently teaches the technique and grants Mimicry XP."),
                        new ArticleBlock("Technique Slots",
                            "Learned techniques are not active until equipped in the Techniques window. Techniques have different slot costs. Each Analyzer Memory rank adds 2 slots, and Overclocked Analyzer adds another 2."),
                        new ArticleBlock("Active and Passive Techniques",
                            "Equipped active techniques are granted as usable abilities and can be placed on the hotbar. Passive traits apply while equipped but are not castable. You cannot change equipped techniques during combat.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I start learning techniques?", "Buy Combat Analyzer I, witness an eligible creature technique nearby, and stay near that creature until it dies."),
                        new QuestionAnswer("Why didn't I learn it?", "You must meet the technique's Mimicry rank requirement. Learning is then a chance roll based on Mimicry, Pattern Recognition, and Perception."),
                        new QuestionAnswer("Where do I equip techniques?", "Open Techniques from the Character Sheet while out of combat."),
                        new QuestionAnswer("Do learned techniques disappear?", "No. A successfully recorded technique remains learned, but it only functions while equipped.")
                    },
                    new[] { "Abilities", "Skills", "Perks", "Combat Basics", "Useful Windows" }),

                new(
                    "Attributes",
                    "Character",
                    "Current attribute effects for AP choices and combat stats.",
                    "AP choices and effects",
                    new[]
                    {
                        new ArticleBlock("Might",
                            "Improves melee weapon damage, maximum STM, natural STM regeneration, and carrying capacity."),
                        new ArticleBlock("Perception",
                            "Improves melee weapon accuracy, ranged and throwing weapon damage, and critical hit rate support."),
                        new ArticleBlock("Vitality",
                            "Improves maximum HP, natural HP regeneration, physical defense, and resistance to incoming critical hits."),
                        new ArticleBlock("Willpower",
                            "Improves Force attack, Force defense, maximum FP, natural FP regeneration, and Force ability effectiveness."),
                        new ArticleBlock("Agility",
                            "Improves ranged and throwing weapon accuracy and evasion."),
                        new ArticleBlock("Social",
                            "Improves XP gain and leadership capabilities. It also improves guild point acquisition, quest credit rewards, XP debt reduction on death, and reduces ship repair bills."),
                        new ArticleBlock("Resource Scaling and Regeneration",
                            "Each Willpower point adds 3 maximum FP. Every 2 Might points add 3 maximum STM. Natural HP, FP, and STM regeneration occurs every 30 seconds; the Character Sheet shows the amount restored by each natural-regeneration tick.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I get AP?", "Every 10 earned SP grants 1 AP, up to 40."),
                        new QuestionAnswer("What stats do weapons use?", "Melee weapons use Might for damage and Perception for accuracy. Ranged and throwing weapons use Perception for damage and Agility for accuracy."),
                        new QuestionAnswer("What helps XP gain?", "Social improves XP gain."),
                        new QuestionAnswer("What helps stamina?", "Might increases maximum STM and natural STM regeneration.")
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
                            "Death debt scales with your earned SP. Each Social point above 10 reduces it by 3 percent, and each effective citizen Medical Center level reduces it by 5 percent, up to an 80 percent combined reduction."),
                        new ArticleBlock("Subdual",
                            "If another player defeats you while using subdual, you are subdued instead of going through normal respawn. You are knocked down for 60 seconds, then retain a 5 minute penalty of -50 Accuracy, -50 Evasion, and reduced movement speed."),
                        new ArticleBlock("Resting",
                            "Resting restores HP, FP, and STM every 6 seconds, with natural recovery based on Vitality, Willpower, and Might. You cannot rest in combat, within 20 meters of an enemy, or within 20 meters of a party member in combat. Dungeon resting requires a safe-rest location. Moving, attacking, taking damage, or logging out ends the rest."),
                    },
                    new[]
                    {
                        new QuestionAnswer("Where do I respawn?", "At your registered medical facility, or at the default respawn point if you have not registered one."),
                        new QuestionAnswer("Why did I get XP debt?", "Respawning after death adds XP debt. Higher Social and Medical Center upgrades reduce the amount."),
                        new QuestionAnswer("Why can't I rest?", "You may be in combat, too close to enemies, near a party member in combat, or outside a dungeon safe-rest location."),
                        new QuestionAnswer("What is subdual?", "A PvP defeat setting that replaces normal death with 60 seconds knocked down and a 5 minute accuracy, evasion, and movement penalty.")
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
                            "Accuracy is compared against Evasion to decide whether an attack lands. Final hit chance is clamped between 20 and 95 percent, so very high or very low stats still leave uncertainty."),
                        new ArticleBlock("Damage and Defense",
                            "Physical attacks use Physical DEF and Force attacks use Force DEF; higher defense is better. Fire, poison, electrical, and ice use matching resistances. Sonic has no dedicated resistance. For percentage damage-taken modifiers, lower values are better."),
                        new ArticleBlock("Status Resistance",
                            "Mind, Mobility, Trauma, and Disruption resistance protect against matching categories of harmful status effects. The relevant resistance is shown by the effect or ability when applicable."),
                        new ArticleBlock("Critical Hits",
                            "Critical Rate raises your chance to critically hit, while Critical Damage makes critical hits stronger. The target's Vitality can affect the final critical chance."),
                        new ArticleBlock("Deflection and Guard",
                            $"Melee Deflection can negate hostile melee weapon auto-attacks, and Ranged Deflection can negate hostile ranged weapon auto-attacks. Both require a weapon and no shield. Shield Deflection covers both melee and ranged weapon auto-attacks and completely replaces weapon deflection while a shield is equipped; the chances never stack or roll in sequence. Deflection does not work against activated combat abilities or Force powers, and only one deflection attempt can occur in an incoming combat round. Guard is a separate damage-stage outcome that reduces incoming physical damage by {Combat.BaseGuardDamageReductionPercent} to {Combat.MaximumGuardDamageReductionPercent} percent and increases enmity."),
                        new ArticleBlock("Combat Readiness",
                            "Combat Readiness increases activated ability damage, healing, and temporary HP. It does not reduce cooldowns.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why did I miss?", "Your Accuracy was checked against the target's Evasion, and final hit chance is always between 20 and 95 percent."),
                        new QuestionAnswer("What defense matters?", "Physical DEF helps against physical attacks, Force DEF helps against Force attacks, and resistances help against matching elemental or status types."),
                        new QuestionAnswer("Are deflection and guard the same?", "No. Deflection can stop a hostile weapon auto-attack from landing, while Guard reduces physical damage after the hit."),
                        new QuestionAnswer("Does readiness lower cooldowns?", "No. Combat Readiness improves activated ability damage, healing, and temporary HP.")
                    },
                    new[] { "Abilities", "Attributes", "Skills", "Death & Recovery", "Espionage", "Useful Windows" }),

                new(
                    "Espionage",
                    "Activities",
                    "Standard-character infiltration, slicing, poisoncraft, trapcraft, and how Espionage XP is earned.",
                    "Stealth, locks, poisons, traps",
                    new[]
                    {
                        new ArticleBlock("Character Requirement",
                            "Espionage perks are for Standard characters and use the Espionage skill. Their branches cover infiltration and back attacks, slicing and poisoncraft, traps, disguises, and utility."),
                        new ArticleBlock("Stealth and Back Attacks",
                            "Stealth is activated out of combat and drains STM while maintained. Hostile actions break stealth. Back Attack bonuses require attacking from behind the target, so position matters."),
                        new ArticleBlock("Slicing Lockboxes",
                            "Slicing perks let you attempt matching-tier lockboxes. Success depends on Lockpicking and Perception. A failed attempt leaves the box intact but starts a 30 second retry wait on that box; success grants its loot and Espionage XP."),
                        new ArticleBlock("Poisoncraft",
                            "Poisoncraft recipes create weapon coatings. Applying a coating loads venom charges onto a valid weapon, and damaging attacks consume charges to deliver the venom's effect."),
                        new ArticleBlock("Trapcraft",
                            "Trapcraft recipes create trap kits used by trap abilities. You can maintain 1 trap by default, with Trap Management increasing the limit. Traps must be at least 3 meters apart, take 3 seconds to arm, last up to 5 minutes, and the oldest trap is removed if you exceed your limit."),
                        new ArticleBlock("Detecting and Disarming",
                            "Kit-built traps can be concealed. Characters with sufficient detection and trap tier can reveal or disarm them. A failed disarm triggers the trap; successful enemy triggers and successful disarms can grant Espionage XP."),
                        new ArticleBlock("Espionage XP",
                            "Espionage XP comes from bypassing hostile NPCs while stealthed, slicing, crafting espionage items, enemy trap triggers, and successful trap disarms. A stealth bypass requires you to evade Detection, move through the NPC's nearby threat area, and leave it while still hidden. Each NPC grants XP only once per life; being detected grants a smaller amount.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Who can use espionage perks?", "Standard characters who meet the relevant Espionage skill and perk requirements."),
                        new QuestionAnswer("Why can't I retry a lockbox?", "A failed slicing attempt starts a 30 second retry wait on that box."),
                        new QuestionAnswer("How many traps can I place?", "One by default; Trap Management increases your active-trap limit."),
                        new QuestionAnswer("What breaks stealth?", "Hostile actions break stealth, and maintaining it continuously drains STM.")
                    },
                    new[] { "Skills", "Perks", "Abilities", "Combat Basics", "Crafting" }),

                new(
                    "Disguises",
                    "Social",
                    "Creating identities, public and private fields, activation cooldowns, retirement, restoration, and permanent wiping.",
                    "Alternate identities",
                    new[]
                    {
                        new ArticleBlock("Opening Disguises",
                            "Open Disguises from the Character Sheet, or use /disguise or /disguises. A disguise stores a private slot label only you can see, plus a public description, appearance, portrait, sound set, and optional scrambled account identifier."),
                        new ArticleBlock("Separate Presented Identities",
                            "Your normal identity and every disguise are remembered separately. A private label someone saved for your normal identity does not carry over to a disguise, and a label saved for one disguise does not carry over to another. Each observer may label the same disguise differently. Activating or deactivating a disguise automatically restores the labels that observer previously saved for the identity you are presenting."),
                        new ArticleBlock("Identity Slots",
                            "You begin with 1 identity slot. Each False Identities rank adds 1, up to 4 total slots. Every saved identity, including a retired one, occupies a slot. Retiring does not free it; only permanently wiping it does."),
                        new ArticleBlock("Activating",
                            "An active disguise changes your presented identity and appearance. The base wait between disguise activations is 30 minutes. Cover Story reduces that wait by 40 or 70 percent, with a minimum wait of 5 minutes. Deactivating a disguise is immediate."),
                        new ArticleBlock("Retiring and Restoring",
                            "Retiring an identity deactivates it and prevents activation, but keeps its saved setup. You can restore a retired identity from the Disguises window if you want to use it again."),
                        new ArticleBlock("Permanently Wiping",
                            "An Identity Broker can permanently wipe a retired identity for 100,000 credits or 25,000 Available RP XP. Wiping frees the slot and removes other characters' private-label references to that identity. This cannot be undone."),
                        new ArticleBlock("Staff Accountability",
                            "A disguise does not hide your underlying character from staff. Staff tools and server audit logs retain the real character and account identity. Player-created labels are in-character memory aids, not proof of a character's real identity in an out-of-character dispute.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How many disguises can I keep?", "One initially, up to four total with False Identities."),
                        new QuestionAnswer("Why can't I activate another disguise?", "The activation cooldown may still be running. Cover Story reduces it, but it cannot go below 5 minutes."),
                        new QuestionAnswer("Does retiring free the slot?", "No. Restore it for reuse, or permanently wipe it at an Identity Broker to free the slot."),
                        new QuestionAnswer("What does a permanent wipe remove?", "The saved identity and other characters' private-label references to it. The wipe cannot be undone."),
                        new QuestionAnswer("Does a saved name carry between disguises?", "No. Your normal identity and every disguise are remembered separately."),
                        new QuestionAnswer("Can a disguise hide my identity from staff?", "No. Staff tools and audit logs retain your real character and account identity.")
                    },
                    new[] { "Communication", "Useful Windows", "Skills", "Perks" }),

                new(
                    "Crafting",
                    "Activities",
                    "Recipes, CP actions, quality, durability, enhancements, failure risks, blueprint research, and refining.",
                    "Recipes and refining",
                    new[]
                    {
                        new ArticleBlock("Recipes",
                            "The Recipes window lets you search recipes, filter to craftable recipes, and review level, quantity, enhancement slots, required components, and requirements."),
                        new ArticleBlock("Starting A Craft",
                            "Use the proper crafting device, choose a recipe or blueprint, and supply the required inventory components. Starting from a blueprint can cost credits and consumes one licensed run. Components and selected enhancements are held by the crafting session once added."),
                        new ArticleBlock("CP, Progress, Quality, Durability",
                            "Crafting spends CP on actions. Progress must reach the goal before Durability reaches 0. Quality increases crafting XP, vendor value, and each enhancement's transfer chance. A maximum-quality food craft gains 5 minutes of duration; when made from a blueprint, it also gains extra charges equal to the blueprint level."),
                        new ArticleBlock("Using Food",
                            "Food effects last 30 minutes by default. You cannot consume another food item while an existing food effect is active. Maximum-quality crafted food gains 5 minutes, and maximum-quality food made from a blueprint also gains extra charges equal to the blueprint level."),
                        new ArticleBlock("Enhancements",
                            "Enhancements must be in your inventory, match the recipe's enhancement slot type, and be within 5 levels of the recipe. Some enhancements make the craft harder."),
                        new ArticleBlock("Success and Failure",
                            "A successful craft consumes the committed components and enhancements, creates the item, and rolls each enhancement transfer separately; an enhancement is not returned if that roll fails. Success grants crafting skill XP and records first-time recipe completion. Overall craft failure grants reduced XP, and each selected component or enhancement has a 65 percent chance to be lost."),
                        new ArticleBlock("Closing the Window",
                            "Closing before craft mode begins returns inserted items. Closing after the craft begins immediately fails the craft, so finish the attempt before closing the window."),
                        new ArticleBlock("Research",
                            "Research terminals require Research I. Research jobs cost credits and time, work with weapon, armor, and food recipes, and can create a level 1 blueprint or improve one up to level 10. The recipe determines the required Research rank. You may run 1 personal job plus 1 per Research Projects rank, but each terminal handles only one job at a time. Cancelling forfeits credits and progress but returns the blueprint."),
                        new ArticleBlock("Refining",
                            "Refineries turn raw materials into refined materials. Refining requires the material's Refining rank, takes 6 seconds, consumes Power Cores, and grants Gathering XP. One Power Core processes 3 items plus 1 additional item per Refinery Management rank.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What ends a craft?", "Success happens when Progress reaches the goal. Failure happens if Durability reaches 0 first."),
                        new QuestionAnswer("What does Quality do?", "It increases crafting XP, vendor value, and enhancement transfer chance. Maximum-quality food gains 5 minutes, plus extra charges equal to blueprint level when a blueprint was used."),
                        new QuestionAnswer("Why can't I use an enhancement?", "It may not match the slot type, may be too far from the recipe level, or may not be in your inventory."),
                        new QuestionAnswer("What happens if I close crafting?", "Before craft mode, inserted items return to you. After craft mode starts, closing immediately fails the craft."),
                        new QuestionAnswer("What does research do?", "Research spends credits and time to create or improve weapon, armor, and food blueprints up to level 10.")
                    },
                    new[] { "Skills", "Perks", "Gathering & Fishing", "Training Store", "Housing & Markets", "Useful Windows" }),

                new(
                    "Gathering & Fishing",
                    "Activities",
                    "Harvesters, scavenging, refining links, fishing rods, bait, and gathering-skill progression.",
                    "Resources and fishing",
                    new[]
                    {
                        new ArticleBlock("Harvesting Resources",
                            "Use a charged harvester on a matching resource node. The harvester and resource can require an appropriate Harvesting perk rank. Harvesting takes 5 seconds, consumes a harvester charge, grants Gathering XP, and Might can provide bonus material."),
                        new ArticleBlock("Scavenging",
                            "Scavenge sites require the displayed Scavenging rank. Each search rolls Perception against the site, grants Gathering XP even on failure, and marks that site fully searched. Hard Look plus Perception can grant a second search attempt, Treasure Hunter improves item results, and Credit Finder improves credit finds."),
                        new ArticleBlock("Refining Materials",
                            "Raw materials can be processed at refineries when you have the matching Refining rank. Refining consumes Power Cores and grants Gathering XP; see Crafting for the batch-size and timing details."),
                        new ArticleBlock("Loading A Fishing Rod",
                            "Equip a fishing rod in your right hand. Use the rod on bait in your inventory to load it. Loading different bait first unloads any remaining bait back into your inventory."),
                        new ArticleBlock("Fishing",
                            "Click a fishing point while within 10 meters, with a baited rod in your right hand. Casting takes 6 to 8 seconds and movement interrupts it. An actual fishing attempt consumes 1 bait. Agriculture skill, rod type, bait, location, and time of day affect the result, and fishing grants Agriculture XP.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why won't my harvester work?", "It may be out of charges, too far from the node, the wrong harvester, or require a higher matching Harvesting rank."),
                        new QuestionAnswer("What improves scavenging?", "Scavenging unlocks harder sites; Perception, Hard Look, Treasure Hunter, and Credit Finder improve different parts of the result."),
                        new QuestionAnswer("How do I load bait?", "Equip the fishing rod, use it, and target bait in your inventory."),
                        new QuestionAnswer("Why did fishing stop?", "Moving during the 6 to 8 second cast interrupts the attempt.")
                    },
                    new[] { "Skills", "Perks", "Crafting", "Useful Windows" }),

                new(
                    "Training Store",
                    "Progression",
                    "Available RP XP, exact tome prices, refund currencies, Cantina discounts, Kyber Tokens, and lightsaber construction.",
                    "XP store and tokens",
                    new[]
                    {
                        new ArticleBlock("Available XP",
                            "Available XP is separate from skill XP. You can spend it in the Training Store or distribute it into a selected skill from the Skills window."),
                        new ArticleBlock("Training Store Items",
                            "The Training Store sells Perk Refund Tomes for 10,000 Available XP and Stat Refund Tomes for 300,000 Available XP before discounts. Each effective Cantina level from your citizen property lowers those prices by 10 percent."),
                        new ArticleBlock("Perk Refund Tome",
                            "Using a Perk Refund Tome adds 1 Perk Refund Token and consumes the tome. Perk Refund Tokens cap at 99."),
                        new ArticleBlock("Stat Refund Tome",
                            "Using a Stat Refund Tome adds 1 Stat Refund Token and consumes the tome. Stat Refund Tokens cap at 99."),
                        new ArticleBlock("Currencies Window",
                            "The Currencies window shows Rebuild Tokens, Perk Refund Tokens, Stat Refund Tokens, and attuned Kyber Tokens."),
                        new ArticleBlock("Kyber and Lightsabers",
                            "Kyber Tokens are issued by staff. Use a Kyber Token item to attune it into the Currencies window. A Force-sensitive character can then consume the attuned token at a Lightsaber Workbench to construct a lightsaber or saberstaff. Up to 2 optional weapon enhancements and an optional Weapon Submission Token placed into the construction are also consumed when the saber is built.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I get perk refund tokens?", "Buy a Perk Refund Tome from the Training Store and use it."),
                        new QuestionAnswer("How do I get stat refund tokens?", "Buy a Stat Refund Tome from the Training Store and use it."),
                        new QuestionAnswer("What lowers tome prices?", "Cantina upgrades from your citizen property can reduce Training Store XP prices."),
                        new QuestionAnswer("Where do I see tokens?", "Open Currencies from the Character Sheet."),
                        new QuestionAnswer("What is a Kyber Token for?", "A Force-sensitive character consumes an attuned Kyber Token at a Lightsaber Workbench to construct a saber.")
                    },
                    new[] { "Perk Refunds", "Rebuilds", "Skills", "Crafting", "Guilds & Citizenship", "Useful Windows" }),

                new(
                    "Beasts & Stables",
                    "Activities",
                    "Taming, stable capacity, companion limits, beast XP and perks, food, revival, eggs, DNA, and incubation.",
                    "Taming and pets",
                    new[]
                    {
                        new ArticleBlock("Taming",
                            "Tame requires the Tame perk, no active beast, a valid creature target, and open stable capacity. Your Tame rank controls the highest creature level you can tame. Chance starts at 40 percent, changes by 3 percentage points for each Beast Mastery rank above or below the creature's level, gains 3 points per Social, and is capped at 75 percent. Failure makes the creature hostile toward you."),
                        new ArticleBlock("Stable Capacity",
                            "You can keep 1 beast plus 1 more for each Stabling perk rank. The Stables window shows your count, marks the active beast in green, and lets you make a beast active or inactive. A beast and a droid both use the single active henchman-companion slot, so they cannot be active together."),
                        new ArticleBlock("Beast XP and SP",
                            "Beasts gain XP, level up, and earn their own unallocated SP. Tame rank limits how far a beast can level, and beast level cannot exceed 50."),
                        new ArticleBlock("Beast Perks",
                            "With an active beast, the Perks window can switch to Beast Perks. Beast perks spend the beast's SP and use the beast's level and role requirements."),
                        new ArticleBlock("Pet Food",
                            "Pet food requires an active beast and the food tier's minimum beast level. It grants a 30 minute beast XP bonus. A favorite food adds 10 percentage points to the tier bonus, while a hated food subtracts 5. A beast that already has the food effect is not hungry yet."),
                        new ArticleBlock("Reviving Beasts",
                            "When your active beast is unconscious, Revive Beast can bring it back if no other companion is active. Higher ranks revive with more HP, Social improves the HP returned by higher ranks, and the ability has a 90 second recast."),
                        new ArticleBlock("Eggs and DNA",
                            "Using a beast egg requires Tame I, no active beast, and open stable capacity. Releasing a beast is irreversible and gives DNA based on the beast's level."),
                        new ArticleBlock("Incubation",
                            "Incubators require DNA Manipulation I and use DNA plus enzymes over timed stages to produce improved beast eggs. Your personal concurrent-job limit is 1 plus Incubation Management rank, up to 3, while each incubator handles only one job. Incubation Processing shortens job time. Cancelling an active job permanently loses all DNA and enzymes committed to it.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Why can't I tame this creature?", "You may already have an active beast, lack stable capacity, lack Tame rank, or the creature may not be tameable."),
                        new QuestionAnswer("How many beasts can I keep?", "One beast plus one additional beast for each Stabling perk rank."),
                        new QuestionAnswer("How do beast perks work?", "They spend the beast's SP, not yours, and use the beast's level and role."),
                        new QuestionAnswer("How do I revive my beast?", "Use Revive Beast when your active beast is unconscious and no other companion is active.")
                    },
                    new[] { "Perks", "Skills", "Crafting", "Droids", "Useful Windows" }),

                new(
                    "Droids",
                    "Activities",
                    "Assembly parts, controllers, activation requirements, companion limits, appearance, AI instructions, and cooldowns.",
                    "Build and program droids",
                    new[]
                    {
                        new ArticleBlock("Droid Assembly",
                            "Droid assembly terminals require Droid Assembly I. A complete droid needs a CPU, head, body, arms, legs, personality, and name. Assembly consumes the selected parts and creates a Droid Control Unit."),
                        new ArticleBlock("Part Tiers",
                            "Your Droid Assembly rank must meet the CPU tier. Head, body, arms, and legs cannot exceed the CPU tier. The chosen parts determine the droid's statistics, equipment, appearance options, and AI capacity."),
                        new ArticleBlock("Activating A Droid",
                            "Use the controller's first action to activate the droid. Droid tier requires an average combat level of tier x 10 minus 10. That level is half the sum of your Armor rank and your highest supported weapon, Force, Devices, or First Aid rank, rounded down. You cannot activate or adjust droids in space, and a droid uses the same single companion slot as a beast."),
                        new ArticleBlock("Appearance and Equipment",
                            "Use the controller's appearance action while that droid is active. Droids can carry equipment and inventory appropriate to their assembled design; their state is saved back to the controller when dismissed."),
                        new ArticleBlock("Programming AI",
                            "Dismiss the droid before adjusting its AI. Your Droid Assembly rank must meet its tier. Uploading a matching instruction disc permanently consumes the disc and teaches its routine; instruction tier cannot exceed droid tier. Choose learned routines up to the controller's available AI-slot budget."),
                        new ArticleBlock("Dismissal and Destruction",
                            "Dismissing a droid or losing it in combat saves it to the controller and starts a 30 minute Droid Controller cooldown before it can be activated again. Entering space also dismisses an active droid.")
                    },
                    new[]
                    {
                        new QuestionAnswer("What parts does a droid need?", "A CPU, head, body, arms, legs, personality, and name."),
                        new QuestionAnswer("Why can't I activate my droid?", "You may be in space, have another companion, lack the required average combat level, or still have a Droid Controller cooldown."),
                        new QuestionAnswer("How do I change droid AI?", "Dismiss it, use the controller's AI action, upload eligible instruction discs, and equip routines within its AI-slot budget."),
                        new QuestionAnswer("Can I use a beast and droid together?", "No. They share one active henchman-companion slot.")
                    },
                    new[] { "Beasts & Stables", "Crafting", "Skills", "Perks", "Ships & Space" }),

                new(
                    "Quests & Key Items",
                    "Activities",
                    "Active quests, objectives, abandoning, rewards, key items, achievements, guild tasks, and player contracts.",
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
                            "Quests can grant credits, skill XP, items, key items, or guild points."),
                        new ArticleBlock("Key Items",
                            "Key Items are character-specific records of important unlocks, permissions, passes, maps, receipts, and other progress. Open Key Items from the Character Sheet."),
                        new ArticleBlock("Achievements",
                            "Achievements show long-term account-wide progress. The Achievements window marks acquired achievements in green, locked achievements in red, and shows the acquired date when available."),
                        new ArticleBlock("Guild Tasks and Contracts",
                            "Guildmasters offer rotating guild tasks that award credits and Guild Points. Player-authored jobs are handled separately at Quest Contract Boards; accepted contracts also appear in your active Quest window.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Where are my quest objectives?", "Open Quests and select the quest. Objectives appear under the journal text."),
                        new QuestionAnswer("Why don't I see completed quests?", "The Quest window focuses on active quests, so completed quests are hidden there."),
                        new QuestionAnswer("What are key items?", "Permanent progress records such as passes, maps, receipts, unlocks, and permissions."),
                        new QuestionAnswer("Where are achievements?", "Open Achievements from the Character Sheet."),
                        new QuestionAnswer("Where are player contracts?", "Use a Quest Contract Board to browse, publish, turn in, or claim contract deliveries.")
                    },
                    new[] { "Useful Windows", "Skills", "Training Store", "Quest Contracts", "Guilds & Citizenship", "Housing & Markets" }),

                new(
                    "Quest Contracts",
                    "Activities",
                    "Publishing player jobs, item objectives, escrow, posting fees, acceptance, turn-in, expiration, and deliveries.",
                    "Player-posted item jobs",
                    new[]
                    {
                        new ArticleBlock("Contract Boards",
                            "Quest Contract Boards let you browse and accept player-posted item-delivery jobs, create drafts, publish your own contracts, turn in requested items, manage your postings, and claim pending deliveries."),
                        new ArticleBlock("Creating A Contract",
                            "A contract must request between 1 and 3 item objectives, with 1 to 99 items per objective. It must offer at least 1 credit and may include up to 2 reward items. Each account may have up to 3 published contracts active at once."),
                        new ArticleBlock("Drafts, Escrow, and Posting Fee",
                            "Adding a reward item to a draft immediately removes it from your inventory and holds it in escrow. Removing it from the draft returns it if you have room; deleting the draft returns escrowed items through a pending delivery. Publishing escrows the advertised credit reward and charges a non-refundable posting fee equal to 5 percent of that reward, with a minimum fee of 100 credits."),
                        new ArticleBlock("Accepting and Turning In",
                            "An accepted contract appears in the Quest window. Deliver requested items with Turn In at any Contract Board. Each contract has one completion available, so the first eligible player to complete it receives the escrowed reward."),
                        new ArticleBlock("Cancellation and Expiration",
                            "Published contracts last 30 days. Cancelling or expiring a contract returns its remaining escrow through a pending board delivery, but never refunds the posting fee."),
                        new ArticleBlock("Pending Deliveries",
                            "Claim contract rewards, submitted items, and escrow refunds from any Contract Board. If an item cannot fit in your inventory, it stays in the delivery so you can make room and retry. If a contract becomes inactive while you turn in items, those items are routed back to you as a delivery.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How many contracts can I post?", "Up to 3 active published contracts per account."),
                        new QuestionAnswer("Is the posting fee refunded?", "No. The 5 percent fee, with a 100 credit minimum, is non-refundable."),
                        new QuestionAnswer("Where do accepted contracts appear?", "In your active Quest window."),
                        new QuestionAnswer("Where do I turn in or claim items?", "At any Quest Contract Board."),
                        new QuestionAnswer("What if my inventory is full?", "Undelivered items remain pending; make room and claim the delivery again.")
                    },
                    new[] { "Quests & Key Items", "Housing & Markets", "Crafting", "Useful Windows" }),

                new(
                    "Guilds & Citizenship",
                    "Activities",
                    "Open guilds, rotating tasks, Guild Points and ranks, city registration, taxes, upgrades, and elections.",
                    "Guild work and civic life",
                    new[]
                    {
                        new ArticleBlock("Guilds",
                            "Guilds are freely open and have no joining fee. Guildmasters offer rotating tasks such as hunting or supplying requested goods. Tasks award credits and Guild Points, and accumulating enough Guild Points raises your rank with that guild."),
                        new ArticleBlock("Guild Ranks and Rewards",
                            "Higher guild ranks unlock additional rank stores. Current guild rank, Guild Relations, and Social improve Guild Point rewards; Social and Guild Relations can also improve guild quest credit rewards."),
                        new ArticleBlock("Becoming A Citizen",
                            "Citizenship costs 5,000 credits. Your character must be at least 30 days old and have acquired at least 100 cumulative skill ranks. You may be a citizen of only one city at a time."),
                        new ArticleBlock("Taxes and City Services",
                            "Citizens gain access to their city's public facilities and accrue the city's weekly citizenship tax. Starport flights also charge the city's transportation tax. Review the current rates and pay owed citizenship taxes from the Citizenship window. City Medical Center, Starport, and Cantina upgrades can improve death-debt reduction, ship repair discounts, Training Store prices, and RP XP."),
                        new ArticleBlock("Revoking Citizenship",
                            "Revoking removes public-facility access, city permissions, and election candidacy. A sitting mayor cannot revoke citizenship. Review the warning before confirming."),
                        new ArticleBlock("Elections",
                            "City elections have a 14-day candidate-registration period followed by 7 days of voting. Citizens can run for mayor. Each account has one vote in that city's election, and the selection can be changed or cleared while voting remains open.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Do guilds have a joining fee?", "No. Active guilds are freely open."),
                        new QuestionAnswer("What do Guild Points do?", "They raise your guild rank and unlock rank-store access."),
                        new QuestionAnswer("What does citizenship require?", "5,000 credits, a character at least 30 days old, and at least 100 acquired skill ranks."),
                        new QuestionAnswer("Where do I pay city taxes?", "Open the Citizenship window at the city's citizenship terminal."),
                        new QuestionAnswer("How long is an election?", "Fourteen days of registration followed by seven days of voting.")
                    },
                    new[] { "Quests & Key Items", "Training Store", "Housing & Markets", "Ships & Space", "Skills" }),

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
                        new ArticleBlock("Lease Expiration",
                            "Keep the lease extended before its displayed expiration time. An expired apartment is queued for removal on the next server restart, including its structures and stored contents."),
                        new ArticleBlock("Structures and Permissions",
                            "Properties have structure limits and permissions. Depending on the property, permissions can allow entering, editing structures, retrieving structures, renaming, changing descriptions, accessing storage, or managing special property features."),
                        new ArticleBlock("Bank Storage",
                            "Banks store inventory items up to the bank's item limit. Deposit targets an item in your inventory, and Withdraw returns the selected stored item to you."),
                        new ArticleBlock("Buying From Markets",
                            "Market buy windows let you search, filter by category, sort by price, examine items, and buy listings you can afford. You cannot buy listings from your own account."),
                        new ArticleBlock("Selling On Markets",
                            "Market listing windows let you add inventory items, set prices, list or unlist them, and remove listings back to your inventory. The normal listing limit is 25 items, shown in the window. Listings older than two weeks are automatically unlisted."),
                        new ArticleBlock("Market Till",
                            "When another player buys your listing, the market tax is removed and the remaining credits go to your market till. Use the listing window to collect the till.")
                    },
                    new[]
                    {
                        new QuestionAnswer("Can I rent more than one apartment?", "No. You can have one active apartment lease."),
                        new QuestionAnswer("How far can I extend a lease?", "Up to 30 days ahead."),
                        new QuestionAnswer("What happens if my lease expires?", "The apartment and its contents are removed on the next server restart."),
                        new QuestionAnswer("How many market items can I list?", "The normal limit is 25; the listing window shows your current personal limit."),
                        new QuestionAnswer("Where do sold-item credits go?", "To your market till after market tax."),
                        new QuestionAnswer("Why was my listing unlisted?", "Listings older than two weeks are automatically unlisted.")
                    },
                    new[] { "Crafting", "Training Store", "Quest Contracts", "Guilds & Citizenship", "Ships & Space", "Useful Windows" }),

                new(
                    "Travel & Navigation",
                    "Activities",
                    "CZ-220 orientation, scheduled starport flights, tickets and refunds, persistent transit, and local taxi routes.",
                    "Shuttles and taxis",
                    new[]
                    {
                        new ArticleBlock("Leaving CZ-220",
                            "Complete the CZ-220 orientation quest to receive the CZ-220 Shuttle Pass. Starport flight terminals require this key item before you can book interplanetary travel."),
                        new ArticleBlock("Scheduled Flights",
                            "Starport terminals show each destination's fare, local transportation tax, next departure, and transit time. You may hold only one shuttle ticket at a time."),
                        new ArticleBlock("Boarding",
                            "Be within 15 meters of your departure terminal when boarding is called. If you miss the shuttle, your ticket remains valid and rolls over to the next scheduled departure."),
                        new ArticleBlock("Refunds and Transit",
                            "Before boarding, you may refund the ticket at its departure starport. Only the fare is returned; transportation tax is not refunded. Ticketed and in-transit journeys persist through logout and server restart, and an in-transit character is delivered correctly on return."),
                        new ArticleBlock("Taxi Destinations",
                            "Local taxi travel requires the Taxi Hailing Device key item. Visit a taxi terminal and register its location, then use terminals to travel to registered destinations in the same region for the displayed credit fare.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I leave CZ-220?", "Complete orientation to earn the CZ-220 Shuttle Pass, then use a starport flights terminal."),
                        new QuestionAnswer("What if I miss my shuttle?", "Your ticket rolls over to the next scheduled departure."),
                        new QuestionAnswer("Can I refund a ticket?", "Yes, before boarding at the departure starport. The fare returns, but tax does not."),
                        new QuestionAnswer("How do I unlock taxi routes?", "Obtain the Taxi Hailing Device and register each location at its taxi terminal.")
                    },
                    new[] { "Quests & Key Items", "Guilds & Citizenship", "Ships & Space", "Housing & Markets" }),

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
                    new[] { "Housing & Markets", "Travel & Navigation", "Perks", "Combat Basics", "Useful Windows" }),

                new(
                    "Useful Windows",
                    "Interface",
                    "The main windows and shortcuts new players commonly need.",
                    "Guide and sheet",
                    new[]
                    {
                        new ArticleBlock("Player Guide",
                            "Press B to open this Player Guide. Search checks topic names, summaries, article text, and quick answers. Clear the search to browse everything, and use Related Topics to jump between connected systems."),
                        new ArticleBlock("Character Sheet",
                            "Press C to open the Character Sheet. It shows your SP, AP, attributes, combat values, and character actions."),
                        new ArticleBlock("Guide Button",
                            "The Character Sheet also has a Guide button, so you can return here while reviewing your character."),
                        new ArticleBlock("Character Sheet Actions",
                            "The Character Sheet action list opens Skills, Perks, Techniques, Appearance, Disguises, Quests, Open Trash, HoloCom, Recipes, Currencies, Key Items, Notes, this Guide, Achievements, and Settings."),
                        new ArticleBlock("Skills and Perks",
                            "Skills shows ranks, XP, XP debt, decay locks, and RP XP distribution. Perks shows player perks and, when you have an active beast, beast perks."),
                        new ArticleBlock("Quests and Progress",
                            "Press J to open Quests, which shows active quest objectives. Key Items shows character-specific unlocks and progress records. Achievements shows long-term account-wide progress."),
                        new ArticleBlock("Social Windows",
                            "Notes stores private notes. Settings controls notifications, subdual mode, reset reminders, Mini-Vitals, identity/privacy options, chat colors, and description access. HoloCom opens calling options when you are not in space."),
                        new ArticleBlock("Recovery and Utility",
                            "Open Trash provides a disposal container. Appearance and Disguises manage your visible presentation. If a window becomes unreachable, use /resetwindows. Use /help for the full command list and /bug to report a problem.")
                    },
                    new[]
                    {
                        new QuestionAnswer("How do I open the guide?", "Press B."),
                        new QuestionAnswer("How do I open my character sheet?", "Press C."),
                        new QuestionAnswer("Where is the guide button?", "Open the Character Sheet and use the Guide button near the lower part of the action list."),
                        new QuestionAnswer("Where are currencies?", "Open the Character Sheet and choose Currencies."),
                        new QuestionAnswer("Where are quests and key items?", "Open the Character Sheet and choose Quests or Key Items."),
                        new QuestionAnswer("How do I recover a lost window?", "Use /resetwindows to restore saved window positions.")
                    },
                    new[] { "Common Questions", "Skills", "Perks", "Mimicry & Techniques", "Training Store", "Quests & Key Items", "Communication", "Disguises" })
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
