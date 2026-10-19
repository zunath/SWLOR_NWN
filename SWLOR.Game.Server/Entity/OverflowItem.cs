namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// An item a character upgrade could not keep in the character's inventory. Players claim these into
    /// their inventory with the /overflow chat command.
    /// </summary>
    public class OverflowItem : EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }
        public string Name { get; set; }
        public string Resref { get; set; }
        public string Tag { get; set; }
        public int Quantity { get; set; }
        /// <summary>The item, serialized as <c>ObjectPlugin.Serialize</c> writes it.</summary>
        public string Data { get; set; }
    }
}
