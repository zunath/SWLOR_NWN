

using SWLOR.Game.Server.Data.Contracts;
using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Player]")]
    public class Player : IEntity
    {
        public Player()
        {
            ID = Guid.NewGuid();
            RespawnAreaResref = "";
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public string CharacterName { get; set; }
        public int HitPoints { get; set; }
        public string LocationAreaResref { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double LocationOrientation { get; set; }
        public DateTime CreateTimestamp { get; set; }
        public int UnallocatedSP { get; set; }
        public int HPRegenerationAmount { get; set; }
        public int RegenerationTick { get; set; }
        public int RegenerationRate { get; set; }
        public int VersionNumber { get; set; }
        public int MaxFP { get; set; }
        public int CurrentFP { get; set; }
        public int CurrentFPTick { get; set; }
        public string RespawnAreaResref { get; set; }
        public double RespawnLocationX { get; set; }
        public double RespawnLocationY { get; set; }
        public double RespawnLocationZ { get; set; }
        public double RespawnLocationOrientation { get; set; }
        public DateTime DateSanctuaryEnds { get; set; }
        public bool IsSanctuaryOverrideEnabled { get; set; }
        public int STRBase { get; set; }
        public int DEXBase { get; set; }
        public int CONBase { get; set; }
        public int INTBase { get; set; }
        public int WISBase { get; set; }
        public int CHABase { get; set; }
        public int TotalSPAcquired { get; set; }
        public bool DisplayHelmet { get; set; }
        public Guid? PrimaryResidencePCBaseStructureID { get; set; }
        public DateTime? DatePerkRefundAvailable { get; set; }
        public int AssociationID { get; set; }
        public bool DisplayHolonet { get; set; }
        public bool DisplayDiscord { get; set; }
        public Guid? PrimaryResidencePCBaseID { get; set; }
        public bool IsUsingNovelEmoteStyle { get; set; }
        public bool IsDeleted { get; set; }
        public int XPBonus { get; set; }
        public int LeaseRate { get; set; }
        public Guid? LocationInstanceID { get; set; }
        public int GoldTill { get; set; }
        public int RoleplayPoints { get; set; }
        public int RoleplayXP { get; set; }
        public SpecializationType SpecializationID { get; set; }
        public int? ActiveConcentrationPerkID { get; set; }
        public int ActiveConcentrationTier { get; set; }

        public IEntity Clone()
        {
            return new Player
            {
                ID = ID,
                CharacterName = CharacterName,
                HitPoints = HitPoints,
                LocationAreaResref = LocationAreaResref,
                LocationX = LocationX,
                LocationY = LocationY,
                LocationZ = LocationZ,
                LocationOrientation = LocationOrientation,
                CreateTimestamp = CreateTimestamp,
                UnallocatedSP = UnallocatedSP,
                HPRegenerationAmount = HPRegenerationAmount,
                RegenerationTick = RegenerationTick,
                RegenerationRate = RegenerationRate,
                VersionNumber = VersionNumber,
                MaxFP = MaxFP,
                CurrentFP = CurrentFP,
                CurrentFPTick = CurrentFPTick,
                RespawnAreaResref = RespawnAreaResref,
                RespawnLocationX = RespawnLocationX,
                RespawnLocationY = RespawnLocationY,
                RespawnLocationZ = RespawnLocationZ,
                RespawnLocationOrientation = RespawnLocationOrientation,
                DateSanctuaryEnds = DateSanctuaryEnds,
                IsSanctuaryOverrideEnabled = IsSanctuaryOverrideEnabled,
                STRBase = STRBase,
                DEXBase = DEXBase,
                CONBase = CONBase,
                INTBase = INTBase,
                WISBase = WISBase,
                CHABase = CHABase,
                TotalSPAcquired = TotalSPAcquired,
                DisplayHelmet = DisplayHelmet,
                PrimaryResidencePCBaseStructureID = PrimaryResidencePCBaseStructureID,
                DatePerkRefundAvailable = DatePerkRefundAvailable,
                AssociationID = AssociationID,
                DisplayHolonet = DisplayHolonet,
                DisplayDiscord = DisplayDiscord,
                PrimaryResidencePCBaseID = PrimaryResidencePCBaseID,
                IsUsingNovelEmoteStyle = IsUsingNovelEmoteStyle,
                IsDeleted = IsDeleted,
                XPBonus = XPBonus,
                LeaseRate = LeaseRate,
                LocationInstanceID = LocationInstanceID,
                GoldTill = GoldTill,
                RoleplayPoints = RoleplayPoints,
                RoleplayXP = RoleplayXP,
                SpecializationID = SpecializationID,
                ActiveConcentrationPerkID = ActiveConcentrationPerkID,
                ActiveConcentrationTier = ActiveConcentrationTier
            };
        }
    }
}
