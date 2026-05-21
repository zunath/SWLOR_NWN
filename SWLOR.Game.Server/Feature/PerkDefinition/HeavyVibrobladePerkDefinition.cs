using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

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
            EdgeOfDarkness();
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
                .Description("For 45 seconds, nearby party members including you take 15% less physical and Force damage and are immune to Knockdown and Daze.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 50);
        }


        private void AngerStrike()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.AngerStrike)
                .Name("Anger Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AngerStrike1)
                .Description("Your next attack deals +12 DMG and generates extra enmity.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 8);
        }


        private void BastionStance()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.BastionStance)
                .Name("Bastion Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BastionStance1)
                .Description("While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 12);
        }


        private void BlazingSpikes()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.BlazingSpikes)
                .Name("Blazing Spikes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlazingSpikes1)
                .Description("While active, this effect delivers 10% of physical damage received back to the attacker. Damage dealt increases by 1% per MGT. (Maximum 40%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 40);
        }


        private void BloodWeapon()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.BloodWeapon)
                .Name("Blood Weapon")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BloodWeapon1)
                .Description("For 20 seconds, 2% of the combat damage you deal is restored to your HP.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 45);
        }


        private void Bloodlust()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.Bloodlust)
                .Name("Bloodlust")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Bloodlust1)
                .Description("Sacrifice 40% HP in exchange for 20% of your maximum STM restored. Amount of STM restored increased by 1% per MGT. (Maximum: 80%)")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 42);
        }


        private void CriticalWard()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.CriticalWard)
                .Name("Critical Ward")

                .AddPerkLevel()
                .Description("If you would receive a critical hit, downgrade the attack to a normal hit. The attack will do minimum damage to you.")
                .IncreasesStat(StatType.IncomingCriticalHitDowngradeToMinimumDamage, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 1 : 0)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 40);
        }


        private void CrushingBlow()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.CrushingBlow)
                .Name("Crushing Blow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrushingBlow1)
                .Description("Deal weapon DMG + 20 and generate significant enmity. Reduces the target's Defense by 15% for 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 15);
        }


        private void DefensiveHarmony()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.DefensiveHarmony)
                .Name("Defensive Harmony")

                .AddPerkLevel()
                .Description("HP restoration used on you is 20% more effective. 10% chance to restore 8 STM when healed. Chance increases by 1% per MGT. (Maximum 40%)")
                .IncreasesStat(StatType.HealingReceivedPercentAdjustment, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 20 : 0)
                .IncreasesStat(StatType.HealingReceivedStaminaRestoreChance, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 10 : 0)
                .IncreasesStat(StatType.HealingReceivedStaminaRestoreChanceScalingAbility, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? (int)AbilityType.Might + 1 : 0)
                .IncreasesStat(StatType.HealingReceivedStaminaRestoreChanceMaximum, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 40 : 0)
                .IncreasesStat(StatType.HealingReceivedStaminaRestore, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 8 : 0)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 30);
        }


        private void Earthshatter()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.Earthshatter)
                .Name("Earthshatter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Earthshatter1)
                .Description("You deal weapon DMG + 20 to all enemies within the area of effect (line) from you. Inflicts Force Disruption on each target which disables the use of force abilities for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 35);
        }


        private void EdgeOfDarkness()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.EdgeOfDarkness)
                .Name("Edge of Darkness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EdgeOfDarkness1)
                .Description("You deal weapon DMG + 15 to all enemies within the area of effect (sphere) from you and generate extra enmity.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 38);
        }


        private void EssenceHunter()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.EssenceHunter)
                .Name("Essence Hunter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EssenceHunter1)
                .Description("Your next attack deals +18 DMG and inflicts Essence Drain, reducing the target's Attack by 15% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 12);
        }


        private void EssenceTap()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.EssenceTap)
                .Name("Essence Tap")

                .AddPerkLevel()
                .Description("When you take damage, gain +8% Attack for 15 seconds.")
                .IncreasesStat(StatType.DamageTakenAttackPercentAdjustment, 8)
                .IncreasesStat(StatType.DamageTakenAttackDurationSeconds, 15)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 5);
        }


        private void Flash()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.Flash)
                .Name("Flash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Flash1)
                .Description("Enemies within the area of effect (sphere) around you receive the Flash effect, reducing physical and Force ability hit chance by 20% for 30 seconds. You generate significant enmity toward these enemies.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 18);
        }


        private void FortressStrike()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.FortressStrike)
                .Name("Fortress Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike1)
                .Description("Your next attack deals weapon DMG + 10 and generates extra enmity. You gain +10% Physical Defense for 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike2)
                .Description("Your next attack deals weapon DMG + 20 and generates extra enmity. You gain +20% Physical Defense for 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike3)
                .Description("Your next attack deals weapon DMG + 30 and generates extra enmity. You gain +30% Physical Defense for 16 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 42);
        }


        private void GuardiansReaping()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.GuardiansReaping)
                .Name("Guardian's Reaping")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 20% max HP to you and grants +15% Physical Defense to all nearby allies for 25 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyHPPercentRestore, 20)
                .IncreasesStat(StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment, 15)
                .IncreasesStat(StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds, 25)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 48);
        }


        private void GuardiansResolve()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.GuardiansResolve)
                .Name("Guardian's Resolve")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansResolve1)
                .Description("Gain a damage absorption shield equal to 30% of your max HP for 30 seconds. While active, heal for 25% of damage absorbed.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 28);
        }


        private void LastStand()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.LastStand)
                .Name("Last Stand")

                .AddPerkLevel()
                .Description("When reduced below 25% HP, gain a damage shield equal to 20% of maximum HP for 12 seconds. This can only trigger once per 10 minutes.")
                .IncreasesStat(StatType.LowHPTemporaryHPThresholdPercent, 25)
                .IncreasesStat(StatType.LowHPTemporaryHPPercent, 20)
                .IncreasesStat(StatType.LowHPTemporaryHPDurationSeconds, 12)
                .IncreasesStat(StatType.LowHPTemporaryHPCooldownSeconds, 600)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 20);
        }


        private void LifeSiphon()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.LifeSiphon)
                .Name("Life Siphon")

                .AddPerkLevel()
                .Description("When below 50% HP, your attacks heal you for 15% of damage dealt.")
                .IncreasesStat(StatType.LowHPDamageDealtHPRestoreThresholdPercent, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 50 : 0)
                .IncreasesStat(StatType.LowHPDamageDealtHPPercentRestore, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 20);
        }


        private void Rampart()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.Rampart)
                .Name("Rampart")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Rampart1)
                .Description("All allies within the area of effect (sphere) take 15% less physical damage for 1 minute.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 32);
        }


        private void SacrificialBlade()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SacrificialBlade)
                .Name("Sacrificial Blade")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SacrificialBlade1)
                .Description("Deal weapon DMG + 25 to a single target. Costs 8% max HP.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 15);
        }


        private void SoulAmplification()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulAmplification)
                .Name("Soul Amplification")

                .AddPerkLevel()
                .Description("When you recover HP, gain +15% Attack for 15 seconds.")
                .IncreasesStat(StatType.HealingReceivedAttackPercentAdjustment, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 15 : 0)
                .IncreasesStat(StatType.HealingReceivedAttackDurationSeconds, creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 30);
        }


        private void SoulAscension()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulAscension)
                .Name("Soul Ascension")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulAscension1)
                .Description("For 45 seconds, gain +15% Attack and heal for 20% of physical damage dealt.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 50);
        }


        private void SoulBarrier()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulBarrier)
                .Name("Soul Barrier")

                .AddPerkLevel()
                .Description("When HP drops below 50% of maximum, a temporary shield forms which absorbs damage equal to 25% of max HP for 12 seconds. This can only trigger once every 3 minutes.")
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPThresholdPercent, 50)
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPPercent, 25)
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPDurationSeconds, 12)
                .IncreasesStat(StatType.LowHPNoSaveTemporaryHPCooldownSeconds, 180)
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 35);
        }


        private void SoulBurst()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulBurst)
                .Name("Soul Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulBurst1)
                .Description("Deal weapon DMG + 35 to all enemies within area of effect (cone). Costs 40% HP which is reduced by 1% per MGT. (Minimum 10%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 25);
        }


        private void SoulDevourer()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulDevourer)
                .Name("Soul Devourer")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulDevourer1)
                .Description("While active, gain +35% Attack and +15% critical chance, but each attack you make deals 40% of the damage back to you. Damage reduced by 1% per MGT. (Minimum 10%)")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 18);
        }


        private void SoulReaping()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulReaping)
                .Name("Soul Reaping")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 15% max HP and grants +20% Attack for 30 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyHPPercentRestore, 15)
                .IncreasesStat(StatType.DefeatedEnemyAttackPercentAdjustment, 20)
                .IncreasesStat(StatType.DefeatedEnemyAttackDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 48);
        }


        private void SoulSacrifice()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulSacrifice)
                .Name("Soul Sacrifice")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulSacrifice1)
                .Description("Sacrifice 50% max HP to gain +35% Attack and +20% critical chance for 30 seconds. HP sacrificed decreases by 1% per MGT. (Minimum 20%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 32);
        }


        private void SoulStorm()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulStorm)
                .Name("Soul Storm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStorm1)
                .Description("Sacrifice 40% HP to increase the damage of all nearby allies within the area of effect (sphere) by 20% for 1 minute. HP sacrificed decreases by 1 percentage point per MGT. (Minimum 10%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 38);
        }


        private void SoulStrike()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.SoulStrike)
                .Name("Soul Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStrike1)
                .Description("Your next attack deals +15 DMG and heals you for 25% of damage dealt.")
                .Price(2)
                .RequirementSkill(SkillType.HeavyVibroblade, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStrike2)
                .Description("Your next attack deals +30 DMG and heals you for 40% of damage dealt.")
                .Price(4)
                .RequirementSkill(SkillType.HeavyVibroblade, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulStrike3)
                .Description("Your next attack deals +45 DMG and heals you for 60% of damage dealt. Amount healed increased by 1% per MGT. (Maximum 90%)")
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 45);
        }


        private void UnbreakableWill()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeDefense, PerkType.UnbreakableWill)
                .Name("Unbreakable Will")

                .AddPerkLevel()
                .Description("Grants +5% Attack Deflection. When attacks are deflected, you restore 10% of maximum STM. Deflection increases by 0.5% per MGT. (Maximum: 20%)")
                .IncreasesStat(
                    StatType.AttackDeflection,
                    creature => Math.Min(20, 5 + Math.Max(0, GetAbilityScore(creature, AbilityType.Might)) / 2))
                .IncreasesStat(
                    StatType.DeflectionStaminaRestorePercent,
                    10)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 22);
        }


        private void VampiricFury()
        {
            _builder.Create(PerkCategoryType.HeavyVibrobladeOffense, PerkType.VampiricFury)
                .Name("Vampiric Fury")

                .AddPerkLevel()
                .Description("Critical hits restore HP equal to 40% of damage dealt. Amount healed increases by 1% per MGT. (Maximum 75%)")
                .IncreasesStat(
                    StatType.CriticalHPPercentOfDamageRestore,
                    creature => EquipmentPredicates.HasMainHandHeavyVibroblade(creature)
                        ? Math.Min(75, 40 + Math.Max(0, GetAbilityScore(creature, AbilityType.Might)))
                        : 0)
                .Price(3)
                .RequirementSkill(SkillType.HeavyVibroblade, 22);
        }
    }
}

