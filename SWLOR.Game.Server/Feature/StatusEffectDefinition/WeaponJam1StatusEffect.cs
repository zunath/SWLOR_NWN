using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeaponJam1StatusEffect : StatusEffectBase
    {
        public override string Name => "Weapon Jam";
        public override EffectIconType Icon => EffectIconType.WeaponJam1StatusEffect;

        public WeaponJam1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -6;
        }
    }
}
