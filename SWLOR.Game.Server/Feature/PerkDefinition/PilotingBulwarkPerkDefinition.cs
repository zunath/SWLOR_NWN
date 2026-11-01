using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Bulwark style of Piloting (plan section 6, Appendix A): a tank ported from the Vibroblade Bulwark line,
    /// the Heavy Vibroblade Immortal line and the Armor Provoke. Rows, prices, ranks and text are the Design Bible
    /// tab "Piloting". Bulwark, Fortification and the resistances reuse the ground stats; Anger Strike,
    /// Critical Ward and Unbreakable set space-only stats, because the ground stats of the same perks add up with a
    /// pilot's ground perks and key their cooldowns to hit points.
    /// </summary>
    public class PilotingBulwarkPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ShieldRam();
            AngerStrike();
            Bulwark();
            Defiance();
            Fortification();
            Hail();
            BulwarkStance();
            CriticalWard();
            Unbreakable();
            Invincible();

            return _builder.Build();
        }

        private void ShieldRam()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkShieldRam)
                .Name("Shield Ram")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkShieldRam1)
                .Description("On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 4% of your Defense.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkShieldRam2)
                .Description("On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 6% of your Defense.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkShieldRam3)
                .Description("On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 8% of your Defense.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkShieldRam4)
                .Description("On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 10% of your Defense.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40);
        }

        private void AngerStrike()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkAngerStrike)
                .Name("Anger Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkAngerStrikeTrait)
                .Description("Defiance hits generate +150 enmity, and your next attack within 30 s after using Defiance deals +12 DMG.")
                .IncreasesStat(StatType.SpaceDefianceEnmityBonus, 150)
                .IncreasesStat(StatType.SpaceDefianceNextAttackDamageBonus, 12)
                .IncreasesStat(StatType.SpaceDefianceNextAttackDamageDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 10)
                .RequirementMustHavePerk(PerkType.BulwarkDefiance);
        }

        private void Bulwark()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkBulwark)
                .Name("Bulwark")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkBulwarkTrait)
                .Description("Gain +8% Defense.")
                .IncreasesStat(StatType.DefensePercentAdjustment, 8)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 8)

                .AddPerkLevel()
                .Description("Gain +10% Defense.")
                .IncreasesStat(StatType.DefensePercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 22)

                .AddPerkLevel()
                .Description("Gain +12% Defense.")
                .IncreasesStat(StatType.DefensePercentAdjustment, 12)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 45);
        }

        private void Defiance()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkDefiance)
                .Name("Defiance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkDefiance1)
                .Description("Your next weapon hit deals weapon DMG + 10 and generates +350 enmity plus the damage dealt as enmity. You gain +10% Defense for 30 s.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkDefiance2)
                .Description("Your next weapon hit deals weapon DMG + 20 and generates +450 enmity plus the damage dealt as enmity. You gain +20% Defense for 30 s.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkDefiance3)
                .Description("Your next weapon hit deals weapon DMG + 30 and generates +550 enmity plus the damage dealt as enmity. You gain +30% Defense for 30 s.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 38);
        }

        private void Fortification()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkFortification)
                .Name("Fortification")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkFortificationTrait)
                .Description("Gain +15 Mind, Trauma and Mobility Resistance.")
                .IncreasesStat(StatType.MindResistance, 15)
                .IncreasesStat(StatType.TraumaResistance, 15)
                .IncreasesStat(StatType.MobilityResistance, 15)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 15);
        }

        private void Hail()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkHail)
                .Name("Hail")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkHail1)
                .Description("Goad your target into attacking you. Enmity generated increases by 1% per VIT. On a valid hit, your threat catches up to the highest visible rival plus the VIT-scaled bonus. Repeated goads while already ahead add nothing. No forced target lock.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkHail2)
                .Description("Goad your target and all other enemies near it into attacking you, with the same catch-up rule as Hail I. No forced target lock.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 32);
        }

        private void BulwarkStance()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkShipStance)
                .Name("Bulwark Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkShipStance)
                .Description("While active, gain +30% enmity generation and +20% Defense, but your Attack is reduced by 20%. You generate +100% threat from damage dealt, before general enmity modifiers.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 20);
        }

        private void CriticalWard()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkCriticalWard)
                .Name("Critical Ward")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkCriticalWardTrait)
                .Description("If you would receive a critical hit, it becomes a normal hit dealing minimum damage. Once every 16 s, reduced by 0.5 s per MGT to a minimum of 12 s.")
                .IncreasesStat(StatType.SpaceIncomingCriticalHitDowngradeToMinimumDamage, 1)
                .IncreasesStat(
                    StatType.SpaceIncomingCriticalHitDowngradeCooldownMilliseconds,
                    creature => SpaceCombatMath.CriticalWardCooldownMilliseconds(GetAbilityScore(creature, AbilityType.Might)))
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 25);
        }

        private void Unbreakable()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkUnbreakable)
                .Name("Unbreakable")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkUnbreakableTrait)
                .Description("When reduced below 25% hull, gain +40% Defense for 30 s. Once per 3 minutes.")
                .IncreasesStat(StatType.SpaceLowHullDefenseThresholdPercent, 25)
                .IncreasesStat(StatType.SpaceLowHullDefensePercentAdjustment, 40)
                .IncreasesStat(StatType.SpaceLowHullDefenseDurationSeconds, 30)
                .IncreasesStat(StatType.SpaceLowHullDefenseCooldownSeconds, 180)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 35);
        }

        private void Invincible()
        {
            _builder.Create(PerkCategoryType.PilotingBulwark, PerkType.BulwarkInvincible)
                .Name("Invincible")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkInvincible)
                .Description("For 45 s, you take 50% less damage and are immune to Knockdown and Dazed.")
                .Price(6)
                .RequirementSkill(SkillType.Piloting, 50);
        }
    }
}
