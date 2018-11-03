
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;
using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PlayerCharacters")]
    public class PlayerCharacter : IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PlayerCharacter()
        {
            RespawnAreaResref = "";
            PCCooldowns = new HashSet<PCCooldown>();
            PCCraftedBlueprints = new HashSet<PCCraftedBlueprint>();
            PCCustomEffects = new HashSet<PCCustomEffect>();
            PCImpoundedItems = new HashSet<PCImpoundedItem>();
            PCKeyItems = new HashSet<PCKeyItem>();
            PCMapPins = new HashSet<PCMapPin>();
            PCMapProgressions = new HashSet<PCMapProgression>();
            PCObjectVisibilities = new HashSet<PCObjectVisibility>();
            PCOverflowItems = new HashSet<PCOverflowItem>();
            PCPerkRefunds = new HashSet<PCPerkRefund>();
            PCPerks = new HashSet<PCPerk>();
            PCQuestStatus = new HashSet<PCQuestStatus>();
            PCRegionalFames = new HashSet<PCRegionalFame>();
            PCSkills = new HashSet<PCSkill>();
        }

        [ExplicitKey]
        public string PlayerID { get; set; }
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
        public int? PrimaryResidencePCBaseStructureID { get; set; }
        public DateTime? DatePerkRefundAvailable { get; set; }
        public int AssociationID { get; set; }
        public bool DisplayHolonet { get; set; }
        public bool DisplayDiscord { get; set; }
        public int? PrimaryResidencePCBaseID { get; set; }
        public bool IsUsingNovelEmoteStyle { get; set; }
        public bool IsDeleted { get; set; }


        public virtual ICollection<PCCooldown> PCCooldowns { get; set; }
        public virtual ICollection<PCCraftedBlueprint> PCCraftedBlueprints { get; set; }
        public virtual ICollection<PCCustomEffect> PCCustomEffects { get; set; }
        public virtual ICollection<PCImpoundedItem> PCImpoundedItems { get; set; }
        public virtual ICollection<PCKeyItem> PCKeyItems { get; set; }
        public virtual ICollection<PCMapPin> PCMapPins { get; set; }
        public virtual ICollection<PCMapProgression> PCMapProgressions { get; set; }
        public virtual ICollection<PCObjectVisibility> PCObjectVisibilities { get; set; }
        public virtual PCOutfit PCOutfit { get; set; }
        public virtual ICollection<PCOverflowItem> PCOverflowItems { get; set; }
        public virtual ICollection<PCPerkRefund> PCPerkRefunds { get; set; }
        public virtual ICollection<PCPerk> PCPerks { get; set; }
        public virtual ICollection<PCQuestStatus> PCQuestStatus { get; set; }
        public virtual ICollection<PCRegionalFame> PCRegionalFames { get; set; }
        public virtual ICollection<PCSkill> PCSkills { get; set; }
    }
}
