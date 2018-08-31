namespace SWLOR.Game.Server.ValueObject.Skill
{
    public class PlayerSkillPointTracker
    {
        public int SkillID { get; set; }
        public int Points { get; set; }
        public int RegisteredLevel { get; set; }

        public PlayerSkillPointTracker(int skillID)
        {
            SkillID = skillID;
            Points = 0;
            RegisteredLevel = -1;
        }

    }
}
