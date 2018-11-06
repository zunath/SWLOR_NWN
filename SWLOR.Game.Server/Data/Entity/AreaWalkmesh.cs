using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[AreaWalkmesh]")]
    public class AreaWalkmesh: IEntity
    {
        [ExplicitKey] 
        public Guid ID { get; set; }
        public Guid AreaID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
    }
}
