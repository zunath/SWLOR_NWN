using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HungerOfTheDark1StatusEffect : StatusEffectBase
    {
        public override string Name => "Hunger of the Dark";
        public override EffectIconType Icon => EffectIconType.HungerOfTheDark1StatusEffect;
        public override bool PersistsOnLogout => false;

        public HungerOfTheDark1StatusEffect()
        {
            StatGroup.Stats[StatType.DarkForceDamageHPPercentRestore] = 12;
            StatGroup.Stats[StatType.DefeatedEnemyFPRestore] = 3;
        }
    }
}
