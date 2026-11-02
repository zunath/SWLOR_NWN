using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// City Projects (Player City Revamp, "City Projects"): what a project costs, who may start one, how donations are capped at the
    /// need, when a supplied project completes, and how credits and materials come back when it is cancelled. No database or engine
    /// calls: <see cref="CityProjects"/> reads and writes the records and asks these rules.
    /// </summary>
    public static class CityProjectRules
    {
        /// <summary>A fully supplied project takes 2 days of real time to build.</summary>
        public const int BuildDays = 2;

        /// <summary>The credits a facility upgrade costs for each level it builds (50,000 times the level), as upgrades cost today.</summary>
        public const int CreditsPerLevel = 50000;

        /// <summary>
        /// The credits Home Ground costs. PROPOSED, NEEDS THE OWNER'S APPROVAL: the plan sets no price. 150,000 is what a level 3 facility
        /// upgrade costs; Home Ground helps every citizen's gathering and every garden, so it is priced with the middle upgrades.
        /// </summary>
        public const long HomeGroundCredits = 150000;

        public const int MaxFacilityLevel = 5;

        /// <summary>The donors the Gazette names.</summary>
        public const int TopDonorCount = 3;

        #region What a project is

        public static bool IsFacilityUpgrade(CityProjectType type)
        {
            return type is CityProjectType.MedicalCenterUpgrade or CityProjectType.StarportUpgrade
                or CityProjectType.CantinaUpgrade or CityProjectType.JumpBeaconUpgrade;
        }

        /// <summary>The upgrade a facility project raises, or Invalid for Home Ground.</summary>
        public static PropertyUpgradeType GetUpgradeType(CityProjectType type)
        {
            switch (type)
            {
                case CityProjectType.MedicalCenterUpgrade: return PropertyUpgradeType.MedicalCenterLevel;
                case CityProjectType.StarportUpgrade: return PropertyUpgradeType.StarportLevel;
                case CityProjectType.CantinaUpgrade: return PropertyUpgradeType.CantinaLevel;
                case CityProjectType.JumpBeaconUpgrade: return PropertyUpgradeType.JumpBeaconLevel;
                default: return PropertyUpgradeType.Invalid;
            }
        }

        public static CityProjectType GetProjectType(PropertyUpgradeType upgrade)
        {
            switch (upgrade)
            {
                case PropertyUpgradeType.MedicalCenterLevel: return CityProjectType.MedicalCenterUpgrade;
                case PropertyUpgradeType.StarportLevel: return CityProjectType.StarportUpgrade;
                case PropertyUpgradeType.CantinaLevel: return CityProjectType.CantinaUpgrade;
                case PropertyUpgradeType.JumpBeaconLevel: return CityProjectType.JumpBeaconUpgrade;
                default: return CityProjectType.Invalid;
            }
        }

        /// <summary>The top level a facility reaches: 5, or 3 for a Jump Beacon.</summary>
        public static int GetMaxLevel(CityProjectType type)
        {
            return type == CityProjectType.JumpBeaconUpgrade ? 3 : MaxFacilityLevel;
        }

        /// <summary>The credits a project costs: 50,000 times the level a facility upgrade builds, or the Home Ground price.</summary>
        public static long GetCredits(CityProjectType type, int targetLevel)
        {
            if (type == CityProjectType.HomeGround)
                return HomeGroundCredits;

            return IsFacilityUpgrade(type) ? (long)CreditsPerLevel * targetLevel : 0;
        }

        /// <summary>The project's name as a player reads it: "Starport level 3" or "Home Ground".</summary>
        public static string GetName(CityProjectType type, int targetLevel)
        {
            switch (type)
            {
                case CityProjectType.MedicalCenterUpgrade: return $"Medical Center level {targetLevel}";
                case CityProjectType.StarportUpgrade: return $"Starport level {targetLevel}";
                case CityProjectType.CantinaUpgrade: return $"Cantina level {targetLevel}";
                case CityProjectType.JumpBeaconUpgrade: return $"Jump Beacon level {targetLevel}";
                case CityProjectType.HomeGround: return "Home Ground";
                default: return "Project";
            }
        }

        public static string GetName(CityProject project)
        {
            return GetName(project.ProjectType, project.TargetLevel);
        }

        #endregion

        #region Starting

        /// <summary>
        /// Why a project cannot be started, or <see cref="CityProjectRefusal.None"/>. Checked in this order. The credits can come
        /// from both balances, restricted funds first, so the check is against what the city can spend.
        /// </summary>
        /// <param name="type">The project.</param>
        /// <param name="currentLevel">The facility's current level (1 when it has never been upgraded). Ignored for Home Ground.</param>
        /// <param name="cityLevel">The city's level.</param>
        /// <param name="mayorCityManagementRank">The mayor's City Management perk rank.</param>
        /// <param name="isStation">Whether the city is a player station.</param>
        /// <param name="homeGroundBuilt">Whether Home Ground is already built.</param>
        /// <param name="alreadyOpen">Whether a project of this type is already open.</param>
        /// <param name="spendable">What the city can spend on it (restricted funds and the treasury).</param>
        /// <param name="canStartProjects">Whether the player holds the power to start projects.</param>
        public static CityProjectRefusal CheckStart(
            CityProjectType type,
            int currentLevel,
            int cityLevel,
            int mayorCityManagementRank,
            bool isStation,
            bool homeGroundBuilt,
            bool alreadyOpen,
            long spendable,
            bool canStartProjects)
        {
            if (!canStartProjects)
                return CityProjectRefusal.NoPermission;
            if (type == CityProjectType.Invalid)
                return CityProjectRefusal.Unknown;
            if (type == CityProjectType.JumpBeaconUpgrade && !isStation)
                return CityProjectRefusal.StationOnly;
            if (alreadyOpen)
                return CityProjectRefusal.AlreadyOpen;

            long credits;
            if (type == CityProjectType.HomeGround)
            {
                if (homeGroundBuilt)
                    return CityProjectRefusal.AlreadyBuilt;

                credits = HomeGroundCredits;
            }
            else
            {
                if (currentLevel >= GetMaxLevel(type))
                    return CityProjectRefusal.MaxLevel;
                if (cityLevel <= currentLevel)
                    return CityProjectRefusal.CityLevelTooLow;
                if (mayorCityManagementRank + 1 <= currentLevel)
                    return CityProjectRefusal.MayorPerkTooLow;

                credits = GetCredits(type, currentLevel + 1);
            }

            return spendable < credits ? CityProjectRefusal.CannotAfford : CityProjectRefusal.None;
        }

        /// <summary>The message a player reads when the project cannot be started.</summary>
        public static string Describe(CityProjectRefusal refusal, long credits = 0)
        {
            switch (refusal)
            {
                case CityProjectRefusal.NoPermission: return "Insufficient permissions.";
                case CityProjectRefusal.MaxLevel: return "Cannot upgrade further.";
                case CityProjectRefusal.CityLevelTooLow: return "Increase city level to upgrade.";
                case CityProjectRefusal.MayorPerkTooLow: return "Mayor city management perk too low.";
                case CityProjectRefusal.AlreadyOpen: return "That project is already under way.";
                case CityProjectRefusal.AlreadyBuilt: return "Home Ground is already built.";
                case CityProjectRefusal.CannotAfford: return $"The treasury needs at least {credits:N0} cr to start this project.";
                case CityProjectRefusal.StationOnly: return "Only a player station has a Jump Beacon.";
                case CityProjectRefusal.Unknown: return "That project cannot be built.";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// A new project for the city, in the <see cref="CityProjectState.Starting"/> state: nothing is held yet. Null when the catalog has no
        /// materials for it.
        /// </summary>
        /// <param name="targetLevel">The level a facility upgrade builds, or 0 for Home Ground.</param>
        public static CityProject Create(string cityId, CityProjectType type, int targetLevel, string startedByPlayerId, DateTime nowUtc)
        {
            var recipe = CityProjectCatalog.Get(type, targetLevel);
            if (recipe == null)
                return null;

            return new CityProject
            {
                CityId = cityId,
                ProjectType = type,
                State = CityProjectState.Starting,
                TargetLevel = type == CityProjectType.HomeGround ? 0 : targetLevel,
                CreditCost = GetCredits(type, targetLevel),
                RequiredMaterials = new Dictionary<string, int>(recipe.Materials),
                StartedByPlayerId = startedByPlayerId ?? string.Empty,
                StartedUtc = nowUtc
            };
        }

        /// <summary>Holds the project's credits in the city's escrow under the project's id, restricted funds first. False when the city cannot pay.</summary>
        public static bool HoldCredits(WorldProperty city, CityProject project)
        {
            return CityTreasuryRules.PostEscrow(city, project.Id, project.CreditCost, restrictedFirst: true).Succeeded;
        }

        #endregion

        #region Donations

        /// <summary>How many of a material the project still needs.</summary>
        public static int GetRemaining(CityProject project, string resref)
        {
            if (!project.RequiredMaterials.TryGetValue(resref ?? string.Empty, out var required))
                return 0;

            return Math.Max(0, required - project.DonatedMaterials.GetValueOrDefault(resref));
        }

        /// <summary>What the project still needs, by resref, leaving out what is supplied.</summary>
        public static Dictionary<string, int> GetRemaining(CityProject project)
        {
            return project.RequiredMaterials.Keys
                .Select(resref => (Resref: resref, Remaining: GetRemaining(project, resref)))
                .Where(x => x.Remaining > 0)
                .ToDictionary(x => x.Resref, x => x.Remaining);
        }

        /// <summary>Whether every material is in.</summary>
        public static bool IsSupplied(CityProject project)
        {
            return project.RequiredMaterials.Keys.All(resref => GetRemaining(project, resref) <= 0);
        }

        /// <summary>The most of a material the project takes from a donor offering <paramref name="offered"/>: never more than it still needs.</summary>
        public static int ClampDonation(CityProject project, string resref, int offered)
        {
            return Math.Clamp(offered, 0, GetRemaining(project, resref));
        }

        /// <summary>Whether the project takes donations: it is open and not yet fully supplied.</summary>
        public static bool AcceptsDonations(CityProject project)
        {
            return project.State == CityProjectState.Open && !IsSupplied(project);
        }

        /// <summary>
        /// Takes a donation, never more than the project still needs, and records the donor. The project that receives the last
        /// material starts its 2 days. The caller removes exactly the returned number of items from the donor.
        /// </summary>
        /// <param name="project">The project, changed in place.</param>
        /// <param name="donorId">The donor's player id.</param>
        /// <param name="donorLabel">The name strangers know the donor by, for the Gazette.</param>
        /// <param name="resref">The material.</param>
        /// <param name="offered">How many the donor offers.</param>
        /// <param name="nowUtc">The time now.</param>
        /// <returns>How many the project took.</returns>
        public static int Accept(CityProject project, string donorId, string donorLabel, string resref, int offered, DateTime nowUtc)
        {
            if (!AcceptsDonations(project) || string.IsNullOrWhiteSpace(donorId))
                return 0;

            var accepted = ClampDonation(project, resref, offered);
            if (accepted <= 0)
                return 0;

            project.DonatedMaterials[resref] = project.DonatedMaterials.GetValueOrDefault(resref) + accepted;

            if (!project.DonorMaterials.TryGetValue(donorId, out var donor))
            {
                donor = new Dictionary<string, int>();
                project.DonorMaterials[donorId] = donor;
            }

            donor[resref] = donor.GetValueOrDefault(resref) + accepted;
            if (!string.IsNullOrWhiteSpace(donorLabel))
                project.DonorLabels[donorId] = donorLabel;

            if (IsSupplied(project) && !project.SuppliedUtc.HasValue)
            {
                project.SuppliedUtc = nowUtc;
                project.CompletesUtc = GetCompletionUtc(nowUtc);
            }

            return accepted;
        }

        /// <summary>The donors by the items they gave, most first, at most <paramref name="count"/>.</summary>
        public static List<CityProjectDonor> GetTopDonors(CityProject project, int count = TopDonorCount)
        {
            return project.DonorMaterials
                .Select(pair => new CityProjectDonor(
                    pair.Key,
                    project.DonorLabels.GetValueOrDefault(pair.Key) is { Length: > 0 } label ? label : "Someone",
                    pair.Value.Values.Sum()))
                .Where(donor => donor.Items > 0)
                .OrderByDescending(donor => donor.Items)
                .ThenBy(donor => donor.PlayerId, StringComparer.Ordinal)
                .Take(Math.Max(0, count))
                .ToList();
        }

        #endregion

        #region Building

        /// <summary>When a project supplied at <paramref name="suppliedUtc"/> completes.</summary>
        public static DateTime GetCompletionUtc(DateTime suppliedUtc)
        {
            return suppliedUtc.AddDays(BuildDays);
        }

        /// <summary>
        /// Whether the project is ready to be applied: it is supplied and its completion time has passed. Projects are applied at server
        /// start, so one that came due while the server was down is due at the next start, with the completion time it always had.
        /// </summary>
        public static bool IsDue(CityProject project, DateTime nowUtc)
        {
            return project.State == CityProjectState.Open &&
                   project.CompletesUtc.HasValue &&
                   project.CompletesUtc.Value <= nowUtc &&
                   IsSupplied(project);
        }

        /// <summary>
        /// Applies a finished project to its city: a facility reaches the level it builds, or Home Ground gets its completion time. Safe to
        /// repeat after a restart, since a level is never lowered and the first completion time stands.
        /// </summary>
        public static void ApplyCompletion(WorldProperty city, CityProject project)
        {
            if (project.ProjectType == CityProjectType.HomeGround)
            {
                city.HomeGroundCompletedUtc ??= project.CompletesUtc ?? DateTime.UtcNow;
                return;
            }

            var upgrade = GetUpgradeType(project.ProjectType);
            if (upgrade == PropertyUpgradeType.Invalid)
                return;

            var current = city.Upgrades.GetValueOrDefault(upgrade, 1);
            city.Upgrades[upgrade] = Math.Max(current, project.TargetLevel);
        }

        /// <summary>Whether Home Ground is built: its completion time has come.</summary>
        public static bool IsHomeGroundBuilt(WorldProperty city, DateTime nowUtc)
        {
            return city?.HomeGroundCompletedUtc != null && city.HomeGroundCompletedUtc.Value <= nowUtc;
        }

        #endregion

        #region Cancelling

        /// <summary>The materials a cancelled project owes, one entry for each donor and material.</summary>
        public static List<CityProjectRefund> GetRefunds(CityProject project)
        {
            return project.DonorMaterials
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .SelectMany(donor => donor.Value
                    .Where(material => material.Value > 0)
                    .OrderBy(material => material.Key, StringComparer.Ordinal)
                    .Select(material => new CityProjectRefund(donor.Key, material.Key, material.Value)))
                .ToList();
        }

        /// <summary>
        /// The overflow record's id for one chunk of a donor's returned material. It never changes, so a refund repeated after a restart
        /// writes the same records instead of a second set.
        /// </summary>
        public static string GetOverflowRecordId(string projectId, string donorId, string resref, int chunk)
        {
            return $"city-project:{projectId}:{donorId}:{resref}:{chunk}";
        }

        #endregion

        #region Text

        /// <summary>How many materials the project has taken, counting each material only up to what it needs.</summary>
        public static int GetDonatedCount(CityProject project)
        {
            return project.RequiredMaterials.Sum(pair => Math.Min(pair.Value, project.DonatedMaterials.GetValueOrDefault(pair.Key)));
        }

        /// <summary>How many materials the project needs in all.</summary>
        public static int GetRequiredCount(CityProject project)
        {
            return project.RequiredMaterials.Values.Sum();
        }

        /// <summary>Where the project is, in a line: what it still needs, or when it completes.</summary>
        public static string DescribeStatus(CityProject project, DateTime nowUtc)
        {
            if (!IsSupplied(project))
            {
                var remaining = GetRequiredCount(project) - GetDonatedCount(project);
                return $"Needs {remaining} more {(remaining == 1 ? "material" : "materials")}.";
            }

            if (!project.CompletesUtc.HasValue)
                return "All materials are in.";

            var left = project.CompletesUtc.Value - nowUtc;
            return left > TimeSpan.Zero
                ? $"All materials are in. It completes in {FarmingService.FarmingRules.FormatDuration(left)}."
                : "All materials are in and the build time is up. It is applied at the next server start.";
        }

        /// <summary>The materials as a player reads them: "6 Reinforced Construction Parts, 3 Reinforced Power Supply Units".</summary>
        public static string DescribeMaterials(IReadOnlyDictionary<string, int> materials, Func<string, string> nameOf)
        {
            return string.Join(", ", materials
                .Where(x => x.Value > 0)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Value} {nameOf(x.Key)}"));
        }

        /// <summary>The Gazette line for a project that was started.</summary>
        public static string BuildStartedText(CityProject project, Func<string, string> nameOf)
        {
            return $"The city began a project: {GetName(project)}. It needs {project.CreditCost:N0} credits from the treasury and " +
                   $"{DescribeMaterials(project.RequiredMaterials, nameOf)}, which anyone may donate at City Hall.";
        }

        /// <summary>The Gazette line for a project that completed, naming its top donors.</summary>
        public static string BuildCompletedText(CityProject project)
        {
            var donors = GetTopDonors(project);
            var text = $"The city finished its project: {GetName(project)}.";
            if (donors.Count == 0)
                return text;

            return text + " Thank you to the top donors: " +
                   string.Join(", ", donors.Select(donor => $"{donor.Label} ({donor.Items} {(donor.Items == 1 ? "item" : "items")})")) + ".";
        }

        /// <summary>The Gazette line for a project that was cancelled.</summary>
        public static string BuildCancelledText(CityProject project)
        {
            return $"The city cancelled its project: {GetName(project)}. Donated materials went back to their donors.";
        }

        #endregion
    }
}
