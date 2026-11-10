using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSanctuary1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Sanctuary";
        public override EffectIconType Icon => EffectIconType.ForceSanctuary1StatusEffect;
        public override bool PersistsOnLogout => false;

        // Zone pulses re-apply this every few seconds with the same fixed stats.
        public override bool CanRefreshInPlace(IStatusEffect active) => active is ForceSanctuary1StatusEffect;

        public ForceSanctuary1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
        }
    }
}
