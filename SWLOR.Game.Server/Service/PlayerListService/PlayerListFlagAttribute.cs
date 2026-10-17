namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// Player-facing label, hover text, and icon for a player list flag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PlayerListFlagAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string IconResref { get; }

        public PlayerListFlagAttribute(string name, string description, string iconResref)
        {
            Name = name;
            Description = description;
            IconResref = iconResref;
        }
    }
}
