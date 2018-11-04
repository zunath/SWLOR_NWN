
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkCategories]")]
    public class PerkCategory: IEntity
    {
        public PerkCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int PerkCategoryID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }
    }
}
