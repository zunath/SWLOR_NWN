using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class SustainedFireStatusEffect : StatusEffectBase
    {
        public override string Name => "Sustained Fire";
        public override EffectIconType Icon => EffectIconType.SpottersRhythmStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public int Stacks { get; }
        public int MaximumStacks { get; }
        public int DamageBonus { get; }

        public SustainedFireStatusEffect() : this(0, 0, 0) { }
        public SustainedFireStatusEffect(int stacks, int maximumStacks, int damageBonus)
        {
            Stacks = stacks;
            MaximumStacks = maximumStacks;
            DamageBonus = damageBonus;
        }
        public override IStatusEffect Clone() => new SustainedFireStatusEffect(Stacks, MaximumStacks, DamageBonus);
    }
}
