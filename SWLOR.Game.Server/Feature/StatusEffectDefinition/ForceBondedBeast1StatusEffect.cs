using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceBondedBeast1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force-Bonded Beast";
        public override EffectIconType Icon => EffectIconType.ForceBondedBeast1StatusEffect;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        public ForceBondedBeast1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -10;
        }

        protected override void Tick(uint creature)
        {
            Stat.RestoreFP(creature, 1);
        }
    }
}
