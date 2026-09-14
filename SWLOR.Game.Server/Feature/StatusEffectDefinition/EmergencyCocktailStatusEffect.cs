using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EmergencyCocktailStatusEffect : StatusEffectBase
    {
        public override string Name => "Emergency Cocktail";
        public override EffectIconType Icon => EffectIconType.EmergencyCocktailStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        public EmergencyCocktailStatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -12;
            StatGroup.Resists[ResistanceType.Poison] = 50;
        }

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, 1);
        }
    }
}
