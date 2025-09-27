using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipModuleFeat
    {
        public string SlotName { get; set; }
        public int NameTlkId { get; set; }
        public int DescriptionTlkId { get; set; }
        public string TextureName { get; set; }

        public ShipModuleFeat(string slotName, int nameTlkId, int descriptionTlkId, string textureName)
        {
            SlotName = slotName;
            NameTlkId = nameTlkId;
            DescriptionTlkId = descriptionTlkId;
            TextureName = textureName;
        }

        /// <summary>
        /// Retrieves the name and description of all feats used for ship modules.
        /// </summary>
        /// <returns>A list of ship module feats.</returns>
        public static Dictionary<FeatType, ShipModuleFeat> GetAll()
        {
            return new Dictionary<FeatType, ShipModuleFeat>
            {
                {FeatType.ShipModule1, new ShipModuleFeat("Slot #1", 16859670, 16859700, "ife_sm1") },
                {FeatType.ShipModule2, new ShipModuleFeat("Slot #2", 16859671, 16859701, "ife_sm2") },
                {FeatType.ShipModule3, new ShipModuleFeat("Slot #3", 16859672, 16859702, "ife_sm3") },
                {FeatType.ShipModule4, new ShipModuleFeat("Slot #4", 16859673, 16859703, "ife_sm4") },
                {FeatType.ShipModule5, new ShipModuleFeat("Slot #5", 16859674, 16859704, "ife_sm5") },
                {FeatType.ShipModule6, new ShipModuleFeat("Slot #6", 16859675, 16859705, "ife_sm6") },
                {FeatType.ShipModule7, new ShipModuleFeat("Slot #7", 16859676, 16859706, "ife_sm7") },
                {FeatType.ShipModule8, new ShipModuleFeat("Slot #8", 16859677, 16859707, "ife_sm8") },
                {FeatType.ShipModule9, new ShipModuleFeat("Slot #9", 16859678, 16859708, "ife_sm9") },
                { FeatType.ShipModule10, new ShipModuleFeat("Slot #10",16859679, 16859709, "ife_sm10") },
                { FeatType.ShipModule11, new ShipModuleFeat("Slot #11",16859680, 16859710, "ife_sm11") },
                { FeatType.ShipModule12, new ShipModuleFeat("Slot #12",16859681, 16859711, "ife_sm12") },
                { FeatType.ShipModule13, new ShipModuleFeat("Slot #13",16859682, 16859712, "ife_sm13") },
                { FeatType.ShipModule14, new ShipModuleFeat("Slot #14",16859683, 16859713, "ife_sm14") },
                { FeatType.ShipModule15, new ShipModuleFeat("Slot #15",16859684, 16859714, "ife_sm15") },
                { FeatType.ShipModule16, new ShipModuleFeat("Slot #16",16859685, 16859715, "ife_sm16") },
                { FeatType.ShipModule17, new ShipModuleFeat("Slot #17",16859686, 16859716, "ife_sm17") },
                { FeatType.ShipModule18, new ShipModuleFeat("Slot #18",16859687, 16859717, "ife_sm18") },
                { FeatType.ShipModule19, new ShipModuleFeat("Slot #19",16859688, 16859718, "ife_sm19") },
                { FeatType.ShipModule20, new ShipModuleFeat("Slot #20",16859689, 16859719, "ife_sm20") },
                { FeatType.ShipModule21, new ShipModuleFeat("Slot #21",16859690, 16859720, "ife_sm21") },
                { FeatType.ShipModule22, new ShipModuleFeat("Slot #22",16859691, 16859721, "ife_sm22") },
                { FeatType.ShipModule23, new ShipModuleFeat("Slot #23",16859692, 16859722, "ife_sm23") },
                { FeatType.ShipModule24, new ShipModuleFeat("Slot #24",16859693, 16859723, "ife_sm24") },
                { FeatType.ShipModule25, new ShipModuleFeat("Slot #25",16859694, 16859724, "ife_sm25") },
                { FeatType.ShipModule26, new ShipModuleFeat("Slot #26",16859695, 16859725, "ife_sm26") },
                { FeatType.ShipModule27, new ShipModuleFeat("Slot #27",16859696, 16859726, "ife_sm27") },
                { FeatType.ShipModule28, new ShipModuleFeat("Slot #28",16859697, 16859727, "ife_sm28") },
                { FeatType.ShipModule29, new ShipModuleFeat("Slot #29",16859698, 16859728, "ife_sm29") },
                { FeatType.ShipModule30, new ShipModuleFeat("Slot #30",16859699, 16859729, "ife_sm30") },
            };
        }

    }
}
