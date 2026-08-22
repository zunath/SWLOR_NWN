using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardianReflexesStatusEffect : StatusEffectBase
    {
        private readonly int _guardChance;

        public override string Name => "Guardian Reflexes";
        public override EffectIconType Icon => EffectIconType.GuardianReflexesStatusEffect;

        public GuardianReflexesStatusEffect() : this(25)
        {
        }

        public GuardianReflexesStatusEffect(int guardChance)
        {
            _guardChance = guardChance;
            StatGroup.Stats[StatType.Guard] = _guardChance;
        }

        public override IStatusEffect Clone()
        {
            return new GuardianReflexesStatusEffect(_guardChance);
        }
    }
}
