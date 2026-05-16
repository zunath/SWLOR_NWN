using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FieldRecovery1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Field Recovery I";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override float Frequency => 4f;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(FieldRecovery2StatusEffect),
        };

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, ScaleBySourceSocial(1, 2));
        }
    }
}
