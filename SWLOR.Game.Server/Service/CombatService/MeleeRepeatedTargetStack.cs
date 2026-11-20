using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Stack built by consecutive landed auto-attacks on one target (Vibroblade Rundown).
    /// Switching targets restarts it at one stack.
    /// </summary>
    public sealed class MeleeRepeatedTargetStack
    {
        public uint Target { get; private set; }
        public int Stacks { get; private set; }

        public MeleeRepeatedTargetStack(uint target)
        {
            Target = target;
        }

        /// <summary>
        /// Records a landed auto-attack on <paramref name="target"/>.
        /// Returns true when the hit raised the stack, false when it was already at the cap.
        /// </summary>
        public bool RegisterHit(uint target, int maxStacks)
        {
            if (target != Target)
            {
                Target = target;
                Stacks = 0;
            }

            var previousStacks = Stacks;
            Stacks = Math.Min(Stacks + 1, Math.Max(1, maxStacks));
            return Stacks > previousStacks;
        }

        /// <summary>
        /// Overhead text for a hit that raised the stack. The stats are generic, so the perk
        /// name comes from the configured status icon.
        /// </summary>
        public static string GetStackText(EffectIconType icon, int stacks, int maxStacks, int damageBonus)
        {
            var name = icon == EffectIconType.RundownStatusEffect
                ? "Rundown"
                : "Repeated Target";
            return $"{name} {stacks}/{maxStacks} (+{damageBonus} DMG)";
        }
    }
}
