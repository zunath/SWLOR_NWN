using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class DarkKnightPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            DarkNebula(builder);

            return builder.Build();
        }

        private static void DarkNebula(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.DarkKnight, PerkType.DarkNebula)
                .Name("Dark Nebula")
                .Description("Your next attack deals 2 times the amount of damage to nearby enemies and you restore HP equal to your damage.")

                .AddPerkLevel()
                .Description("Grants the Dark Nebula ability.")
                .RequirementSkill(SkillType.Darkness, 50)
                .RequirementSkill(SkillType.GreatSword, 50)
                .RequirementSkill(SkillType.HeavyArmor, 50)
                .RequirementQuest("a_dark_knights_test")
                .Price(15)
                .GrantsFeat(Feat.DarkNebula);
        }
    }
}