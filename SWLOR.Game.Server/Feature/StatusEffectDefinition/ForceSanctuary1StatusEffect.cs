using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSanctuary1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Sanctuary";
        public override EffectIconType Icon => EffectIconType.ForceSanctuary1StatusEffect;
        public override bool PersistsOnLogout => false;

        public ForceSanctuary1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
        }
    }
}
