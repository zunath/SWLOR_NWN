using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipModuleFeat
    {
        public Feat Feat { get; set; }
        public int NameTlkId { get; set; }
        public int DescriptionTlkId { get; set; }

        public ShipModuleFeat(Feat feat, int nameTlkId, int descriptionTlkId)
        {
            Feat = feat;
            NameTlkId = nameTlkId;
            DescriptionTlkId = descriptionTlkId;
        }

        /// <summary>
        /// Retrieves the name and description of all feats used for ship modules.
        /// </summary>
        /// <returns>A list of ship module feats.</returns>
        public static List<ShipModuleFeat> GetAll()
        {
            return new List<ShipModuleFeat>
            {
                {new ShipModuleFeat(Feat.ShipModule1, 16859670, 16859700) },
                {new ShipModuleFeat(Feat.ShipModule2, 16859671, 16859701) },
                {new ShipModuleFeat(Feat.ShipModule3, 16859672, 16859702) },
                {new ShipModuleFeat(Feat.ShipModule4, 16859673, 16859703) },
                {new ShipModuleFeat(Feat.ShipModule5, 16859674, 16859704) },
                {new ShipModuleFeat(Feat.ShipModule6, 16859675, 16859705) },
                {new ShipModuleFeat(Feat.ShipModule7, 16859676, 16859706) },
                {new ShipModuleFeat(Feat.ShipModule8, 16859677, 16859707) },
                {new ShipModuleFeat(Feat.ShipModule9, 16859678, 16859708) },
                {new ShipModuleFeat(Feat.ShipModule10, 16859679, 16859709) },
                {new ShipModuleFeat(Feat.ShipModule11, 16859680, 16859710) },
                {new ShipModuleFeat(Feat.ShipModule12, 16859681, 16859711) },
                {new ShipModuleFeat(Feat.ShipModule13, 16859682, 16859712) },
                {new ShipModuleFeat(Feat.ShipModule14, 16859683, 16859713) },
                {new ShipModuleFeat(Feat.ShipModule15, 16859684, 16859714) },
                {new ShipModuleFeat(Feat.ShipModule16, 16859685, 16859715) },
                {new ShipModuleFeat(Feat.ShipModule17, 16859686, 16859716) },
                {new ShipModuleFeat(Feat.ShipModule18, 16859687, 16859717) },
                {new ShipModuleFeat(Feat.ShipModule19, 16859688, 16859718) },
                {new ShipModuleFeat(Feat.ShipModule20, 16859689, 16859719) },
                {new ShipModuleFeat(Feat.ShipModule21, 16859690, 16859720) },
                {new ShipModuleFeat(Feat.ShipModule22, 16859691, 16859721) },
                {new ShipModuleFeat(Feat.ShipModule23, 16859692, 16859722) },
                {new ShipModuleFeat(Feat.ShipModule24, 16859693, 16859723) },
                {new ShipModuleFeat(Feat.ShipModule25, 16859694, 16859724) },
                {new ShipModuleFeat(Feat.ShipModule26, 16859695, 16859725) },
                {new ShipModuleFeat(Feat.ShipModule27, 16859696, 16859726) },
                {new ShipModuleFeat(Feat.ShipModule28, 16859697, 16859727) },
                {new ShipModuleFeat(Feat.ShipModule29, 16859698, 16859728) },
                {new ShipModuleFeat(Feat.ShipModule30, 16859699, 16859729) },
            };
        }

    }
}
