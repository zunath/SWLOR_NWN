using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkFeat]")]
    public class PerkFeat: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int PerkID { get; set; }
        public int FeatID { get; set; }
        public int PerkLevelUnlocked { get; set; }
        public int BaseFPCost { get; set; }
        public int ConcentrationFPCost { get; set; }
        public int ConcentrationTickInterval { get; set; }

        public IEntity Clone()
        {
            return new PerkFeat
            {
                ID = ID,
                PerkID = PerkID,
                FeatID = FeatID,
                PerkLevelUnlocked = PerkLevelUnlocked,
                BaseFPCost = BaseFPCost,
                ConcentrationFPCost = ConcentrationFPCost,
                ConcentrationTickInterval = ConcentrationTickInterval
            };
        }
    }
}
