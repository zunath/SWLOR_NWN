using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class UntouchableInstinct1StatusEffect : StatusEffectBase
    {
        public override string Name => "Untouchable Instinct";
        public override EffectIconType Icon => EffectIconType.UntouchableInstinct1StatusEffect;
        public override bool PersistsOnLogout => false;

        public UntouchableInstinct1StatusEffect()
        {
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 30;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 20;
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = 20;
        }
    }
}
