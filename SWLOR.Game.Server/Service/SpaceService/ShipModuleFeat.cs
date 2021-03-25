using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipModuleFeat
    {
        public string SlotName { get; set; }
        public int NameTlkId { get; set; }
        public int DescriptionTlkId { get; set; }

        public ShipModuleFeat(string slotName, int nameTlkId, int descriptionTlkId)
        {
            SlotName = slotName;
            NameTlkId = nameTlkId;
            DescriptionTlkId = descriptionTlkId;
        }

        /// <summary>
        /// Retrieves the name and description of all feats used for ship modules.
        /// </summary>
        /// <returns>A list of ship module feats.</returns>
        public static Dictionary<Feat, ShipModuleFeat> GetAll()
        {
            return new Dictionary<Feat, ShipModuleFeat>
            {
                {Feat.ShipModule1, new ShipModuleFeat("Slot #1", 16859670, 16859700) },
                {Feat.ShipModule2, new ShipModuleFeat("Slot #2", 16859671, 16859701) },
                {Feat.ShipModule3, new ShipModuleFeat("Slot #3", 16859672, 16859702) },
                {Feat.ShipModule4, new ShipModuleFeat("Slot #4", 16859673, 16859703) },
                {Feat.ShipModule5, new ShipModuleFeat("Slot #5", 16859674, 16859704) },
                {Feat.ShipModule6, new ShipModuleFeat("Slot #6", 16859675, 16859705) },
                {Feat.ShipModule7, new ShipModuleFeat("Slot #7", 16859676, 16859706) },
                {Feat.ShipModule8, new ShipModuleFeat("Slot #8", 16859677, 16859707) },
                {Feat.ShipModule9, new ShipModuleFeat("Slot #9", 16859678, 16859708) },
                { Feat.ShipModule10, new ShipModuleFeat("Slot #10",16859679, 16859709) },
                { Feat.ShipModule11, new ShipModuleFeat("Slot #11",16859680, 16859710) },
                { Feat.ShipModule12, new ShipModuleFeat("Slot #12",16859681, 16859711) },
                { Feat.ShipModule13, new ShipModuleFeat("Slot #13",16859682, 16859712) },
                { Feat.ShipModule14, new ShipModuleFeat("Slot #14",16859683, 16859713) },
                { Feat.ShipModule15, new ShipModuleFeat("Slot #15",16859684, 16859714) },
                { Feat.ShipModule16, new ShipModuleFeat("Slot #16",16859685, 16859715) },
                { Feat.ShipModule17, new ShipModuleFeat("Slot #17",16859686, 16859716) },
                { Feat.ShipModule18, new ShipModuleFeat("Slot #18",16859687, 16859717) },
                { Feat.ShipModule19, new ShipModuleFeat("Slot #19",16859688, 16859718) },
                { Feat.ShipModule20, new ShipModuleFeat("Slot #20",16859689, 16859719) },
                { Feat.ShipModule21, new ShipModuleFeat("Slot #21",16859690, 16859720) },
                { Feat.ShipModule22, new ShipModuleFeat("Slot #22",16859691, 16859721) },
                { Feat.ShipModule23, new ShipModuleFeat("Slot #23",16859692, 16859722) },
                { Feat.ShipModule24, new ShipModuleFeat("Slot #24",16859693, 16859723) },
                { Feat.ShipModule25, new ShipModuleFeat("Slot #25",16859694, 16859724) },
                { Feat.ShipModule26, new ShipModuleFeat("Slot #26",16859695, 16859725) },
                { Feat.ShipModule27, new ShipModuleFeat("Slot #27",16859696, 16859726) },
                { Feat.ShipModule28, new ShipModuleFeat("Slot #28",16859697, 16859727) },
                { Feat.ShipModule29, new ShipModuleFeat("Slot #29",16859698, 16859728) },
                { Feat.ShipModule30, new ShipModuleFeat("Slot #30",16859699, 16859729) },
            };
        }

    }
}
