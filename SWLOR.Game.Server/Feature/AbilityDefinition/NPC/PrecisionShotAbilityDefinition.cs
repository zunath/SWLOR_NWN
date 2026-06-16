using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class PrecisionShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.PrecisionShot,
                "Precision Shot",
                Animation.PointPistol,
                InnateAbilityProfile.Rifle,
                RecastGroup.PrecisionShot,
                1.1f,
                15f,
                4,
                18,
                12,
                typeof(MarkedForDeathStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Special_Red_White,
                maxRange: 12f);

            return _builder.Build();
        }
    }
}
