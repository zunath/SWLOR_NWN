using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ChitinGuardAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSelfBuff(
                _builder,
                FeatType.ChitinGuard,
                "Chitin Guard",
                Animation.ShieldWall,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.ChitinGuard,
                0.8f,
                32f,
                5,
                typeof(IronCarapaceStatusEffect),
                30f,
                VisualEffect.Vfx_Imp_Ac_Bonus);

            return _builder.Build();
        }
    }
}
