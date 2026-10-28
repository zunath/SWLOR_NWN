using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Command style of Piloting (plan section 6, Appendix A): orders and repairs ported from the Leadership
    /// Vanguard Command and Field Steward lines and the First Aid Med Kit. Rows, prices, ranks and text are the
    /// Design Bible tab "Piloting". The traits set space-only stats, because the ground stats of the same
    /// perks add up with a pilot's ground Leadership and First Aid perks.
    /// </summary>
    public class PilotingCommandPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            EmergencyRepair();
            MarkTarget();
            RepairEfficiency();
            FocusFire();
            CrewMorale();
            BreakFormation();
            Formation();
            CommandPresence();
            FleetAction();

            return _builder.Build();
        }

        private void EmergencyRepair()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandEmergencyRepair)
                .Name("Emergency Repair")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandEmergencyRepair1)
                .Description("Repair an allied ship within command range for 22 hull plus 2% of its max hull plus WIL scaling.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandEmergencyRepair2)
                .Description("Repair an allied ship within command range for 84 hull plus 3.5% of its max hull plus WIL scaling.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandEmergencyRepair3)
                .Description("Repair an allied ship within command range for 160 hull plus 5% of its max hull plus WIL scaling.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandEmergencyRepair4)
                .Description("Repair an allied ship within command range for 216 hull plus 6% of its max hull plus WIL scaling.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40);
        }

        private void MarkTarget()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandMarkTarget)
                .Name("Mark Target")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandMarkTargetTrait)
                .Description("When your hostile ability damages or debuffs an enemy, party ships within command range gain +8% damage for 30 s. SOC scaling can raise this to +10%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50). Once per ability use, at most once every 45 s.")
                .IncreasesStat(StatType.SpaceMarkTargetDamagePercent, 8)
                .IncreasesStat(StatType.SpaceMarkTargetMaximumDamagePercent, 10)
                .IncreasesStat(StatType.SpaceMarkTargetDurationSeconds, 30)
                .IncreasesStat(StatType.SpaceMarkTargetCooldownSeconds, 45)
                .IncreasesStat(StatType.SpaceMarkTargetNameStrRef, 16780505)
                .IncreasesStat(StatType.SpaceMarkTargetIcon, (int)EffectIconType.MarkTarget1StatusEffect)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 5)

                .AddPerkLevel()
                .Description("When your hostile ability damages or debuffs an enemy, party ships within command range gain +12% damage and +10% Accuracy for 30 s. SOC scaling can raise these to +15% and +12%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50). Once per ability use, at most once every 45 s.")
                .IncreasesStat(StatType.SpaceMarkTargetDamagePercent, 12)
                .IncreasesStat(StatType.SpaceMarkTargetMaximumDamagePercent, 15)
                .IncreasesStat(StatType.SpaceMarkTargetAccuracyPercent, 10)
                .IncreasesStat(StatType.SpaceMarkTargetMaximumAccuracyPercent, 12)
                .IncreasesStat(StatType.SpaceMarkTargetDurationSeconds, 30)
                .IncreasesStat(StatType.SpaceMarkTargetCooldownSeconds, 45)
                .IncreasesStat(StatType.SpaceMarkTargetNameStrRef, 16780507)
                .IncreasesStat(StatType.SpaceMarkTargetIcon, (int)EffectIconType.MarkTarget2StatusEffect)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 25);
        }

        private void RepairEfficiency()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandRepairEfficiency)
                .Name("Repair Efficiency")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandRepairEfficiencyTrait)
                .Description("Repairs from your modules and abilities are 5% stronger.")
                .IncreasesStat(StatType.SpaceRepairEfficiencyPercentAdjustment, 5)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 8)

                .AddPerkLevel()
                .Description("Repairs from your modules and abilities are 10% stronger.")
                .IncreasesStat(StatType.SpaceRepairEfficiencyPercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 22)

                .AddPerkLevel()
                .Description("Repairs from your modules and abilities are 15% stronger.")
                .IncreasesStat(StatType.SpaceRepairEfficiencyPercentAdjustment, 15)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 45);
        }

        private void FocusFire()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandFocusFire)
                .Name("Focus Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandFocusFire1)
                .Description("Party ships within command range deal +6% damage for 30 s. SOC scaling can raise this to +8%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50).")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandFocusFire2)
                .Description("Party ships within command range deal +8% damage for 30 s. SOC scaling can raise this to +10%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50).")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandFocusFire3)
                .Description("Party ships within command range deal +10% damage and gain +5% Accuracy for 30 s. SOC scaling can raise these to +12% and +7%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50).")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 38);
        }

        private void CrewMorale()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandCrewMorale)
                .Name("Crew Morale")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandCrewMoraleTrait)
                .Description("Crew and droid crew aboard your ship work their stations at +10% effectiveness. Leadership rank adds +1% crew effectiveness per 10 ranks (+5% at 50). Crew effectiveness is capped at 105%, so these bonuses matter most for droid crew.")
                .IncreasesStat(StatType.SpaceCrewEffectivenessPercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 15);
        }

        private void BreakFormation()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandBreakFormation)
                .Name("Break Formation")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandBreakFormation1)
                .Description("Enemies within command range suffer -10% Accuracy for 30 s. SOC scaling can raise the penalty to 12%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50).")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandBreakFormation2)
                .Description("Enemies within command range suffer -15% Accuracy and Weakened (-12% Attack) for 30 s. SOC scaling can raise these to 18% and 15%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50).")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 32);
        }

        private void Formation()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandFormation)
                .Name("Formation")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandFormation)
                .Description("Aura: party ships within command range take 6% less damage. SOC scaling can raise this to 7%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50). An aura, not a stance, so it doesn't block a stance.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 20);
        }

        private void CommandPresence()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandPresence)
                .Name("Command Presence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandPresenceTrait)
                .Description("Your command auras and area orders reach further, and non-capstone orders last 4 s longer. SOC scaling can raise the duration bonus to +5 s. Command range also grows with your Leadership rank, by 0.5% per rank (+25% at Leadership 50), with or without this perk.")
                .IncreasesStat(StatType.SpaceCommandRangeBonusMeters, 16)
                .IncreasesStat(StatType.SpaceCommandDurationBonusBaseSeconds, 4)
                .IncreasesStat(StatType.SpaceCommandDurationBonusMaximumSeconds, 5)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 35);
        }

        private void FleetAction()
        {
            _builder.Create(PerkCategoryType.PilotingCommand, PerkType.CommandFleetAction)
                .Name("Fleet Action")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CommandFleetAction)
                .Description("For 45 s, party ships within command range, yours included, gain +12% damage, +6% Accuracy and +6% Critical Rate, and restore 1 STM every 3 s. SOC scaling can raise the bonuses to +15%, +8% and +8%. Your Leadership rank raises the result by a further 0.5% per rank (+25% at Leadership 50).")
                .Price(6)
                .RequirementSkill(SkillType.Piloting, 50);
        }
    }
}
