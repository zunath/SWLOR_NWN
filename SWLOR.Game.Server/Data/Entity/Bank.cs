using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("Banks")]
    public class Bank: IEntity
    {
        public Bank()
        {
            BankItems = new HashSet<BankItem>();
        }

        [ExplicitKey]
        public int BankID { get; set; }
        public string AreaName { get; set; }
        public string AreaTag { get; set; }
        public string AreaResref { get; set; }
        public virtual ICollection<BankItem> BankItems { get; set; }
    }
}
