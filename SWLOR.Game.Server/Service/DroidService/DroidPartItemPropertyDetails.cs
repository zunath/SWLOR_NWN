using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidPartItemPropertyDetails
    {
        public DroidPartItemPropertySubType PartType { get; set; }
        public int Tier { get; set; }
        public int Level { get; set; }
        public int HP { get; set; }
        public int STM { get; set; }
        public int AISlots { get; set; }
        public int AGI { get; set; }
        public int MGT { get; set; }
        public int PER { get; set; }
        public int SOC { get; set; }
        public int VIT { get; set; }
        public int WIL { get; set; }
        public int Vibroblade { get; set; }
        public int Vibroknife { get; set; }
        public int Lightsaber { get; set; }
        public int HeavyVibroblade { get; set; }
        public int Spear { get; set; }
        public int TwinBlade { get; set; }
        public int Saberstaff { get; set; }
        public int Katar { get; set; }
        public int Staff { get; set; }
        public int Pistol { get; set; }
        public int Rifle { get; set; }
        public int Throwing { get; set; }
        public Dictionary<ResistanceType, int> Resistances { get; set; }

        public DroidPartItemPropertyDetails()
        {
            PartType = DroidPartItemPropertySubType.Invalid;
            Resistances = new Dictionary<ResistanceType, int>();
        }
    }
}
