using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.ScavengeService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Gathering perk lines: Prospector (volume) and Treasure Seeker (rare finds). Every perk acts on the Prospecting session, the
    /// Search board or refining, through the stats <see cref="ProspectingEffects"/>, <see cref="SearchEffects"/> and the refinery read.
    /// Each line has ten purchases at Gathering ranks 5/10/15/20/25/30/35/40/45/50 priced 1/1/2/2/3/3/3/3/3/4 SP; a ranked perk spends
    /// two or three of them, and a rank restates what the rank below it gave because only the purchased rank's bonuses apply. Neither
    /// line touches Quick Harvest or Quick Search.
    /// </summary>
    public class GatheringPerkDefinition : IPerkListDefinition
    {
        // Existing gathering icons until each perk has artwork of its own.
        private const string ProspectorIcon = "ife_Miner";
        private const string TreasureSeekerIcon = "ife_treas_hunt";

        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            EfficientStrikes();
            BulkRefining();
            RichVeins();
            StripMiningStyle();
            CoreSample();
            MotherLode();

            FineTrowel();
            SharpChisel();
            SteadyDig();
            CarefulHandsStyle();
            KeenEye();
            DeepScan();
            RichPickings();
            Jackpot();

            return _builder.Build();
        }

        private static string StyleDescription(GatheringStyle style, string window) =>
            $"Gathering style, picked in the {window} window with one style active at a time. {GatheringStyles.Description(style)}";

        private static string ItemWord(int count) => count == 1 ? "item" : "items";

        private static string StrikeText(int strike, int heavy) =>
            $"In Prospecting, a Strike costs {strike} less Stability and a Heavy Strike {heavy} less, to a minimum of 1.";

        private static string TimesText(int bonus) => bonus == 1 ? "1 more" : $"{bonus} more";

        private static int DigWeight(int bonus) => ScavengeLoot.DeeperWeightMultiplier + bonus;

        private static string DigRiskText()
        {
            var base1 = SearchRules.DigRiskBase;
            var lowered = (base1 * (100 - SearchPerkRules.SteadyDigRiskReductionPercent) + 50) / 100;
            return $"a {base1}% risk becomes {lowered}%";
        }

        private void EfficientStrikes()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.EfficientStrikes)
                .Name("Efficient Strikes")
                .Icon(ProspectorIcon)

                .AddPerkLevel()
                .Description(StrikeText(ProspectingPerkRules.EfficientStrikesStrikeRank1, ProspectingPerkRules.EfficientStrikesHeavyRank1))
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 5)
                .IncreasesStat(StatType.ProspectingStrikeStabilityReduction, ProspectingPerkRules.EfficientStrikesStrikeRank1)
                .IncreasesStat(StatType.ProspectingHeavyStrikeStabilityReduction, ProspectingPerkRules.EfficientStrikesHeavyRank1)

                .AddPerkLevel()
                .Description(StrikeText(ProspectingPerkRules.EfficientStrikesStrikeRank2, ProspectingPerkRules.EfficientStrikesHeavyRank2))
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 25)
                .IncreasesStat(StatType.ProspectingStrikeStabilityReduction, ProspectingPerkRules.EfficientStrikesStrikeRank2)
                .IncreasesStat(StatType.ProspectingHeavyStrikeStabilityReduction, ProspectingPerkRules.EfficientStrikesHeavyRank2);
        }

        private void BulkRefining()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.BulkRefining)
                .Name("Bulk Refining")
                .Icon(ProspectorIcon)

                .AddPerkLevel()
                .Description($"Each power core refines {ProspectingPerkRules.BulkRefiningRank1} more {ItemWord(ProspectingPerkRules.BulkRefiningRank1)} than your Gathering rank allows.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .IncreasesStat(StatType.ProspectingRefineBatch, ProspectingPerkRules.BulkRefiningRank1)

                .AddPerkLevel()
                .Description($"Each power core refines {ProspectingPerkRules.BulkRefiningRank2} more {ItemWord(ProspectingPerkRules.BulkRefiningRank2)} than your Gathering rank allows.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .IncreasesStat(StatType.ProspectingRefineBatch, ProspectingPerkRules.BulkRefiningRank2)

                .AddPerkLevel()
                .Description($"Each power core refines {ProspectingPerkRules.BulkRefiningRank3} more {ItemWord(ProspectingPerkRules.BulkRefiningRank3)} than your Gathering rank allows.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 45)
                .IncreasesStat(StatType.ProspectingRefineBatch, ProspectingPerkRules.BulkRefiningRank3);
        }

        private void RichVeins()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.RichVeins)
                .Name("Rich Veins")
                .Icon(ProspectorIcon)

                .AddPerkLevel()
                .Description($"In Prospecting, claiming a rich seam with Extract costs {ProspectingPerkRules.RichVeinsChargeReduction} less charge.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 15)
                .IncreasesStat(StatType.ProspectingRichExtractChargeReduction, ProspectingPerkRules.RichVeinsChargeReduction)

                .AddPerkLevel()
                .Description($"In Prospecting, claiming a rich seam with Extract costs {ProspectingPerkRules.RichVeinsChargeReduction} less charge and {ProspectingPerkRules.RichVeinsStabilityReductionRank2} less Stability, to a minimum of 1.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .IncreasesStat(StatType.ProspectingRichExtractChargeReduction, ProspectingPerkRules.RichVeinsChargeReduction)
                .IncreasesStat(StatType.ProspectingRichExtractStabilityReduction, ProspectingPerkRules.RichVeinsStabilityReductionRank2);
        }

        private void StripMiningStyle()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.StripMiningStyle)
                .Name("Strip Mining")
                .Icon(ProspectorIcon)
                .TriggerRefund(GatheringStyles.RemoveUnowned)

                .AddPerkLevel()
                .Description(StyleDescription(GatheringStyle.StripMining, "Prospecting"))
                .Price(2)
                .RequirementSkill(SkillType.Gathering, GatheringStyles.StyleRank);
        }

        private void CoreSample()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.CoreSample)
                .Name("Core Sample")
                .Icon(ProspectorIcon)

                .AddPerkLevel()
                .Description("In Prospecting, the window shows how many seams the node still holds and whether each is a rich seam or a gem pocket, before you find them. A Heavy Strike shows a chance to shatter a gem pocket only when one is hidden there.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 35)
                .IncreasesStat(StatType.ProspectingSeamSightUnlocked, 1);
        }

        private void MotherLode()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.MotherLode)
                .Name("Mother Lode")
                .Icon(ProspectorIcon)

                .AddPerkLevel()
                .Description($"In Prospecting, the first Heavy Strike of each node takes {ProspectingPerkRules.MotherLodeMultiplier} times its usual units, never more than the node holds. Units sealed in a seam you have not claimed are lost with it.")
                .Price(4)
                .RequirementSkill(SkillType.Gathering, 50)
                .IncreasesStat(StatType.ProspectingMotherLodeUnlocked, 1);
        }

        private void FineTrowel()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.FineTrowel)
                .Name("Fine Trowel")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Search, a Dig Deeper find rolls the site's rare entries with {DigWeight(SearchPerkRules.FineTrowelRank1)} times their usual weight instead of {DigWeight(0)}.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 5)
                .IncreasesStat(StatType.SearchDigRareWeight, SearchPerkRules.FineTrowelRank1)

                .AddPerkLevel()
                .Description($"In Search, a Dig Deeper find rolls the site's rare entries with {DigWeight(SearchPerkRules.FineTrowelRank2)} times their usual weight instead of {DigWeight(0)}.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .IncreasesStat(StatType.SearchDigRareWeight, SearchPerkRules.FineTrowelRank2);
        }

        private void SharpChisel()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.SharpChisel)
                .Name("Sharp Chisel")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Prospecting, a Heavy Strike is {ProspectingPerkRules.SharpChiselRank1} points less likely to shatter a gem pocket you have not surveyed ({ProspectingRules.ShatterPercent}% becomes {ProspectingRules.ShatterPercent - ProspectingPerkRules.SharpChiselRank1}%).")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .IncreasesStat(StatType.ProspectingShatterReduction, ProspectingPerkRules.SharpChiselRank1)

                .AddPerkLevel()
                .Description($"In Prospecting, a Heavy Strike is {ProspectingPerkRules.SharpChiselRank2} points less likely to shatter a gem pocket you have not surveyed ({ProspectingRules.ShatterPercent}% becomes {ProspectingRules.ShatterPercent - ProspectingPerkRules.SharpChiselRank2}%).")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .IncreasesStat(StatType.ProspectingShatterReduction, ProspectingPerkRules.SharpChiselRank2);
        }

        private void SteadyDig()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.SteadyDig)
                .Name("Steady Dig")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Search, the risk that Dig Deeper ruins a spot is {SearchPerkRules.SteadyDigRiskReductionPercent}% lower ({DigRiskText()}).")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 15)
                .IncreasesStat(StatType.SearchDigRiskReductionPercent, SearchPerkRules.SteadyDigRiskReductionPercent);
        }

        private void CarefulHandsStyle()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.CarefulHandsStyle)
                .Name("Careful Hands")
                .Icon(TreasureSeekerIcon)
                .TriggerRefund(GatheringStyles.RemoveUnowned)

                .AddPerkLevel()
                .Description(StyleDescription(GatheringStyle.CarefulHands, "Prospecting or Search"))
                .Price(2)
                .RequirementSkill(SkillType.Gathering, GatheringStyles.StyleRank);
        }

        private void KeenEye()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.KeenEye)
                .Name("Keen Eye")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Search, a Search finds {SearchPerkRules.KeenEyePoints} point more often on every spot.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 25)
                .IncreasesStat(StatType.SearchChancePoints, SearchPerkRules.KeenEyePoints);
        }

        private void DeepScan()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.DeepScan)
                .Name("Deep Scan")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Prospecting, a Survey reveals {TimesText(ProspectingPerkRules.DeepScanReveals)} seam than your Gathering rank allows, up to the {CraftRankMilestones.MaxProspectingSurveyReveals} a node holds, and reveals gem pockets before rich seams.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 35)
                .IncreasesStat(StatType.ProspectingSurveyReveals, ProspectingPerkRules.DeepScanReveals)
                .IncreasesStat(StatType.ProspectingPocketsFirstUnlocked, 1);
        }

        private void RichPickings()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.RichPickings)
                .Name("Rich Pickings")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Search, a Search that finds something rolls the site's rare entries with {(1 + SearchPerkRules.RichPickingsRareWeight)} times their usual weight.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 45)
                .IncreasesStat(StatType.SearchFindRareWeight, SearchPerkRules.RichPickingsRareWeight);
        }

        private void Jackpot()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.Jackpot)
                .Name("Jackpot")
                .Icon(TreasureSeekerIcon)

                .AddPerkLevel()
                .Description($"In Search, the first Dig Deeper find of each board rolls the site's rare entries with {SearchPerkRules.JackpotRareWeight} more times their usual weight.")
                .Price(4)
                .RequirementSkill(SkillType.Gathering, 50)
                .IncreasesStat(StatType.SearchJackpotRareWeight, SearchPerkRules.JackpotRareWeight);
        }
    }
}
