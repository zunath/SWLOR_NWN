using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Assault1StatusEffect : StatusEffectBase
    {
        public override string Name => "Assault I";
        public override EffectIconType Icon => EffectIconType.DamageResistance;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(Assault2StatusEffect),
            typeof(Assault3StatusEffect),
        };

        public Assault1StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 6;
        }
    }
}
