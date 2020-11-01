using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("BaseStructureType")]
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

        public IEntity Clone()
        {
            return new BaseStructureType
            {
                ID = ID,
                Name = Name,
                IsActive = IsActive,
                CanPlaceInside = CanPlaceInside,
                CanPlaceOutside = CanPlaceOutside
            };
        }
    }
}
