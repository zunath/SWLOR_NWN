using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ArmorPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Provoke();
            Endure();

            return _builder.Build();
        }

        private void Provoke()
        {
            _builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Provoke)
                .Name("Provoke")

                .AddPerkLevel()
                .Description("Goads a single target into attacking you.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.Provoke1)

                .AddPerkLevel()
                .Description("Goads all enemies within range into attacking you.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.Provoke2);
        }

        private void Endure()
        {
            _builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Endure)
                .Name("Endure")

                .AddPerkLevel()
                .Description("1% chance per MGT mod to prevent melee damage dealt from an enemy once per round. Must be wearing full heavy armor. (Max: 10%)")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 15)

                .AddPerkLevel()
                .Description("2% chance per MGT mod to prevent melee damage dealt from an enemy once per round. Must be wearing full heavy armor. (Max: 20%)")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("3% chance per MGT mod to prevent melee damage dealt from an enemy once per round. Must be wearing full heavy armor. (Max: 30%)")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 50);
        }
    }
}
