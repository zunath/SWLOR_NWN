using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class TwoHandedPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            PowerAttack();
            SuperiorWeaponFocus();
            IncreasedMultiplier();
            Cleave();
            WeaponFocusHeavyVibroblades();
            ImprovedCriticalHeavyVibroblades();
            HeavyVibrobladeProficiency();
            HeavyVibrobladeMastery();
            CrescentMoon();
            HardSlash();
            WeaponFocusPolearms();
            ImprovedCriticalPolearms();
            PolearmProficiency();
            PolearmMastery();
            Skewer();
            DoubleThrust();
            WeaponFocusTwinBlades();
            ImprovedCriticalTwinBlades();
            TwinBladeProficiency();
            TwinBladeMastery();
            SpinningWhirl();
            CrossCut();
            WeaponFocusSaberstaffs();
            ImprovedCriticalSaberstaffs();
            SaberstaffProficiency();
            SaberstaffMastery();
            CircleSlash();
            DoubleStrike();
            ImprovedTwoWeaponFighting();
            StrongStyleSaberstaff();

            return _builder.Build();
        }

        private void PowerAttack()
        {
            _builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.PowerAttack)
                .Name("Power Attack")

                .AddPerkLevel()
                .Description("Grants the Power Attack feat which grants a 3 DMG bonus at the cost of -5 to accuracy. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .GrantsFeat(FeatType.PowerAttack)

                .AddPerkLevel()
                .Description("Grants the Improved Power Attack feat which grants a 6 DMG bonus at the cost of -10 to accuracy. [Cross Skill]")
                .Price(4)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ImprovedPowerAttack);
        }

        private void SuperiorWeaponFocus()
        {
            _builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.SuperiorWeaponFocus)
                .Name("Superior Weapon Focus")

                .AddPerkLevel()
                .Description("Grants an additional 5 accuracy while wielding two-handed weapons.")
                .Price(5)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .GrantsFeat(FeatType.SuperiorWeaponFocus);
        }

        private void IncreasedMultiplier()
        {
            _builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.IncreasedMultiplier)
                .Name("Increased Multiplier")

                .AddPerkLevel()
                .Description("Increases the maximum critical damage of two-handed weapons by 50%.")
                .Price(6)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .GrantsFeat(FeatType.IncreaseMultiplier);
        }

        private void Cleave()
        {
            _builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.Cleave)
                .Name("Cleave")

                .AddPerkLevel()
                .Description("Grants a free attack against any opponent who is within melee range when an enemy is killed. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .GrantsFeat(FeatType.Cleave);
        }

        private void WeaponFocusHeavyVibroblades()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.WeaponFocusHeavyVibroblades)
                .Name("Weapon Focus - Heavy Vibroblades")

                .AddPerkLevel()
                .Description("Your accuracy with heavy vibroblades is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusHeavyVibroblades)

                .AddPerkLevel()
                .Description("Your base damage with heavy vibroblades is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationHeavyVibroblades);
        }

        private void ImprovedCriticalHeavyVibroblades()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.ImprovedCriticalHeavyVibroblades)
                .Name("Improved Critical - Heavy Vibroblades")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with heavy vibroblades by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalHeavyVibroblades);
        }

        private void HeavyVibrobladeProficiency()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HeavyVibrobladeProficiency)
                .Name("Heavy Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Heavy Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.HeavyVibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .GrantsFeat(FeatType.HeavyVibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .GrantsFeat(FeatType.HeavyVibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .GrantsFeat(FeatType.HeavyVibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .GrantsFeat(FeatType.HeavyVibrobladeProficiency5);
        }

        private void HeavyVibrobladeMastery()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HeavyVibrobladeMastery)
                .Name("Heavy Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Heavy Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .GrantsFeat(FeatType.HeavyVibrobladeMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Heavy Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .GrantsFeat(FeatType.HeavyVibrobladeMastery2);
        }

        private void CrescentMoon()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.CrescentMoon)
                .Name("Crescent Moon")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a DC10 Fortitude check to inflict stun for 3 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrescentMoon1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a DC15 Fortitude check to inflict stun for 3 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrescentMoon2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a DC20 Fortitude check to inflict stun for 3 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrescentMoon3);
        }

        private void HardSlash()
        {
            _builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HardSlash)
                .Name("Hard Slash")

                .AddPerkLevel()
                .Description("Instantly deals 16 DMG to your target.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HardSlash1)

                .AddPerkLevel()
                .Description("Instantly deals 24 DMG to your target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HardSlash2)

                .AddPerkLevel()
                .Description("Instantly deals 38 DMG to your target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HardSlash3);
        }

        private void WeaponFocusPolearms()
        {
            _builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.WeaponFocusPolearms)
                .Name("Weapon Focus - Polearms")

                .AddPerkLevel()
                .Description("Your accuracy with polearms is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusPolearms)

                .AddPerkLevel()
                .Description("Your base damage with polearms is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationPolearms);
        }

        private void ImprovedCriticalPolearms()
        {
            _builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.ImprovedCriticalPolearms)
                .Name("Improved Critical - Polearms")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with polearms by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalPolearms);
        }

        private void PolearmProficiency()
        {
            _builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.PolearmProficiency)
                .Name("Polearm Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 polearms.")
                .Price(2)
                .GrantsFeat(FeatType.PolearmProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .GrantsFeat(FeatType.PolearmProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .GrantsFeat(FeatType.PolearmProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .GrantsFeat(FeatType.PolearmProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .GrantsFeat(FeatType.PolearmProficiency5);
        }

        private void PolearmMastery()
        {
            _builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.PolearmMastery)
                .Name("Polearm Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Polearm.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .GrantsFeat(FeatType.PolearmMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Polearm.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .GrantsFeat(FeatType.PolearmMastery2);
        }

        private void Skewer()
        {
            _builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.Skewer)
                .Name("Skewer")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a DC10 Will check to interrupt the target's abilities and concentration.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Skewer1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a DC15 Will check to interrupt the target's abilities and concentration.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Skewer2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a DC20 Will check to interrupt the target's abilities and concentration.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Skewer3);
        }

        private void DoubleThrust()
        {
            _builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.DoubleThrust)
                .Name("Double Thrust")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 11 DMG.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleThrust1)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 19 DMG.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleThrust2)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 29 DMG.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleThrust3);
        }

        private void WeaponFocusTwinBlades()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.WeaponFocusTwinBlades)
                .Name("Weapon Focus - Twin Blades")

                .AddPerkLevel()
                .Description("Your accuracy with twin blades is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusTwinBlades)

                .AddPerkLevel()
                .Description("Your base damage with twin blades is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationTwinBlades);
        }

        private void ImprovedCriticalTwinBlades()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.ImprovedCriticalTwinBlades)
                .Name("Improved Critical - Twin Blades")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with twin blades by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalTwinBlades);
        }

        private void TwinBladeProficiency()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.TwinBladeProficiency)
                .Name("Twin Blade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 twin blades.")
                .Price(2)
                .GrantsFeat(FeatType.TwinBladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .GrantsFeat(FeatType.TwinBladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .GrantsFeat(FeatType.TwinBladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .GrantsFeat(FeatType.TwinBladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .GrantsFeat(FeatType.TwinBladeProficiency5);
        }

        private void TwinBladeMastery()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.TwinBladeMastery)
                .Name("Twin Blade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack while equipped with twin blades.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .GrantsFeat(FeatType.TwinBladeMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack while equipped with twin blades.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .GrantsFeat(FeatType.TwinBladeMastery2);
        }

        private void SpinningWhirl()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.SpinningWhirl)
                .Name("Spinning Whirl")

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 10 DMG each.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl1)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 18 DMG each.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl2)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 28 DMG each.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl3);
        }

        private void CrossCut()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.CrossCut)
                .Name("Cross Cut")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 8 DMG and has a DC10 Reflex check to reduce your target's Evasion by 2 for 1 minute.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrossCut1)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 17 DMG and has a DC15 Reflex check to reduce your target's Evasion by 4 for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrossCut2)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 25 DMG and has a DC20 Reflex check to reduce your target's Evasion by 6 for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrossCut3);
        }

        private void WeaponFocusSaberstaffs()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.WeaponFocusSaberstaffs)
                .Name("Weapon Focus - Saberstaffs")

                .AddPerkLevel()
                .Description("Your accuracy with saberstaves is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponFocusSaberstaffs)

                .AddPerkLevel()
                .Description("Your base damage with saberstaves is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponSpecializationSaberstaffs);
        }

        private void ImprovedCriticalSaberstaffs()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.ImprovedCriticalSaberstaffs)
                .Name("Improved Critical - Saberstaffs")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with saberstaves by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ImprovedCriticalSaberstaffs);
        }

        private void SaberstaffProficiency()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.SaberstaffProficiency)
                .Name("Saberstaff Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Saberstaffs.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffProficiency5);
        }

        private void SaberstaffMastery()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.SaberstaffMastery)
                .Name("Saberstaff Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Saberstaff.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Saberstaff.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberstaffMastery2);
        }

        private void CircleSlash()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.CircleSlash)
                .Name("Circle Slash")

                .AddPerkLevel()
                .Description("Attacks up to 6 nearby enemies for 14 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleSlash1)

                .AddPerkLevel()
                .Description("Attacks up to 6 nearby enemies for 24 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleSlash2)

                .AddPerkLevel()
                .Description("Attacks up to 6 nearby enemies for 32 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleSlash3);
        }

        private void DoubleStrike()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.DoubleStrike)
                .Name("Double Strike")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 15 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DoubleStrike1)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 24 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DoubleStrike2)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 32 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DoubleStrike3);
        }

        private void ImprovedTwoWeaponFighting()
        {
            _builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.ImprovedTwoWeaponFightingTwoHanded)
                .Name("Improved Two Weapon Fighting (Two-Handed)")

                .AddPerkLevel()
                .Description("Grants an additional off-hand attack when dual wielding or using a double-sided weapon, and reduces the two-weapon fighting penalty to 0%/-10%. [Cross Skill]")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCannotHavePerk(PerkType.ImprovedTwoWeaponFightingOneHanded)
                .GrantsFeat(FeatType.ImprovedTwoWeaponFighting);
        }

        private void StrongStyleSaberstaff()
        {
            _builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.StrongStyleSaberstaff)
                .Name("Strong Style (Saberstaff)")
                .TriggerRefund((player) =>
                {
                    Ability.ToggleAbility(player, AbilityToggleType.StrongStyleSaberstaff, false);
                })
                .TriggerPurchase((player) =>
                {
                    Ability.ToggleAbility(player, AbilityToggleType.StrongStyleSaberstaff, false);
                })

                .AddPerkLevel()
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .Description("Saberstaff attacks use your Perception stat for accuracy and Might stat for damage while active. Additionally, your saberstaff damage is increased by your MGT modifier while active.")
                .Price(1)
                .GrantsFeat(FeatType.StrongStyleSaberstaff);
        }
    }
}
