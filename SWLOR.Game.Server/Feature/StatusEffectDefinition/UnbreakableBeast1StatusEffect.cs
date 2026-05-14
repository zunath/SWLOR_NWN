using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class UnbreakableBeast1StatusEffect : StatusEffectBase
    {
        public override string Name => "Unbreakable Beast";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;

        public UnbreakableBeast1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -25;
            StatGroup.Stats[StatType.MindResistance] = 100;
            StatGroup.Stats[StatType.MobilityResistance] = 100;
        }
    }
}
