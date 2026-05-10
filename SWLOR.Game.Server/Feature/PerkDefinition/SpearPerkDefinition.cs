using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class SpearPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AdaptivePrecisionStrike();
            BreachStrike();
            CalmingStance();
            CripplingDefense();
            DisablingStrike();
            DisruptionExpert();
            DisruptionField();
            ErosionStrike();
            FlankingBarrage();
            Flanking();
            FlankingStance();
            ForceNullification();
            ForcePiercing();
            ForceSuppression();
            ForceWarding();
            Forcebane();
            FractureStrike();
            HamperingBarrage();
            ImprovedAttentiveness();
            InterruptionStrike();
            LateralStrike();
            OpportunistFlow();
            PerceptiveStance();
            RestorationStrike();
            SideAssault();
            SweepingFlank();
            TotalForceDenial();

            return _builder.Build();
        }


        private void AdaptivePrecisionStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.AdaptivePrecisionStrike)
                .Name("Adaptive Precision Strike")

                .AddPerkLevel()
                .Description("Attacks from the side have a 5% chance to bypass 35% of your target's Evasion. This chance increases by 1% per Perception. (Maximum 30%)")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 48);
        }


        private void BreachStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.BreachStrike)
                .Name("Breach Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BreachStrike1)
                .Description("Deal weapon DMG + 10. Reflex DC14 check to inflict Breach, which reduces Evasion and Defense by 20% for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 18);
        }


        private void CalmingStance()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.CalmingStance)
                .Name("Calming Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CalmingStance1)
                .Description("While active, your STM regenerates by 3 every second. Your attack, force attack, defense, and force defense are reduced by 40%.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 45);
        }


        private void CripplingDefense()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.CripplingDefense)
                .Name("Crippling Defense")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingDefense1)
                .Description("All enemies within area of effect (sphere) around you receive Crippled Defense, reducing Defense by 35% for 15 seconds. Additionally restores 25 STM.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 50);
        }


        private void DisablingStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.DisablingStrike)
                .Name("Disabling Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisablingStrike1)
                .Description("Your next attack deals +12 DMG and has a DC12 Will check to inflict Force Disruption for 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisablingStrike2)
                .Description("Your next attack deals +18 DMG and has a DC16 Will check to inflict Force Disruption for 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisablingStrike3)
                .Description("Your next attack deals +26 DMG and has a DC20 Will check to inflict Force Disruption for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 40);
        }


        private void DisruptionExpert()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.DisruptionExpert)
                .Name("Disruption Expert")

                .AddPerkLevel()
                .Description("Your Force Disruption effects last 50% longer and reduce Force Defense by an additional 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 42);
        }


        private void DisruptionField()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.DisruptionField)
                .Name("Disruption Field")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisruptionField1)
                .Description("Forms a disruption field at a targeted location. All enemies within the area of effect (sphere) lose 5% of FP per second. Field lasts for 20 seconds")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 25);
        }


        private void ErosionStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ErosionStrike)
                .Name("Erosion Strike")

                .AddPerkLevel()
                .Description("Your target makes a Will DC12 check when you damage them.  If they fail this check, they receive Force Erosion which reduces Force Defense by 10% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 5)

                .AddPerkLevel()
                .Description("The Force Erosion effect additionally reduces FP by 2 every second.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 32);
        }


        private void FlankingBarrage()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.FlankingBarrage)
                .Name("Flanking Barrage")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlankingBarrage1)
                .Description("Deal weapon DMG + 20 from the side to your target and reduce their Attack by 12% for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 20);
        }


        private void Flanking()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.Flanking)
                .Name("Flanking")

                .AddPerkLevel()
                .Description("Attacks from the side deal +10% damage.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 5)

                .AddPerkLevel()
                .Description("Attacks from the side have +10% accuracy and +8% critical chance.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 32);
        }


        private void FlankingStance()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.FlankingStance)
                .Name("Flanking Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlankingStance1)
                .Description("While active, attacks from the side deal +20% damage and have +15% accuracy. Your defense and force defense are reduced by 25%.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 12);
        }


        private void ForceNullification()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForceNullification)
                .Name("Force Nullification")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceNullification1)
                .Description("Deal weapon DMG + 22 and completely disable all force abilities of the target for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 30);
        }


        private void ForcePiercing()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForcePiercing)
                .Name("Force Piercing")

                .AddPerkLevel()
                .Description("Critical hit chance increases by 5%. Additionally, critical hits reduce FP by 10% of the damage dealt.")
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, creature => EquipmentPredicates.HasMainHandSpear(creature) ? 5 : 0)
                .IncreasesStat(StatType.CriticalTargetFPLossPercentOfDamage, creature => EquipmentPredicates.HasMainHandSpear(creature) ? 10 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Spear, 18);
        }


        private void ForceSuppression()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForceSuppression)
                .Name("Force Suppression")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceSuppression1)
                .Description("Deals weapon DMG + 20 and reduces your target's Force Attack by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 20);
        }


        private void ForceWarding()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForceWarding)
                .Name("Force Warding")

                .AddPerkLevel()
                .Description("Increases Force Evasion by 15%.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 45)

                .AddPerkLevel()
                .Description("When a Force ability is evaded, you receive the Force Warding buff which increases your Force Defense by 30% for 20 seconds and restores 15 STM. This can only trigger once every 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 48);
        }


        private void Forcebane()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.Forcebane)
                .Name("Forcebane")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Forcebane1)
                .Description("Enemies within the area of effect (sphere) receive the Forcebane debuff, losing 50% of current FP and preventing FP recovery for 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 50);
        }


        private void FractureStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.FractureStrike)
                .Name("Fracture Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FractureStrike1)
                .Description("Deal weapon DMG + 12 to all enemies in area of effect (line). Will DC16 check to inflict Fractured Focus, which doubles the FP cost of abilities for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 38);
        }


        private void HamperingBarrage()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.HamperingBarrage)
                .Name("Hampering Barrage")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HamperingBarrage1)
                .Description("Deal weapon DMG + 30 to all enemies within area of effect (cone). Reflex DC16 check to inflict Disoriented for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 40);
        }


        private void ImprovedAttentiveness()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.ImprovedAttentiveness)
                .Name("Improved Attentiveness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImprovedAttentiveness1)
                .Description("Your party members, excluding you, receive +25% to accuracy for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 28);
        }


        private void InterruptionStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.InterruptionStrike)
                .Name("Interruption Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.InterruptionStrike1)
                .Description("Your target's ability activation is interrupted.  Additionally, target has a Will DC12 check to inflict Foggy Mind which increases activation times by 2 seconds for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.InterruptionStrike2)
                .Description("Your target's ability activation is interrupted.  Additionally, target has a Will DC18 check to inflict Foggy Mind which increases activation times by 2 seconds for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 28);
        }


        private void LateralStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.LateralStrike)
                .Name("Lateral Strike")

                .AddPerkLevel()
                .Description("Attacks from the side restore 2 STM. This can only trigger once every 4 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 8)

                .AddPerkLevel()
                .Description("Attacks from the side restore 6 STM. This can only trigger once every 4 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 22);
        }


        private void OpportunistFlow()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.OpportunistsFlow)
                .Name("Opportunist's Flow")

                .AddPerkLevel()
                .Description("After dealing damage from a side attack, your next attack's delay is 20% quicker.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 35);
        }


        private void PerceptiveStance()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.PerceptiveStance)
                .Name("Perceptive Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PerceptiveStance1)
                .Description("While active, gain +10% critical chance and +15% critical damage. Additionally, attacks have a 10% chance to interrupt ability activation. Chance to interrupt increases by 1% per Might. (Maximum 30%)")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 15);
        }


        private void RestorationStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.RestorationStrike)
                .Name("Restoration Strike")

                .AddPerkLevel()
                .Description("Critical hit rate increases by 10%. Additionally, if you were at the side of your target, crticial hits have a 35% chance to restore 15 STM.")
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, creature => EquipmentPredicates.HasMainHandSpear(creature) ? 10 : 0)
                .IncreasesStat(StatType.CriticalSideAttackStaminaRestoreChance, creature => EquipmentPredicates.HasMainHandSpear(creature) ? 35 : 0)
                .IncreasesStat(StatType.CriticalSideAttackStaminaRestore, creature => EquipmentPredicates.HasMainHandSpear(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Spear, 38);
        }


        private void SideAssault()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.SideAssault)
                .Name("Side Assault")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SideAssault1)
                .Description("Your next attack deals +12 DMG. If you are facing the side of your target, this increases to +16 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 15)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SideAssault2)
                .Description("Your next attack deals +25 DMG. If you are facing the side of your target, this increases to +35 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SideAssault3)
                .Description("Your next attack deals +35 DMG. If you are facing the side of your target, this increases to +50 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 42);
        }


        private void SweepingFlank()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.SweepingFlank)
                .Name("Sweeping Flank")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SweepingFlank1)
                .Description("Deal weapon DMG + 18 to all enemies within area of effect (cone). Fortitude DC16 check to inflict Exposed, which reduces defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 25);
        }


        private void TotalForceDenial()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.TotalForceDenial)
                .Name("Total Force Denial")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TotalForceDenial1)
                .Description("Deal weapon DMG + 28 to all enemies in area of effect (cone) and has a Will DC16 check to inflict Force Disruption for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 35);
        }
    }
}
