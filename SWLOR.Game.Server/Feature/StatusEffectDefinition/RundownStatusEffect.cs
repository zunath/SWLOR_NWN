using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
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
