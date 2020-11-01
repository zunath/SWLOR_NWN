using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("PCObjectVisibility")]
    public class PCObjectVisibility: IEntity
    {
        public PCObjectVisibility()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string VisibilityObjectID { get; set; }
        public bool IsVisible { get; set; }

        public IEntity Clone()
        {
            return new PCObjectVisibility
            {
                ID = ID,
                PlayerID = PlayerID,
                VisibilityObjectID = VisibilityObjectID,
                IsVisible = IsVisible
            };
        }
    }
}
