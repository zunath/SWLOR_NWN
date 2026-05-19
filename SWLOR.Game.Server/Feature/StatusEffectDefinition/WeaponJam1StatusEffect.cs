using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeaponJam1StatusEffect : StatusEffectBase
    {
        public override string Name => "Weapon Jam I";
        public override EffectIconType Icon => EffectIconType.WeaponJam1StatusEffect;

        public WeaponJam1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -6;
        }
    }
}
