using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class BloodFrenzyFlurryAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.BloodFrenzyFlurry,
                "Blood Frenzy Flurry",
                Animation.Whirlwind,
                InnateAbilityProfile.Vibroblade,
                RecastGroup.BloodFrenzyFlurry,
                0.8f,
                20f,
                6,
                20,
                12,
                typeof(BleedStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Blood_Spark_Medium,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 5f);

            return _builder.Build();
        }
    }
}
