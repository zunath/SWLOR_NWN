using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DMActionType]")]
    public class DMActionType: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new DMActionType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
