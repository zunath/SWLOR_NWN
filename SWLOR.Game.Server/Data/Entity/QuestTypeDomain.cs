
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestTypeDomain]")]
    public class QuestTypeDomain: IEntity
    {
        [Key]
        public int QuestTypeID { get; set; }
        public string Name { get; set; }
    }
}
