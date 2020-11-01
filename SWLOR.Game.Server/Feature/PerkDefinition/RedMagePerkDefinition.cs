using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class RedMagePerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Manafont(builder);
            Protect(builder);
            TransferMP(builder);
            TransferStamina(builder);
            PiercingStab(builder);
            Blind(builder);
            RecoveryStab(builder);
            Convert(builder);
            Refresh(builder);
            RapierFinesse(builder);
            Jolt(builder);
            PoisonStab(builder);
            ShockSpikes(builder);
            DeliberateStab(builder);

            return builder.Build();
        }

        private static void Manafont(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.Manafont)
                .Name("Manafont")
                .Description("Spells may be cast for free for the next 30 seconds.")

                .AddPerkLevel()
                .Description("Grants the Manafont ability.")
                .RequirementSkill(SkillType.RedMagic, 50)
                .RequirementSkill(SkillType.Rapier, 50)
                .RequirementSkill(SkillType.MysticArmor, 50)
                .RequirementQuest("a_red_mages_test")
                .Price(15)
                .GrantsFeat(Feat.Manafont);
        }

        private static void Protect(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.Protect)
                .Name("Protect")
                .Description("Grants protect to a single target, increasing damage resistance.")
                
                .AddPerkLevel()
                .Description("Grants the Protect ability.")
                .RequirementSkill(SkillType.RedMagic, 7)
                .RequirementSkill(SkillType.Rapier, 10)
                .Price(2)
                .GrantsFeat(Feat.Protect);
        }

        private static void TransferMP(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.TransferMP)
                .Name("Transfer MP")
                .Description("Restores MP for a single target.")

                .AddPerkLevel()
                .Description("Restores 10 MP for a single target.")
                .RequirementSkill(SkillType.RedMagic, 15)
                .RequirementSkill(SkillType.Rapier, 10)
                .Price(2)
                .GrantsFeat(Feat.TransferMP1)

                .AddPerkLevel()
                .Description("Restores 20 MP for a single target.")
                .RequirementSkill(SkillType.RedMagic, 30)
                .RequirementSkill(SkillType.Rapier, 20)
                .Price(2)
                .GrantsFeat(Feat.TransferMP2);
        }

        private static void TransferStamina(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.TransferStamina)
                .Name("Transfer Stamina")
                .Description("Restores Stamina for a single target.")

                .AddPerkLevel()
                .Description("Restores 10 STM for a single target.")
                .RequirementSkill(SkillType.RedMagic, 15)
                .RequirementSkill(SkillType.Rapier, 10)
                .Price(2)
                .GrantsFeat(Feat.TransferStamina1)

                .AddPerkLevel()
                .Description("Restores 20 STM for a single target.")
                .RequirementSkill(SkillType.RedMagic, 30)
                .RequirementSkill(SkillType.Rapier, 20)
                .Price(2)
                .GrantsFeat(Feat.TransferStamina2);
        }

        private static void PiercingStab(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.PiercingStab)
                .Name("Piercing Stab")
                .Description("Your next melee attack inflicts Bleed on your target.")

                .AddPerkLevel()
                .Description("Inflicts Bleed for 30 seconds on your next attack.")
                .RequirementSkill(SkillType.Rapier, 5)
                .Price(2)
                .GrantsFeat(Feat.PiercingStab1)

                .AddPerkLevel()
                .Description("Inflicts Bleed II for 30 seconds on your next attack.")
                .RequirementSkill(SkillType.RedMagic, 10)
                .RequirementSkill(SkillType.Rapier, 15)
                .Price(2)
                .GrantsFeat(Feat.PiercingStab2)

                .AddPerkLevel()
                .Description("Inflicts Bleed III for 30 seconds on your next attack.")
                .RequirementSkill(SkillType.RedMagic, 20)
                .RequirementSkill(SkillType.Rapier, 30)
                .Price(2)
                .GrantsFeat(Feat.PiercingStab3);
        }

        private static void Blind(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.Blind)
                .Name("Blind")
                .Description("You inflict blindness on a single target for a short period of time.")

                .AddPerkLevel()
                .Description("Inflicts blindness on a single target for 15 seconds.")
                .RequirementSkill(SkillType.RedMagic, 10)
                .RequirementSkill(SkillType.Rapier, 5)
                .Price(2)
                .GrantsFeat(Feat.Blind1)

                .AddPerkLevel()
                .Description("Inflicts blindness on a single target for 30 seconds.")
                .RequirementSkill(SkillType.RedMagic, 20)
                .RequirementSkill(SkillType.Rapier, 10)
                .Price(2)
                .GrantsFeat(Feat.Blind2);
        }

        private static void RecoveryStab(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.RecoveryStab)
                .Name("Recovery Stab")
                .Description("Your next melee attack restores HP to all nearby party members.")

                .AddPerkLevel()
                .Description("Restores 2d6 HP to all nearby party members.")
                .RequirementSkill(SkillType.RedMagic, 15)
                .RequirementSkill(SkillType.Rapier, 20)
                .Price(4)
                .GrantsFeat(Feat.RecoveryStab1)

                .AddPerkLevel()
                .Description("Restores 3d8 HP to all nearby party members.")
                .RequirementSkill(SkillType.RedMagic, 30)
                .RequirementSkill(SkillType.Rapier, 35)
                .Price(4)
                .GrantsFeat(Feat.RecoveryStab2)

                .AddPerkLevel()
                .Description("Restores 3d8 HP to all nearby party members and grants Regen for 24 seconds.")
                .RequirementSkill(SkillType.RedMagic, 45)
                .RequirementSkill(SkillType.Rapier, 45)
                .Price(4)
                .GrantsFeat(Feat.RecoveryStab3);
        }

        private static void Convert(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.Convert)
                .Name("Convert")
                .Description("Your HP and MP values are switched.")

                .AddPerkLevel()
                .Description("Grants the Convert ability.")
                .RequirementSkill(SkillType.RedMagic, 40)
                .RequirementSkill(SkillType.Rapier, 35)
                .Price(8)
                .GrantsFeat(Feat.Convert);
        }

        private static void Refresh(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.Refresh)
                .Name("Refresh")
                .Description("Restores MP over time to a single target.")

                .AddPerkLevel()
                .Description("Grants the Refresh ability.")
                .RequirementSkill(SkillType.RedMagic, 35)
                .RequirementSkill(SkillType.Rapier, 20)
                .Price(6)
                .GrantsFeat(Feat.Refresh);
        }

        private static void RapierFinesse(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.RapierFinesse)
                .Name("Rapier Finesse")
                .Description("You make melee attack rolls with your DEX if it is higher than your STR. Must be equipped with a rapier.")

                .AddPerkLevel()
                .Description("Grants the Rapier Finesse ability.")
                .RequirementSkill(SkillType.RedMagic, 5)
                .RequirementSkill(SkillType.Rapier, 10)
                .Price(4)
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (GetBaseItemType(item) != BaseItem.Rapier ||
                        slot != InventorySlot.RightHand)
                        return;

                    Creature.AddFeat(player, Feat.WeaponFinesse);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    if (GetBaseItemType(item) != BaseItem.Rapier ||
                        GetItemInSlot(InventorySlot.RightHand, player) != item)
                        return;

                    Creature.RemoveFeat(player, Feat.WeaponFinesse);
                }); ;
        }

        private static void Jolt(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.Jolt)
                .Name("Jolt")
                .Description("Deals unaspected damage to a single target.")

                .AddPerkLevel()
                .Description("Deals 1d6 unaspected damage to a single target.")
                .RequirementSkill(SkillType.RedMagic, 10)
                .RequirementSkill(SkillType.Rapier, 5)
                .Price(3)
                .GrantsFeat(Feat.Jolt1)

                .AddPerkLevel()
                .Description("Deals 2d6 unaspected damage to a single target.")
                .RequirementSkill(SkillType.RedMagic, 25)
                .RequirementSkill(SkillType.Rapier, 20)
                .Price(3)
                .GrantsFeat(Feat.Jolt2)

                .AddPerkLevel()
                .Description("Deals 2d10 unaspected damage to a single target.")
                .RequirementSkill(SkillType.RedMagic, 45)
                .RequirementSkill(SkillType.Rapier, 35)
                .Price(4)
                .GrantsFeat(Feat.Jolt3);
        }

        private static void PoisonStab(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.PoisonStab)
                .Name("Poison Stab")
                .Description("Your next melee attack inflicts poison on your target.")

                .AddPerkLevel()
                .Description("Your next melee attack inflicts poison on your target.")
                .RequirementSkill(SkillType.RedMagic, 10)
                .RequirementSkill(SkillType.Rapier, 15)
                .Price(3)
                .GrantsFeat(Feat.PoisonStab1)

                .AddPerkLevel()
                .Description("Your next melee attack inflicts poison on your target.")
                .RequirementSkill(SkillType.RedMagic, 20)
                .RequirementSkill(SkillType.Rapier, 30)
                .Price(4)
                .GrantsFeat(Feat.PoisonStab2);
        }

        private static void ShockSpikes(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.ShockSpikes)
                .Name("Shock Spikes")
                .Description("Grants an electrical damage shield for a short period of time.")

                .AddPerkLevel()
                .Description("Grants an electrical damage shield for the next 5 minutes.")
                .RequirementSkill(SkillType.RedMagic, 15)
                .RequirementSkill(SkillType.Rapier, 10)
                .Price(3)
                .GrantsFeat(Feat.ShockSpikes1)

                .AddPerkLevel()
                .Description("Grants an electrical damage shield for the next 5 minutes.")
                .RequirementSkill(SkillType.RedMagic, 30)
                .RequirementSkill(SkillType.Rapier, 20)
                .Price(3)
                .GrantsFeat(Feat.ShockSpikes2);
        }

        private static void DeliberateStab(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.DeliberateStab)
                .Name("Deliberate Stab")
                .Description("Your next stab effect will last twice as long as normal.")

                .AddPerkLevel()
                .Description("Grants the Deliberate Stab ability.")
                .RequirementSkill(SkillType.RedMagic, 45)
                .RequirementSkill(SkillType.Rapier, 40)
                .Price(6)
                .GrantsFeat(Feat.DeliberateStab);
        }
    }
}
