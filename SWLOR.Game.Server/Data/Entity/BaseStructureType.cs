using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BaseStructureType]")]
    public class BaseStructureType: IEntity
    {

        public BaseStructureType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool CanPlaceInside { get; set; }
        public bool CanPlaceOutside { get; set; }
    }
}
