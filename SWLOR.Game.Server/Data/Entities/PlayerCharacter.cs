using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PlayerCharacter
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PlayerCharacter()
        {
            NortheastAreas = new HashSet<Area>();
            NorthwestAreas = new HashSet<Area>();
            SoutheastAreas = new HashSet<Area>();
            SouthwestAreas = new HashSet<Area>();
            BankItems = new HashSet<BankItem>();
            ChatLogs = new HashSet<ChatLog>();
            ChatLogs1 = new HashSet<ChatLog>();
            ClientLogEvents = new HashSet<ClientLogEvent>();
            PCImpoundedItems = new HashSet<PCImpoundedItem>();
            PCBasePermissions = new HashSet<PCBasePermission>();
            PCBaseStructurePermissions = new HashSet<PCBaseStructurePermission>();
            PCBases = new HashSet<PCBase>();
            PCCooldowns = new HashSet<PCCooldown>();
            PCCustomEffects = new HashSet<PCCustomEffect>();
            PCKeyItems = new HashSet<PCKeyItem>();
            PCMapPins = new HashSet<PCMapPin>();
            PCObjectVisibilities = new HashSet<PCObjectVisibility>();
            PCOverflowItems = new HashSet<PCOverflowItem>();
            PCPerkRefunds = new HashSet<PCPerkRefund>();
            PCPerks = new HashSet<PCPerk>();
            PCQuestKillTargetProgresses = new HashSet<PCQuestKillTargetProgress>();
            PCQuestItemProgresses = new HashSet<PCQuestItemProgress>();
            PCQuestStatus = new HashSet<PCQuestStatus>();
            PCRegionalFames = new HashSet<PCRegionalFame>();
            PCSearchSiteItems = new HashSet<PCSearchSiteItem>();
            PCSearchSites = new HashSet<PCSearchSite>();
            PCSkills = new HashSet<PCSkill>();
        }

        [Key]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public string CharacterName { get; set; }

        public int HitPoints { get; set; }
        
        [StringLength(16)]
        public string LocationAreaResref { get; set; }

        public double LocationX { get; set; }

        public double LocationY { get; set; }

        public double LocationZ { get; set; }

        public double LocationOrientation { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreateTimestamp { get; set; }
        
        public int UnallocatedSP { get; set; }
        
        public int HPRegenerationAmount { get; set; }

        public int RegenerationTick { get; set; }

        public int RegenerationRate { get; set; }

        public int VersionNumber { get; set; }

        public int MaxFP { get; set; }

        public int CurrentFP { get; set; }

        public int CurrentFPTick { get; set; }
        
        [StringLength(16)]
        public string RespawnAreaResref { get; set; }

        public double RespawnLocationX { get; set; }

        public double RespawnLocationY { get; set; }

        public double RespawnLocationZ { get; set; }

        public double RespawnLocationOrientation { get; set; }
        
        [Column(TypeName = "datetime2")]
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

        public bool DisplayHolonet { get; set; }

        public bool DisplayDiscord { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? DatePerkRefundAvailable { get; set; }

        public int AssociationID { get; set; }

        public int? PrimaryResidencePCBaseID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> NortheastAreas { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> NorthwestAreas { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> SoutheastAreas { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> SouthwestAreas { get; set; }

        public virtual Association Association { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BankItem> BankItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChatLog> ChatLogs { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChatLog> ChatLogs1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ClientLogEvent> ClientLogEvents { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCImpoundedItem> PCImpoundedItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCooldown> PCCooldowns { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCustomEffect> PCCustomEffects { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCKeyItem> PCKeyItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCMapPin> PCMapPins { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCObjectVisibility> PCObjectVisibilities { get; set; }

        public virtual PCOutfit PCOutfit { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCOverflowItem> PCOverflowItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCPerkRefund> PCPerkRefunds { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCPerk> PCPerks { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestItemProgress> PCQuestItemProgresses { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCQuestStatus> PCQuestStatus { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCRegionalFame> PCRegionalFames { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCSearchSiteItem> PCSearchSiteItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCSearchSite> PCSearchSites { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCSkill> PCSkills { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBase> PCBases { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBasePermission> PCBasePermissions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructurePermission> PCBaseStructurePermissions { get; set; }

        public virtual PCBaseStructure PrimaryResidencePCBaseStructure { get; set; }

        public virtual PCBase PrimaryResidencePCBase { get; set; }
    }
}
