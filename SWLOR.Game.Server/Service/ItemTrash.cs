using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the item trash audit. The logic lives in <see cref="IItemTrashService"/>; every member here
    /// forwards to the container's instance.
    /// </summary>
    public static class ItemTrash
    {
        private static IItemTrashService Instance => FacadeAccessor.Resolve<IItemTrashService>();

        /// <summary>
        /// Writes the audit record of an item about to be destroyed, and of every item inside it. A possessing DM's
        /// avatar is recorded as the DM. A failed audit is logged and never stops the destruction.
        /// </summary>
        /// <param name="player">The player (or DM) who disposed of the item.</param>
        /// <param name="item">The item about to be destroyed.</param>
        /// <param name="source">Where the disposal came from, such as <c>trash-added</c>.</param>
        public static void Audit(uint player, uint item, string source)
        {
            Instance.Audit(player, item, source);
        }
    }
}
