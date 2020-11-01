using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class WhiteMagePerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Benediction(builder);
            Cure(builder);
            Poisona(builder);
            Protectra(builder);
            Curaga(builder);
            Clarity(builder);
            Regen(builder);
            Raise(builder);
            CombatMage(builder);
            TeleportBalamb(builder);
            Stone(builder);
            Dia(builder);

            return builder.Build();
        }

        private static void Benediction(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Benediction)
                .Name("Benediction")
                .Description("You and all nearby party members are healed to full.")

                .AddPerkLevel()
                .Description("Grants the Benediction ability.")
                .RequirementSkill(SkillType.WhiteMagic, 50)
                .RequirementSkill(SkillType.Rod, 50)
                .RequirementSkill(SkillType.MysticArmor, 50)
                .RequirementQuest("a_white_mages_test")
                .Price(15)
                .GrantsFeat(Feat.Benediction);
        }

        private static void Cure(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Cure)
                .Name("Cure")
                .Description("Restores HP of a single target.")

                .AddPerkLevel()
                .Description("Grants the Cure ability.")
                .Price(2)
                .GrantsFeat(Feat.Cure1)

                .AddPerkLevel()
                .Description("Grants the Cure II ability.")
                .RequirementSkill(SkillType.WhiteMagic, 15)
                .RequirementSkill(SkillType.Rod, 10)
                .Price(4)
                .GrantsFeat(Feat.Cure2)

                .AddPerkLevel()
                .Description("Grants the Cure III ability.")
                .RequirementSkill(SkillType.WhiteMagic, 30)
                .RequirementSkill(SkillType.Rod, 20)
                .Price(6)
                .GrantsFeat(Feat.Cure3);
        }

        private static void Poisona(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Poisona)
                .Name("Poisona")
                .Description("Removes poison from a single target.")
                
                .AddPerkLevel()
                .Description("Grants the Poisona ability.")
                .RequirementSkill(SkillType.WhiteMagic, 10)
                .RequirementSkill(SkillType.Rod, 5)
                .Price(3)
                .GrantsFeat(Feat.Poisona);
        }

        private static void Protectra(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Protectra)
                .Name("Protectra")
                .Description("Grants Protect to all nearby party members, increasing damage resistance.")

                .AddPerkLevel()
                .Description("Grants the Protectra ability.")
                .RequirementSkill(SkillType.WhiteMagic, 7)
                .RequirementSkill(SkillType.Rod, 10)
                .Price(8)
                .GrantsFeat(Feat.Protectra);
        }

        private static void Curaga(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Curaga)
                .Name("Curaga")
                .Description("Restores the HP of all nearby party members.")

                .AddPerkLevel()
                .Description("Grants the Curaga ability.")
                .RequirementSkill(SkillType.WhiteMagic, 25)
                .RequirementSkill(SkillType.Rod, 15)
                .Price(4)
                .GrantsFeat(Feat.Curaga1)

                .AddPerkLevel()
                .Description("Grants the Curaga II ability.")
                .RequirementSkill(SkillType.WhiteMagic, 40)
                .RequirementSkill(SkillType.Rod, 30)
                .Price(4)
                .GrantsFeat(Feat.Curaga2);
        }

        private static void Clarity(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Clarity)
                .Name("Clarity")
                .Description("Increases your natural MP regeneration.")

                .AddPerkLevel()
                .Description("Natural MP regeneration increases by 2.")
                .RequirementSkill(SkillType.WhiteMagic, 15)
                .RequirementSkill(SkillType.Rod, 10)
                .Price(2)

                .AddPerkLevel()
                .Description("Natural MP regeneration increases by 4.")
                .RequirementSkill(SkillType.WhiteMagic, 30)
                .RequirementSkill(SkillType.Rod, 20)
                .Price(2);
        }

        private static void Regen(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Regen)
                .Name("Regen")
                .Description("Grants regeneration to a single target for a short period of time.")

                .AddPerkLevel()
                .Description("Grants the Regen ability.")
                .RequirementSkill(SkillType.WhiteMagic, 20)
                .RequirementSkill(SkillType.Rod, 15)
                .Price(4)
                .GrantsFeat(Feat.Regen1)

                .AddPerkLevel()
                .Description("Grants the Regen II ability.")
                .RequirementSkill(SkillType.WhiteMagic, 40)
                .RequirementSkill(SkillType.Rod, 30)
                .Price(4)
                .GrantsFeat(Feat.Regen2);
        }

        private static void Raise(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Raise)
                .Name("Raise")
                .Description("Revives a target who is knocked out.")
                
                .AddPerkLevel()
                .Description("Grants the Raise ability.")
                .RequirementSkill(SkillType.WhiteMagic, 25)
                .RequirementSkill(SkillType.Rod, 20)
                .Price(6)
                .GrantsFeat(Feat.Raise);
        }

        private static void CombatMage(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.CombatMage)
                .Name("Combat Mage")
                .Description("Attacks with rods restore MP.")

                .AddPerkLevel()
                .Description("1 MP per hit is restored.")
                .RequirementSkill(SkillType.Rod, 15)
                .Price(4)

                .AddPerkLevel()
                .Description("2 MP per hit is restored.")
                .RequirementSkill(SkillType.Rod, 30)
                .Price(4)

                .AddPerkLevel()
                .Description("3 MP per hit is restored.")
                .RequirementSkill(SkillType.Rod, 45)
                .Price(4);
        }

        private static void TeleportBalamb(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.TeleportBalamb)
                .Name("Teleport-Balamb")
                .Description("Teleports you and all nearby party members to Balamb Garden.")

                .AddPerkLevel()
                .Description("Grants the Teleport-Balamb ability.")
                .RequirementSkill(SkillType.WhiteMagic, 35)
                .Price(4)
                .GrantsFeat(Feat.TeleportBalamb);
        }

        private static void Stone(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Stone)
                .Name("Stone")
                .Description("Deals earth damage to a single target.")

                .AddPerkLevel()
                .Description("Deals 1d6 earth damage to a single target.")
                .RequirementSkill(SkillType.WhiteMagic, 5)
                .RequirementSkill(SkillType.Rod, 5)
                .Price(2)
                .GrantsFeat(Feat.Stone1)

                .AddPerkLevel()
                .Description("Deals 2d6 earth damage to a single target.")
                .RequirementSkill(SkillType.WhiteMagic, 20)
                .RequirementSkill(SkillType.Rod, 15)
                .Price(2)
                .GrantsFeat(Feat.Stone2)

                .AddPerkLevel()
                .Description("Deals 2d10 earth damage to a single target.")
                .RequirementSkill(SkillType.WhiteMagic, 35)
                .RequirementSkill(SkillType.Rod, 25)
                .Price(2)
                .GrantsFeat(Feat.Stone3);
        }

        private static void Dia(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.Dia)
                .Name("Dia")
                .Description("Inflicts Dia on a single target which deals Holy damage over time.")

                .AddPerkLevel()
                .Description("Grants the Dia I ability.")
                .RequirementSkill(SkillType.WhiteMagic, 10)
                .RequirementSkill(SkillType.Rod, 5)
                .Price(2)
                .GrantsFeat(Feat.Dia1)

                .AddPerkLevel()
                .Description("Grants the Dia II ability.")
                .RequirementSkill(SkillType.WhiteMagic, 30)
                .RequirementSkill(SkillType.Rod, 15)
                .Price(2)
                .GrantsFeat(Feat.Dia2)

                .AddPerkLevel()
                .Description("Grants the Dia III ability.")
                .RequirementSkill(SkillType.WhiteMagic, 40)
                .RequirementSkill(SkillType.Rod, 30)
                .Price(4)
                .GrantsFeat(Feat.Dia3);
        }
    }
}
