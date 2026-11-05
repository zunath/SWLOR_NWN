using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Items a craft moved from the player record into the inventory (a refund, or the finished item) that the saved character file
    /// may not hold yet. Kept until the file is known to hold them; a login whose file predates <see cref="Sequence"/> gives them
    /// again. See <c>CraftSessionLifecycle</c>.
    /// </summary>
    public class PendingCraftReturn
    {
        /// <summary>The craft file sequence the transfer was made at. A character file whose sequence is at least this holds the items.</summary>
        public int Sequence { get; set; }

        /// <summary>The serialized items the transfer put in the inventory.</summary>
        public List<string> Items { get; set; } = new();
    }
}
