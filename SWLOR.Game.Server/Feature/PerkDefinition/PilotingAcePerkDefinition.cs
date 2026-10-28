using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Ace style of Piloting (plan section 6, Appendix A): energy weapons and evasion, ported from the
    /// Pistol's Skirmisher and Gambler lines and the Rifle's Sustained Fire. Rows, prices, ranks and text are the
    /// Design Bible tab "Piloting".
    /// </summary>
    public class PilotingAcePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DisablingShot();
            MobileFootwork();
            SustainedFire();
            DoubleShot();
            KitingInstinct();
            PointBlankBurst();
            AceStance();
            HighNoon();
            EvasiveReload();
            AceInTheHole();

            return _builder.Build();
        }

        private void DisablingShot()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceDisablingShot)
                .Name("Disabling Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDisablingShot1)
                .Description("Fire one energy weapon at your target for weapon DMG + 8 and inflict Disarming Shot for 30 s, reducing its Attack by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDisablingShot2)
                .Description("Fire one energy weapon at your target for weapon DMG + 18 and inflict Disarming Shot for 30 s, reducing its Attack by 12%.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDisablingShot3)
                .Description("Fire one energy weapon at your target for weapon DMG + 28 and inflict Disarming Shot for 30 s, reducing its Attack by 15%.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDisablingShot4)
                .Description("Fire one energy weapon at your target for weapon DMG + 38 and inflict Disarming Shot for 30 s, reducing its Attack by 18%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40);
        }

        private void MobileFootwork()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceMobileFootwork)
                .Name("Mobile Footwork")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceMobileFootworkTrait)
                .Description("After using a hostile ability, gain +8% Evasion for 30 s.")
                .IncreasesStat(StatType.SpaceHostileAbilityUsedEvasionPercentAdjustment, 8)
                .IncreasesStat(StatType.SpaceHostileAbilityUsedEvasionDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 5);
        }

        private void SustainedFire()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceSustainedFire)
                .Name("Sustained Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceSustainedFireTrait)
                .Description("Each consecutive energy-weapon hit against the same target grants +1 DMG, up to five stacks. Stacks expire after 30 s.")
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit, 1)
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageBonusMax, 5)
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 8)

                .AddPerkLevel()
                .Description("Each consecutive energy-weapon hit against the same target grants +2 DMG, up to five stacks. Stacks expire after 30 s.")
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit, 2)
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageBonusMax, 10)
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 22)

                .AddPerkLevel()
                .Description("Each consecutive energy-weapon hit against the same target grants +3 DMG, up to five stacks. Stacks expire after 30 s.")
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit, 3)
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageBonusMax, 15)
                .IncreasesStat(StatType.SpaceEnergyRepeatedTargetDamageDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 45);
        }

        private void DoubleShot()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceDoubleShot)
                .Name("Double Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDoubleShot1)
                .Description("Fire one energy weapon twice at your target for weapon DMG + 6 each. If either shot critically hits, restore 2 STM.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDoubleShot2)
                .Description("Fire one energy weapon twice at your target for weapon DMG + 13 each. If either shot critically hits, restore 3 STM.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceDoubleShot3)
                .Description("Fire one energy weapon twice at your target for weapon DMG + 20 each. If either shot critically hits, restore 4 STM.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 38);
        }

        private void KitingInstinct()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceKitingInstinct)
                .Name("Kiting Instinct")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceKitingInstinctTrait)
                .Description("When you move after using a hostile ability, gain +10% Movement Speed for 30 s.")
                .IncreasesStat(StatType.SpaceHostileAbilityUsedMovementSpeedPercentAdjustment, 10)
                .IncreasesStat(StatType.SpaceHostileAbilityUsedMovementSpeedDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 15);
        }

        private void PointBlankBurst()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AcePointBlankBurst)
                .Name("Point Blank Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AcePointBlankBurst1)
                .Description("Deal weapon DMG + 16 to enemies near your ship and gain +10% Evasion for 30 s.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AcePointBlankBurst2)
                .Description("Deal weapon DMG + 30 to enemies near your ship and gain +15% Evasion for 30 s.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 32);
        }

        private void AceStance()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceStance)
                .Name("Ace Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceStance)
                .Description("While active, gain +15% Evasion and +10% Movement Speed, but your Attack is reduced by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 20);
        }

        private void HighNoon()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceHighNoon)
                .Name("High Noon")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceHighNoonTrait)
                .Description("Critical hits against targets above 70% hull deal +15% damage.")
                .IncreasesStat(StatType.CriticalDamageHighHPTargetThresholdPercent, 70)
                .IncreasesStat(StatType.CriticalDamageHighHPTargetPercentAdjustment, 15)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 25);
        }

        private void EvasiveReload()
        {
            // The discount is Piloting's own: it applies only to the next Piloting ability.
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceEvasiveReload)
                .Name("Evasive Reload")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceEvasiveReloadTrait)
                .Description("After you evade an attack, your next hostile ability within 30 s costs 3 less STM.")
                .IncreasesStat(StatType.AvoidedAttackNextSkillAbilitySkillType, (int)SkillType.Piloting)
                .IncreasesStat(StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment, -3)
                .IncreasesStat(StatType.AvoidedAttackNextSkillAbilityWindowSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 35);
        }

        private void AceInTheHole()
        {
            _builder.Create(PerkCategoryType.PilotingAce, PerkType.AceInTheHole)
                .Name("Ace in the Hole")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AceInTheHole)
                .Description("Fire one energy weapon at your target for weapon DMG + 30. If it critically hits, your next three weapon cycles have +20% Critical Rate and no extra delay.")
                .Price(6)
                .RequirementSkill(SkillType.Piloting, 50);
        }
    }
}
