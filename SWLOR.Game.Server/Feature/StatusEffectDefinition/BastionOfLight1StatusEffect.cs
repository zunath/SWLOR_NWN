using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BastionOfLight1StatusEffect : StatusEffectBase
    {
        public override string Name => "Bastion of Light";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;

        public BastionOfLight1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -10;
        }
    }
}
