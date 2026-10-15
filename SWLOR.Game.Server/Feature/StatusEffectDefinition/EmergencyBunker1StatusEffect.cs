using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EmergencyBunker1StatusEffect : StatusEffectBase
    {
        public override string Name => "Emergency Bunker";
        public override EffectIconType Icon => EffectIconType.EmergencyBunker1StatusEffect;
        public override bool PersistsOnLogout => false;

        // Zone pulses re-apply this every few seconds with the same fixed stats.
        public override bool CanRefreshInPlace(IStatusEffect active) => active is EmergencyBunker1StatusEffect;

        public EmergencyBunker1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -15;
        }
    }
}
