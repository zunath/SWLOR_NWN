
using Dapper.Contrib.Extensions;

namespace SWLOR.Game.Server.Data
{
    using System;
    using System.Collections.Generic;
    
    using SWLOR.Game.Server.Data.Contracts;
    
    public partial class Background: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Background()
        {
            this.Name = "";
            this.Description = "";
            this.Bonuses = "";
        }

        [ExplicitKey]
        public int BackgroundID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Bonuses { get; set; }
        public bool IsActive { get; set; }
    }
}
