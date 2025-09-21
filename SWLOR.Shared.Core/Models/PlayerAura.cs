namespace SWLOR.Shared.Core.Models
{
    public class PlayerAura
    {
        public List<PlayerAuraDetail> Auras { get; set; }
        public List<uint> PartyMembersInRange { get; set; }
        public List<uint> CreaturesInRange { get; set; }

        public PlayerAura()
        {
            Auras = new List<PlayerAuraDetail>();
            PartyMembersInRange = new List<uint>();
            CreaturesInRange = new List<uint>();
        }
    }
}
