using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain2StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Drain II";
        public override EffectIconType Icon => EffectIconType.DamageDecrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceDrain3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceDrain1StatusEffect),
        };

        public ForceDrain2StatusEffect()
        {
        }
    }
}
