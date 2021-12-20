using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class OneHandedPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Doublehand(builder);
            DualWield(builder);
            WeaponFocusVibroblades(builder);
            ImprovedCriticalVibroblades(builder);
            VibrobladeProficiency(builder);
            VibrobladeMastery(builder);
            HackingBlade(builder);
            RiotBlade(builder);
            WeaponFocusFinesseVibroblades(builder);
            ImprovedCriticalFinesseVibroblades(builder);
            FinesseVibrobladeProficiency(builder);
            FinesseVibrobladeMastery(builder);
            PoisonStab(builder);
            Backstab(builder);
            WeaponFocusLightsabers(builder);
            ImprovedCriticalLightsabers(builder);
            LightsaberProficiency(builder);
            LightsaberMastery(builder);
            ForceLeap(builder);
            SaberStrike(builder);

            return builder.Build();
        }

        private void Doublehand(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Doublehand)
                .Name("Doublehand")
                
                .AddPerkLevel()
                .Description("Increases damage of one-handed weapons to 1.5xMGT when no off-hand item is equipped.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.Doublehand)
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    var rightHand = GetItemInSlot(InventorySlot.RightHand, player);
                    var leftHand = GetItemInSlot(InventorySlot.LeftHand, player);

                    // Item is going to right hand and no item is in left hand.
                    if (slot == InventorySlot.RightHand && !GetIsObjectValid(leftHand))
                    {
                        WeaponPlugin.SetOneHalfStrength(item, true, true);
                    }

                    // Item is going to left hand and an item is already in the right hand.
                    if (slot == InventorySlot.LeftHand && GetIsObjectValid(rightHand))
                    {
                        WeaponPlugin.SetOneHalfStrength(rightHand, false, true);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    var itemType = GetBaseItemType(item);
                    var rightHand = GetItemInSlot(InventorySlot.RightHand, player);
                    var rightType = GetBaseItemType(rightHand);
                    var leftHand = GetItemInSlot(InventorySlot.LeftHand, player);
                    var leftType = GetBaseItemType(leftHand);

                    // Item is being unequipped from right hand and there's a weapon in left hand.
                    if (slot == InventorySlot.RightHand &&
                        GetIsObjectValid(leftHand) &&
                        Item.OneHandedMeleeItemTypes.Contains(leftType))
                    {
                        WeaponPlugin.SetOneHalfStrength(leftHand, true, true);
                    }

                    // Item is being unequipped from left hand and there's a weapon in the right hand.
                    if(slot == InventorySlot.LeftHand &&
                       GetIsObjectValid(rightHand) &&
                       Item.OneHandedMeleeItemTypes.Contains(rightType))
                    {
                        WeaponPlugin.SetOneHalfStrength(rightHand, true, true);
                    }

                    // Always remove the item's one-half bonus
                    if (Item.OneHandedMeleeItemTypes.Contains(itemType))
                    {
                        WeaponPlugin.SetOneHalfStrength(item, false, true);
                    }
                });
        }

        private void DualWield(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.DualWield)
                .Name("Dual Wield")

                .AddPerkLevel()
                .Description("Enables the use of two one-handed weapons at the same time at -30%/-50% to hit.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.DualWield)

                .AddPerkLevel()
                .Description("Grants Two-weapon Fighting feat which reduces attack penalty from -30/-50 to -20/-40 when fighting with two weapons.")
                .Price(5)
                .RequirementSkill(SkillType.OneHanded, 35)
                .GrantsFeat(FeatType.TwoWeaponFighting)

                .AddPerkLevel()
                .Description("Grants Ambidexterity feat which reduces the attack penalty of your off-hand weapon by 20%.")
                .Price(6)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Ambidexterity);
        }

        private void WeaponFocusVibroblades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.WeaponFocusVibroblades)
                .Name("Weapon Focus - Vibroblades")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with vibroblades.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusVibroblades)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with vibroblades.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WeaponSpecializationVibroblades);
        }

        private void ImprovedCriticalVibroblades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.ImprovedCriticalVibroblades)
                .Name("Improved Critical - Vibroblades")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a vibroblade.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ImprovedCriticalVibroblades);
        }

        private void VibrobladeProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.VibrobladeProficiency)
                .Name("Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.VibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.VibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.VibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.VibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.VibrobladeProficiency5);
        }

        private void VibrobladeMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.VibrobladeMastery)
                .Name("Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.VibrobladeBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.VibrobladeBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.VibrobladeBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.VibrobladeBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Vibroblade.")
                .Price(12)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.VibrobladeMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Vibroblade.")
                .Price(12)
                .RequirementSkill(SkillType.OneHanded, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.VibrobladeMastery2);
        }

        private void HackingBlade(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.HackingBlade)
                .Name("Hacking Blade")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6.5 DMG and has a 50% chance to inflict Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.HackingBlade1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8.0 DMG and has a 75% chance to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 11.5 DMG and has a 100% chance to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade3);
        }

        private void RiotBlade(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.RiotBlade)
                .Name("Riot Blade")

                .AddPerkLevel()
                .Description("Instantly deals 2.0 DMG to your target.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.RiotBlade1)

                .AddPerkLevel()
                .Description("Instantly deals 4.5 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.RiotBlade2)

                .AddPerkLevel()
                .Description("Instantly deals 7.0 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade3);
        }

        private void WeaponFocusFinesseVibroblades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.WeaponFocusFinesseVibroblades)
                .Name("Weapon Focus - Finesse Vibroblades")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with finesse vibroblades.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusFinesseVibroblades)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with finesse vibroblades.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WeaponSpecializationFinesseVibroblades);
        }

        private void ImprovedCriticalFinesseVibroblades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.ImprovedCriticalFinesseVibroblades)
                .Name("Improved Critical - Finesse Vibroblades")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a finesse vibroblade.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ImprovedCriticalFinesseVibroblades);
        }

        private void FinesseVibrobladeProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.FinesseVibrobladeProficiency)
                .Name("Finesse Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Finesse Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency5);
        }

        private void FinesseVibrobladeMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.FinesseVibrobladeMastery)
                .Name("Finesse Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.FinesseVibrobladeBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.FinesseVibrobladeBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.FinesseVibrobladeBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.FinesseVibrobladeBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }
                })

                .AddPerkLevel()
                .Description("Grants an additional attack with a Finesse Vibroblade.")
                .Price(12)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FinesseVibrobladeMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack with a Finesse Vibroblade.")
                .Price(12)
                .RequirementSkill(SkillType.OneHanded, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FinesseVibrobladeMastery2);
        }

        private void PoisonStab(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.PoisonStab)
                .Name("Poison Stab")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6.0 DMG and has a 50% chance to inflict Poison for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.PoisonStab1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 7.5 DMG and has a 75% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 11.0 DMG and has a 100% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab3);
        }

        private void Backstab(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.Backstab)
                .Name("Backstab")

                .AddPerkLevel()
                .Description("Deals 4.0 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.Backstab1)

                .AddPerkLevel()
                .Description("Deals 9.0 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab2)

                .AddPerkLevel()
                .Description("Deals 14.0 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab3);
        }

        private void WeaponFocusLightsabers(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.WeaponFocusLightsabers)
                .Name("Weapon Focus - Lightsabers")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with lightsabers.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponFocusLightsabers)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with lightsabers.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponSpecializationLightsabers);
        }

        private void ImprovedCriticalLightsabers(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.ImprovedCriticalLightsabers)
                .Name("Improved Critical - Lightsabers")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a lightsaber.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ImprovedCriticalLightsabers);
        }

        private void LightsaberProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.LightsaberProficiency)
                .Name("Lightsaber Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Lightsabers.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency5);
        }

        private void LightsaberMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.LightsaberMastery)
                .Name("Lightsaber Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.LightsaberBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.LightsaberBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.LightsaberBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.LightsaberBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Lightsaber.")
                .Price(12)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Lightsaber.")
                .Price(12)
                .RequirementSkill(SkillType.OneHanded, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberMastery2);
        }

        private void ForceLeap(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.ForceLeap)
                .Name("Force Leap")

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 1.5 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap1)

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 4.0 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap2)

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 6.5 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap3);
        }

        private void SaberStrike(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.SaberStrike)
                .Name("Saber Strike")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6.5 DMG and has a 50% chance to inflict Breach for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8.0 DMG and has a 75% chance to inflict Breach for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 11.5 DMG and has a 100% chance to inflict Breach for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike3);
        }
    }
}
