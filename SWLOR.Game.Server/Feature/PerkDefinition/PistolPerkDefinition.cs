using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class PistolPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DeadMansHand();
            DeadeyeReload();
            DisarmingShot();
            DoubleShot();
            DuelistsDistance();
            EvasiveReload();
            FanTheHammer();
            GunfighterStance();
            GunslingerFocus();
            HighNoon();
            InterruptingShot();
            KitingInstinct();
            LastWord();
            LowShot();
            MobileFootwork();
            PointBlankBurst();
            QuickDraw();
            RapidShot();
            ReloadTempo();
            RicochetShot();
            SkirmishersNerve();
            SkirmisherStance();
            SmokeRound();
            SnapRoll();

            return _builder.Build();
        }

        private void DeadMansHand()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.DeadMansHand)
                .Name("Dead Man's Hand")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DeadMansHand1)
                .Description("Fire six shots at your target and nearby enemies, each for weapon DMG + 10. Your target is prioritized and secondary targets cannot be hit more than twice.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 50);
        }

        private void DeadeyeReload()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.DeadeyeReload)
                .Name("Deadeye Reload")

                .AddPerkLevel()
                .Description("After using a pistol combat ability, your next auto-attack within 6 seconds deals +10 DMG.")
                .IncreasesStat(StatType.PistolAbilityUsedNextAutoAttackDamageBonus, 10)
                .IncreasesStat(StatType.PistolAbilityUsedNextAutoAttackDamageDurationSeconds, 6)
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 22);
        }

        private void DisarmingShot()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.DisarmingShot)
                .Name("Disarming Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisarmingShot1)
                .Description("Deals weapon DMG + 8 and inflicts Weakened, reducing Attack by 10% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisarmingShot2)
                .Description("Deals weapon DMG + 18 and inflicts Weakened, reducing Attack by 15% for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisarmingShot3)
                .Description("Deals weapon DMG + 32 and inflicts Weakened, reducing Attack by 20% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 42);
        }

        private void DoubleShot()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.DoubleShot)
                .Name("Double Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleShot1)
                .Description("Instantly attacks twice, each for weapon DMG + 7.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleShot2)
                .Description("Instantly attacks twice, each for weapon DMG + 15.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleShot3)
                .Description("Instantly attacks twice, each for weapon DMG + 24.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 30);
        }

        private void DuelistsDistance()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.DuelistsDistance)
                .Name("Duelist's Distance")

                .AddPerkLevel()
                .Description("Deal +12% pistol damage to enemies within 8 meters that are not targeting you.")
                .IncreasesStat(StatType.DamageToNearbyNonTargetingTargetPercentAdjustment, creature => EquipmentPredicates.HasPistol(creature) ? 12 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 40);
        }

        private void EvasiveReload()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.EvasiveReload)
                .Name("Evasive Reload")

                .AddPerkLevel()
                .Description("Using Snap Roll or Ricochet Shot reduces Disarming Shot cooldowns by 10 seconds.")
                .IncreasesStat(StatType.AbilityUsedRecastReductionTriggerGroup, (int)RecastGroup.SnapRoll)
                .IncreasesStat(StatType.AbilityUsedRecastReductionSecondaryTriggerGroup, (int)RecastGroup.RicochetShot)
                .IncreasesStat(StatType.AbilityUsedRecastReductionTargetGroup, (int)RecastGroup.DisarmingShot)
                .IncreasesStat(StatType.AbilityUsedRecastReductionSeconds, 10)
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 32);
        }

        private void FanTheHammer()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.FanTheHammer)
                .Name("Fan the Hammer")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FanTheHammer1)
                .Description("Fires at up to 3 enemies in a cone for weapon DMG + 12 each.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FanTheHammer2)
                .Description("Fires at up to 5 enemies in a cone for weapon DMG + 20 each.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 35);
        }

        private void GunfighterStance()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.GunfighterStance)
                .Name("Gunfighter Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GunfighterStance1)
                .Description("While active, grants +15% Attack and +10% Haste, but reduces Defense by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 15);
        }

        private void GunslingerFocus()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.GunslingerFocus)
                .Name("Gunslinger Focus")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GunslingerFocus1)
                .Description("For 20 seconds, Quick Draw and Double Shot abilities cost 2 less STM and deal +10 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 45);
        }

        private void HighNoon()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.HighNoon)
                .Name("High Noon")

                .AddPerkLevel()
                .Description("Your first pistol attack after entering combat gains +30% critical chance and deals +20 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 38);
        }

        private void InterruptingShot()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.InterruptingShot)
                .Name("Interrupting Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.InterruptingShot1)
                .Description("Interrupts your target's ability activation and inflicts Foggy Mind for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.InterruptingShot2)
                .Description("Deals weapon DMG + 20, interrupts your target's ability activation, and inflicts Foggy Mind for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 35);
        }

        private void KitingInstinct()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.KitingInstinct)
                .Name("Kiting Instinct")

                .AddPerkLevel()
                .Description("When attacked in melee, you have a 20% chance to restore 3 STM and gain +10% Evasion for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 20);
        }

        private void LastWord()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.LastWord)
                .Name("Last Word")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LastWord1)
                .Description("Interrupts all enemies in a cone, deals weapon DMG + 35, and inflicts Dazed for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 50);
        }

        private void LowShot()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.LowShot)
                .Name("Low Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LowShot1)
                .Description("Deals weapon DMG + 20 and inflicts Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 30);
        }

        private void MobileFootwork()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.MobileFootwork)
                .Name("Mobile Footwork")

                .AddPerkLevel()
                .Description("After using a pistol ability, gain +10% Evasion for 6 seconds.")
                .IncreasesStat(StatType.PistolAbilityUsedEvasionPercentAdjustment, 10)
                .IncreasesStat(StatType.PistolAbilityUsedEvasionDurationSeconds, 6)
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 5);
        }

        private void PointBlankBurst()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.PointBlankBurst)
                .Name("Point Blank Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PointBlankBurst1)
                .Description("Deals weapon DMG + 18 to enemies in a cone. Inflicts Knockdown for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 38);
        }

        private void QuickDraw()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.QuickDraw)
                .Name("Quick Draw")

                .AddPerkLevel()
                .GrantsFeat(FeatType.QuickDraw1)
                .Description("Instantly deals weapon DMG + 12 to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.QuickDraw2)
                .Description("Instantly deals weapon DMG + 24 to your target.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.QuickDraw3)
                .Description("Instantly deals weapon DMG + 36 to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.QuickDraw4)
                .Description("Instantly deals weapon DMG + 50. Targets below 30% HP take an additional +20 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 42);
        }

        private void RapidShot()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.RapidShot)
                .Name("Rapid Shot")

                .AddPerkLevel()
                .Description("Reduces pistol attack delay by 10%.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, creature => EquipmentPredicates.HasPistol(creature) ? 10 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 12)

                .AddPerkLevel()
                .Description("Reduces pistol attack delay by 20% total.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, creature => EquipmentPredicates.HasPistol(creature) ? 20 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 32)

                .AddPerkLevel()
                .Description("Reduces pistol attack delay by 30% total. Auto-attacks have a 10% chance to restore 2 STM.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, creature => EquipmentPredicates.HasPistol(creature) ? 30 : 0)
                .IncreasesStat(StatType.AutoAttackStaminaRestoreChance, 10)
                .IncreasesStat(StatType.AutoAttackStaminaRestore, 2)
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 48);
        }

        private void ReloadTempo()
        {
            _builder.Create(PerkCategoryType.PistolGunslinger, PerkType.ReloadTempo)
                .Name("Reload Tempo")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 10 STM and reduces Quick Draw cooldowns by 15 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyStaminaRestore, 10)
                .IncreasesStat(StatType.DefeatedEnemyRecastReductionGroup, (int)RecastGroup.QuickDraw)
                .IncreasesStat(StatType.DefeatedEnemyRecastReductionSeconds, 15)
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 40);
        }

        private void RicochetShot()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.RicochetShot)
                .Name("Ricochet Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RicochetShot1)
                .Description("A shot bounces to up to 3 enemies for weapon DMG + 12 each. Each target is Blinded for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 25);
        }

        private void SkirmishersNerve()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.SkirmishersNerve)
                .Name("Skirmisher's Nerve")

                .AddPerkLevel()
                .Description("When reduced below 40% HP, your next pistol ability costs 0 STM and grants +20% Evasion for 8 seconds. This can only trigger once every 2 minutes.")
                .IncreasesStat(StatType.LowHPEvasionThresholdPercent, 40)
                .IncreasesStat(StatType.LowHPEvasionPercentAdjustment, 20)
                .IncreasesStat(StatType.LowHPEvasionDurationSeconds, 8)
                .IncreasesStat(StatType.LowHPEvasionCooldownSeconds, 120)
                .IncreasesStat(StatType.LowHPNextAbilityNoStaminaCostThresholdPercent, 40)
                .IncreasesStat(StatType.LowHPNextAbilityNoStaminaCostSkillType, (int)SkillType.Pistol)
                .IncreasesStat(StatType.LowHPNextAbilityNoStaminaCostDurationSeconds, 8)
                .IncreasesStat(StatType.LowHPNextAbilityNoStaminaCostCooldownSeconds, 120)
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 48);
        }

        private void SkirmisherStance()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.SkirmisherStance)
                .Name("Skirmisher Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SkirmisherStance1)
                .Description("While active, grants +15% Evasion and reduces Enmity generation by 20%, but reduces Attack by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Pistol, 15);
        }

        private void SmokeRound()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.SmokeRound)
                .Name("Smoke Round")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SmokeRound1)
                .Description("Enemies in the target area are Blinded for 12 seconds. You reduce enmity against affected enemies.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 45);
        }

        private void SnapRoll()
        {
            _builder.Create(PerkCategoryType.PistolSkirmisher, PerkType.SnapRoll)
                .Name("Snap Roll")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SnapRoll1)
                .Description("Gain +25% Evasion for 6 seconds and reduce your current enmity by 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Pistol, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SnapRoll2)
                .Description("Gain +35% Evasion for 8 seconds and your next pistol attack within 8 seconds deals +10 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Pistol, 28);
        }
    }
}

