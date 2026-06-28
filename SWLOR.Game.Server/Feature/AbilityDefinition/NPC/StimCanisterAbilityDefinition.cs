using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class StimCanisterAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.StimCanister,
                "Stim Canister",
                Animation.ThrowGrenade,
                InnateAbilityProfile.Throwing,
                RecastGroup.StimCanister,
                1.2f,
                24f,
                6,
                14,
                10,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Sphere,
                4f,
                0f,
                CombatDamageType.Poison,
                ResistanceType.Poison,
                VisualEffect.Vfx_Imp_Poison_S,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Acid,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
