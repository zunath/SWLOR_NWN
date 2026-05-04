namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// Crafting provenance rules for collect-item quest objectives.
    /// </summary>
    public enum CollectItemCraftRequirementType
    {
        /// <summary>
        /// No craft check; any item with the correct resref counts.
        /// </summary>
        None = 0,

        /// <summary>
        /// Item must be player-crafted and the crafter UUID must match the PC turning it in.
        /// </summary>
        CraftedByTurnInPlayer = 1,

        /// <summary>
        /// Item must be player-crafted (crafter UUID present); any crafter is accepted.
        /// </summary>
        CraftedByAnyPlayer = 2,
    }
}
