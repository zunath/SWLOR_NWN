using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.TaxiService;

namespace SWLOR.Game.Server.Entity
{
    public class Player: EntityBase
    {
        public Player()
        {
            Init();
        }

        public Player(string id)
        {
            Init();
            Id = id;
        }

        private void Init()
        {
            Settings = new PlayerSettings();
            RacialStat = AbilityType.Invalid;
            BaseStats = new Dictionary<AbilityType, int>
            {
                {AbilityType.Vitality, 0},
                {AbilityType.Might, 0},
                {AbilityType.Social, 0},
                {AbilityType.Perception, 0},
                {AbilityType.Agility, 0},
                {AbilityType.Willpower, 0}
            };
            UpgradedStats = new Dictionary<AbilityType, int>
            {
                {AbilityType.Vitality, 0},
                {AbilityType.Might, 0},
                {AbilityType.Social, 0},
                {AbilityType.Perception, 0},
                {AbilityType.Agility, 0},
                {AbilityType.Willpower, 0}
            };

            Defenses = new Dictionary<CombatDamageType, int>();

            foreach (var type in Combat.GetAllDamageTypes())
            {
                Defenses[type] = 0;
            }

            ActiveShipId = Guid.Empty.ToString();
            IsUsingDualPistolMode = false;
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
            CraftedRecipes = new Dictionary<RecipeType, DateTime>();
            CharacterType = CharacterType.Invalid;
            KeyItems = new Dictionary<KeyItemType, DateTime>();
            Guilds = new Dictionary<GuildType, PlayerGuild>();
            Factions = new Dictionary<FactionType, PlayerFactionStanding>();
            TaxiDestinations = new Dictionary<int, List<TaxiDestinationType>>();
            ObjectVisibilities = new Dictionary<string, VisibilityType>();
            WindowGeometries = new Dictionary<GuiWindowType, GuiRectangle>();
            AppearanceScale = 1.0f;
            Control = new Dictionary<SkillType, int>();
            Craftsmanship = new Dictionary<SkillType, int>();
            CPBonus = new Dictionary<SkillType, int>();
            AbilityToggles = new Dictionary<AbilityToggleType, bool>();
            Currencies = new Dictionary<CurrencyType, int>();
        }


        [Indexed]
        public int Version { get; set; }
        [Indexed]
        public string Name { get; set; }
        public int MaxHP { get; set; }
        public int MaxFP { get; set; }
        public int MaxStamina { get; set; }
        public int HP { get; set; }
        public int FP { get; set; }
        public int Stamina { get; set; }
        public int TemporaryFoodHP { get; set; }
        public int BAB { get; set; }

        [Indexed]
        public string LocationAreaResref { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float LocationZ { get; set; }
        public float LocationOrientation { get; set; }
        public float RespawnLocationX { get; set; }
        public float RespawnLocationY { get; set; }
        public float RespawnLocationZ { get; set; }
        public float RespawnLocationOrientation { get; set; }
        [Indexed]
        public string RespawnAreaResref { get; set; }
        public int UnallocatedXP { get; set; }
        public int UnallocatedSP { get; set; }
        public int UnallocatedAP { get; set; }
        public int TotalSPAcquired { get; set; }
        public int TotalAPAcquired { get; set; }
        public int RegenerationTick { get; set; }
        public int HPRegen { get; set; }
        public int FPRegen { get; set; }
        public int STMRegen { get; set; }
        public int XPDebt { get; set; }
        public int DMXPBonus { get; set; }
        [Indexed]
        public bool IsDeleted { get; set; }
        public bool IsUsingDualPistolMode { get; set; }
        public DateTime? DatePerkRefundAvailable { get; set; }
        [Indexed]
        public CharacterType CharacterType { get; set; }
        public EmoteStyle EmoteStyle { get; set; }
        public string SerializedHotBar { get; set; }
        public string ActiveShipId { get; set; }
        public AppearanceType OriginalAppearanceType { get; set; }
        public float MovementRate { get; set; }
        public int AbilityRecastReduction { get; set; }
        public int MarketTill { get; set; }
        [Indexed]
        public string CitizenPropertyId { get; set; }
        public int PropertyOwedTaxes { get; set; }
        public int Attack { get; set; }
        public int ForceAttack { get; set; }
        public int Evasion { get; set; }
        public bool RebuildComplete { get; set; }
        public string ActiveBeastId { get; set; }

        public PlayerSettings Settings { get; set; }
        public Dictionary<SkillType, int> Control { get; set; }
        public Dictionary<SkillType, int> Craftsmanship { get; set; }
        public Dictionary<SkillType, int> CPBonus { get; set; }
        public AbilityType RacialStat { get; set; }
        public Dictionary<AbilityType, int> BaseStats { get; set; }
        public Dictionary<AbilityType, int> UpgradedStats { get; set; }
        public RoleplayProgress RoleplayProgress { get; set; }
        public Dictionary<string, List<MapPin>> MapPins { get; set; }
        public Dictionary<string, string> MapProgressions { get; set; }
        public Dictionary<SkillType, PlayerSkill> Skills { get; set; }
        public Dictionary<PerkType, int> Perks { get; set; }
        public Dictionary<RecastGroup, DateTime> RecastTimes { get; set; }
        public Dictionary<string, PlayerQuest> Quests { get; set; }
        public Dictionary<PerkType, DateTime> UnlockedPerks { get; set; }
        public Dictionary<RecipeType, DateTime> UnlockedRecipes { get; set; }
        public Dictionary<RecipeType, DateTime> CraftedRecipes { get; set; }
        public Dictionary<KeyItemType, DateTime> KeyItems{ get; set; }
        public Dictionary<GuildType, PlayerGuild> Guilds { get; set; }
        public Dictionary<FactionType, PlayerFactionStanding> Factions { get; set; }
        public Dictionary<int, List<TaxiDestinationType>> TaxiDestinations { get; set; }
        public Dictionary<string, VisibilityType> ObjectVisibilities { get; set; }
        public Dictionary<CombatDamageType, int> Defenses { get; set; }
        public Dictionary<GuiWindowType, GuiRectangle> WindowGeometries { get; set; }
        public Dictionary<AbilityToggleType, bool> AbilityToggles { get; set; }
        public Dictionary<CurrencyType, int> Currencies { get; set; }
        public float AppearanceScale { get; set; }
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
        public bool IsHolonetEnabled { get; set; }
        public bool ShowHelmet { get; set; }
        public bool ShowCloak { get; set; }
        public bool IsSubdualModeEnabled { get; set; }
        public bool IsLightsaberForceShareEnabled { get; set; }
        public bool DisplayServerResetReminders { get; set; }

        public PlayerSettings()
        {
            DisplayAchievementNotification = true;
            ShowHelmet = true;
            ShowCloak = true;
            IsHolonetEnabled = true;
            IsSubdualModeEnabled = false;
            IsLightsaberForceShareEnabled = true;
            DisplayServerResetReminders = true;
        }
    }

    public class PlayerGuild
    {
        public int Rank { get; set; }
        public int Points { get; set; }
    }

    public class PlayerFactionStanding
    {
        public int Standing { get; set; }
        public int Points { get; set; }
    }
}
