using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Drain I";
        public override EffectIconType Icon => EffectIconType.DamageDecrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(ForceDrain2StatusEffect),
            typeof(ForceDrain3StatusEffect),
        };

        public ForceDrain1StatusEffect()
        {
        }
    }
}
