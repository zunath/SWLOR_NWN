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
            Doublehand();
            Alertness();

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
                .Description("Goads the selected target and all other enemies within 8m of it into attacking you. Enmity generated increases by 1% per VIT.")
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
                .GrantsFeat(FeatType.DualWieldTrait)
                .Description("Off-hand attack delay is reduced by 10% when wielding two weapons or a double weapon.")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 5)
                .IncreasesStat(StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 10 : 0)

                .AddPerkLevel()
                .Description("Off-hand attack delay is reduced by 20% total when wielding two weapons or a double weapon.")
                .Price(3)
                .RequirementSkill(SkillType.Armor, 25)
                .IncreasesStat(StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 20 : 0)

                .AddPerkLevel()
                .Description("Off-hand attack delay is reduced by 30% total when wielding two weapons or a double weapon.")
                .Price(4)
                .RequirementSkill(SkillType.Armor, 40)
                .IncreasesStat(StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 30 : 0);
        }

        private void Doublehand()
        {
            _builder.Create(PerkCategoryType.General, PerkType.Doublehand)
                .Name("Doublehand")
                .AddPerkLevel()
                .GrantsFeat(FeatType.DoublehandTrait)
                .Description("Adds +10% weapon DMG when wielding one eligible one-handed melee or throwing weapon with an empty off hand. Stacks with the natural +20% Single Weapon bonus, for +30% total.")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 5)
                .IncreasesStat(StatType.SingleWeaponDamagePercentAdjustment, 10)
                .AddPerkLevel()
                .Description("Adds +25% total weapon DMG when wielding one eligible one-handed melee or throwing weapon with an empty off hand. Stacks with the natural +20% Single Weapon bonus, for +45% total.")
                .Price(3)
                .RequirementSkill(SkillType.Armor, 25)
                .IncreasesStat(StatType.SingleWeaponDamagePercentAdjustment, 25)
                .AddPerkLevel()
                .Description("Adds +40% total weapon DMG when wielding one eligible one-handed melee or throwing weapon with an empty off hand. Stacks with the natural +20% Single Weapon bonus, for +60% total.")
                .Price(4)
                .RequirementSkill(SkillType.Armor, 40)
                .IncreasesStat(StatType.SingleWeaponDamagePercentAdjustment, 40);
        }

        private void Alertness()
        {
            _builder.Create(PerkCategoryType.General, PerkType.Alertness)
                .Name("Alertness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AlertnessTrait)
                .Description("Increases Detection by 10, improving your chance to notice stealthed creatures.")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 5)
                .IncreasesStat(StatType.Detection, 10)

                .AddPerkLevel()
                .Description("Increases Detection by 15, improving your chance to notice stealthed creatures.")
                .Price(3)
                .RequirementSkill(SkillType.Armor, 25)
                .IncreasesStat(StatType.Detection, 15)

                .AddPerkLevel()
                .Description("Increases Detection by 20, improving your chance to notice stealthed creatures.")
                .Price(4)
                .RequirementSkill(SkillType.Armor, 40)
                .IncreasesStat(StatType.Detection, 20);
        }

    }
}
