using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class KeyItem: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int KeyItemCategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public IEntity Clone()
        {
            return new KeyItem
            {
                ID = ID,
                KeyItemCategoryID = KeyItemCategoryID,
                Name = Name,
                Description = Description
            };
        }
    }
}
