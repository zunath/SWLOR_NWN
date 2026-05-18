using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FieldRecovery2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Field Recovery II";
        public override EffectIconType Icon => EffectIconType.FieldRecovery2StatusEffect;

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, ScaleBySourceSocial(2, 3));
        }
    }
}
