namespace SWLOR.Game.Server.Entity
{
    public class PlayerMigration: EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }
        public int SkillRanks { get; set; }
        public int StatDistributionPoints { get; set; }

    }
}
