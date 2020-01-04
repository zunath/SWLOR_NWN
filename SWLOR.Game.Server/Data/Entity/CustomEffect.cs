using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class CustomEffect: IEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public int IconID { get; set; }
        public CustomEffectCategoryType CustomEffectCategoryID { get; set; }
    }
}
