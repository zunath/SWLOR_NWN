using System;

namespace SWLOR.Game.Server.ValueObject
{
    public class AreaWalkmesh
    {
        public AreaWalkmesh()
        {
            ID = Guid.NewGuid();
        }
        public Guid ID { get; set; }
        public string AreaResref { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
    }
}
