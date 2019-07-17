using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[AreaWalkmesh]")]
    public class AreaWalkmesh: IEntity
    {
        public AreaWalkmesh()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey] 
        public Guid ID { get; set; }
        public Guid AreaID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }

        public IEntity Clone()
        {
            return new AreaWalkmesh
            {
                ID = ID,
                AreaID = AreaID,
                LocationX = LocationX,
                LocationY = LocationY,
                LocationZ = LocationZ
            };
        }
    }
}
