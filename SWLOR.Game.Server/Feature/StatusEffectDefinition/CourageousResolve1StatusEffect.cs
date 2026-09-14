using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CourageousResolve1StatusEffect : StatusEffectBase
    {
        private readonly int _mindResistance;

        public override string Name => "Courageous Resolve";
        public override EffectIconType Icon => EffectIconType.CourageousResolve1StatusEffect;
        public override bool PersistsOnLogout => false;

        public CourageousResolve1StatusEffect() : this(10)
        {
        }

        public CourageousResolve1StatusEffect(int mindResistance)
        {
            _mindResistance = mindResistance;
        }

        public override IStatusEffect Clone()
        {
            return new CourageousResolve1StatusEffect(_mindResistance);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MindResistance] = _mindResistance;
        }
    }
}
