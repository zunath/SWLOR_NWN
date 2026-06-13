using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ArmorPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Provoke();
            DualWield();

            return _builder.Build();
        }

        private void Provoke()
        {
            _builder.Create(PerkCategoryType.General, PerkType.Provoke)
                .Name("Provoke")

                .AddPerkLevel()
                .Description("Goads a single target into attacking you. Enmity generated increases by 1% per VIT.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.Provoke1)

                .AddPerkLevel()
                .Description("Goads the selected target and all other enemies within range into attacking you. Enmity generated increases by 1% per VIT.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.Provoke2);
        }

        private void DualWield()
        {
            _builder.Create(PerkCategoryType.General, PerkType.DualWield)
                .Name("Dual Wield")

                .AddPerkLevel()
                .Description("Off-hand attack delay is reduced by 10% when making off-hand attacks.")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 5)
                .IncreasesStat(StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 10 : 0)

                .AddPerkLevel()
                .Description("Off-hand attack delay is reduced by 20% total when making off-hand attacks.")
                .Price(3)
                .RequirementSkill(SkillType.Armor, 25)
                .IncreasesStat(StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 20 : 0)

                .AddPerkLevel()
                .Description("Off-hand attack delay is reduced by 30% total when making off-hand attacks.")
                .Price(4)
                .RequirementSkill(SkillType.Armor, 40)
                .IncreasesStat(StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 30 : 0);
        }

    }
}

