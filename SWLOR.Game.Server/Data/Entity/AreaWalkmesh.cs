using Dapper.Contrib.Extensions;

namespace SWLOR.Game.Server.Data
{
    using System;
    using System.Collections.Generic;
    
    using SWLOR.Game.Server.Data.Contracts;
    
    public partial class AreaWalkmesh: IEntity
    {
        [Key] 
        public int AreaWalkmeshID { get; set; }
        public string AreaID { get; set; }
        public Nullable<double> LocationX { get; set; }
        public Nullable<double> LocationY { get; set; }
        public double LocationZ { get; set; }
    
        public virtual Area Area { get; set; }
    }
}
