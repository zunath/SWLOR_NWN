

using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCWeapon")]
    public class PCWeapon: IEntity
    {
        [ExplicitKey]
        public Guid PlayerID { get; set; }
        public string Weapon1 { get; set; }
        public string Weapon2 { get; set; }
        public string Weapon3 { get; set; }
        public string Weapon4 { get; set; }
        public string Weapon5 { get; set; }
        public string Weapon6 { get; set; }
        public string Weapon7 { get; set; }
        public string Weapon8 { get; set; }
        public string Weapon9 { get; set; }
        public string Weapon10 { get; set; }

        public IEntity Clone()
        {
            return new PCWeapon
            {
                PlayerID = PlayerID,
                Weapon1 = Weapon1,
                Weapon2 = Weapon2,
                Weapon3 = Weapon3,
                Weapon4 = Weapon4,
                Weapon5 = Weapon5,
                Weapon6 = Weapon6,
                Weapon7 = Weapon7,
                Weapon8 = Weapon8,
                Weapon9 = Weapon9,
                Weapon10 = Weapon10
            };
        }
    }
}
