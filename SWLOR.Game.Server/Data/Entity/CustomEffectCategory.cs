
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CustomEffectCategory]")]
    public class CustomEffectCategory: IEntity
    {
        [ExplicitKey]
        public int CustomEffectCategoryID { get; set; }
        public string Name { get; set; }
    }
}
