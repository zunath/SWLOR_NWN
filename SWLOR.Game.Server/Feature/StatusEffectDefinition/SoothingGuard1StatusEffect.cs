using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoothingGuard1StatusEffect : StatusEffectBase
    {
        public override string Name => "Soothing Guard I";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public SoothingGuard1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -10;
        }
    }
}
