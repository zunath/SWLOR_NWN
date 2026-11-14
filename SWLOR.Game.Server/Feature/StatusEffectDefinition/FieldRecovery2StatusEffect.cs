using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FieldRecovery2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Field Recovery II";
        public override EffectIconType Icon => EffectIconType.FieldRecovery2StatusEffect;
        public override float Frequency => 4f;

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, ScaleBySourceSocial(2, 3));
        }
    }
}
