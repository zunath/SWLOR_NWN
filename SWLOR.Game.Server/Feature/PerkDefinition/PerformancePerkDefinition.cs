using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class PerformancePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Dance();
            LightFeet();
            CrowdPleaser();
            PowerStep();
            Encore();
            Quickstep();
            VigorReel();
            PartnerDance();
            StandingOvation();
            PeakForm();
            Showstopper();
            PlayInstrument();
            SteadyHands();
            Harmony();
            KeenEar();
            Resonance();
            AnglersLullaby();
            StillWaters();
            Duet();
            Requiem();
            SilverTongue();
            PerfectPitch();
            Virtuoso();

            return _builder.Build();
        }

        private void Dance()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.Dance)
                .Name("Dance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Dance1)
                .Description("Enters dance mode: your hotbar is replaced with your dance steps until you stop. Your audience's Inspired grants +6% skill XP, and each minute they watch clears 1% of their XP debt. SOC scaling can raise the XP bonus to +8%. Unlocks the Cantina Shuffle routine.")
                .IncreasesStat(StatType.PerformanceDanceXPBonus, 6)
                .IncreasesStat(StatType.PerformanceDanceDebtClearPercent, 1)
                .Price(2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Dance2)
                .Description("Your audience's Inspired grants +12% skill XP, and each minute they watch clears 2% of their XP debt. SOC scaling can raise the XP bonus to +15%.")
                .IncreasesStat(StatType.PerformanceDanceXPBonus, 12)
                .IncreasesStat(StatType.PerformanceDanceDebtClearPercent, 2)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Dance3)
                .Description("Your audience's Inspired grants +18% skill XP, and each minute they watch clears 3% of their XP debt. SOC scaling can raise the XP bonus to +22%.")
                .IncreasesStat(StatType.PerformanceDanceXPBonus, 18)
                .IncreasesStat(StatType.PerformanceDanceDebtClearPercent, 3)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Dance4)
                .Description("Your audience's Inspired grants +24% skill XP, and each minute they watch clears 4% of their XP debt. SOC scaling can raise the XP bonus to +30%.")
                .IncreasesStat(StatType.PerformanceDanceXPBonus, 24)
                .IncreasesStat(StatType.PerformanceDanceDebtClearPercent, 4)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 45);
        }

        private void LightFeet()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.LightFeet)
                .Name("Light Feet")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LightFeetTrait)
                .Description("Unlocks the Light Feet routine. Inspired from your dances also grants +10% movement speed while out of combat.")
                .IncreasesStat(StatType.PerformanceLightFeetSpeedPercent, 10)
                .Price(2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LightFeet2Trait)
                .Description("The Light Feet routine grants +15% movement speed while out of combat.")
                .IncreasesStat(StatType.PerformanceLightFeetSpeedPercent, 15)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 40);
        }

        private void CrowdPleaser()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.CrowdPleaser)
                .Name("Crowd Pleaser")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrowdPleaserTrait)
                .Description("Your audience banks Inspired 10% faster.")
                .IncreasesStat(StatType.PerformanceBankingBonusPercent, 10)
                .Price(2)
                .RequirementSkill(SkillType.Performance, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrowdPleaser2Trait)
                .Description("Your audience banks Inspired 20% faster.")
                .IncreasesStat(StatType.PerformanceBankingBonusPercent, 20)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 38);
        }

        private void PowerStep()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.PowerStep)
                .Name("Power Step")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PowerStepTrait)
                .Description("Unlocks the Power Step routine. Inspired from your dances also grants +1 MGT.")
                .Price(3)
                .RequirementSkill(SkillType.Performance, 15);
        }

        private void Encore()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.Encore)
                .Name("Encore")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EncoreTrait)
                .Description("Your audience can bank up to 20 more minutes of Inspired.")
                .IncreasesStat(StatType.PerformanceInspiredCapBonusMinutes, 20)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Encore2Trait)
                .Description("Your audience can bank up to 40 more minutes of Inspired.")
                .IncreasesStat(StatType.PerformanceInspiredCapBonusMinutes, 40)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Encore3Trait)
                .Description("Your audience can bank up to 60 more minutes of Inspired.")
                .IncreasesStat(StatType.PerformanceInspiredCapBonusMinutes, 60)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 48);
        }

        private void Quickstep()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.Quickstep)
                .Name("Quickstep")

                .AddPerkLevel()
                .GrantsFeat(FeatType.QuickstepTrait)
                .Description("Unlocks the Quickstep routine. Inspired from your dances also grants +1 AGI.")
                .Price(3)
                .RequirementSkill(SkillType.Performance, 22);
        }

        private void VigorReel()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.VigorReel)
                .Name("Vigor Reel")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VigorReelTrait)
                .Description("Unlocks the Vigor Reel routine. Inspired from your dances also grants +1 VIT.")
                .Price(4)
                .RequirementSkill(SkillType.Performance, 25);
        }

        private void PartnerDance()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.PartnerDance)
                .Name("Partner Dance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PartnerDanceTrait)
                .Description("While another dancer performs within your Performance range, watchers following either of you also receive the other dancer's routine bonus. Watchers receive at most two routine bonuses.")
                .Price(4)
                .RequirementSkill(SkillType.Performance, 32);
        }

        private void StandingOvation()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.StandingOvation)
                .Name("Standing Ovation")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StandingOvationTrait)
                .Description("Inspired from your dances also carries Standing Ovation: the first time each 10 minutes the holder drops below 25% HP, they restore 10% of their maximum HP.")
                .Price(3)
                .RequirementSkill(SkillType.Performance, 35);
        }

        private void PeakForm()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.PeakForm)
                .Name("Peak Form")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PeakFormTrait)
                .Description("Power Step, Quickstep, and Vigor Reel grant +2 to their attribute instead of +1.")
                .IncreasesStat(StatType.PerformanceRoutineAttributeBonus, 1)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 42);
        }

        private void Showstopper()
        {
            _builder.Create(PerkCategoryType.PerformanceDancing, PerkType.Showstopper)
                .Name("Showstopper")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShowstopperTrait)
                .Description("You can blend two unlocked routines into one dance; your audience receives both routine bonuses. Your Flourishes bank 5 minutes of Inspired instead of 2.")
                .Price(6)
                .RequirementSkill(SkillType.Performance, 50);
        }

        private void PlayInstrument()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.PlayInstrument)
                .Name("Play Instrument")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PlayInstrument1)
                .Description("Enters instrument mode with your equipped instrument: your hotbar is replaced with your notes until you stop. Your audience hears your band's song; their Inspired grants +6% skill XP, and each minute they watch clears 1% of their XP debt. SOC scaling can raise the XP bonus to +8%. Unlocks the Cantina Standards set.")
                .IncreasesStat(StatType.PerformanceMusicXPBonus, 6)
                .IncreasesStat(StatType.PerformanceMusicDebtClearPercent, 1)
                .Price(2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PlayInstrument2)
                .Description("Your audience's Inspired grants +12% skill XP, and each minute they watch clears 2% of their XP debt. SOC scaling can raise the XP bonus to +15%.")
                .IncreasesStat(StatType.PerformanceMusicXPBonus, 12)
                .IncreasesStat(StatType.PerformanceMusicDebtClearPercent, 2)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PlayInstrument3)
                .Description("Your audience's Inspired grants +18% skill XP, and each minute they watch clears 3% of their XP debt. SOC scaling can raise the XP bonus to +22%.")
                .IncreasesStat(StatType.PerformanceMusicXPBonus, 18)
                .IncreasesStat(StatType.PerformanceMusicDebtClearPercent, 3)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PlayInstrument4)
                .Description("Your audience's Inspired grants +24% skill XP, and each minute they watch clears 4% of their XP debt. SOC scaling can raise the XP bonus to +30%.")
                .IncreasesStat(StatType.PerformanceMusicXPBonus, 24)
                .IncreasesStat(StatType.PerformanceMusicDebtClearPercent, 4)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 45);
        }

        private void SteadyHands()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.SteadyHands)
                .Name("Steady Hands")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SteadyHandsTrait)
                .Description("Unlocks the Steady Hands set. Inspired from your music also grants +2 Control and +2 Craftsmanship for all crafting skills.")
                .IncreasesStat(StatType.PerformanceSteadyHandsBonus, 2)
                .Price(2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SteadyHands2Trait)
                .Description("The Steady Hands set grants +4 Control and +4 Craftsmanship for all crafting skills.")
                .IncreasesStat(StatType.PerformanceSteadyHandsBonus, 4)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 48);
        }

        private void Harmony()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.Harmony)
                .Name("Harmony")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HarmonyTrait)
                .Description("While a dancer performs within your Performance range, your audience banks Inspired 10% faster.")
                .IncreasesStat(StatType.PerformanceHarmonyBankingBonusPercent, 10)
                .Price(2)
                .RequirementSkill(SkillType.Performance, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Harmony2Trait)
                .Description("While a dancer performs within your Performance range, your audience banks Inspired 20% faster.")
                .IncreasesStat(StatType.PerformanceHarmonyBankingBonusPercent, 20)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 30);
        }

        private void KeenEar()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.KeenEar)
                .Name("Keen Ear")

                .AddPerkLevel()
                .GrantsFeat(FeatType.KeenEarTrait)
                .Description("Unlocks the Keen Ear set. Inspired from your music also grants +1 PER.")
                .Price(3)
                .RequirementSkill(SkillType.Performance, 15);
        }

        private void Resonance()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.Resonance)
                .Name("Resonance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ResonanceTrait)
                .Description("Your Performance range is increased by 3m.")
                .IncreasesStat(StatType.PerformanceRangeBonusMeters, 3)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Resonance2Trait)
                .Description("Your Performance range is increased by 6m.")
                .IncreasesStat(StatType.PerformanceRangeBonusMeters, 6)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 38);
        }

        private void AnglersLullaby()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.AnglersLullaby)
                .Name("Angler's Lullaby")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AnglersLullabyTrait)
                .Description("Unlocks the Angler's Lullaby set. Inspired from your music also grants +10% fishing catch chance.")
                .IncreasesStat(StatType.PerformanceAnglersLullabyBonus, 10)
                .Price(3)
                .RequirementSkill(SkillType.Performance, 22);
        }

        private void StillWaters()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.StillWaters)
                .Name("Still Waters")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StillWatersTrait)
                .Description("Unlocks the Still Waters set. Inspired from your music also grants +1 WIL.")
                .Price(4)
                .RequirementSkill(SkillType.Performance, 25);
        }

        private void Duet()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.Duet)
                .Name("Duet")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DuetTrait)
                .Description("While another musician plays in your band, watchers following either of you also receive the other musician's set bonus. Watchers receive at most two set bonuses.")
                .Price(4)
                .RequirementSkill(SkillType.Performance, 32);
        }

        private void Requiem()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.Requiem)
                .Name("Requiem")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RequiemTrait)
                .Description("Inspired from your music also carries Requiem: the holder's next death while Inspired adds no XP debt. Requiem is then used up until the holder banks Inspired again.")
                .Price(3)
                .RequirementSkill(SkillType.Performance, 35);
        }

        private void SilverTongue()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.SilverTongue)
                .Name("Silver Tongue")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SilverTongueTrait)
                .Description("Unlocks the Silver Tongue set. Inspired from your music also grants +1 SOC.")
                .Price(4)
                .RequirementSkill(SkillType.Performance, 40);
        }

        private void PerfectPitch()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.PerfectPitch)
                .Name("Perfect Pitch")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PerfectPitchTrait)
                .Description("Keen Ear, Still Waters, and Silver Tongue grant +2 to their attribute instead of +1.")
                .IncreasesStat(StatType.PerformanceSetAttributeBonus, 1)
                .Price(4)
                .RequirementSkill(SkillType.Performance, 42);
        }

        private void Virtuoso()
        {
            _builder.Create(PerkCategoryType.PerformanceInstruments, PerkType.Virtuoso)
                .Name("Virtuoso")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VirtuosoTrait)
                .Description("You can weave two unlocked sets into one performance; your audience receives both set bonuses. Your Flourishes bank 5 minutes of Inspired instead of 2.")
                .Price(6)
                .RequirementSkill(SkillType.Performance, 50);
        }
    }
}
