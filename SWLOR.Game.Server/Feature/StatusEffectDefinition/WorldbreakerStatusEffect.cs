using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WorldbreakerStatusEffect : StatusEffectBase
    {
        public override string Name => "Worldbreaker";
        public override EffectIconType Icon => EffectIconType.WorldbreakerStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public WorldbreakerStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = -10;
        }
    }
}
