using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class GrenadeBurstAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.GrenadeBurst,
                "Grenade Burst",
                Animation.ThrowGrenade,
                InnateAbilityProfile.Devices,
                RecastGroup.GrenadeBurst,
                1.5f,
                22f,
                6,
                18,
                12,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Fire,
                ResistanceType.Fire,
                VisualEffect.Vfx_Imp_Flame_M,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Fire,
                maxRange: 10f);

            return _builder.Build();
        }
    }
}
