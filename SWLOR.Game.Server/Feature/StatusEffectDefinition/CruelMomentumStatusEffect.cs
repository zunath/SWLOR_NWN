using System;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Finishing Drive's stacking momentum: each cast adds a stack (up to <see cref="MaxStacks"/>),
    /// granting +<see cref="PotencyPerStack"/>% technique damage (Mimicry potency) per stack.
    /// Reapplied by the ability with the new stack count; magnitude is passed in via constructor.
    /// </summary>
    public sealed class CruelMomentumStatusEffect : StatusEffectBase
    {
        public const int MaxStacks = 3;
        private const int PotencyPerStack = 8;

        public int Stacks { get; }

        public override string Name => $"Cruel Momentum ({Stacks})";
        public override EffectIconType Icon => EffectIconType.CruelMomentumStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        // Parameterless constructor is required by the reflection-driven status-effect registry.
        public CruelMomentumStatusEffect()
            : this(1)
        {
        }

        public CruelMomentumStatusEffect(int stacks)
        {
            Stacks = Math.Clamp(stacks, 1, MaxStacks);
            StatGroup.Stats[StatType.MimicryPotencyPercent] = Stacks * PotencyPerStack;
        }
    }
}
