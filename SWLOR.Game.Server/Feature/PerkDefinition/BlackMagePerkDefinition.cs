using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BlackMagePerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            ElementalSeal(builder);
            Blizzard(builder);
            Fire(builder);
            Thunder(builder);
            MPBoost(builder);
            Battlemage(builder);
            ElementalSpread(builder);
            Warp(builder);
            Sleep(builder);
            ClearMind(builder);
            BlazeSpikes(builder);

            return builder.Build();
        }

        private static void ElementalSeal(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.ElementalSeal)
                .Name("Elemental Seal")
                .Description("Triples the effectiveness of black magic for 30 seconds.")
                
                .AddPerkLevel()
                .Description("Grants the Elemental Seal ability.")
                .RequirementSkill(SkillType.BlackMagic, 50)
                .RequirementSkill(SkillType.Staff, 50)
                .RequirementSkill(SkillType.MysticArmor, 50)
                .RequirementQuest("a_black_mages_test")
                .Price(15)
                .GrantsFeat(Feat.ElementalSeal);
        }

        private static void Blizzard(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.Blizzard)
                .Name("Blizzard")
                .Description("Grants the Ice black magic ability.")

                .AddPerkLevel()
                .Description("Deals ice damage to a single target.")
                .Price(2)
                .GrantsFeat(Feat.Blizzard1)

                .AddPerkLevel()
                .Description("Deals ice damage to a single target and slows movement for 15 seconds.")
                .RequirementSkill(SkillType.BlackMagic, 15)
                .RequirementSkill(SkillType.Staff, 10)
                .Price(3)
                .GrantsFeat(Feat.Blizzard2)

                .AddPerkLevel()
                .Description("Deals ice damage to a single target and slows movement for 30 seconds.")
                .RequirementSkill(SkillType.BlackMagic, 30)
                .RequirementSkill(SkillType.Staff, 20)
                .Price(4)
                .GrantsFeat(Feat.Blizzard3);
        }

        private static void Fire(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.Fire)
                .Name("Fire")
                .Description("Grants the Fire black magic ability.")

                .AddPerkLevel()
                .Description("Deals fire damage to a single target.")
                .RequirementSkill(SkillType.BlackMagic, 5)
                .RequirementSkill(SkillType.Staff, 5)
                .Price(2)
                .GrantsFeat(Feat.Fire1)

                .AddPerkLevel()
                .Description("Deals fire damage to a single target and inflicts Burn for 15 seconds.")
                .RequirementSkill(SkillType.BlackMagic, 20)
                .RequirementSkill(SkillType.Staff, 15)
                .Price(3)
                .GrantsFeat(Feat.Fire2)

                .AddPerkLevel()
                .Description("Deals fire damage to a single target and inflicts Burn for 30 seconds.")
                .RequirementSkill(SkillType.BlackMagic, 35)
                .RequirementSkill(SkillType.Staff, 25)
                .Price(4)
                .GrantsFeat(Feat.Fire3);
        }

        private static void Thunder(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.Thunder)
                .Name("Thunder")
                .Description("Grants the Thunder black magic ability.")

                .AddPerkLevel()
                .Description("Deals electrical damage to a single target.")
                .RequirementSkill(SkillType.BlackMagic, 10)
                .RequirementSkill(SkillType.Staff, 10)
                .Price(2)
                .GrantsFeat(Feat.Thunder1)

                .AddPerkLevel()
                .Description("Deals electrical damage to a single target and inflicts stun for 2 seconds.")
                .RequirementSkill(SkillType.BlackMagic, 25)
                .RequirementSkill(SkillType.Staff, 20)
                .Price(3)
                .GrantsFeat(Feat.Thunder2)

                .AddPerkLevel()
                .Description("Deals electrical damage to a single target and inflicts stun for 6 seconds.")
                .RequirementSkill(SkillType.BlackMagic, 40)
                .RequirementSkill(SkillType.Staff, 30)
                .Price(4)
                .GrantsFeat(Feat.Thunder3);
        }

        private static void MPBoost(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.MPBoost)
                .Name("MP Boost")
                .Description("Increases your maximum MP pool.")
                .TriggerPurchase((player, type, level) =>
                {
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    Stat.AdjustMaxMP(dbPlayer, 10);
                    DB.Set(playerId, dbPlayer);
                })
                .TriggerRefund((player, type, level) =>
                {
                    var amountToRemove = level * 10;
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    Stat.AdjustMaxMP(dbPlayer, -amountToRemove);
                    DB.Set(playerId, dbPlayer);
                })

                .AddPerkLevel()
                .Description("Increases your maximum MP by 10.")
                .RequirementSkill(SkillType.BlackMagic, 15)
                .RequirementSkill(SkillType.Staff, 20)
                .Price(4)

                .AddPerkLevel()
                .Description("Increases your maximum MP by 20.")
                .RequirementSkill(SkillType.BlackMagic, 30)
                .RequirementSkill(SkillType.Staff, 40)
                .Price(4)

                .AddPerkLevel()
                .Description("Increases your maximum MP by 30.")
                .RequirementSkill(SkillType.BlackMagic, 45)
                .RequirementSkill(SkillType.Staff, 50)
                .Price(4);
        }

        private static void Battlemage(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.Battlemage)
                .Name("Battlemage")
                .Description("Attacks with a staff restore MP on each hit.")

                .AddPerkLevel()
                .Description("Attacks with a staff restore 1 MP per hit.")
                .RequirementSkill(SkillType.Staff, 15)
                .Price(6)

                .AddPerkLevel()
                .Description("Attacks with a staff restore 2 MP per hit.")
                .RequirementSkill(SkillType.Staff, 30)
                .Price(6)

                .AddPerkLevel()
                .Description("Attacks with a staff restore 3 MP per hit.")
                .RequirementSkill(SkillType.Staff, 45)
                .Price(6);
        }

        private static void ElementalSpread(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.ElementalSpread)
                .Name("Elemental Spread")
                .Description("Your next blizzard, fire, or thunder spell will target all nearby enemies.")
                
                .AddPerkLevel()
                .Description("Grants the Elemental Spread ability.")
                .RequirementSkill(SkillType.BlackMagic, 25)
                .Price(4)
                .GrantsFeat(Feat.ElementalSpread);
        }

        private static void Warp(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.Warp)
                .Name("Warp")
                .Description("Teleports targets to their home point.")

                .AddPerkLevel()
                .Description("Returns your target to their home point.")
                .RequirementSkill(SkillType.BlackMagic, 15)
                .Price(2)
                .GrantsFeat(Feat.Warp1)

                .AddPerkLevel()
                .Description("Returns you and your entire party to their respective home points.")
                .RequirementSkill(SkillType.BlackMagic, 35)
                .Price(4)
                .GrantsFeat(Feat.Warp2);
        }

        private static void Sleep(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.Sleep)
                .Name("Sleep")
                .Description("Puts a single target to sleep for between 15 and 30 seconds. Effect will be removed if target is damaged.")
                
                .AddPerkLevel()
                .Description("Grants the Sleep ability.")
                .RequirementSkill(SkillType.BlackMagic, 10)
                .RequirementSkill(SkillType.Staff, 5)
                .Price(2)
                .GrantsFeat(Feat.Sleep);
        }

        private static void ClearMind(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.ClearMind)
                .Name("Clear Mind")
                .Description("Increases your natural MP regeneration.")

                .AddPerkLevel()
                .Description("Increases your natural MP regeneration by 2.")
                .RequirementSkill(SkillType.BlackMagic, 15)
                .RequirementSkill(SkillType.Staff, 10)
                .Price(2)

                .AddPerkLevel()
                .Description("Increases your natural MP regeneration by 4.")
                .RequirementSkill(SkillType.BlackMagic, 30)
                .RequirementSkill(SkillType.Staff, 20)
                .Price(2);
        }

        private static void BlazeSpikes(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.BlazeSpikes)
                .Name("Blaze Spikes")
                .Description("Grants a fire damage shield to you for the next 5 minutes.")

                .AddPerkLevel()
                .Description("Grants a fire damage shield to you for the next 5 minutes.")
                .RequirementSkill(SkillType.BlackMagic, 15)
                .RequirementSkill(SkillType.Staff, 10)
                .Price(3)
                .GrantsFeat(Feat.BlazeSpikes1)

                .AddPerkLevel()
                .Description("Grants a fire damage shield to you for the next 5 minutes.")
                .RequirementSkill(SkillType.BlackMagic, 30)
                .RequirementSkill(SkillType.Staff, 20)
                .Price(3)
                .GrantsFeat(Feat.BlazeSpikes2);
        }
    }
}
