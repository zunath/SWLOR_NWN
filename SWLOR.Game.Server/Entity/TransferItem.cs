namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One kind of item a <see cref="TransferEntry"/> moves: a stackable blueprint and a count, or a single item saved exactly as the
    /// engine wrote it (<see cref="Data"/>), such as a crate that carries an animal's timers or an enzyme with its rolled stats.
    /// </summary>
    public class TransferItem
    {
        public TransferItem()
        {
            Resref = string.Empty;
            Name = string.Empty;
            Data = string.Empty;
        }

        /// <summary>The item's blueprint.</summary>
        public string Resref { get; set; }

        /// <summary>The item's name, for the character's messages.</summary>
        public string Name { get; set; }

        /// <summary>How many. Always 1 for an item with <see cref="Data"/>.</summary>
        public int Count { get; set; }

        /// <summary>The single item as saved by <c>ObjectPlugin.Serialize</c>, or empty for plain stackable items made from <see cref="Resref"/>.</summary>
        public string Data { get; set; }
    }
}
