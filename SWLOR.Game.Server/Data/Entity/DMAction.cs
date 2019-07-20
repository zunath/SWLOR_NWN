using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DMAction]")]
    public class DMAction: IEntity
    {
        public DMAction()
        {
            ID = Guid.NewGuid();
            DateOfAction = DateTime.UtcNow;
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public int DMActionTypeID { get; set; }
        public string Name { get; set; }
        public string CDKey { get; set; }
        public DateTime DateOfAction { get; set; }
        public string Details { get; set; }

        public IEntity Clone()
        {
            return new DMAction
            {
                ID = ID,
                DMActionTypeID = DMActionTypeID,
                Name = Name,
                CDKey = CDKey,
                DateOfAction = DateOfAction,
                Details = Details
            };
        }
    }
}
