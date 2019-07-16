using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CraftBlueprintCategory]")]
    public class CraftBlueprintCategory: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public CraftBlueprintCategory Clone()
        {
            return new CraftBlueprintCategory
            {
                ID = ID,
                Name = Name,
                IsActive = IsActive
            };
        }
    }
}
