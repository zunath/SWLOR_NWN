using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FieldRecovery1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Field Recovery I";
        public override EffectIconType Icon => EffectIconType.FieldRecovery1StatusEffect;
        public override float Frequency => 4f;

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, ScaleBySourceSocial(1, 2));
        }
    }
}
