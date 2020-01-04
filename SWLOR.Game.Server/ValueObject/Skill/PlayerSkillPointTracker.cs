namespace SWLOR.Game.Server.ValueObject.Skill
{
    public class PlayerSkillPointTracker
    {
        public Enumeration.Skill SkillID { get; set; }
        public int Points { get; set; }
        public int RegisteredLevel { get; set; }

        public PlayerSkillPointTracker(Enumeration.Skill skillID)
        {
            SkillID = skillID;
            Points = 0;
            RegisteredLevel = -1;
        }

    }
}
