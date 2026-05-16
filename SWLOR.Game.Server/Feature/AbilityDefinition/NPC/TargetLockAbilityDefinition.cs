using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class TargetLockAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.TargetLock,
                "Target Lock",
                InnateAbilityProfile.Rifle,
                RecastGroup.TargetLock,
                0.8f,
                20f,
                3,
                8,
                15,
                typeof(VulnerableStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Magical_Vision,
                maxRange: 12f);

            return _builder.Build();
        }
    }
}
