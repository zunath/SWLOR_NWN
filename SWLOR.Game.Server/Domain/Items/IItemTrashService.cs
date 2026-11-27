namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// The audit trail of items players destroy. The static facade <c>Service.ItemTrash</c> forwards to the container's
    /// instance of this interface.
    /// </summary>
    public interface IItemTrashService
    {
        /// <summary>
        /// Writes the audit record of an item about to be destroyed, and of every item inside it. A possessing DM's
        /// avatar is recorded as the DM. A failed audit is logged and never stops the destruction.
        /// </summary>
        /// <param name="player">The player (or DM) who disposed of the item.</param>
        /// <param name="item">The item about to be destroyed.</param>
        /// <param name="source">Where the disposal came from, such as <c>trash-added</c>.</param>
        void Audit(uint player, uint item, string source);
    }
}
