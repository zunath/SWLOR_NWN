namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public enum StatusEffectSourceType
    {
        Invalid = 0,
        Normal = 1,
        WeaponSkill = 2,
        Stance = 3,

        /// <summary>
        /// A party-buff command effect (e.g. Leadership's Press the Attack, Cleanse Order,
        /// Decisive Command). Command effects are mutually exclusive per source leader: applying
        /// one removes any other Command-classified effect the same leader previously applied to
        /// the same target. See <see cref="SWLOR.Game.Server.Service.StatusEffect.RemoveOtherCommandStatuses"/>.
        /// </summary>
        Command = 4,

        /// <summary>
        /// A crafting style (Engineering's Overclock and Safe Mode, and the Smithery, Agriculture and Gathering styles that follow
        /// the same pattern): a choice the player makes in a craft window that carries stat adjustments the minigame engine reads.
        /// Every bulk removal (cleanse, purge, remove by category or stat, remove all, the clear on death) skips these effects
        /// (<see cref="StatusEffectSelectors"/>); only the craft window, a perk refund, logout, a full rebuild and entering space
        /// remove one. The engine itself strips every native effect when a creature dies, so a style is picked again after a respawn.
        /// Exclusivity is per skill and belongs to the skill's own style helper, such as
        /// <c>CircuitStyles</c>, because a crafter may hold one style of each skill at the same time.
        /// </summary>
        CraftingStyle = 5,
    }
}
