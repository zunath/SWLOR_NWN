using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.ImplantService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TaxiService;

namespace SWLOR.Game.Server.Entity
{
    public class Player: EntityBase
    {
        public Player()
        {
            Settings = new PlayerSettings();
            ImplantStats = new PlayerImplantStats();
            BaseStats = new Dictionary<AbilityType, int>
            {
                {AbilityType.Constitution, 0},
                {AbilityType.Strength, 0},
                {AbilityType.Charisma, 0},
                {AbilityType.Dexterity, 0},
                {AbilityType.Intelligence, 0},
                {AbilityType.Wisdom, 0}
            };

            ShowHelmet = true;
            IsUsingDualPistolMode = false;
            IsHolonetEnabled = true;
            EmoteStyle = EmoteStyle.Regular;
            MovementRate = 1.0f;
            MapPins = new Dictionary<string, List<MapPin>>();
            MapProgressions = new Dictionary<string, string>();
            RoleplayProgress = new RoleplayProgress();
            Skills = new Dictionary<SkillType, PlayerSkill>();
            Perks = new Dictionary<PerkType, int>();
            RecastTimes = new Dictionary<RecastGroup, DateTime>();
            Quests = new Dictionary<string, PlayerQuest>();
            UnlockedPerks = new Dictionary<PerkType, DateTime>();
            UnlockedRecipes = new Dictionary<RecipeType, DateTime>();
            CharacterType = CharacterType.Invalid;
            KeyItems = new Dictionary<KeyItemType, DateTime>();
            Guilds = new Dictionary<GuildType, PlayerGuild>();
            SavedOutfits = new Dictionary<int, string>();
            Ships = new Dictionary<Guid, ShipStatus>();
            Implants = new Dictionary<ImplantSlotType, PlayerImplant>();
            Factions = new Dictionary<FactionType, PlayerFactionStanding>();
            TaxiDestinations = new Dictionary<int, List<TaxiDestinationType>>();
        }

        public override string KeyPrefix => "Player";

        public int Version { get; set; }
        public string Name { get; set; }
        public int MaxHP { get; set; }
        public int MaxFP { get; set; }
        public int MaxStamina { get; set; }
        public int HP { get; set; }
        public int FP { get; set; }
        public int Stamina { get; set; }
        public int BAB { get; set; }
        public string LocationAreaResref { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float LocationZ { get; set; }
        public float LocationOrientation { get; set; }
        public float RespawnLocationX { get; set; }
        public float RespawnLocationY { get; set; }
        public float RespawnLocationZ { get; set; }
        public float RespawnLocationOrientation { get; set; }
        public string RespawnAreaResref { get; set; }
        public int UnallocatedXP { get; set; }
        public int UnallocatedSP { get; set; }
        public int TotalSPAcquired { get; set; }
        public int RegenerationTick { get; set; }
        public int XPDebt { get; set; }
        public bool IsDeleted { get; set; }
        public bool ShowHelmet { get; set; }
        public bool IsUsingDualPistolMode { get; set; }
        public DateTime? DatePerkRefundAvailable { get; set; }
        public CharacterType CharacterType { get; set; }
        public bool IsHolonetEnabled { get; set; }
        public EmoteStyle EmoteStyle { get; set; }
        public string SerializedHotBar { get; set; }
        public Guid ActiveShipId { get; set; }
        public Guid SelectedShipId { get; set; }
        public AppearanceType OriginalAppearanceType { get; set; }
        public float MovementRate { get; set; }
        public int AbilityRecastReduction { get; set; }

        public PlayerSettings Settings { get; set; }
        public PlayerImplantStats ImplantStats { get; set; }
        public Dictionary<AbilityType, int> BaseStats { get; set; }
        public RoleplayProgress RoleplayProgress { get; set; }
        public Dictionary<string, List<MapPin>> MapPins { get; set; }
        public Dictionary<string, string> MapProgressions { get; set; }
        public Dictionary<SkillType, PlayerSkill> Skills { get; set; }
        public Dictionary<PerkType, int> Perks { get; set; }
        public Dictionary<RecastGroup, DateTime> RecastTimes { get; set; }
        public Dictionary<string, PlayerQuest> Quests { get; set; }
        public Dictionary<PerkType, DateTime> UnlockedPerks { get; set; }
        public Dictionary<RecipeType, DateTime> UnlockedRecipes { get; set; }
        public Dictionary<KeyItemType, DateTime> KeyItems{ get; set; }
        public Dictionary<GuildType, PlayerGuild> Guilds { get; set; }
        public Dictionary<int, string> SavedOutfits { get; set; }
        public Dictionary<Guid, ShipStatus> Ships { get; set; }
        public Dictionary<ImplantSlotType, PlayerImplant> Implants { get; set; }
        public Dictionary<FactionType, PlayerFactionStanding> Factions { get; set; }
        public Dictionary<int, List<TaxiDestinationType>> TaxiDestinations { get; set; }
    }

    public class MapPin
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string Note { get; set; }
    }

    public class RoleplayProgress
    {
        public int RPPoints { get; set; }
        public ulong TotalRPExpGained { get; set; }
        public ulong SpamMessageCount { get; set; }
    }

    public class PlayerSkill
    {
        public int Rank { get; set; }
        public int XP { get; set; }
        public bool IsLocked { get; set; }
    }

    public class PlayerQuest
    {
        public int CurrentState { get; set; }
        public int TimesCompleted { get; set; }
        public DateTime? DateLastCompleted { get; set; }

        public Dictionary<NPCGroupType, int> KillProgresses { get; set; } = new Dictionary<NPCGroupType, int>();
        public Dictionary<string, int> ItemProgresses { get; set; } = new Dictionary<string, int>();
    }

    public class PlayerSettings
    {
        public int? BattleThemeId { get; set; }
        public bool DisplayAchievementNotification { get; set; }

        public PlayerSettings()
        {
            DisplayAchievementNotification = true;
        }
    }

    public class PlayerGuild
    {
        public int Rank { get; set; }
        public int Points { get; set; }
    }

    public class PlayerImplant
    {
        public string ItemId { get; set; }
        public string ItemSerializedData { get; set; }
    }

    public class PlayerFactionStanding
    {
        public int Standing { get; set; }
        public int Points { get; set; }
    }

    public class PlayerImplantStats
    {
        public Dictionary<AbilityType, int> Attributes { get; set; }
        public int HPRegen { get; set; }
        public int FPRegen { get; set; }
        public int STMRegen { get; set; }

        public PlayerImplantStats()
        {
            Attributes = new Dictionary<AbilityType, int>
            {
                {AbilityType.Constitution, 0},
                {AbilityType.Strength, 0},
                {AbilityType.Charisma, 0},
                {AbilityType.Dexterity, 0},
                {AbilityType.Intelligence, 0},
                {AbilityType.Wisdom, 0}
            };
        }
    }
}
