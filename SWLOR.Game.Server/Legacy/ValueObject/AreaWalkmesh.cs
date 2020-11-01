using System;

namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class AreaWalkmesh
    {
        public AreaWalkmesh()
        {
            ID = Guid.NewGuid();
        }
        public Guid ID { get; set; }
        public Guid AreaID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
    }
}
