using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Core category, one method per topic.</summary>
    internal static class PlayerGuideCoreTopics
    {
        internal static PlayerGuideTopic CreateCommonQuestions()
        {
            return new(
                "Common Questions",
                "Core",
                "Start here for progression, recovery, combat, crafting, travel, companions, quests, communication, and essential windows.",
                "Start here",
                new[]
                {
                    new ArticleBlock("Skills",
                        "Skills gain XP individually. Skill ranks create Skill Points for perks until you have earned 400 SP from skill ranks. Languages do not count toward this 400-rank cap."),
                    new ArticleBlock("Ability Points (AP)",
                        "Every 10 earned SP grants 1 Ability Point, up to 40 AP. Spend AP from the Character Sheet to improve attributes such as Might, Perception, Vitality, Willpower, Agility, and Social."),
                    new ArticleBlock("Perks",
                        "Perk ranks cost SP. Perks can require certain progress before you buy the next rank, and they can grant active abilities, passive bonuses, or other effects."),
                    new ArticleBlock("Skill Decay",
                        "Once you are at 400 skill ranks, a new rank lowers another eligible unlocked skill by 1 rank and resets that skill's XP to 0. Skills above a rank limit you set are picked first. Languages do not count toward this cap."),
                    new ArticleBlock("Perk Refunds",
                        "Manual perk refunds remove the selected perk completely, return all SP paid for its purchased ranks, consume 1 Perk Refund Token, and start a 1 hour real-time wait before another manual refund."),
                    new ArticleBlock("XP Debt",
                        "Earned skill XP pays down XP debt before the remainder can apply to a skill. If your debt uses all of the XP, that skill gains no XP from that reward."),
                    new ArticleBlock("Death and Resting",
                        "If you die, you can wait for another player to revive you or respawn to your registered medical facility. Resting restores HP, FP, and STM over time, but combat, nearby enemies, nearby party combat, movement, attacks, or damage can prevent or interrupt it. Dungeons require a safe-rest area."),
                    new ArticleBlock("Crafting",
                        "Crafting devices use recipes, materials, and optional blueprints or enhancements to make gear, food, and other items. Each crafting skill has its own minigame with a Help page: fill its progress meter before its integrity meter runs out, and raise quality on the way. Quality improves crafting XP, vendor value, and which enhancements transfer. Research terminals improve blueprints, and refineries turn raw materials into refined materials."),
                    new ArticleBlock("Travel and Companions",
                        "Complete the CZ-220 orientation to earn the shuttle pass used for interplanetary travel. Taxi terminals handle registered local routes. You may have only one active henchman companion at a time, either a beast or a droid."),
                    new ArticleBlock("Recognizing Players",
                        "Names above other player characters are private memory labels, not global character names. An unrecognized character appears as a gray public description. Use /name <label> on another player to save a label only your character can see for that presented identity; this does not rename them for anyone else. Use /forgetname to remove only your private label. Use /name <description> on yourself to set the gray public description seen by players who have not labeled your current identity. Labels and descriptions are limited to 64 characters and cannot include color codes.")
                },
                new[]
                {
                    new QuestionAnswer("How do skills work?", "Skill XP raises individual skills. Skill ranks create earned SP until 400; languages do not count toward that cap."),
                    new QuestionAnswer("How do I use AP?", "Earn 1 AP every 10 earned SP, then spend AP from the Character Sheet to improve attributes."),
                    new QuestionAnswer("What does skill decay mean?", "At 400 skill ranks, gaining another eligible rank lowers another unlocked eligible skill by 1 rank. Skills above a rank limit you set are picked first."),
                    new QuestionAnswer("How do perk refunds work?", "Manual refunds return all SP paid for the selected perk, consume a token, and start a 1 hour wait."),
                    new QuestionAnswer("Why did I get no skill XP?", "XP debt may have used the entire XP reward, the skill may be at the rank limit you set, or you may be at the 400-rank cap with no available skill that can decay."),
                    new QuestionAnswer("Where do I go after death?", "Respawning returns you to your registered medical facility and adds XP debt."),
                    new QuestionAnswer("How do I leave CZ-220?", "Complete the orientation quest to earn the CZ-220 Shuttle Pass, then use a starport flights terminal."),
                    new QuestionAnswer("What should I open first?", "Press B for this guide, C for the Character Sheet, and J for active Quests."),
                    new QuestionAnswer("How do I use /name?", "Use /name <label> on another player to save a private label only you can see. Use it on yourself to set your gray public description. Neither action changes another player's character name. Labels and descriptions are limited to 64 characters and cannot include color codes. Label the intended player before sending tells if several characters share the same description.")
                },
                new[] { "Communication", "Skills", "Attributes", "Perks", "Skill Decay", "Perk Refunds", "XP Debt", "Death & Recovery", "Combat Basics", "Crafting", "Travel & Navigation", "Beasts & Stables", "Droids", "Quests & Key Items", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateSkills()
        {
            return new(
                "Skills",
                "Core",
                "Skill XP, SP, AP, XP debt, Social XP bonuses, rank limits, and the 400-rank cap.",
                "Skill XP, SP, AP",
                new[]
                {
                    new ArticleBlock("Skills Window",
                        "The Skills window shows available XP, XP debt, each visible skill's rank, title, progress to the next rank, raw XP, description, decay lock state, rank limit, and whether RP XP can be distributed to that skill. The Limit box beside the Decay Lock button sets an optional rank limit for that skill."),
                    new ArticleBlock("Skill Cap",
                        "A character can earn up to 400 total ranks from skills that count toward the skill cap. Languages do not count toward this cap."),
                    new ArticleBlock("Skill Rank Limits",
                        "Every skill, languages included, can have a rank limit from 0 to its max rank, set in the Limit box in the Skills window. A skill at or above its limit gains no ranks. Set a language's limit to 0 to stop learning it. Open Skill Decay for details."),
                    new ArticleBlock("SP and AP",
                        "When a skill that counts toward the cap ranks up, you gain 1 unallocated SP and 1 earned SP until earned SP reaches 400. Every 10 earned SP grants 1 unallocated AP, up to 40 AP. Your displayed total SP also includes the 10 starting SP."),
                    new ArticleBlock("XP Adjustments",
                        "Skill XP can be adjusted before it is applied. Your Social attribute adds 2.5 percent skill XP per Social point. Some effects, event rewards, or active companions such as beasts and droids can also change the final amount."),
                    new ArticleBlock("XP Debt",
                        "XP debt is paid first. If earned XP is less than your debt, all of that XP removes debt and none goes to the skill."),
                    new ArticleBlock("Roleplay XP",
                        "Qualifying in-character Talk, Whisper, and Comms around other players builds toward automatic RP XP awards. OOC text does not count. Social and citizen Cantina upgrades improve the award. Available RP XP can be distributed to eligible skills from the Skills window or spent in the Training Store.")
                },
                new[]
                {
                    new QuestionAnswer("What is the skill cap?", "400 total ranks from skills that count toward the cap. Languages do not count."),
                    new QuestionAnswer("What is a skill rank limit?", "An optional rank you set for one skill in the Skills window. The skill gains no ranks at or above it."),
                    new QuestionAnswer("How do I get AP?", "Every 10 earned SP grants 1 unallocated AP, up to 40."),
                    new QuestionAnswer("Why did XP go to debt?", "XP debt is paid down before skill XP is applied.")
                },
                new[] { "Attributes", "XP Debt", "Skill Decay", "Perks", "Training Store", "Crafting", "Communication", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateSkillDecay()
        {
            return new(
                "Skill Decay",
                "Core",
                "What can decay, what locking and rank limits do, and why decay can affect perks.",
                "Locks, limits, and 400-rank cap",
                new[]
                {
                    new ArticleBlock("When Decay Happens",
                        "Decay can happen when a skill gains a rank while your total ranks from skills are at the 400-rank cap. Languages do not count toward this cap."),
                    new ArticleBlock("Eligible Skills",
                        "The decay pool contains other skills that count toward the 400-rank cap, are not locked, are not the skill currently gaining XP, and have rank greater than 0. A skill never decays below its limit."),
                    new ArticleBlock("Which Skill Decays",
                        "Decay takes a rank from unlocked skills above their limits first. Only when none remain does it pick from unlocked skills with no limit."),
                    new ArticleBlock("Lock States",
                        "LOCKED means the skill never loses ranks to decay. UNLOCKED means it can be selected if otherwise eligible. N/A means the skill does not count toward the 400-rank cap and never decays, so it has no decay lock. Languages can still have a limit."),
                    new ArticleBlock("Rank Limits",
                        "The Limit box beside the Decay Lock button in the Skills window shows None by default, or a rank from 0 to the skill's max rank. A skill at or above its limit gains no ranks. Its XP still pays down XP debt, the rest is discarded, and a warning says the skill is at your limit. Languages discard the XP without a warning. If a limit is reached partway through an award, the leftover XP is discarded. Languages go up to rank 20, so a language limit above 20 is saved as 20."),
                    new ArticleBlock("Limits and Locks Together",
                        "A lock means never lose ranks. A limit means settle at this rank. A limit below the skill's current rank is allowed: the skill keeps its ranks and gains nothing, and only moves down through decay. A lock plus a limit at the same rank freezes the skill."),
                    new ArticleBlock("No Available Decay",
                        "If you are already at the 400-rank cap and every other skill is locked, at its limit, or at rank 0, that skill cannot gain XP from normal skill XP rewards."),
                    new ArticleBlock("RP XP and Limits",
                        "The RP XP window refuses a skill at its limit, or one the cap blocks, and never sends more RP XP than the skill can use to reach its limit. RP XP is not lost when an award is blocked."),
                    new ArticleBlock("Rebuilds",
                        "Skill limits survive full rebuilds and Rebuild Tokens. A rebuild still clears decay locks."),
                    new ArticleBlock("Perk Requirement Effects",
                        "After decay, perks tied to that skill are checked. If you no longer meet a perk's requirements, the perk is reduced to your effective level and SP is returned for removed levels.")
                },
                new[]
                {
                    new QuestionAnswer("What can decay?", "Only other unlocked skills that count toward the 400-rank cap, have rank greater than 0, and are above their limit if they have one. Skills above their limits are picked first."),
                    new QuestionAnswer("What does LOCKED mean?", "The skill never loses ranks to decay."),
                    new QuestionAnswer("What does a skill limit do?", "A skill at or above its limit gains no ranks, and it never decays below the limit. Set it with the Limit box in the Skills window."),
                    new QuestionAnswer("Do limits stop RP XP?", "The RP XP window refuses a skill at its limit and only sends the RP XP it can use to reach the limit."),
                    new QuestionAnswer("Why did a perk change?", "Decay can make you fail a skill requirement, causing an automatic perk level refund.")
                },
                new[] { "Skills", "XP Debt", "Perk Refunds", "Perks", "Common Questions" });
        }

        internal static PlayerGuideTopic CreateXpDebt()
        {
            return new(
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
                new[] { "Skills", "Attributes", "Death & Recovery", "Useful Windows", "Common Questions" });
        }

        internal static PlayerGuideTopic CreateDeathAndRecovery()
        {
            return new(
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
                new[] { "XP Debt", "Skills", "Attributes", "Combat Basics", "PvP Combat", "Useful Windows" });
        }
    }
}
