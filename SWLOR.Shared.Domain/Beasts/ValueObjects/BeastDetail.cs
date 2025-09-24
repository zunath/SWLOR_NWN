using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Crafting.ValueObjects;

namespace SWLOR.Shared.Domain.Beasts.ValueObjects
{
    public class BeastDetail
    {
        public string Name { get; set; }
        public AppearanceType Appearance { get; set; }
        public float AppearanceScale { get; set; }
        public int PortraitId { get; set; }
        public int SoundSetId { get; set; }
        public BeastRoleType Role { get; set; }
        public AbilityType AccuracyStat { get; set; }
        public AbilityType DamageStat { get; set; }
        public Dictionary<int, BeastLevel> Levels { get; set; }
        public List<MutationDetail> PossibleMutations { get; set; }

        public BeastDetail()
        {
            AppearanceScale = 1f;
            Levels = new Dictionary<int, BeastLevel>();
            PossibleMutations = new List<MutationDetail>();
        }
    }
}
