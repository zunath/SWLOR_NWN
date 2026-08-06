using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class MeleeRepeatedTargetDamageStatusEffect : StatusEffectBase
    {
        private const float VisualDurationSeconds = 10f;

        public int Stacks { get; }

        public override string Name => "Melee Repeated Target Damage";
        public override EffectIconType Icon { get; }
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public MeleeRepeatedTargetDamageStatusEffect()
            : this(1, EffectIconType.Invalid)
        {
        }

        public MeleeRepeatedTargetDamageStatusEffect(int stacks, EffectIconType icon)
        {
            Stacks = stacks;
            Icon = icon;
        }

        public override IStatusEffect Clone()
        {
            return new MeleeRepeatedTargetDamageStatusEffect(Stacks, Icon);
        }

        public static void Refresh(uint attacker, int stacks, EffectIconType icon)
        {
            if (!GetIsObjectValid(attacker))
                return;

            StatusEffect.RemoveStatusEffect(
                attacker,
                typeof(MeleeRepeatedTargetDamageStatusEffect),
                false);

            if (stacks <= 0 || icon == EffectIconType.Invalid)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new MeleeRepeatedTargetDamageStatusEffect(stacks, icon),
                VisualDurationSeconds);
        }
    }
}
