using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TaxiService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service.AchievementService
{
    public static class AchievementTracking
    {
        private const int MaximumFactionStanding = 5000;

        private static readonly AchievementType[] OrbitAchievements =
        {
            AchievementType.ExploreHutlarOrbit,
            AchievementType.ExploreMonCalaOrbit,
            AchievementType.ExploreTatooineOrbit,
            AchievementType.ExploreViscaraOrbit,
            AchievementType.ExploreKorribanOrbit,
            AchievementType.ExploreDathomirOrbit,
            AchievementType.ExploreDantooineOrbit,
            AchievementType.ExploreNarShaddaaOrbit,
        };

        private static readonly GuildType[] ActiveGuilds = Enum
            .GetValues(typeof(GuildType))
            .Cast<GuildType>()
            .Where(type => type.GetAttribute<GuildType, GuildAttribute>().IsActive)
            .ToArray();

        private static readonly SkillType[] ActiveNonBasicLanguages = Enum
            .GetValues(typeof(SkillType))
            .Cast<SkillType>()
            .Where(type => type != SkillType.Basic)
            .Where(type =>
            {
                var detail = type.GetAttribute<SkillType, SkillAttribute>();
                return detail.IsActive && detail.Category == SkillCategoryType.Languages;
            })
            .ToArray();

        public static void EvaluatePersistedAchievements(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var account = DB.Get<Account>(GetPCPublicCDKey(player)) ?? new Account(GetPCPublicCDKey(player));
            account.Achievements ??= new Dictionary<AchievementType, DateTime>();
            bool Needs(AchievementType type) => !account.Achievements.ContainsKey(type);

            if (Needs(AchievementType.ANewBond) && HasOwnedBeast(playerId))
                Achievement.GiveAchievement(player, AchievementType.ANewBond);
            if (Needs(AchievementType.ApexCompanion) && HasApexBeast(playerId))
                Achievement.GiveAchievement(player, AchievementType.ApexCompanion);
            if (Needs(AchievementType.LearnedBehavior) && (dbPlayer.LearnedTechniques?.Count ?? 0) >= 10)
                Achievement.GiveAchievement(player, AchievementType.LearnedBehavior);
            if (Needs(AchievementType.FieldResearcher) && CountIncubationFieldNotes(dbPlayer) >= 10)
                Achievement.GiveAchievement(player, AchievementType.FieldResearcher);
            if (Needs(AchievementType.RenaissanceCrafter) && HasCraftedAllDisciplines(dbPlayer))
                Achievement.GiveAchievement(player, AchievementType.RenaissanceCrafter);
            if (Needs(AchievementType.LocalKnowledge) && HasAllTaxiDestinations(dbPlayer))
                Achievement.GiveAchievement(player, AchievementType.LocalKnowledge);
            if (Needs(AchievementType.TheGuildedAge) && HasGuildBreadth(dbPlayer))
                Achievement.GiveAchievement(player, AchievementType.TheGuildedAge);
            if (Needs(AchievementType.AKnownQuantity) &&
                dbPlayer.Factions?.Values.Any(x => x.Standing >= MaximumFactionStanding) == true)
                Achievement.GiveAchievement(player, AchievementType.AKnownQuantity);
            if (Needs(AchievementType.Polyglot) && HasThreeMasteredLanguages(dbPlayer))
                Achievement.GiveAchievement(player, AchievementType.Polyglot);

            if (Needs(AchievementType.ClearForDeparture) || Needs(AchievementType.AllSystemsGreen))
            {
                var ships = GetOwnedShips(playerId);
                if (Needs(AchievementType.ClearForDeparture) && ships.Count > 0)
                    Achievement.GiveAchievement(player, AchievementType.ClearForDeparture);
                if (Needs(AchievementType.AllSystemsGreen) && ships.Any(x => HasCompleteModuleSet(x.Status)))
                    Achievement.GiveAchievement(player, AchievementType.AllSystemsGreen);
            }

            if (Needs(AchievementType.TheGrandTour))
                CheckGrandTour(player);
        }

        public static void OnAchievementGranted(uint player, AchievementType achievementType)
        {
            if (OrbitAchievements.Contains(achievementType))
                CheckGrandTour(player);
        }

        public static void CheckGrandTour(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var account = DB.Get<Account>(GetPCPublicCDKey(player));
            if (account?.Achievements != null && OrbitAchievements.All(account.Achievements.ContainsKey))
                Achievement.GiveAchievement(player, AchievementType.TheGrandTour);
        }

        public static void RecordSlicingSuccess(uint player, SlicingSourceType source, bool usedTool)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var account = GetOrCreateAccount(player);
            EnsureProgressCollections(account);
            var changed = account.AchievementProgress.SlicingSourcesCompleted.Add((int)source);
            if (changed)
                DB.Set(account);

            if (!usedTool)
                Achievement.GiveAchievement(player, AchievementType.CleanSlice);

            if (account.AchievementProgress.SlicingSourcesCompleted.Contains((int)SlicingSourceType.Lockbox) &&
                account.AchievementProgress.SlicingSourcesCompleted.Contains((int)SlicingSourceType.Terminal))
            {
                Achievement.GiveAchievement(player, AchievementType.TwoKindsOfTrouble);
            }
        }

        public static void RecordFishCaught(
            uint player,
            int fishLevel,
            int agricultureRank,
            FishingLocationType location)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (fishLevel >= 52)
                Achievement.GiveAchievement(player, AchievementType.NoSuchThingAsLuck);
            if (fishLevel >= agricultureRank + 5)
                Achievement.GiveAchievement(player, AchievementType.AgainstTheCurrent);

            var planet = GetFishingPlanet(location);
            if (planet == PlanetType.Invalid)
                return;

            var account = GetOrCreateAccount(player);
            EnsureProgressCollections(account);
            if (account.AchievementProgress.FishingPlanetsCaught.Add((int)planet))
                DB.Set(account);

            if (account.AchievementProgress.FishingPlanetsCaught.Count >= 5)
                Achievement.GiveAchievement(player, AchievementType.GalacticAngler);
        }

        public static PlanetType GetFishingPlanet(FishingLocationType location)
        {
            var value = (int)location;
            if (value is >= 1 and <= 9) return PlanetType.Viscara;
            if (value is >= 10 and <= 15) return PlanetType.MonCala;
            if (value is >= 16 and <= 19) return PlanetType.Hutlar;
            if (value is >= 20 and <= 21) return PlanetType.Tatooine;
            if (value is >= 22 and <= 26) return PlanetType.Dathomir;
            if (value is >= 27 and <= 31) return PlanetType.Dantooine;
            return PlanetType.Invalid;
        }

        public static bool IsFullyOperational(ConstructedDroid droid)
        {
            return droid != null &&
                   !string.IsNullOrWhiteSpace(droid.SerializedCPU) &&
                   !string.IsNullOrWhiteSpace(droid.SerializedHead) &&
                   !string.IsNullOrWhiteSpace(droid.SerializedBody) &&
                   !string.IsNullOrWhiteSpace(droid.SerializedArms) &&
                   !string.IsNullOrWhiteSpace(droid.SerializedLegs) &&
                   (droid.ActivePerks?.Count ?? 0) > 0;
        }

        public static bool HasCompleteModuleSet(ShipStatus status)
        {
            return status != null &&
                   (status.HighPowerModules?.Count ?? 0) > 0 &&
                   (status.LowPowerModules?.Count ?? 0) > 0 &&
                   (status.ConfigurationModules?.Count ?? 0) > 0;
        }

        public static bool IsLowHull(ShipStatus status)
        {
            return status != null && status.MaxHull > 0 && status.Hull > 0 && status.Hull * 10 <= status.MaxHull;
        }

        public static bool GuardPreventedLethalHit(int currentHitPoints, int incomingDamage, int adjustedDamage)
        {
            return currentHitPoints > 0 &&
                   incomingDamage >= currentHitPoints &&
                   adjustedDamage < currentHitPoints;
        }

        public static bool HasEligiblePartyMember(uint player)
        {
            return Party.GetAllPartyMembers(player).Any(member =>
                member != player &&
                GetIsObjectValid(member) &&
                GetIsPC(member) &&
                !GetIsDM(member) &&
                GetArea(member) == GetArea(player));
        }

        public static void RecordPublicPropertyVisit(uint visitor, string ownerPlayerId)
        {
            if (!GetIsPC(visitor) ||
                GetIsDM(visitor) ||
                string.IsNullOrWhiteSpace(ownerPlayerId) ||
                ownerPlayerId == GetObjectUUID(visitor))
            {
                return;
            }

            var owner = DB.Get<Player>(ownerPlayerId);
            if (owner == null ||
                string.IsNullOrWhiteSpace(owner.AccountId) ||
                owner.AccountId == GetPCPublicCDKey(visitor))
            {
                return;
            }

            Achievement.QueueAchievementForPlayerId(ownerPlayerId, AchievementType.OpenDoorPolicy);
        }

        public static bool HasCraftedAllDisciplines(Player dbPlayer)
        {
            if (dbPlayer?.CraftedRecipes == null)
                return false;

            var disciplines = new HashSet<SkillType>();
            foreach (var recipeType in dbPlayer.CraftedRecipes.Keys)
            {
                if (!Craft.RecipeExists(recipeType))
                    continue;

                var skill = Craft.GetRecipe(recipeType).Skill;
                if (skill is SkillType.Smithery or SkillType.Engineering or SkillType.Fabrication or SkillType.Agriculture)
                    disciplines.Add(skill);
            }

            return disciplines.Count == 4;
        }

        public static bool HasAllTaxiDestinations(Player dbPlayer)
        {
            var unlocked = dbPlayer?.TaxiDestinations?.Values
                .SelectMany(x => x)
                .ToHashSet() ?? new HashSet<TaxiDestinationType>();
            return Enum.GetValues(typeof(TaxiDestinationType))
                .Cast<TaxiDestinationType>()
                .Where(x => x != TaxiDestinationType.Invalid)
                .All(unlocked.Contains);
        }

        public static bool HasGuildBreadth(Player dbPlayer)
        {
            return dbPlayer?.Guilds != null &&
                   ActiveGuilds.All(guild => dbPlayer.Guilds.TryGetValue(guild, out var progress) && progress.Rank >= 1);
        }

        public static bool HasThreeMasteredLanguages(Player dbPlayer)
        {
            return dbPlayer?.Skills != null && ActiveNonBasicLanguages.Count(language =>
                dbPlayer.Skills.TryGetValue(language, out var skill) && skill.Rank >= 20) >= 3;
        }

        private static int CountIncubationFieldNotes(Player dbPlayer)
        {
            return dbPlayer.KeyItems?.Keys.Count(keyItem =>
                IncubationFieldNote.TryGetNoteForKeyItem(keyItem, out _)) ?? 0;
        }

        private static bool HasOwnedBeast(string playerId)
        {
            var query = new DBQuery<Beast>().AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
            return DB.SearchCount(query) > 0;
        }

        private static bool HasApexBeast(string playerId)
        {
            var query = new DBQuery<Beast>().AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);
            return count > 0 && DB.Search(query.AddPaging(count, 0)).Any(beast => beast.Level >= 50);
        }

        private static List<PlayerShip> GetOwnedShips(string playerId)
        {
            var query = new DBQuery<PlayerShip>().AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);
            return count <= 0
                ? new List<PlayerShip>()
                : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static Account GetOrCreateAccount(uint player)
        {
            var accountId = GetPCPublicCDKey(player);
            var account = DB.Get<Account>(accountId) ?? new Account(accountId);
            EnsureProgressCollections(account);
            return account;
        }

        private static void EnsureProgressCollections(Account account)
        {
            account.AchievementProgress ??= new AchievementProgress();
            account.AchievementProgress.SlicingSourcesCompleted ??= new HashSet<int>();
            account.AchievementProgress.FishingPlanetsCaught ??= new HashSet<int>();
        }
    }
}
