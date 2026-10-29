using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CrossfireStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Crossfire Stance";
        public override float Frequency => 1f;
        public override EffectIconType Icon => EffectIconType.CrossfireStanceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public CrossfireStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 10;
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = 10;
            StatGroup.Stats[StatType.OrdinaryAttackDamagePercentAdjustment] = -20;
            StatGroup.Stats[StatType.CadenceExtendedDurationSeconds] = 18;
        }
        protected override void Tick(uint creature) => PairedRangedTraits.Reconcile(creature);
        protected override void Remove(uint creature) => PairedRangedTraits.ExitExtendedCadence(creature);
    }
}

