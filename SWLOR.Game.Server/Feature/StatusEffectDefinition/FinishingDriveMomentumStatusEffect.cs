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
    ///
    /// Named for its own ability rather than "Cruel Momentum": that name belongs to the unrelated
    /// Force Dark Ravager trait, and sharing one class between the two left the Force trait applying
    /// Mimicry potency instead of the Force accuracy its description promises.
    /// </summary>
    public sealed class FinishingDriveMomentumStatusEffect : StatusEffectBase
    {
        public const int MaxStacks = 3;
        private const int PotencyPerStack = 8;

        public int Stacks { get; }

        public override string Name => $"Finishing Drive Momentum ({Stacks})";
        public override EffectIconType Icon => EffectIconType.FinishingDriveMomentumStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        // Parameterless constructor is required by the reflection-driven status-effect registry.
        public FinishingDriveMomentumStatusEffect()
            : this(1)
        {
        }

        public FinishingDriveMomentumStatusEffect(int stacks)
        {
            Stacks = Math.Clamp(stacks, 1, MaxStacks);
            StatGroup.Stats[StatType.MimicryPotencyPercent] = Stacks * PotencyPerStack;
        }
    }
}
