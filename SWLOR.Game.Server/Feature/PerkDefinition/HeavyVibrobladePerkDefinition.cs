using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.AbsoluteDefense)
                .Name("Absolute Defense")

                .AddPerkLevel()
                .Description("All party members, excluding you, gain +40% Defense, +40% Force Defense, and immunity to Knockdown and Dazed for 15 seconds. Your HP, STM, and FP are restored by 25% of maximum.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 50);
        }


        private void AngerStrike()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.AngerStrike)
                .Name("Anger Strike")

                .AddPerkLevel()
                .Description("Your next attack deals +12 DMG and generates extra enmity.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 8);
        }


        private void BastionStance()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.BastionStance)
                .Name("Bastion Stance")

                .AddPerkLevel()
                .Description("While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 12);
        }


        private void BlazingSpikes()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.BlazingSpikes)
                .Name("Blazing Spikes")

                .AddPerkLevel()
                .Description("While active, this effect delivers 10% of physical damage received back to the attacker. Damage dealt increases by 1% per Might. (Maximum 40%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 40);
        }


        private void BloodWeapon()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.BloodWeapon)
                .Name("Blood Weapon")

                .AddPerkLevel()
                .Description("2% of the combat damage you deal is restored to your HP.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45);
        }


        private void Bloodlust()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.Bloodlust)
                .Name("Bloodlust")

                .AddPerkLevel()
                .Description("Sacrifice 40% HP in exchange for 20% of your maximum STM restored. Amount of STM restored increased by 1% per Might. (Maximum: 80%)")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 42)

                .AddPerkLevel()
                .Description("Gain +10% Attack against bleeding targets.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 40);
        }


        private void CriticalWard()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.CriticalWard)
                .Name("Critical Ward")

                .AddPerkLevel()
                .Description("If you would receive a critical hit, perform a Fortitude DC18 check to downgrade the attack to a normal hit. The attack will do minimum damage to you.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40);
        }


        private void CrushingBlow()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.CrushingBlow)
                .Name("Crushing Blow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrushingBlow1)
                .Description("Deal weapon DMG + 20 and generate significant enmity. Reduces the target's Defense by 15% for 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 15);
        }


        private void DefensiveHarmony()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.DefensiveHarmony)
                .Name("Defensive Harmony")

                .AddPerkLevel()
                .Description("HP restoration used on you is 20% more effective. 10% chance to restore 8 STM when healed. Chance increases by 1% per Might. (Maximum 40%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30);
        }


        private void Earthshatter()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.Earthshatter)
                .Name("Earthshatter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Earthshatter1)
                .Description("You deal weapon DMG + 20 to all enemies within the area of effect (line) from you.  Fortitude DC16 check to inflict Force Disruption on each target which disables the use of force abilities for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35);
        }


        private void EdgeOfDarkness()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.EdgeOfDarkness)
                .Name("Edge of Darkness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EdgeOfDarkness1)
                .Description("You deal weapon DMG + 15 to all enemies within the area of effect (sphere) from you and generate extra enmity.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 38);
        }


        private void EssenceHunter()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.EssenceHunter)
                .Name("Essence Hunter")

                .AddPerkLevel()
                .Description("Your next attack deals +18 DMG and has a DC15 Fortitude check to inflict Essence Drain, reducing the target's Attack by 15% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 12);
        }


        private void EssenceTap()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.EssenceTap)
                .Name("Essence Tap")

                .AddPerkLevel()
                .Description("When you take damage, gain +8% Attack for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5);
        }


        private void Flash()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.Flash)
                .Name("Flash")

                .AddPerkLevel()
                .Description("Enemies within the area of effect (sphere) around you receive the Flash effect which reduces their accuracy by 20% for 30 seconds. You generate significant enmity toward these enemies.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 18);
        }


        private void FortressStrike()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.FortressStrike)
                .Name("Fortress Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike1)
                .Description("Your next attack deals weapon DMG + 10 and generates extra enmity. You gain +10% Physical Defense for 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortressStrike2)
                .Description("Your next attack deals weapon DMG + 20 and generates extra enmity. You gain +20% Physical Defense for 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 25)

                .AddPerkLevel()
                .Description("Your next attack deals weapon DMG + 30 and generates extra enmity. You gain +30% Physical Defense for 16 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 42);
        }


        private void GuardiansReaping()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.GuardiansReaping)
                .Name("Guardian's Reaping")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 20% max HP to you and grants +15% Physical Defense to all nearby allies for 25 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 48);
        }


        private void GuardiansResolve()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.GuardiansResolve)
                .Name("Guardian's Resolve")

                .AddPerkLevel()
                .Description("Gain a damage absorption shield equal to 30% of your max HP for 30 seconds. While active, heal for 25% of damage absorbed.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 28);
        }


        private void LastStand()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.LastStand)
                .Name("Last Stand")

                .AddPerkLevel()
                .Description("When reduced below 25% HP, perform a Fortitude DC15 check. If passed, gain a damage shield equal to 20% of maximum HP for 12 seconds. This can only trigger once per 10 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20);
        }


        private void LifeSiphon()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.LifeSiphon)
                .Name("Life Siphon")

                .AddPerkLevel()
                .Description("When below 50% HP, your attacks heal you for 15% of damage dealt and generate +20% enmity.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20);
        }


        private void Rampart()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.Rampart)
                .Name("Rampart")

                .AddPerkLevel()
                .Description("All allies within the area of effect (sphere) from you receive a +25% defense bonus for 1 minute.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 32);
        }


        private void SacrificialBlade()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SacrificialBlade)
                .Name("Sacrificial Blade")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SacrificialBlade1)
                .Description("Deal weapon DMG + 25 to a single target. Costs 8% max HP.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 15);
        }


        private void SoulAmplification()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulAmplification)
                .Name("Soul Amplification")

                .AddPerkLevel()
                .Description("When you recover HP, gain +15% Attack for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30);
        }


        private void SoulAscension()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulAscension)
                .Name("Soul Ascension")

                .AddPerkLevel()
                .Description("You receive the Soul Ascension effect which grants +35% Attack and heals you for 50% of physical damage dealt. This effect lasts for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 50);
        }


        private void SoulBarrier()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulBarrier)
                .Name("Soul Barrier")

                .AddPerkLevel()
                .Description("When HP drops below 50% of maximum, a temporary shield forms which absorbs damage equal to 25% of max HP.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 35);
        }


        private void SoulBurst()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulBurst)
                .Name("Soul Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoulBurst1)
                .Description("Deal weapon DMG + 35 to all enemies within area of effect (cone). Costs 40% HP which is reduced by 1% per Might. (Minimum 10%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25);
        }


        private void SoulDevourer()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulDevourer)
                .Name("Soul Devourer")

                .AddPerkLevel()
                .Description("While active, gain +35% Attack and +15% critical chance,  but each attack you make deals 40% of the damage back to you. Damage reduced by 1% per Might. (Minimum 10%)")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 18);
        }


        private void SoulReaping()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulReaping)
                .Name("Soul Reaping")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 15% max HP and grants +20% Attack for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 48);
        }


        private void SoulSacrifice()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulSacrifice)
                .Name("Soul Sacrifice")

                .AddPerkLevel()
                .Description("Sacrifice 50% max HP to gain +35% Attack and +20% critical chance for 30 seconds. HP sacrificed decreases by 1% per Might. (Minimum 20%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 32);
        }


        private void SoulStorm()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulStorm)
                .Name("Soul Storm")

                .AddPerkLevel()
                .Description("Sacrifice 40% HP to raise the Attack of all nearby allies in area of effect (sphere) to you by 35%. HP sacrificed decreases by 1% per Might. (Minimum 10%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 38);
        }


        private void SoulStrike()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.SoulStrike)
                .Name("Soul Strike")

                .AddPerkLevel()
                .Description("Your next attack deals +15 DMG and heals you for 25% of damage dealt.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 8)

                .AddPerkLevel()
                .Description("Your next attack deals +30 DMG and heals you for 40% of damage dealt.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 28)

                .AddPerkLevel()
                .Description("Your next attack deals +45 DMG and heals you for 60% of damage dealt. Amount healed increased by 1% per Might. (Maximum 90%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45);
        }


        private void UnbreakableWill()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.UnbreakableWill)
                .Name("Unbreakable Will")

                .AddPerkLevel()
                .Description("Grants +5% Attack Deflection. When attacks are deflected, you restore 10% of maximum STM. Deflection increases by 0.5% per Might. (Maximum: 20%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 22);
        }


        private void VampiricFury()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.VampiricFury)
                .Name("Vampiric Fury")

                .AddPerkLevel()
                .Description("Critical hits restore HP equal to 40% of damage dealt. Amount healed increases by 1% per Might. (Maximum 75%)")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 22);
        }
    }
}
