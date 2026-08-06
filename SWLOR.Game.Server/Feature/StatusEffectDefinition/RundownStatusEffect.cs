using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Anchors the Rundown icon identity for the gameplay-icon pipeline. Combat no longer applies
    /// this class directly - the shared MeleeRepeatedTargetDamageStatusEffect carries the stacks
    /// and shows this icon through StatType.MeleeRepeatedTargetDamageStatusEffectIcon - but the
    /// manifest, EffectIconType.RundownStatusEffect, effecticons.2da, and the ief_rndwn artwork
    /// all key off this definition, so it stays as their source of truth.
    /// </summary>
    public sealed class RundownStatusEffect : StatusEffectBase
    {
        // No configured decay applies to Rundown, so this keeps the icon alive across normal attack
        // cadence but clears it once the attacker has stopped landing qualifying hits.
        private const float VisualDurationSeconds = 10f;

        public int Stacks { get; }

        public override string Name => "Rundown";
        public override EffectIconType Icon => EffectIconType.RundownStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public RundownStatusEffect()
            : this(1)
        {
        }

        public RundownStatusEffect(int stacks)
        {
            Stacks = stacks;
        }

        public override IStatusEffect Clone()
        {
            return new RundownStatusEffect(Stacks);
        }

        public static void Refresh(uint attacker, int stacks)
        {
            if (!GetIsObjectValid(attacker))
                return;

            StatusEffect.RemoveStatusEffect(attacker, typeof(RundownStatusEffect), false);

            if (stacks <= 0)
                return;

            StatusEffect.ApplyStatusEffect(attacker, attacker, new RundownStatusEffect(stacks), VisualDurationSeconds);
        }
    }
}
