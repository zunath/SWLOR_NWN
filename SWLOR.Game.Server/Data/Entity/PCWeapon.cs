

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCWeapon: IEntity
    {
        [Key]
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
    }
}
