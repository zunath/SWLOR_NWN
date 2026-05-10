using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceStunStatusEffect : StatusEffectBase
    {
        private bool _isDazed;

        public override string Name => "Force Stun";
        public override EffectIconType Icon => _isDazed
            ? EffectIconType.Dazed
            : EffectIconType.Invalid;
        public override StatusEffectCategory Categories => _isDazed
            ? StatusEffectCategory.Debuff | StatusEffectCategory.Control
            : StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        public override string CanApply(uint creature)
        {
            return GetIsImmune(creature, ImmunityType.Dazed)
                ? "Target is immune to daze."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            var duration = GetDurationSeconds(durationTicks);
            var dc = Combat.CalculateSavingThrowDC(Source, SavingThrow.Will, 12);
            var checkResult = WillSave(creature, dc, SavingThrowType.None, Source);

            if (checkResult == SavingThrowResultType.Failed)
            {
                _isDazed = true;
                ApplyDaze(creature, duration);
            }
            else if (checkResult == SavingThrowResultType.Success)
            {
                StatGroup.Stats[StatType.Accuracy] = -2;
                StatGroup.Stats[StatType.Defense] = -2;
            }
            else
            {
                IsFlaggedForRemoval = true;
                return;
            }

            CombatPoint.AddCombatPoint(Source, creature, SkillType.Force, 3);
            Enmity.ModifyEnmity(Source, creature, 850);
        }

        protected override void Reapply(uint creature)
        {
            if (_isDazed)
            {
                ApplyDaze(creature, GetDurationSeconds(DurationTicks));
            }
        }

        private void ApplyDaze(uint creature, float duration)
        {
            var effect = EffectDazed();
            effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue));
            effect = TagEffect(effect, Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);

            Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.Dazed);
        }
    }
}
