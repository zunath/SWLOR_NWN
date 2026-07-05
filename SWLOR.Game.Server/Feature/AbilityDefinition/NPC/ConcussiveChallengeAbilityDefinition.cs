using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ConcussiveChallengeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ConcussiveChallenge,
                "Concussive Challenge",
                Animation.FireForgetTaunt,
                InnateAbilityProfile.Vibroblade,
                RecastGroup.ConcussiveChallenge,
                1.0f,
                24f,
                5,
                30,
                6,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                6f,
                0f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Magical_Vision,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Mind,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
