using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceChokeDamageStatusEffect : ForceDamageOverTimeStatusEffectBase
    {
        public override string Name => "Force Choke";
        public override EffectIconType Icon => EffectIconType.ForceChokeDamageStatusEffect;
        private readonly int _baseTotalDamage;

        public ForceChokeDamageStatusEffect()
            : this(8)
        {
        }

        public ForceChokeDamageStatusEffect(int baseTotalDamage)
            : base(baseTotalDamage)
        {
            _baseTotalDamage = baseTotalDamage;
        }

        public override IStatusEffect Clone()
        {
            return new ForceChokeDamageStatusEffect(_baseTotalDamage);
        }
    }
}
