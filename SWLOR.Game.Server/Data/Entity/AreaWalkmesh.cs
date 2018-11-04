using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[AreaWalkmesh]")]
    public class AreaWalkmesh: IEntity
    {
        [Key] 
        public int AreaWalkmeshID { get; set; }
        public string AreaID { get; set; }
        public double? LocationX { get; set; }
        public double? LocationY { get; set; }
        public double LocationZ { get; set; }
    }
}
