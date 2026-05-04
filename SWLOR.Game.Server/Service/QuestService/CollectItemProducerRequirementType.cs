namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// Rules for whether a collect-item objective must be produced by a player (crafting, fishing, etc.).
    /// </summary>
    public enum CollectItemProducerRequirementType
    {
        /// <summary>
        /// No producer check; any item with the correct resref counts.
        /// </summary>
        None = 0,

        /// <summary>
        /// Item must carry a player producer UUID and it must match the PC turning the item in.
        /// </summary>
        ProducedByTurnInPlayer = 1,

        /// <summary>
        /// Item must carry a player producer UUID (any player); vendor or unassigned items fail.
        /// </summary>
        ProducedByAnyPlayer = 2,
    }
}
