using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Enumeration
{
    public enum GuildType
    {
        [Guild("Unknown", false)]
        Invalid = 0,
        [Guild("Hunter's Guild", true)]
        HuntersGuild = 1,
        [Guild("Engineering Guild", true, TasksRefreshAfterCompletion = true, TaskXpSkill = SkillType.Engineering)]
        EngineeringGuild = 2,
        [Guild("Smithery Guild", true, TasksRefreshAfterCompletion = true, TaskXpSkill = SkillType.Smithery)]
        SmitheryGuild = 3,
        [Guild("Fabrication Guild", true, TasksRefreshAfterCompletion = true, TaskXpSkill = SkillType.Fabrication)]
        FabricationGuild = 4,
        [Guild("Agriculture Guild", true, TasksRefreshAfterCompletion = true, TaskXpSkill = SkillType.Agriculture)]
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

        /// <summary>
        /// The skill a hand-in of one of this guild's tasks gives XP in (<see cref="SWLOR.Game.Server.Service.GuildTaskXp"/>), or
        /// <see cref="SkillType.Invalid"/> when its tasks give none, as the Hunter's Guild's do not.
        /// </summary>
        public SkillType TaskXpSkill { get; set; } = SkillType.Invalid;

        public GuildAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
