using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Hasten2StatusEffect : StatusEffectBase
    {
        public override string Name => "Hasten II";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(Hasten1StatusEffect),
        };

        public Hasten2StatusEffect()
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = 25;
        }
    }
}
