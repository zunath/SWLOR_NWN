using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class HoarfrostGlobAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.HoarfrostGlob,
                "Hoarfrost Glob",
                Animation.CastOutAnimation,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.HoarfrostGlob,
                1.3f,
                16f,
                4,
                13,
                11,
                typeof(FreezingStatusEffect),
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Imp_Head_Cold,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
