using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AkimboStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Akimbo Stance";
        public override float Frequency => 1f;
        public override EffectIconType Icon => EffectIconType.AkimboStanceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public AkimboStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 10;
            StatGroup.Stats[StatType.OrdinaryAttackDamagePercentAdjustment] = -15;
            StatGroup.Stats[StatType.OrdinaryAttackHasteRampPerStack] = 2;
            StatGroup.Stats[StatType.OrdinaryAttackHasteRampMaximumStacks] = 5;
            StatGroup.Stats[StatType.OrdinaryAttackHasteRampGrantCooldownSeconds] = 2;
            StatGroup.Stats[StatType.OrdinaryAttackHasteRampDurationSeconds] = 6;
        }
        protected override void Tick(uint creature)
        {
            OrdinaryAttackTraits.Reconcile(creature);
        }
        protected override void Remove(uint creature) => OrdinaryAttackTraits.Clear(creature);
    }
}

