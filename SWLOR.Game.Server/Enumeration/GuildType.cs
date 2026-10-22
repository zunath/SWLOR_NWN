namespace SWLOR.Game.Server.Enumeration
{
    public enum GuildType
    {
        [Guild("Unknown", false)]
        Invalid = 0,
        [Guild("Hunter's Guild", true)]
        HuntersGuild = 1,
        [Guild("Engineering Guild", true, TasksRefreshAfterCompletion = true)]
        EngineeringGuild = 2,
        [Guild("Smithery Guild", true, TasksRefreshAfterCompletion = true)]
        SmitheryGuild = 3,
        [Guild("Fabrication Guild", true, TasksRefreshAfterCompletion = true)]
        FabricationGuild = 4,
        [Guild("Agriculture Guild", true, TasksRefreshAfterCompletion = true)]
        AgricultureGuild = 5,
    }

    public class GuildAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// When true, a task the player completes goes back on the guild's task board at once, so the player can take it
        /// again. When false, a completed task stays off the board until the next server start.
        /// </summary>
        public bool TasksRefreshAfterCompletion { get; set; }

        public GuildAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
