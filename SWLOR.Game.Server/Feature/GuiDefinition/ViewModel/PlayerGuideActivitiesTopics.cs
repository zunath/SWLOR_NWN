using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Activities category, one method per topic.</summary>
    internal static class PlayerGuideActivitiesTopics
    {
        internal static PlayerGuideTopic CreateEspionage()
        {
            return new(
                "Espionage",
                "Activities",
                "Infiltration, slicing, poisoncraft, trapcraft, Force Sensitive spies, and how Espionage XP is earned.",
                "Stealth, locks, poisons, traps",
                new[]
                {
                    new ArticleBlock("Character Requirement",
                        "Standard and Force Sensitive characters both train the Espionage skill. Tradecraft (disguises) and most of Infiltrator (stealth, slicing, back attacks) are shared at the same ranks and prices. Standard characters keep Tactical Escape, Shadow Step, Ghost Protocol, and all of Saboteur, including poisons and traps. Force Sensitive characters get Force Decoy, Cloud Mind, and Shared Presence in those three Infiltrator slots instead."),
                    new ArticleBlock("Starting at Rank 0",
                        "Choose Stealth I, Poisoncraft I, Slicing I, or Trapcraft I in the Perks window. All four are available at Espionage rank 0; Force Sensitive characters start with Stealth I or Slicing I. You can train with your chosen activity without buying the other starter perks. Craft at a marked Espionage Workbench beside a market terminal in Veles Shops."),
                    new ArticleBlock("Stealth and Back Attacks",
                        "Stealth is activated out of combat and drains STM while maintained, and no STM recovers while hidden. Other players within 5m spot you more easily and more often, and a lucky player can always spot you. Hostile actions break stealth. Back Attack bonuses require attacking from behind the target, so position matters. The rank 50 capstones that hide you, Ghost Protocol and Shared Presence, require Stealth I."),
                    new ArticleBlock("Force Sensitive Spies",
                        "Force Decoy (ranks 12 and 35) drops your enmity and makes organic enemies attacking you miss you more often. Cloud Mind (ranks 25 and 45) stops an organic enemy from spotting anyone in stealth until it takes damage; a clouded player still spots stealthers on a natural 20. Shared Presence (rank 50, needs Stealth I) hides you and party members within 5m for up to 30 seconds out of combat, even members without the Stealth perk. These three count as Force powers and cost FP. Stealth itself still drains STM for everyone."),
                    new ArticleBlock("Slicing Lockboxes and Sealed Crates",
                        "Right-click a lockbox and choose Activate Item, or use a Sealed Field Crate to slice its electronic lock. Rotate or swap circuit tiles to connect the entry to the core before trace runs out. Every board is different and leaves little spare trace, so plan your route before you move. Each Slicing rank above a target's tier adds 1 trace, and Lockpicking gear plus Perception add up to 2 more. Tier 1 crates can be found in the Viscaran Wildlands, CZ-220 maintenance level, and Abandoned Nano Station. Slicing II-V unlock at Espionage ranks 22, 30, 42, and 48. Each tier awards XP until the next unlock; tier 5 trains to 50."),
                    new ArticleBlock("Slicing Supplies and Failure",
                        "Consumables are optional. The Veles General Store sells tier 1 Copper Trace Fuses, Ratchet Bypass Pins, and Continuity Samplers. Select supplies inside the slicing window. Advanced tools come from slicing rewards; higher-tier trace fuses are crafted by engineers. Cancelling before commitment is free. After a move or information-tool use, aborting counts as failure. The first lockbox failure cannot destroy it; later failures can. Field crates are shared and respawn after 45-75 minutes."),
                    new ArticleBlock("Poisoncraft",
                        "Start with Venom Coating I: 3 Kath Hound Blood and 2 Viscaran Rosemary. Recipe level 3 means crafting difficulty; its required Espionage rank is 0. Poisoncraft II-V unlock at ranks 15, 28, 40, and 48. Each coating recipe trains to the next unlock, with tier 5 training to 50. Right-click a vial, choose Activate Item, and select a melee or thrown weapon in your inventory. Anyone can apply coatings; energy blades cannot be coated."),
                    new ArticleBlock("Trapcraft",
                        "Start with Snare Kit I: 3 Ruined Electronics and 2 Refined Veldite. Right-click the kit and choose Activate Item to deploy at your feet; no active trap ability is needed. Stay still during placement. Trapcraft II-IV unlock at ranks 18, 30, and 45; Master Saboteur unlocks tier 5 at 50. Kit recipes and hostile NPC triggers train to the next tier. You can maintain 1 trap by default. Traps need 3 meters of spacing, arm after 3 seconds, and last 5 minutes. Trap Management increases capacity; placing over capacity removes the oldest trap."),
                    new ArticleBlock("Detecting and Disarming",
                        "Kit-built traps are concealed. Matching Trapcraft training reveals nearby traps and allows disarming; Master Saboteur grants tier 5 access. A failed disarm triggers the trap. Disarming another player's trap grants no XP. Dismantling your own trap does not return a kit."),
                    new ArticleBlock("Espionage XP",
                        "Espionage XP comes from bypassing hostile NPCs while stealthed, slicing, crafting espionage items, enemy trap triggers, and successful trap disarms. A stealth bypass requires you to evade Detection, move through the NPC's nearby threat area, and leave it while still hidden. Each NPC grants XP only once per life; being detected grants a smaller amount. Enemies under Cloud Mind and stealth granted by Shared Presence give no bypass XP.")
                },
                new[]
                {
                    new QuestionAnswer("Who can use espionage perks?", "Both character types. Saboteur, Tactical Escape, Shadow Step, and Ghost Protocol are Standard-only; Force Decoy, Cloud Mind, and Shared Presence are Force Sensitive-only. Anyone can apply venom coatings."),
                    new QuestionAnswer("Do I need stealth to learn poisons or slicing?", "No. Poisoncraft I, Slicing I, and Trapcraft I each start at rank 0 and can train independently."),
                    new QuestionAnswer("Why did my recipe or slicing tier stop giving XP?", "You reached the next tier's unlock rank. Purchase that tier's perk and use its recipes or targets to continue training."),
                    new QuestionAnswer("Where do I craft poisons and snares?", "At a marked Espionage Workbench beside a market terminal in Veles Shops."),
                    new QuestionAnswer("How many traps can I place?", "One by default; Trap Management increases your active-trap limit."),
                    new QuestionAnswer("What breaks stealth?", "Hostile actions break stealth, and maintaining it continuously drains STM with no STM recovery until you leave stealth.")
                },
                new[] { "Skills", "Perks", "Abilities", "Combat Basics", "Crafting" });
        }

        internal static PlayerGuideTopic CreateCrafting()
        {
            return new(
                "Crafting",
                "Activities",
                "Recipes, crafting minigames, quality, enhancements, quick craft, failure risks, blueprint research, and refining.",
                "Recipes and refining",
                new[]
                {
                    new ArticleBlock("Recipes",
                        "The Recipes window lets you search recipes, filter to craftable recipes, and review level, quantity, enhancement slots, required components, and requirements."),
                    new ArticleBlock("Starting A Craft",
                        "Use the proper crafting device, choose a recipe or blueprint, and supply the required inventory components. Starting from a blueprint can cost credits and consumes one licensed run. Components and selected enhancements are held by the crafting session once added."),
                    new ArticleBlock("Crafting Minigames",
                        "Each crafting skill has its own minigame: Smithery uses the Plasma Forge, Engineering the Circuit, Fabrication the Fabricator, Agriculture the Kitchen, and Espionage the Reaction. Every window has a Help page that opens the first time you craft there, and every action shows its exact cost and result before you click. Perk lines and styles change how each minigame plays."),
                    new ArticleBlock("Progress, Quality, Integrity",
                        "Each minigame has a progress meter, a quality meter, an integrity meter, and CP to spend on actions. At the Plasma Forge they are Shape, Finish, and Integrity; in the Kitchen, Doneness, flavor, and Freshness. The craft succeeds the moment progress is full, and the action that would finish it is marked. Quality is the item's quality and raises crafting XP and vendor value."),
                    new ArticleBlock("Using Food",
                        "Food effects last 30 minutes by default. You cannot consume another food item while an existing food effect is active. Higher-quality crafted food lasts up to 5 minutes longer, and maximum-quality food made from a blueprint also gains extra charges equal to the blueprint level."),
                    new ArticleBlock("Enhancements",
                        "Enhancements must be in your inventory, match the recipe's enhancement slot type, and be within 5 levels of the recipe. Some enhancements make the craft harder. Each slot has a quality threshold, 50% for the first slot, 85% for the second, and 100% for the third; an enhancement transfers whenever the finished quality meets its slot's threshold. The window shows which thresholds you have met."),
                    new ArticleBlock("Success and Failure",
                        "A successful craft consumes the committed components and enhancements and creates the item; an enhancement whose threshold was not met is not returned. Success grants crafting skill XP and records first-time recipe completion. If the integrity meter reaches 0 the piece breaks: you gain 15 percent of the XP a 0 percent quality success would give, blueprint bonus included, and each component stack and each selected enhancement has a 65 percent chance to be lost. The window shows this rule before you commit materials."),
                    new ArticleBlock("Closing the Window",
                        "Closing before the craft begins returns inserted items. After it begins, closing the window, walking away from the station, dying, or logging out fails the craft, so finish the attempt first."),
                    new ArticleBlock("Quick Craft",
                        "Once your rank is at least the recipe's level plus 5 and you have crafted it by hand once, Quick Craft makes up to 10 at a time with no minigame, materials permitting. It makes the base item only: no enhancements, no blueprint, and no XP. Cancelling stops the batch and keeps the finished items."),
                    new ArticleBlock("Research",
                        "Research terminals require Fabrication rank 10. Each research level starts with a short Experiment that can shorten the job, chooses the blueprint bonus at levels 3, 5, and 8, and grants Fabrication XP. Research jobs cost credits and time, work with weapon, armor, and food recipes, and can create a level 1 blueprint or improve one up to level 10. The recipe determines the required Fabrication rank: 10 per recipe tier, up to 50. You may run 1 personal job, plus 1 at Fabrication 25 and another at Fabrication 50, but each terminal handles only one job at a time. Once its experiment has run, a job cannot be cancelled."),
                    new ArticleBlock("Refining",
                        "Refineries turn raw materials into refined materials. Refining requires the Gathering rank for the material's tier (0, 10, 20, 30, or 40), takes 6 seconds, consumes Power Cores, and grants Gathering XP. One Power Core processes 3 items plus 1 additional item at Gathering 0, 10, 20, 30, 40, and 45.")
                },
                new[]
                {
                    new QuestionAnswer("What ends a craft?", "It succeeds the moment its progress meter is full. It breaks if its integrity meter reaches 0 first, and it fails if you close the window or walk away."),
                    new QuestionAnswer("What does Quality do?", "It increases crafting XP and vendor value, decides which enhancement thresholds are met, and lengthens food duration. Maximum-quality food also gains extra charges equal to blueprint level when a blueprint was used."),
                    new QuestionAnswer("When can I use Quick Craft?", "When your rank is at least the recipe's level plus 5 and you have crafted that recipe by hand once. It makes base items with no XP."),
                    new QuestionAnswer("Why can't I use an enhancement?", "It may not match the slot type, may be too far from the recipe level, or may not be in your inventory."),
                    new QuestionAnswer("What happens if I close crafting?", "Before the craft begins, inserted items return to you. After it begins, closing the window fails the craft."),
                    new QuestionAnswer("What does research do?", "Research spends credits and time to create or improve weapon, armor, and food blueprints up to level 10. Each level starts with a short Experiment.")
                },
                new[] { "Skills", "Perks", "Gathering & Fishing", "Training Store", "Housing & Markets", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateGatheringAndFishing()
        {
            return new(
                "Gathering & Fishing",
                "Activities",
                "Prospecting, scavenging, refining links, fishing fights, bait, and gathering-skill progression.",
                "Resources and fishing",
                new[]
                {
                    new ArticleBlock("Harvesting Resources",
                        "Use a charged harvester on a matching vein or tree. The harvester and resource can require a minimum Gathering rank: tier 1 at rank 0, then one more tier every 10 ranks. This opens Prospecting: Survey reveals hidden seams, Strike and Heavy Strike take ore or wood, and Extract opens a found seam for a gem or extra ore, all while keeping the node's Stability above 0. Quick Harvest instead takes what is left at the old pace, with no seams. Actions spend harvester charges and grant Gathering XP; the window's Help page has the details."),
                    new ArticleBlock("Scavenging",
                        "Scavenge sites require a Gathering rank that matches the site's tier. Opening one offers Quick Search, which searches the whole site at once, or a 3x3 search board. On the board, your Gathering rank sets how often a Search of promising ground finds something. Search a spot, Sift to see which neighboring spots are promising, or Dig Deeper at a searched spot for better odds at rare items, at a shown risk of spoiling it. Every action grants Gathering XP, and a site can be searched once, by one scavenger at a time. Treasure Hunter improves item results, and Credit Finder improves credit finds."),
                    new ArticleBlock("Refining Materials",
                        "Raw materials can be processed at refineries when you have the matching Gathering rank. Refining consumes Power Cores and grants Gathering XP; see Crafting for the batch-size and timing details."),
                    new ArticleBlock("Loading A Fishing Rod",
                        "Equip a fishing rod in your right hand. Use the rod on bait in your inventory to load it. Loading different bait first unloads any remaining bait back into your inventory."),
                    new ArticleBlock("Fishing",
                        "Click a fishing point while within 10 meters, with a baited rod in your right hand. Casting takes 6 to 8 seconds and movement interrupts it. A bite consumes 1 bait; rod type, bait, location, and time of day decide which fish bites. Then choose Quick Reel, which lands the fish at once on a roll of your Agriculture rank against the fish's level, or fight it on the Line: read its next move and reel it in before it escapes or snaps the line. Fighting takes longer, pays more XP for your time, and can land more fish. Fishing grants Agriculture XP.")
                },
                new[]
                {
                    new QuestionAnswer("Why won't my harvester work?", "It may be out of charges, too far from the node, the wrong harvester, or require a higher Gathering rank."),
                    new QuestionAnswer("Should I fight a fish or Quick Reel it?", "Quick Reel is fast and uses the old catch roll. A fight takes longer but pays more XP for your time, and a skilled fight lands more fish."),
                    new QuestionAnswer("What improves scavenging?", "Gathering rank unlocks harder sites and makes promising ground on the board pay off more often; Treasure Hunter and Credit Finder improve different parts of the result."),
                    new QuestionAnswer("How do I load bait?", "Equip the fishing rod, use it, and target bait in your inventory."),
                    new QuestionAnswer("Why did fishing stop?", "Moving during the 6 to 8 second cast interrupts the attempt.")
                },
                new[] { "Skills", "Perks", "Crafting", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateBeastsAndStables()
        {
            return new(
                "Beasts & Stables",
                "Activities",
                "Taming, stable capacity, companion limits, beast XP and perks, food, revival, eggs, DNA, and incubation.",
                "Taming and pets",
                new[]
                {
                    new ArticleBlock("Taming",
                        "Tame requires the Tame perk, no active beast, a valid creature target, and open stable capacity. Your Tame rank controls the highest creature level you can tame. Chance starts at 40 percent, changes by 3 percentage points for each Beast Mastery rank above or below the creature's level, gains 3 points per Social, and is capped at 75 percent. Failure makes the creature hostile toward you."),
                    new ArticleBlock("Stable Capacity",
                        "You can keep 1 beast plus 1 more for each Stabling perk rank. At Beast Mastery 50, Dorra Vesk at the Anchorhead stables on Tatooine offers a two-part quest line; each part you complete adds 1 more, for up to 8 beasts with all five Stabling ranks. The Stables window shows your count, marks the active beast in green, and lets you make a beast active or inactive. A beast and a droid both use the single active henchman-companion slot, so they cannot be active together."),
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
                    new QuestionAnswer("How many beasts can I keep?", "One beast plus one additional beast for each Stabling perk rank, plus one for each part of Dorra Vesk's two-part quest line at the Anchorhead stables on Tatooine (Beast Mastery 50 required). The maximum is eight."),
                    new QuestionAnswer("How do beast perks work?", "They spend the beast's SP, not yours, and use the beast's level and role."),
                    new QuestionAnswer("How do I revive my beast?", "Use Revive Beast when your active beast is unconscious and no other companion is active.")
                },
                new[] { "Perks", "Skills", "Crafting", "Droids", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateDroids()
        {
            return new(
                "Droids",
                "Activities",
                "Assembly parts, controllers, activation requirements, companion limits, appearance, AI instructions, and cooldowns.",
                "Build and program droids",
                new[]
                {
                    new ArticleBlock("Droid Assembly",
                        "Anyone can use a droid assembly terminal. A complete droid needs a CPU, head, body, arms, legs, personality, and name. Assembly consumes the selected parts and creates a Droid Control Unit."),
                    new ArticleBlock("Part Tiers",
                        "Your Engineering rank must reach the CPU tier: tier 1 at rank 0, then one more tier every 10 ranks. Head, body, arms, and legs cannot exceed the CPU tier. The chosen parts determine the droid's statistics, equipment, appearance options, and AI capacity."),
                    new ArticleBlock("Activating A Droid",
                        "Use the controller's first action to activate the droid. Droid tier requires an average combat level of tier x 10 minus 10. That level is half the sum of your Armor rank and your highest supported weapon, Force, Devices, or First Aid rank, rounded down. You cannot activate or adjust droids in space, and a droid uses the same single companion slot as a beast."),
                    new ArticleBlock("Appearance and Equipment",
                        "Use the controller's appearance action while that droid is active. Droids can carry equipment and inventory appropriate to their assembled design; their state is saved back to the controller when dismissed."),
                    new ArticleBlock("Programming AI",
                        "Dismiss the droid before adjusting its AI. Your Engineering rank must reach its tier. Uploading a matching instruction disc permanently consumes the disc and teaches its routine; instruction tier cannot exceed droid tier. Choose learned routines up to the controller's available AI-slot budget."),
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
                new[] { "Beasts & Stables", "Crafting", "Skills", "Perks", "Ships & Space" });
        }

        internal static PlayerGuideTopic CreateQuestsAndKeyItems()
        {
            return new(
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
                new[] { "Useful Windows", "Skills", "Training Store", "Quest Contracts", "Guilds & Citizenship", "Housing & Markets" });
        }

        internal static PlayerGuideTopic CreateQuestContracts()
        {
            return new(
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
                new[] { "Quests & Key Items", "Housing & Markets", "Crafting", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateGuildsAndCitizenship()
        {
            return new(
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
                new[] { "Quests & Key Items", "Training Store", "Housing & Markets", "Ships & Space", "Skills" });
        }

        internal static PlayerGuideTopic CreateHousingAndMarkets()
        {
            return new(
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
                new[] { "Crafting", "Training Store", "Quest Contracts", "Guilds & Citizenship", "Ships & Space", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateTravelAndNavigation()
        {
            return new(
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
                        "Before boarding, you may refund the ticket at any starport flights terminal, then book a new flight from that terminal. Only the fare is returned; transportation tax is not refunded. Ticketed and in-transit journeys persist through logout and server restart, and an in-transit character is delivered correctly on return."),
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
                new[] { "Quests & Key Items", "Guilds & Citizenship", "Ships & Space", "Housing & Markets" });
        }

        internal static PlayerGuideTopic CreateShipsAndSpace()
        {
            return new(
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
                new[] { "Housing & Markets", "Travel & Navigation", "Perks", "Combat Basics", "Useful Windows" });
        }
    }
}
