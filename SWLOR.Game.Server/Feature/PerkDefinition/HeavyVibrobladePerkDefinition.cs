using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class HeavyVibrobladePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AbsoluteDefense();
            AngerStrike();
            BastionStance();
            BlazingSpikes();
            BloodWeapon();
            Bloodlust();
            CriticalWard();
            CrushingBlow();
            DefensiveHarmony();
            Earthshatter();
            EssenceHunter();
            EssenceTap();
            Flash();
            FortressStrike();
            GuardiansReaping();
            GuardiansResolve();
            LastStand();
            LifeSiphon();
            Rampart();
            SacrificialBlade();
            SoulAmplification();
            SoulAscension();
            SoulBarrier();
            SoulBurst();
            SoulDevourer();
            SoulReaping();
            SoulSacrifice();
            SoulStorm();
            SoulStrike();
            UnbreakableWill();
            VampiricFury();

            return _builder.Build();
        }


        private void AbsoluteDefense()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.AbsoluteDefense)
                .Name("Absolute Defense")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AbsoluteDefense1)
                .Description("For 45 seconds, party members within 5m including you take 15% less physical and Force damage and are immune to Knockdown and Dazed.")
                .Price(6)
                .RequirementSkill(SkillType.HeavyVibroblade, 50)
                .RequirementQuest(HeavyVibrobladeCapstoneQuestDefinition.AbsoluteDefenseMasteryQuestId);
        }


        private void AngerStrike()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.AngerStrike)
                .Name("Anger Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AngerStrikeTrait)
                .Description("Heavy Vibroblade Defense attacks generate +150 Enmity, and your next attack within 30 seconds after using a Heavy Vibroblade Defense ability deals +12 DMG.")
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, 150)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType, (int)PerkType.FortressStrike)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType, (int)PerkType.BastionStance)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType, (int)PerkType.Flash)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType, (int)PerkType.Rampart)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType, (int)PerkType.Earthshatter)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType, (int)PerkType.AbsoluteDefense)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, 12)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 5);
        }


        private void BastionStance()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.BastionStance)
                .Name("Bastion Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BastionStance1)
                .Description("While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 20);
        }


        private void BlazingSpikes()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.BlazingSpikes)
                .Name("Blazing Spikes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlazingSpikes1)
                .Description("While active, this effect delivers 10% of physical damage received back to the attacker. Damage dealt increases by 1% per MGT. (Maximum 40%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 32);
        }


        private void BloodWeapon()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.BloodWeapon)
                .Name("Blood Weapon")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BloodWeaponTrait)
                .Description("While you have a Heavy Vibroblade Defense Physical Defense or damage-reduction buff, restore HP equal to 1% of combat damage you deal.")
                .IncreasesStat(StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 1)
                .Price(5)
                .RequirementSkill(SkillType.HeavyVibroblade, 40);
        }


        private void Bloodlust()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.Bloodlust)
                .Name("Bloodlust")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BloodlustTrait)
                .Description("After you spend HP on a Heavy Vibroblade Offense ability, restore 15% of maximum STM, increased by 1 percentage point per MGT to a maximum of 35%. This can trigger once every 30 seconds.")
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, 15)
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, (int)AbilityType.Might + 1)
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent, 35)
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds, 30)
                .Price(5)
                .RequirementSkill(SkillType.HeavyVibroblade, 40);
        }


        private void CriticalWard()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.CriticalWard)
                .Name("Critical Ward")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CriticalWardTrait)
                .Description("If you would receive a critical hit, downgrade the attack to a normal hit. The attack will do minimum damage to you. This can trigger once every 16 seconds, reduced by 0.5 seconds per MGT to a minimum of 12 seconds.")
                .IncreasesStat(StatType.IncomingCriticalHitDowngradeToMinimumDamage, 1)
                .IncreasesStat(
                    StatType.IncomingCriticalHitDowngradeCooldownMilliseconds,
                    creature => Math.Max(12000, 16000 - Math.Max(0, GetAbilityScore(creature, AbilityType.Might)) * 500))
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 35);
        }


        private void CrushingBlow()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.CrushingBlow)
                .Name("Crushing Blow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrushingBlowTrait)
                .Description("Heavy Vibroblade Defense attacks reduce affected targets' Defense by 15% for 30 seconds and generate +350 Enmity.")
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType, (int)PerkType.FortressStrike)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType, (int)PerkType.BastionStance)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType, (int)PerkType.Flash)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType, (int)PerkType.Rampart)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType, (int)PerkType.Earthshatter)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType, (int)PerkType.AbsoluteDefense)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlow, 1)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, 350)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 8);
        }


        private void DefensiveHarmony()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.DefensiveHarmony)
                .Name("Defensive Harmony")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DefensiveHarmonyTrait)
                .Description("HP restoration used on you is 20% more effective. 10% chance to restore 8 STM when healed. Chance increases by 1% per MGT. (Maximum 40%)")
                .IncreasesStat(StatType.HealingReceivedPercentAdjustment, 20)
                .IncreasesStat(StatType.HealingReceivedStaminaRestoreChance, 10)
                .IncreasesStat(StatType.HealingReceivedStaminaRestoreChanceScalingAbility, (int)AbilityType.Might + 1)
                .IncreasesStat(StatType.HealingReceivedStaminaRestoreChanceMaximum, 40)
                .IncreasesStat(StatType.HealingReceivedStaminaRestore, 8)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 15);
        }


        private void Earthshatter()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.Earthshatter)
                .Name("Earthshatter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Earthshatter1)
                .Description("You deal weapon DMG + 20 to all enemies in an 8m x 2.5m line from you. Inflicts Force Disruption on each target which disables the use of force abilities for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Earthshatter2)
                .Description("You deal weapon DMG + 35 to all enemies in an 8m x 2.5m line from you. Inflicts Force Disruption on each target which disables the use of force abilities for 30 seconds and generates +350 Enmity.")
                .IncreasesStat(StatType.EarthshatterDamageBonus, 15)
                .IncreasesStat(StatType.EarthshatterEnmityBonus, 350)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 32);
        }

        private void EssenceHunter()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.EssenceHunter)
                .Name("Essence Hunter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EssenceHunterTrait)
                .Description("Heavy Vibroblade Offense weapon abilities also inflict Essence Drain, reducing the target's Attack by 15% for 30 seconds.")
                .IncreasesStat(StatType.HeavyVibrobladeOffenseEssenceHunter, 1)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 8);
        }


        private void EssenceTap()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.EssenceTap)
                .Name("Essence Tap")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EssenceTapTrait)
                .Description("When you take damage, gain +8% Attack for 30 seconds.")
                .IncreasesStat(StatType.DamageTakenAttackPercentAdjustment, 8)
                .IncreasesStat(StatType.DamageTakenAttackDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 5);
        }


        private void Flash()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.Flash)
                .Name("Flash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Flash1)
                .Description("Enemies within a 5m sphere around you receive the Flash effect, reducing physical and Force ability hit chance by 20% for 30 seconds. You generate +650 bonus Enmity toward these enemies.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 10);
        }


        private void FortressStrike()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.FortressStrike)
                .Name("Fortress Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike1)
                .Description("Your next attack deals weapon DMG + 10 and generates +350 Enmity plus damage dealt as Enmity. You gain +10% Physical Defense for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike2)
                .Description("Your next attack deals weapon DMG + 20 and generates +450 Enmity plus damage dealt as Enmity. You gain +20% Physical Defense for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike3)
                .Description("Your next attack deals weapon DMG + 30 and generates +550 Enmity plus damage dealt as Enmity. You gain +30% Physical Defense for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 30);
        }


        private void GuardiansReaping()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.GuardiansReaping)
                .Name("Guardian's Reaping")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansReapingTrait)
                .Description("Defeating an enemy restores 12% max HP to you and grants +10% Physical Defense to all allies within 5m for 30 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyHPPercentRestore, 12)
                .IncreasesStat(StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment, 10)
                .IncreasesStat(StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 45);
        }


        private void GuardiansResolve()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.GuardiansResolve)
                .Name("Guardian's Resolve")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansResolveTrait)
                .Description("When a Heavy Vibroblade Defense ability grants you Physical Defense or reduces incoming damage, you also gain Temporary HP equal to 12% of maximum HP for 30 seconds. You heal for 15% of damage absorbed by this Temporary HP. This can trigger once every 30 seconds.")
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType, (int)PerkType.FortressStrike)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType, (int)PerkType.BastionStance)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType, (int)PerkType.Rampart)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType, (int)PerkType.AbsoluteDefense)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 12)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 30)
                .IncreasesStat(StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 25);
        }


        private void LastStand()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.LastStand)
                .Name("Last Stand")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LastStandTrait)
                .Description("When reduced below 25% HP, gain Temporary HP equal to 20% of maximum HP for 30 seconds. This can only trigger once per 10 minutes.")
                .IncreasesStat(StatType.LowHPTemporaryHPThresholdPercent, 25)
                .IncreasesStat(StatType.LowHPTemporaryHPPercent, 20)
                .IncreasesStat(StatType.LowHPTemporaryHPDurationSeconds, 30)
                .IncreasesStat(StatType.LowHPTemporaryHPCooldownSeconds, 600)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 22);
        }


        private void LifeSiphon()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.LifeSiphon)
                .Name("Life Siphon")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LifeSiphonTrait)
                .Description("When below 40% HP, your attacks heal you for 8% of damage dealt.")
                .IncreasesStat(StatType.LowHPDamageDealtHPRestoreThresholdPercent, 40)
                .IncreasesStat(StatType.LowHPDamageDealtHPPercentRestore, 8)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 15);
        }


        private void Rampart()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.Rampart)
                .Name("Rampart")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Rampart1)
                .Description("All allies within a 5m sphere take 15% less physical damage for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 28);
        }


        private void SacrificialBlade()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SacrificialBlade)
                .Name("Sacrificial Blade")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SacrificialBlade1)
                .Description("Deal weapon DMG + 25 to a single target. Costs 8% max HP.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 10);
        }


        private void SoulAmplification()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulAmplification)
                .Name("Soul Amplification")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulAmplificationTrait)
                .Description("When you recover HP, gain +10% Attack for 30 seconds.")
                .IncreasesStat(StatType.HealingReceivedAttackPercentAdjustment, 10)
                .IncreasesStat(StatType.HealingReceivedAttackDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 25);
        }


        private void SoulAscension()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulAscension)
                .Name("Soul Ascension")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulAscensionTrait)
                .Description("Defeating an enemy after spending HP on a Heavy Vibroblade Offense ability grants +8% Attack and heals you for 8% of physical damage dealt for 30 seconds.")
                .IncreasesStat(StatType.HeavyVibrobladeOffenseSoulAscension, 1)
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds, 30)
                .Price(6)
                .RequirementSkill(SkillType.HeavyVibroblade, 50)
                .RequirementQuest(HeavyVibrobladeCapstoneQuestDefinition.SoulAscensionMasteryQuestId);
        }


        private void SoulBarrier()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulBarrier)
                .Name("Soul Barrier")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulBarrierTrait)
                .Description("When HP drops below 50% of maximum, a temporary shield forms which absorbs damage equal to 25% of max HP for 30 seconds. This can only trigger once every 3 minutes.")
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPThresholdPercent, 50)
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPPercent, 25)
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPDurationSeconds, 30)
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPCooldownSeconds, 180)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 38);
        }


        private void SoulBurst()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulBurst)
                .Name("Soul Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulBurst1)
                .Description("Deal weapon DMG + 35 to all enemies in a 5m x 5m cone. Costs 40% HP which is reduced by 1% per MGT. (Minimum 10%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 18);
        }


        private void SoulDevourer()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulDevourer)
                .Name("Soul Devourer")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulDevourer1)
                .Description("While active, gain +25% Attack and +10% critical chance, but each attack you make deals 45% of the damage back to you. Damage reduced by 1% per MGT. (Minimum 20%)")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 20);
        }


        private void SoulReaping()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulReaping)
                .Name("Soul Reaping")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulReapingTrait)
                .Description("Defeating an enemy restores 10% max HP and grants +15% Attack for 30 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyHPPercentRestore, 10)
                .IncreasesStat(StatType.DefeatedEnemyAttackPercentAdjustment, 15)
                .IncreasesStat(StatType.DefeatedEnemyAttackDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 45);
        }


        private void SoulSacrifice()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulSacrifice)
                .Name("Soul Sacrifice")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulSacrificeTrait)
                .Description("After you spend HP on a Heavy Vibroblade Offense ability, gain +15% Attack and +5% critical chance for 30 seconds. The HP cost reduction scales with MGT.")
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice, 1)
                .IncreasesStat(StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 35);
        }


        private void SoulStorm()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulStorm)
                .Name("Soul Storm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStorm1)
                .Description("Sacrifice 40% HP to increase the damage of all allies within a 5m sphere by 20% for 1 minute. HP sacrificed decreases by 1 percentage point per MGT. (Minimum 10%)")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 30);
        }


        private void SoulStrike()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulStrike)
                .Name("Soul Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStrike1)
                .Description("Your next attack deals +15 DMG and heals you for 15% of damage dealt.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStrike2)
                .Description("Your next attack deals +30 DMG and heals you for 25% of damage dealt.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStrike3)
                .Description("Your next attack deals +45 DMG and heals you for 30% of damage dealt. Amount healed increases by 1 percentage point per 2 MGT to a maximum of 40%.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 28);
        }


        private void UnbreakableWill()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.UnbreakableWill)
                .Name("Unbreakable Will")

                .AddPerkLevel()
                .GrantsFeat(FeatType.UnbreakableWillTrait)
                .Description("Gain +4 Melee Deflection, increased by +1 per 4 MGT to a maximum of +8. When your Melee Deflection negates a melee weapon auto-attack, restore 4 STM. This can trigger once every 6 seconds.")
                .IncreasesStat(
                    StatType.MeleeDeflection,
                    creature => Math.Min(8, 4 + Math.Max(0, GetAbilityScore(creature, AbilityType.Might)) / 4))
                .IncreasesStat(StatType.MeleeDeflectionStaminaRestore, 4)
                .IncreasesStat(StatType.MeleeDeflectionStaminaRestoreCooldownSeconds, 6)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 38);
        }


        private void VampiricFury()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.VampiricFury)
                .Name("Vampiric Fury")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VampiricFuryTrait)
                .Description("Critical hits restore HP equal to 12% of damage dealt, increased by 1 percentage point per 2 MGT to a maximum of 25%. This can trigger once every 8 seconds.")
                .IncreasesStat(
                    StatType.CriticalHPPercentOfDamageRestore,
                    creature => Math.Min(25, 12 + Math.Max(0, GetAbilityScore(creature, AbilityType.Might)) / 2))
                .IncreasesStat(
                    StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds,
                    8)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 22);
        }
    }
}
