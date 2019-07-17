using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[KeyItemCategory]")]
    public class KeyItemCategory: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public IEntity Clone()
        {
            return new KeyItemCategory
            {
                ID = ID,
                Name = Name,
                IsActive = IsActive
            };
        }
    }
}
