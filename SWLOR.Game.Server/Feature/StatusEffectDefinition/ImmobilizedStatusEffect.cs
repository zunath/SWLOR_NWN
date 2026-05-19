using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImmobilizedStatusEffect : StatusEffectBase
    {
        public override string Name => "Immobilized";
        public override EffectIconType Icon => EffectIconType.ImmobilizedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mobility;

        public ImmobilizedStatusEffect()
        {
            StatGroup.Stats[StatType.MovementSpeedDisabled] = 1;
        }

        protected override void Remove(uint creature)
        {
            if (GetIsObjectValid(creature) && !GetIsDead(creature))
            {
                Enmity.AttackHighestEnmityTarget(creature);
            }
        }
    }
}
