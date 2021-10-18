using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerOutfit: EntityBase
    {
        public override string KeyPrefix => "PlayerOutfit";

        public List<PlayerOutfitDetail> Outfits { get; set; }

        public PlayerOutfit()
        {
            Outfits = new List<PlayerOutfitDetail>();
        }
    }

    public class PlayerOutfitDetail
    {
        public string Name { get; set; }
        public string Data { get; set; }

        public int NeckId { get; set; }
        public int TorsoId { get; set; }
        public int BeltId { get; set; }
        public int PelvisId { get; set; }
        public int RobeId { get; set; }

        public int LeftBicepId { get; set; }
        public int LeftFootId { get; set; }
        public int LeftForearmId { get; set; }
        public int LeftHandId { get; set; }
        public int LeftShinId { get; set; }
        public int LeftShoulderId { get; set; }
        public int LeftThighId { get; set; }

        public int RightBicepId { get; set; }
        public int RightFootId { get; set; }
        public int RightForearmId { get; set; }
        public int RightHandId { get; set; }
        public int RightShinId { get; set; }
        public int RightShoulderId { get; set; }
        public int RightThighId { get; set; }
    }
}
