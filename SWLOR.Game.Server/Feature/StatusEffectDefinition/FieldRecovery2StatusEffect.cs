using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FieldRecovery2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Field Recovery II";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override float Frequency => 4f;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(FieldRecovery1StatusEffect),
        };

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, ScaleBySourceSocial(2, 3));
        }
    }
}
