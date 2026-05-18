using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeaponJam2StatusEffect : StatusEffectBase
    {
        public override string Name => "Weapon Jam II";
        public override EffectIconType Icon => EffectIconType.WeaponJam2StatusEffect;

        public WeaponJam2StatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = -10;
        }
    }
}
