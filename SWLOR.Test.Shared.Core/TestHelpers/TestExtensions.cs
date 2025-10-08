using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Enums;
using SWLOR.Shared.Domain.World.Enums;
using FactionType = SWLOR.Shared.Domain.Character.Enums.FactionType;

namespace SWLOR.Test.Shared.Core.TestHelpers
{
    /// <summary>
    /// Extension methods for test data creation and validation
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Creates a player with specific skill levels
        /// </summary>
        public static Player WithSkills(this Player player, params (SkillType skill, int rank, int xp)[] skills)
        {
            foreach (var (skill, rank, xp) in skills)
            {
                player.Skills[skill] = new PlayerSkill 
                { 
                    Rank = rank, 
                    XP = xp, 
                    IsLocked = false 
                };
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific perk levels
        /// </summary>
        public static Player WithPerks(this Player player, params (PerkType perk, int level)[] perks)
        {
            foreach (var (perk, level) in perks)
            {
                player.Perks[perk] = level;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific base stats
        /// </summary>
        public static Player WithBaseStats(this Player player, params (AbilityType ability, int value)[] stats)
        {
            foreach (var (ability, value) in stats)
            {
                player.BaseStats[ability] = value;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific upgraded stats
        /// </summary>
        public static Player WithUpgradedStats(this Player player, params (AbilityType ability, int value)[] stats)
        {
            foreach (var (ability, value) in stats)
            {
                player.UpgradedStats[ability] = value;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific defenses
        /// </summary>
        public static Player WithDefenses(this Player player, params (CombatDamageType type, int value)[] defenses)
        {
            foreach (var (type, value) in defenses)
            {
                player.Defenses[type] = value;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific currencies
        /// </summary>
        public static Player WithCurrencies(this Player player, params (CurrencyType type, int amount)[] currencies)
        {
            foreach (var (type, amount) in currencies)
            {
                player.Currencies[type] = amount;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific faction standings
        /// </summary>
        public static Player WithFactions(this Player player, params (FactionType faction, int standing, int points)[] factions)
        {
            foreach (var (faction, standing, points) in factions)
            {
                player.Factions[faction] = new PlayerFactionStanding 
                { 
                    Standing = standing, 
                    Points = points 
                };
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific guild memberships
        /// </summary>
        public static Player WithGuilds(this Player player, params (GuildType guild, int rank, int points)[] guilds)
        {
            foreach (var (guild, rank, points) in guilds)
            {
                player.Guilds[guild] = new PlayerGuild 
                { 
                    Rank = rank, 
                    Points = points 
                };
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific quest states
        /// </summary>
        public static Player WithQuests(this Player player, params (string questId, int state, int timesCompleted)[] quests)
        {
            foreach (var (questId, state, timesCompleted) in quests)
            {
                player.Quests[questId] = new PlayerQuest 
                { 
                    CurrentState = state, 
                    TimesCompleted = timesCompleted,
                    DateLastCompleted = timesCompleted > 0 ? DateTime.Now.AddDays(-timesCompleted) : null
                };
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific recast times
        /// </summary>
        public static Player WithRecastTimes(this Player player, params (RecastGroupType group, DateTime time)[] recasts)
        {
            foreach (var (group, time) in recasts)
            {
                player.RecastTimes[group] = time;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific key items
        /// </summary>
        public static Player WithKeyItems(this Player player, params (KeyItemType item, DateTime date)[] items)
        {
            foreach (var (item, date) in items)
            {
                player.KeyItems[item] = date;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific recipes
        /// </summary>
        public static Player WithRecipes(this Player player, params (RecipeType recipe, DateTime unlocked, DateTime? crafted)[] recipes)
        {
            foreach (var (recipe, unlocked, crafted) in recipes)
            {
                player.UnlockedRecipes[recipe] = unlocked;
                if (crafted.HasValue)
                {
                    player.CraftedRecipes[recipe] = crafted.Value;
                }
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific ability toggles
        /// </summary>
        public static Player WithAbilityToggles(this Player player, params (AbilityToggleType toggle, bool enabled)[] toggles)
        {
            foreach (var (toggle, enabled) in toggles)
            {
                player.AbilityToggles[toggle] = enabled;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific control and craftsmanship
        /// </summary>
        public static Player WithControlAndCraftsmanship(this Player player, params (SkillType skill, int control, int craftsmanship, int cpBonus)[] skills)
        {
            foreach (var (skill, control, craftsmanship, cpBonus) in skills)
            {
                player.Control[skill] = control;
                player.Craftsmanship[skill] = craftsmanship;
                player.CPBonus[skill] = cpBonus;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific taxi destinations
        /// </summary>
        public static Player WithTaxiDestinations(this Player player, params (int area, TaxiDestinationType[] destinations)[] taxis)
        {
            foreach (var (area, destinations) in taxis)
            {
                player.TaxiDestinations[area] = new List<TaxiDestinationType>(destinations);
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific object visibilities
        /// </summary>
        public static Player WithObjectVisibilities(this Player player, params (string objectId, VisibilityType visibility)[] objects)
        {
            foreach (var (objectId, visibility) in objects)
            {
                player.ObjectVisibilities[objectId] = visibility;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific map pins
        /// </summary>
        public static Player WithMapPins(this Player player, params (string area, MapPin[] pins)[] maps)
        {
            foreach (var (area, pins) in maps)
            {
                player.MapPins[area] = new List<MapPin>(pins);
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific map progressions
        /// </summary>
        public static Player WithMapProgressions(this Player player, params (string area, string status)[] progressions)
        {
            foreach (var (area, status) in progressions)
            {
                player.MapProgressions[area] = status;
            }
            return player;
        }

        /// <summary>
        /// Creates a player with specific settings
        /// </summary>
        public static Player WithSettings(this Player player, Action<PlayerSettings> settingsAction)
        {
            settingsAction(player.Settings);
            return player;
        }

        /// <summary>
        /// Creates a player with specific roleplay progress
        /// </summary>
        public static Player WithRoleplayProgress(this Player player, int rpPoints, ulong totalRPExp, ulong spamCount)
        {
            player.RoleplayProgress.RPPoints = rpPoints;
            player.RoleplayProgress.TotalRPExpGained = totalRPExp;
            player.RoleplayProgress.SpamMessageCount = spamCount;
            return player;
        }

        /// <summary>
        /// Creates a player with specific location
        /// </summary>
        public static Player WithLocation(this Player player, string area, float x, float y, float z, float orientation)
        {
            player.LocationAreaResref = area;
            player.LocationX = x;
            player.LocationY = y;
            player.LocationZ = z;
            player.LocationOrientation = orientation;
            return player;
        }

        /// <summary>
        /// Creates a player with specific respawn location
        /// </summary>
        public static Player WithRespawnLocation(this Player player, string area, float x, float y, float z, float orientation)
        {
            player.RespawnAreaResref = area;
            player.RespawnLocationX = x;
            player.RespawnLocationY = y;
            player.RespawnLocationZ = z;
            player.RespawnLocationOrientation = orientation;
            return player;
        }

        /// <summary>
        /// Creates a player with specific combat stats
        /// </summary>
        public static Player WithCombatStats(this Player player, int attack, int forceAttack, int evasion, int bab)
        {
            player.Attack = attack;
            player.ForceAttack = forceAttack;
            player.Evasion = evasion;
            return player;
        }

        /// <summary>
        /// Creates a player with specific regeneration stats
        /// </summary>
        public static Player WithRegenerationStats(this Player player, int hpRegen, int fpRegen, int stmRegen)
        {
            player.HPRegen = hpRegen;
            player.FPRegen = fpRegen;
            player.STMRegen = stmRegen;
            return player;
        }

        /// <summary>
        /// Creates a player with specific unallocated resources
        /// </summary>
        public static Player WithUnallocatedResources(this Player player, int xp, int sp, int ap)
        {
            player.UnallocatedXP = xp;
            player.UnallocatedSP = sp;
            player.UnallocatedAP = ap;
            return player;
        }

        /// <summary>
        /// Creates a player with specific health stats
        /// </summary>
        public static Player WithHealthStats(this Player player, int maxHP, int maxFP, int maxStamina, int hp, int fp, int stamina)
        {
            player.MaxHP = maxHP;
            player.MaxFP = maxFP;
            player.MaxStamina = maxStamina;
            player.HP = hp;
            player.FP = fp;
            player.Stamina = stamina;
            return player;
        }

        /// <summary>
        /// Creates a player with specific character type and appearance
        /// </summary>
        public static Player WithCharacterType(this Player player, CharacterType characterType, AppearanceType appearanceType)
        {
            player.CharacterType = characterType;
            player.OriginalAppearanceType = appearanceType;
            return player;
        }

        /// <summary>
        /// Creates a player with specific movement and scale
        /// </summary>
        public static Player WithMovementAndScale(this Player player, float movementRate, float appearanceScale)
        {
            player.MovementRate = movementRate;
            player.AppearanceScale = appearanceScale;
            return player;
        }

        /// <summary>
        /// Creates a player with specific property information
        /// </summary>
        public static Player WithProperty(this Player player, string propertyId, int owedTaxes)
        {
            player.CitizenPropertyId = propertyId;
            player.PropertyOwedTaxes = owedTaxes;
            return player;
        }

        /// <summary>
        /// Creates a player with specific market and ship information
        /// </summary>
        public static Player WithMarketAndShip(this Player player, int marketTill, string activeShipId)
        {
            player.MarketTill = marketTill;
            player.ActiveShipId = activeShipId;
            return player;
        }

        /// <summary>
        /// Creates a player with specific recast reduction and ability toggles
        /// </summary>
        public static Player WithRecastReduction(this Player player, int reduction)
        {
            player.AbilityRecastReduction = reduction;
            return player;
        }

        /// <summary>
        /// Creates a player with specific XP debt and DM bonus
        /// </summary>
        public static Player WithXPDebtAndBonus(this Player player, int xpDebt, int dmXPBonus)
        {
            player.XPDebt = xpDebt;
            player.DMXPBonus = dmXPBonus;
            return player;
        }

        /// <summary>
        /// Creates a player with specific rebuild status
        /// </summary>
        public static Player WithRebuildStatus(this Player player, bool rebuildComplete)
        {
            player.RebuildComplete = rebuildComplete;
            return player;
        }

        /// <summary>
        /// Creates a player with specific beast and emote style
        /// </summary>
        public static Player WithBeastAndEmote(this Player player, string activeBeastId, EmoteStyleType emoteStyle)
        {
            player.ActiveBeastId = activeBeastId;
            player.EmoteStyle = emoteStyle;
            return player;
        }

        /// <summary>
        /// Creates a player with specific dual pistol mode
        /// </summary>
        public static Player WithDualPistolMode(this Player player, bool isUsingDualPistolMode)
        {
            player.IsUsingDualPistolMode = isUsingDualPistolMode;
            return player;
        }

        /// <summary>
        /// Creates a player with specific perk refund availability
        /// </summary>
        public static Player WithPerkRefundAvailability(this Player player, DateTime? dateAvailable)
        {
            player.DatePerkRefundAvailable = dateAvailable;
            return player;
        }

        /// <summary>
        /// Creates a player with specific serialized hotbar
        /// </summary>
        public static Player WithSerializedHotBar(this Player player, string serializedHotBar)
        {
            player.SerializedHotBar = serializedHotBar;
            return player;
        }

        /// <summary>
        /// Creates a player with specific temporary food HP
        /// </summary>
        public static Player WithTemporaryFoodHP(this Player player, int temporaryFoodHP)
        {
            player.TemporaryFoodHP = temporaryFoodHP;
            return player;
        }

        /// <summary>
        /// Creates a player with specific deletion status
        /// </summary>
        public static Player WithDeletionStatus(this Player player, bool isDeleted)
        {
            player.IsDeleted = isDeleted;
            return player;
        }

        /// <summary>
        /// Creates a player with specific version
        /// </summary>
        public static Player WithVersion(this Player player, int version)
        {
            player.Version = version;
            return player;
        }

        /// <summary>
        /// Creates a player with specific racial stat
        /// </summary>
        public static Player WithRacialStat(this Player player, AbilityType racialStat)
        {
            player.RacialStat = racialStat;
            return player;
        }
    }
}
