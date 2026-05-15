using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RayshieldScreen2StatusEffect : StatusEffectBase
    {
        public override string Name => "Rayshield Screen II";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(RayshieldScreen1StatusEffect),
        };

        public RayshieldScreen2StatusEffect()
        {
            StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment] = -15;
        }
    }
}
