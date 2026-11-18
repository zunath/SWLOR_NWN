using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Progression category, one method per topic.</summary>
    internal static class PlayerGuideProgressionTopics
    {
        internal static PlayerGuideTopic CreatePerks()
        {
            return new(
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
                new[] { StatusEffectGuideTopics.TopicName, "Force Affinity", "Perk Refunds", "Skills", "Abilities", "Skill Decay", "Lightsaber Combat Styles", "Pistol Combat Styles" });
        }

        internal static PlayerGuideTopic CreatePerkRefunds()
        {
            return new(
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
                new[] { "Perks", "Skill Decay", "Skills", "XP Debt", "Training Store", "Common Questions" });
        }

        internal static PlayerGuideTopic CreateTrainingStore()
        {
            return new(
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
                new[] { "Perk Refunds", "Rebuilds", "Skills", "Crafting", "Guilds & Citizenship", "Useful Windows" });
        }
    }
}
