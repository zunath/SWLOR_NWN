﻿using SWLOR.Game.Server.Data.Contracts;

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
    }
}
