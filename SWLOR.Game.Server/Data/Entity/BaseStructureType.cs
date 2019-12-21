using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class BaseStructureType: IEntity
    {

        public BaseStructureType()
        {
            Name = "";
        }

        [Key]
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
