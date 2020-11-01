using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class KnightPerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Invincible(builder);
            ShieldBash(builder);
            Provoke(builder);
            Cleave(builder);
            SpikedDefense(builder);
            ShieldProficiency(builder);
            Cover(builder);
            Defender(builder);
            Ironclad(builder);
            Flash(builder);
            
            return builder.Build();
        }

        private static void Invincible(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Invincible)
                .Name("Invicible")
                .Description("Grants invincibility for 30 seconds and increases enmity toward all nearby targets.")
                
                .AddPerkLevel()
                .Description("Grants the Invincibility ability.")
                .RequirementSkill(SkillType.Chivalry, 50)
                .RequirementSkill(SkillType.HeavyArmor, 50)
                .RequirementSkill(SkillType.Longsword, 50)
                .RequirementQuest("a_knights_test")
                .Price(15)
                .GrantsFeat(Feat.Invincible);
        }

        private static void ShieldBash(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Bash)
                .Name("Bash")
                .Description("Deals 1d4 damage and stuns your target for a short period of time.")

                .AddPerkLevel()
                .Description("Grants the Bash ability.")
                .RequirementSkill(SkillType.Chivalry, 5)
                .Price(3)
                .GrantsFeat(Feat.Bash);
        }

        private static void Provoke(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Provoke)
                .Name("Provoke")
                .Description("Goads your target into attacking you.")

                .AddPerkLevel()
                .Description("Goads a single enemy into attacking you.")
                .RequirementSkill(SkillType.Chivalry, 10)
                .Price(3)
                .GrantsFeat(Feat.Provoke1)

                .AddPerkLevel()
                .Description("Goads a group of enemies into attacking you.")
                .RequirementSkill(SkillType.Chivalry, 25)
                .Price(4)
                .GrantsFeat(Feat.Provoke2);
        }

        private static void Cleave(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Cleave)
                .Name("Cleave")
                .Description("If the character kills an opponent, he gets a free attack against any opponent who is within melee weapon range.")

                .AddPerkLevel()
                .Description("Grants the Cleave ability.")
                .RequirementSkill(SkillType.Chivalry, 10)
                .Price(3)
                .GrantsFeat(Feat.Cleave);
        }

        private static void SpikedDefense(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.SpikedDefense)
                .Name("Spiked Defense")
                .Description("Grants a temporary damage shield to you.")

                .AddPerkLevel()
                .Description("Grants the Spiked Defense ability.")
                .RequirementSkill(SkillType.Chivalry, 15)
                .Price(3)
                .GrantsFeat(Feat.SpikedDefense);
        }

        private static void ShieldProficiency(PerkBuilder builder)
        {
            const string EffectTag = "SHIELD_PROFICIENCY_CONCEALMENT";
            static void ApplyShieldProficiency(uint player, uint item, int level)
            {
                var concealmentAmount = level * 2;

                var newEffect = SupernaturalEffect(EffectConcealment(concealmentAmount));
                newEffect = TagEffect(newEffect, EffectTag);
                ApplyEffectToObject(DurationType.Permanent, newEffect, player);
            }

            builder.Create(PerkCategoryType.Knight, PerkType.ShieldProficiency)
                .Name("Shield Proficiency")
                .Description("Increases your damage reduction when equipped with a shield.")
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.LeftHand, player);
                    RemoveEffectByTag(player, EffectTag);
                    ApplyShieldProficiency(player, item, level);
                })
                .TriggerRefund((player, type, level) =>
                {
                    RemoveEffectByTag(player, EffectTag);
                })
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    var itemType = GetBaseItemType(item);
                    if (itemType != BaseItem.SmallShield &&
                        itemType != BaseItem.LargeShield &&
                        itemType != BaseItem.TowerShield)
                        return;

                    ApplyShieldProficiency(player, item, level);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    // Must be equipped with a shield.
                    var itemType = GetBaseItemType(item);
                    if (itemType != BaseItem.SmallShield &&
                        itemType != BaseItem.LargeShield &&
                        itemType != BaseItem.TowerShield)
                        return;

                    RemoveEffectByTag(player, EffectTag);
                })

                .AddPerkLevel()
                .Description("Increases damage reduction by 2% when equipped with a shield.")
                .RequirementSkill(SkillType.HeavyArmor, 5)
                .RequirementSkill(SkillType.Chivalry, 10)
                .Price(3)

                .AddPerkLevel()
                .Description("Increases damage reduction by 4% when equipped with a shield.")
                .RequirementSkill(SkillType.HeavyArmor, 10)
                .RequirementSkill(SkillType.Chivalry, 20)
                .Price(3)

                .AddPerkLevel()
                .Description("Increases damage reduction by 6% when equipped with a shield.")
                .RequirementSkill(SkillType.HeavyArmor, 20)
                .RequirementSkill(SkillType.Chivalry, 30)
                .Price(3)

                .AddPerkLevel()
                .Description("Increases damage reduction by 8% when equipped with a shield.")
                .RequirementSkill(SkillType.HeavyArmor, 30)
                .RequirementSkill(SkillType.Chivalry, 40)
                .Price(3)

                .AddPerkLevel()
                .Description("Increases damage reduction by 10% when equipped with a shield.")
                .RequirementSkill(SkillType.HeavyArmor, 40)
                .RequirementSkill(SkillType.Chivalry, 50)
                .Price(3);
        }

        private static void Cover(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Cover)
                .Name("Cover")
                .Description("Protects an ally from damage and you receive it instead.")

                .AddPerkLevel()
                .Description("10% of damage is blocked and you receive it instead.")
                .RequirementSkill(SkillType.Chivalry, 10)
                .RequirementSkill(SkillType.Longsword, 5)
                .Price(4)
                .GrantsFeat(Feat.Cover1)

                .AddPerkLevel()
                .Description("15% of damage is blocked and you receive it instead.")
                .RequirementSkill(SkillType.Chivalry, 20)
                .RequirementSkill(SkillType.Longsword, 10)
                .Price(4)
                .GrantsFeat(Feat.Cover2)

                .AddPerkLevel()
                .Description("20% of damage is blocked and you receive it instead.")
                .RequirementSkill(SkillType.Chivalry, 30)
                .RequirementSkill(SkillType.Longsword, 15)
                .Price(4)
                .GrantsFeat(Feat.Cover3)

                .AddPerkLevel()
                .Description("25% of damage is blocked and you receive it instead.")
                .RequirementSkill(SkillType.Chivalry, 40)
                .RequirementSkill(SkillType.Longsword, 20)
                .Price(4)
                .GrantsFeat(Feat.Cover4);
        }

        private static void Defender(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Defender)
                .Name("Defender")
                .Description("Increases the damage resistance of your party members within range.")

                .AddPerkLevel()
                .Description("Increases the damage resistance of your party members within range by 3.")
                .RequirementSkill(SkillType.Chivalry, 15)
                .RequirementSkill(SkillType.Longsword, 20)
                .Price(3)
                .GrantsFeat(Feat.Defender1)

                .AddPerkLevel()
                .Description("Increases the damage resistance of your party members within range by 5.")
                .RequirementSkill(SkillType.Chivalry, 30)
                .RequirementSkill(SkillType.Longsword, 40)
                .Price(3)
                .GrantsFeat(Feat.Defender2)

                .AddPerkLevel()
                .Description("Increases the damage resistance of your party members within range by 7.")
                .RequirementSkill(SkillType.Chivalry, 45)
                .RequirementSkill(SkillType.Longsword, 50)
                .Price(3)
                .GrantsFeat(Feat.Defender3);
        }

        private static void Ironclad(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Ironclad)
                .Name("Ironclad")
                .Description("Increases your damage resistance.")

                .AddPerkLevel()
                .Description("Increases your damage resistance by 4.")
                .RequirementSkill(SkillType.Chivalry, 10)
                .RequirementSkill(SkillType.Longsword, 15)
                .Price(3)
                .GrantsFeat(Feat.Ironclad1)

                .AddPerkLevel()
                .Description("Increases your damage resistance by 6.")
                .RequirementSkill(SkillType.Chivalry, 20)
                .RequirementSkill(SkillType.Longsword, 25)
                .Price(3)
                .GrantsFeat(Feat.Ironclad2)

                .AddPerkLevel()
                .Description("Increases your damage resistance by 8.")
                .RequirementSkill(SkillType.Chivalry, 40)
                .RequirementSkill(SkillType.Longsword, 45)
                .Price(3)
                .GrantsFeat(Feat.Ironclad3);
        }

        private static void Flash(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.Flash)
                .Name("Flash")
                .Description("Blinds all nearby enemies and increases enmity.")

                .AddPerkLevel()
                .Description("Blinds all nearby enemies for 30 seconds and increases enmity.")
                .RequirementSkill(SkillType.Chivalry, 25)
                .RequirementSkill(SkillType.Longsword, 25)
                .Price(6)
                .GrantsFeat(Feat.Flash1)

                .AddPerkLevel()
                .Description("Blinds all nearby enemies for 45 seconds and increases enmity.")
                .RequirementSkill(SkillType.Chivalry, 50)
                .RequirementSkill(SkillType.Longsword, 50)
                .Price(8)
                .GrantsFeat(Feat.Flash2);
        }
    }
}
