using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class SpinningWhirlAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTelegraphedArea(builder.Create(FeatType.SpinningWhirl1, PerkType.SpinningWhirl).Name("Spinning Whirl I").Level(1), SkillType.TwinBlade, CombatImpactAreaShape.Sphere, 10, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 5, true);
            ConfigureTelegraphedArea(builder.Create(FeatType.SpinningWhirl2, PerkType.SpinningWhirl).Name("Spinning Whirl II").Level(2), SkillType.TwinBlade, CombatImpactAreaShape.Sphere, 18, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 6, true);
            ConfigureTelegraphedArea(builder.Create(FeatType.SpinningWhirl3, PerkType.SpinningWhirl).Name("Spinning Whirl III").Level(3), SkillType.TwinBlade, CombatImpactAreaShape.Sphere, 28, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 8, true);

            return builder.Build();
        }
    }
}
