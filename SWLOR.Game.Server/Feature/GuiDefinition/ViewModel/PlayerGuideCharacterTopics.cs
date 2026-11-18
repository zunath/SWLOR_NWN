using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Character category, one method per topic.</summary>
    internal static class PlayerGuideCharacterTopics
    {
        internal static PlayerGuideTopic CreateAttributes()
        {
            return new(
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
                        "Each Willpower point adds 3 maximum FP. Every 2 Might points add 3 maximum STM. Natural HP and FP regeneration occurs every 30 seconds. STM regenerates continuously, a fifth of its 30-second amount every 6 seconds. The Character Sheet shows how much of each is restored per 30 seconds.")
                },
                new[]
                {
                    new QuestionAnswer("How do I get AP?", "Every 10 earned SP grants 1 AP, up to 40."),
                    new QuestionAnswer("What stats do weapons use?", "Melee weapons use Might for damage and Perception for accuracy. Ranged and throwing weapons use Perception for damage and Agility for accuracy."),
                    new QuestionAnswer("What helps XP gain?", "Social improves XP gain."),
                    new QuestionAnswer("What helps stamina?", "Might increases maximum STM and natural STM regeneration.")
                },
                new[] { "Skills", "Abilities", "Perks", "Rebuilds", "Combat Basics", "Stat Caps", "Death & Recovery", "Common Questions" });
        }

        internal static PlayerGuideTopic CreateRebuilds()
        {
            return new(
                "Rebuilds",
                "Character",
                "Full rebuilds, AP rebuilds, tokens, and what each option changes.",
                "Full and AP rebuilds",
                new[]
                {
                    new ArticleBlock("Full Rebuild",
                        "A full character rebuild refunds your skills, stats, and perks so you can redistribute your starting attributes and skills. Using a rebuild terminal requires 1 Rebuild Token, while some server-required rebuilds can send you to the rebuild area without a token."),
                    new ArticleBlock("Full Rebuild Reset",
                        "The reset returns perk SP, resets skills that count toward the 400-rank cap to rank 0 and XP 0, clears skill decay locks (skill limits are kept), resets attributes to 10, returns your earned AP, and refunds your racial bonus stat. Partial XP toward the next skill rank is lost."),
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
                new[] { "Attributes", "Skills", "Perks", "Perk Refunds", "Training Store", "Useful Windows" });
        }
    }
}
