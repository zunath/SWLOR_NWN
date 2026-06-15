using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CalmingStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Calming Stance";
        public override EffectIconType Icon => EffectIconType.CalmingStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public override float Frequency => 1f;

        public CalmingStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -40;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = -40;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -40;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -40;
        }

        protected override void Tick(uint creature)
        {
            Stat.RestoreStamina(creature, 3);
        }

    }
}
