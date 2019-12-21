

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCHelmet: IEntity
    {
        [Key]
        public Guid PlayerID { get; set; }
        public string Helmet1 { get; set; }
        public string Helmet2 { get; set; }
        public string Helmet3 { get; set; }
        public string Helmet4 { get; set; }
        public string Helmet5 { get; set; }
        public string Helmet6 { get; set; }
        public string Helmet7 { get; set; }
        public string Helmet8 { get; set; }
        public string Helmet9 { get; set; }
        public string Helmet10 { get; set; }

        public IEntity Clone()
        {
            return new PCHelmet
            {
                PlayerID = PlayerID,
                Helmet1 = Helmet1,
                Helmet2 = Helmet2,
                Helmet3 = Helmet3,
                Helmet4 = Helmet4,
                Helmet5 = Helmet5,
                Helmet6 = Helmet6,
                Helmet7 = Helmet7,
                Helmet8 = Helmet8,
                Helmet9 = Helmet9,
                Helmet10 = Helmet10
            };
        }
    }
}
