

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCOutfit]")]
    public class PCOutfit: IEntity
    {
        [ExplicitKey]
        public Guid PlayerID { get; set; }
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

        public IEntity Clone()
        {
            return new PCOutfit
            {
                PlayerID = PlayerID,
                Outfit1 = Outfit1,
                Outfit2 = Outfit2,
                Outfit3 = Outfit3,
                Outfit4 = Outfit4,
                Outfit5 = Outfit5,
                Outfit6 = Outfit6,
                Outfit7 = Outfit7,
                Outfit8 = Outfit8,
                Outfit9 = Outfit9,
                Outfit10 = Outfit10
            };
        }
    }
}
