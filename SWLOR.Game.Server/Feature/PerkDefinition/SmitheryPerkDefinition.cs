using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Smithery perk lines: Forgemaster (fast, hot work) and Artisan (precise finishing). Every perk acts only on the Plasma Forge
    /// minigame through the stats <see cref="ForgePerkStats"/> reads. Each line has ten purchases at Smithery ranks
    /// 5/10/15/20/25/30/35/40/45/50 priced 1/1/2/2/3/3/3/3/3/4 SP; a ranked perk spends two of them, and a rank restates what the
    /// rank below it gave because only the purchased rank's bonuses apply. The two styles are not alternatives of one purchase: a
    /// crafter may own both and toggles between them in the Plasma Forge window.
    /// </summary>
    public class SmitheryPerkDefinition : IPerkListDefinition
    {
        // Generic crafting icons until each perk has artwork of its own.
        private const string ForgemasterIcon = "ife_blacksmith";
        private const string ArtisanIcon = "ife_armorer";

        private const string StylePrefix = "Plasma Forge style, picked in the Plasma Forge window with one style active at a time. ";

        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            PlasmaEfficiency();
            HeatRetention();
            OverdrivePress();
            HotForging();
            HammerRhythm();
            ScorchGuard();
            SurgePlasma();
            FastRebond();
            RedLine();

            SteadyHands();
            CleanContainment();
            MicroHone();
            ColdWorking();
            SteadyQuench();
            CoolHand();
            TemperHold();
            ContainmentFocus();
            PerfectTemper();

            return _builder.Build();
        }

        private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        private void PlasmaEfficiency()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryPlasmaEfficiency)
                .Name("Plasma Efficiency")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Ramp Plasma costs {ForgePerkRules.PlasmaEfficiencyCpReduction} less CP, to a minimum of 1.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 5)
                .IncreasesStat(StatType.ForgeRampCpReduction, ForgePerkRules.PlasmaEfficiencyCpReduction);
        }

        private void HeatRetention()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryHeatRetention)
                .Name("Heat Retention")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, the piece loses {ForgePerkRules.HeatRetentionRank1} less Thermal after every action, to a minimum of 1 lost.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .IncreasesStat(StatType.ForgeCoolingReduction, ForgePerkRules.HeatRetentionRank1)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, the piece loses {ForgePerkRules.HeatRetentionRank2} less Thermal after every action, to a minimum of 1 lost.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .IncreasesStat(StatType.ForgeCoolingReduction, ForgePerkRules.HeatRetentionRank2);
        }

        private void OverdrivePress()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryOverdrivePress)
                .Name("Overdrive Press")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"Adds the Overdrive Press action to the Plasma Forge: a Hydro-Press with {100 + ForgePerkRules.OverdrivePressPercent}% of the Shape. It costs {ForgePerkRules.OverdrivePressCp} CP and {ForgePerkRules.OverdrivePressIntegrityMultiplier} times a Hydro-Press's Integrity, and cannot be used twice in a row.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 15)
                .IncreasesStat(StatType.ForgeOverdrivePressPercent, ForgePerkRules.OverdrivePressPercent);
        }

        private void HotForging()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.HotForgingStyle)
                .Name("Hot Forging")
                .Icon(ForgemasterIcon)
                .TriggerRefund(ForgeStyles.RemoveUnowned)

                .AddPerkLevel()
                .Description($"{StylePrefix}{ForgeStyles.Description(ForgeStyle.HotForging)}")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, ForgePerkRules.StyleRank);
        }

        private void HammerRhythm()
        {
            var each = ForgePerkRules.HammerRhythmPercent;
            var cap = each * ForgePerkRules.HammerRhythmMaxStacks;

            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryHammerRhythm)
                .Name("Hammer Rhythm")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, a Hydro-Press made right after another gains {each}% more Shape, {each * 2}% after two in a row and {cap}% after three or more. Any other action starts the count again.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 25)
                .IncreasesStat(StatType.ForgePressStreakPercent, each);
        }

        private void ScorchGuard()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryScorchGuard)
                .Name("Scorch Guard")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, a Hydro-Press made above the working range cracks {ForgePerkRules.ScorchGuardReductionPercent}% less Integrity.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 35)
                .IncreasesStat(StatType.ForgePressOverheatIntegrityReductionPercent, ForgePerkRules.ScorchGuardReductionPercent);
        }

        private void SurgePlasma()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitherySurgePlasma)
                .Name("Surge Plasma")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"Adds the Surge Plasma action to the Plasma Forge: it raises Thermal by {ForgePerkRules.SurgePlasmaThermal} in one action. It costs {Number(ForgePerkRules.SurgePlasmaCpMultiplier)} times a Ramp Plasma's CP and {ForgeRules.RampIntegrity} Integrity.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .IncreasesStat(StatType.ForgeSurgePlasmaThermal, ForgePerkRules.SurgePlasmaThermal);
        }

        private void FastRebond()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryFastRebond)
                .Name("Fast Rebond")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Rebond costs {ForgePerkRules.FastRebondThermalReduction} less Thermal and {ForgePerkRules.FastRebondCpReduction} less CP, to a minimum of 1 CP.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 45)
                .IncreasesStat(StatType.ForgeRebondThermalReduction, ForgePerkRules.FastRebondThermalReduction)
                .IncreasesStat(StatType.ForgeRebondCpReduction, ForgePerkRules.FastRebondCpReduction);
        }

        private void RedLine()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryRedLine)
                .Name("Red Line")
                .Icon(ForgemasterIcon)

                .AddPerkLevel()
                .Description($"Adds the Red Line Press to the Plasma Forge: once per craft, a Hydro-Press with {ForgePerkRules.RedLineShapeMultiplier} times the Shape that costs no Integrity. It can be used only while Thermal is above the working range.")
                .Price(4)
                .RequirementSkill(SkillType.Smithery, 50)
                .IncreasesStat(StatType.ForgeRedLineCharges, ForgePerkRules.RedLineCharges);
        }

        private void SteadyHands()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitherySteadyHands)
                .Name("Steady Hands")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Vibro-Hone costs {ForgePerkRules.SteadyHandsIntegrityReduction} less Integrity, to a minimum of 1.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 5)
                .IncreasesStat(StatType.ForgeHoneIntegrityReduction, ForgePerkRules.SteadyHandsIntegrityReduction);
        }

        private void CleanContainment()
        {
            var rank1 = ForgePerkRules.CleanContainmentRank1;
            var rank2 = ForgePerkRules.CleanContainmentRank2;
            var chance = ForgeRules.StableContainmentChancePercent;

            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryCleanContainment)
                .Name("Clean Containment")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Stable Containment is announced on {rank1} {ForgePerkRules.PointWord(rank1)} more actions ({chance}% becomes {chance + rank1}%).")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .IncreasesStat(StatType.ForgeContainmentChancePercent, rank1)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Stable Containment is announced on {rank2} {ForgePerkRules.PointWord(rank2)} more actions ({chance}% becomes {chance + rank2}%).")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 25)
                .IncreasesStat(StatType.ForgeContainmentChancePercent, rank2);
        }

        private void MicroHone()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryMicroHone)
                .Name("Micro-Hone")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"Adds the Micro-Hone action to the Plasma Forge: it raises Finish by {ForgePerkRules.MicroHonePercent}% of a Vibro-Hone's best result at any Thermal. It costs {ForgePerkRules.MicroHoneCp} CP and {ForgePerkRules.MicroHoneIntegrity} Integrity.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 15)
                .IncreasesStat(StatType.ForgeMicroHonePercent, ForgePerkRules.MicroHonePercent);
        }

        private void ColdWorking()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.ColdWorkingStyle)
                .Name("Cold Working")
                .Icon(ArtisanIcon)
                .TriggerRefund(ForgeStyles.RemoveUnowned)

                .AddPerkLevel()
                .Description($"{StylePrefix}{ForgeStyles.Description(ForgeStyle.ColdWorking)}")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, ForgePerkRules.StyleRank);
        }

        private void SteadyQuench()
        {
            var usual = (int)Math.Round(ForgeRules.QuenchEdgeMultiplier * 100);

            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitherySteadyQuench)
                .Name("Steady Quench")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Cryo-Quench keeps {usual + ForgePerkRules.SteadyQuenchEdgePercent}% of its center bonus at the edge of the working range instead of {usual}%.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .IncreasesStat(StatType.ForgeQuenchEdgePercent, ForgePerkRules.SteadyQuenchEdgePercent);
        }

        private void CoolHand()
        {
            var usual = (int)Math.Round(ForgeRules.HoneHotEdgeMultiplier * 100);

            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryCoolHand)
                .Name("Cool Hand")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Vibro-Hone in the hot half of the working range falls to {usual + ForgePerkRules.CoolHandHotEdgePercent}% of its best result at the hot edge instead of {usual}%.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 35)
                .IncreasesStat(StatType.ForgeHoneHotEdgePercent, ForgePerkRules.CoolHandHotEdgePercent);
        }

        private void TemperHold()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryTemperHold)
                .Name("Temper Hold")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"Adds the Temper Hold action to the Plasma Forge: for the next {ForgePerkRules.TemperHoldActions} actions the piece does not cool. It costs {ForgePerkRules.TemperHoldCp} CP.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .IncreasesStat(StatType.ForgeTemperHoldActions, ForgePerkRules.TemperHoldActions);
        }

        private void ContainmentFocus()
        {
            var usual = ForgeRules.StableContainmentBonusPercent;

            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryContainmentFocus)
                .Name("Containment Focus")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, Stable Containment gives Vibro-Hone +{usual + ForgePerkRules.ContainmentFocusPercent}% instead of +{usual}%. Hydro-Press is unchanged.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 45)
                .IncreasesStat(StatType.ForgeContainmentHonePercent, ForgePerkRules.ContainmentFocusPercent);
        }

        private void PerfectTemper()
        {
            _builder.Create(PerkCategoryType.Smithery, PerkType.SmitheryPerfectTemper)
                .Name("Perfect Temper")
                .Icon(ArtisanIcon)

                .AddPerkLevel()
                .Description($"In the Plasma Forge, a Cryo-Quench made with Thermal within {ForgePerkRules.PerfectTemperWindow} of the center of the working range gives {ForgePerkRules.PerfectTemperPercent}% more Finish.")
                .Price(4)
                .RequirementSkill(SkillType.Smithery, 50)
                .IncreasesStat(StatType.ForgePerfectTemperPercent, ForgePerkRules.PerfectTemperPercent);
        }
    }
}
