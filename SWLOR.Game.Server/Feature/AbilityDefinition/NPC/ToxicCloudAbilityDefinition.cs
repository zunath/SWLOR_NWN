using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ToxicCloudAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ToxicCloud,
                "Toxic Cloud",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.ToxicCloud,
                1.6f,
                24f,
                7,
                10,
                15,
                typeof(ToxinStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Poison,
                ResistanceType.Poison,
                VisualEffect.Vfx_Imp_Poison_L,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Nature,
                maxRange: 10f);

            return _builder.Build();
        }
    }
}
