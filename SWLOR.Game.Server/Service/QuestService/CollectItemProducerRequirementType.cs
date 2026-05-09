namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// Whether collect-item turn-ins must be crafted (or carry <see cref="Item.PlayerProducedItemVariable"/>), or any matching resref is accepted.
    /// </summary>
    public enum CollectItemProducerRequirementType
    {
        /// <summary>
        /// No producer check; any item with the correct resref counts.
        /// </summary>
        None = 0,

        /// <summary>
        /// The item must have been crafted (or otherwise stamped with <see cref="Item.PlayerProducedItemVariable"/>).
        /// </summary>
        PlayerProduced = 1,
    }
}
