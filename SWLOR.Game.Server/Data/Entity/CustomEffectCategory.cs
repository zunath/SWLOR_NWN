using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CustomEffectCategory]")]
    public class CustomEffectCategory: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new CustomEffectCategory
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
