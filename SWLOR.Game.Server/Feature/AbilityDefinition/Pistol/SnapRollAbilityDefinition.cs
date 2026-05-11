using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class SnapRollAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float RecastDelay = 60f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SnapRoll1(builder);
            SnapRoll2(builder);

            return builder.Build();
        }

        private static void SnapRoll1(AbilityBuilder builder)
        {
            var ability = builder
                .Create(FeatType.SnapRoll1, PerkType.SnapRoll)
                .Name("Snap Roll I")
                .Level(1)
                .HasRecastDelay(RecastGroup.SnapRoll, RecastDelay);

            ConfigureSelfStatus(
                ability,
                typeof(SnapRollStatusEffect),
                duration: 6f,
                stamina: 6,
                activator => Enmity.ModifyEnmityOnAll(activator, -150));
        }

        private static void SnapRoll2(AbilityBuilder builder)
        {
            var ability = builder
                .Create(FeatType.SnapRoll2, PerkType.SnapRoll)
                .Name("Snap Roll II")
                .Level(2)
                .HasRecastDelay(RecastGroup.SnapRoll, RecastDelay);
            Func<IStatusEffect> statusEffectFactory = () => new SnapRollStatusEffect(35);

            ConfigureSelfStatus(
                ability,
                statusEffectFactory,
                duration: 8f,
                stamina: 8,
                activator => Enmity.ModifyEnmityOnAll(activator, -250));
        }
    }
}
