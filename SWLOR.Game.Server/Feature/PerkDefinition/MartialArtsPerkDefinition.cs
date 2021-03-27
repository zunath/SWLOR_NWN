using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class MartialArtsPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Knockdown(builder);
            FlurryOfBlows(builder);
            InnerStrength(builder);
            MartialFinesse(builder);
            WeaponFocusKnuckles(builder);
            ImprovedCriticalKnuckles(builder);
            KnucklesProficiency(builder);
            KnucklesMastery(builder);
            ElectricFist(builder);
            StrikingCobra(builder);
            WeaponFocusStaves(builder);
            ImprovedCriticalStaves(builder);
            StaffProficiency(builder);
            StaffMastery(builder);
            Slam(builder);
            SpinningWhirl(builder);

            return builder.Build();
        }

        private void Knockdown(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Knockdown)
                .Name("Knockdown")

                .AddPerkLevel()
                .Description("Your next attack has a 60% chance to inflict knockdown on your target for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(Feat.Knockdown);
        }

        private void FlurryOfBlows(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.FlurryOfBlows)
                .Name("Flurry of Blows")

                .AddPerkLevel()
                .Description("Grants the Flurry of Blows feat. You receive an extra attack per round when fighting with unarmed attacks or a staff. However, all attacks in that round suffer a -2 attack penalty.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.FlurryOfBlows);
        }

        private void InnerStrength(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.InnerStrength)
                .Name("Inner Strength")

                .AddPerkLevel()
                .Description("Improves critical range by 1.")
                .Price(5)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.InnerStrength1)

                .AddPerkLevel()
                .Description("Improves critical range by 2.")
                .Price(6)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.InnerStrength2);
        }

        private void MartialFinesse(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.MartialFinesse)
                .Name("Martial Finesse")

                .AddPerkLevel()
                .Description("You make melee attack rolls with your DEX score if it is higher than your STR score.")
                .Price(5)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.MartialFinesse);
        }

        private void WeaponFocusKnuckles(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKnuckles, PerkType.WeaponFocusKnuckles)
                .Name("Weapon Focus - Knuckles")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with knuckles.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(Feat.WeaponFocusKnuckles)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with knuckles.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.WeaponSpecializationKnuckles);
        }

        private void ImprovedCriticalKnuckles(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKnuckles, PerkType.ImprovedCriticalKnuckles)
                .Name("Improved Critical - Knuckles")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using knuckles.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.ImprovedCriticalKnuckles);
        }

        private void KnucklesProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKnuckles, PerkType.KnucklesProficiency)
                .Name("Knuckles Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Knuckles.")
                .Price(2)
                .GrantsFeat(Feat.KnucklesProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 10)
                .GrantsFeat(Feat.KnucklesProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(Feat.KnucklesProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 30)
                .GrantsFeat(Feat.KnucklesProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 40)
                .GrantsFeat(Feat.KnucklesProficiency5);
        }

        private void KnucklesMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKnuckles, PerkType.KnucklesMastery)
                .Name("Knuckles Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.KnucklesBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + level;
                        Creature.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.KnucklesBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        Creature.SetBaseAttackBonus(player, bab);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.KnucklesBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + 1;
                        Creature.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.KnucklesBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        Creature.SetBaseAttackBonus(player, bab);
                    }
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Knuckles.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.KnucklesMastery1)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Knuckles.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.KnucklesMastery2)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Knuckles.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.KnucklesMastery3);
        }

        private void ElectricFist(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKnuckles, PerkType.ElectricFist)
                .Name("Electric Fist")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d4 damage and has a 50% chance to inflict Shock for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(Feat.ElectricFist1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d4 damage and has a 75% chance to inflict Shock for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.ElectricFist2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d4 damage and has a 100% chance to inflict Shock for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.ElectricFist3);
        }

        private void StrikingCobra(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKnuckles, PerkType.StrikingCobra)
                .Name("Striking Cobra")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d6 damage and has a 50% chance to inflict Poison for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(Feat.StrikingCobra1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d6 damage and has a 75% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(Feat.StrikingCobra2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d6 damage and has a 100% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.StrikingCobra3);
        }

        private void WeaponFocusStaves(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.WeaponFocusStaves)
                .Name("Weapon Focus - Staves")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with staves.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(Feat.WeaponFocusStaves)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with staves.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.WeaponSpecializationStaves);
        }

        private void ImprovedCriticalStaves(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.ImprovedCriticalStaves)
                .Name("Improved Critical - Staves")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a staff.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.ImprovedCriticalStaff);
        }

        private void StaffProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.StaffProficiency)
                .Name("Staff Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Staves.")
                .Price(2)
                .GrantsFeat(Feat.StaffProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 10)
                .GrantsFeat(Feat.StaffProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(Feat.StaffProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 30)
                .GrantsFeat(Feat.StaffProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 40)
                .GrantsFeat(Feat.StaffProficiency5);
        }

        private void StaffMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.StaffMastery)
                .Name("Staff Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + level;
                        Creature.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        Creature.SetBaseAttackBonus(player, bab);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + 1;
                        Creature.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        Creature.SetBaseAttackBonus(player, bab);
                    }
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.StaffMastery1)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.StaffMastery2)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.StaffMastery3);
        }

        private void Slam(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.Slam)
                .Name("Slam")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d4 damage and has a 50% chance to inflict Blindness for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(Feat.Slam1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d4 damage and has a 75% chance to inflict Blindness for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.Slam2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d4 damage and has a 100% chance to inflict Blindness for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.Slam3);
        }

        private void SpinningWhirl(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.SpinningWhirl)
                .Name("Spinning Whirl")

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 1d8 of damage each.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(Feat.SpinningWhirl1)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 2d6 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.SpinningWhirl2)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 3d6 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(Feat.SpinningWhirl3);
        }
    }
}
