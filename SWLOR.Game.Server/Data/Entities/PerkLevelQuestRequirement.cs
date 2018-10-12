namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PerkLevelQuestRequirement
    {
        public int PerkLevelQuestRequirementID { get; set; }

        public int PerkLevelID { get; set; }

        public int RequiredQuestID { get; set; }

        public virtual PerkLevel PerkLevel { get; set; }

        public virtual Quest Quest { get; set; }
    }
}
