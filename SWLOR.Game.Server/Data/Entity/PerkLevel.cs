
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkLevels]")]
    public class PerkLevel: IEntity
    {
        public PerkLevel()
        {
            Description = "";
        }

        [Key]
        public int PerkLevelID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
    }
}
