
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCOutfits")]
    public class PCOutfit: IEntity
    {
        [ExplicitKey]
        public string PlayerID { get; set; }
        public string Outfit1 { get; set; }
        public string Outfit2 { get; set; }
        public string Outfit3 { get; set; }
        public string Outfit4 { get; set; }
        public string Outfit5 { get; set; }
        public string Outfit6 { get; set; }
        public string Outfit7 { get; set; }
        public string Outfit8 { get; set; }
        public string Outfit9 { get; set; }
        public string Outfit10 { get; set; }
    }
}
