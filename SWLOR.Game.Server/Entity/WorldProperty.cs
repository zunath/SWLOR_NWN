using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public class WorldProperty: EntityBase
    {
        public WorldProperty()
        {
            CustomName = string.Empty;
            CustomDescription = string.Empty;
            ParentPropertyId = string.Empty;
            StationAnchorId = string.Empty;
            PendingPaymentId = string.Empty;
            ChildPropertyIds = new Dictionary<PropertyChildType, List<string>>();
            Dates = new Dictionary<PropertyDateType, DateTime>();
            Positions = new Dictionary<PropertyLocationType, PropertyLocation>();
            Taxes = new Dictionary<PropertyTaxType, int>();
            Upgrades = new Dictionary<PropertyUpgradeType, int>();
            Edicts = new Dictionary<EdictType, CityEdict>();
            MayorRecord = new MayorRecord();
            Escrow = new Dictionary<string, CityEscrowEntry>();
            Officers = new Dictionary<string, CityOfficer>();
            Modifiers = new Dictionary<CityModifierType, CityModifier>();
            ActingMayorPlayerId = string.Empty;
        }

        /// <summary>
        /// A custom name set by players.
        /// </summary>
        [Indexed]
        public string CustomName { get; set; }

        /// <summary>
        /// A custom description set by players.
        /// </summary>
        public string CustomDescription { get; set; }

        /// <summary>
        /// The type of property.
        /// </summary>
        [Indexed]
        public PropertyType PropertyType { get; set; }

        /// <summary>
        /// The player Id of the owner.
        /// </summary>
        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>
        /// The parent property Id
        /// </summary>
        [Indexed]
        public string ParentPropertyId { get; set; }

        /// <summary>
        /// Tracks the child property Ids contained inside this property.
        /// </summary>
        public Dictionary<PropertyChildType, List<string>> ChildPropertyIds { get; set; }

        /// <summary>
        /// Tracks dates and timers specific to this property.
        /// </summary>
        public Dictionary<PropertyDateType, DateTime> Dates { get; set; }

        /// <summary>
        /// Tracks positions specific to this property.
        /// Most properties will use a Static position but in the event the property is mobile, such as Starships,
        /// multiple positions may need to be tracked.
        /// </summary>
        public Dictionary<PropertyLocationType, PropertyLocation> Positions { get; set; }

        /// <summary>
        /// Tracks taxes specific to this property.
        /// </summary>
        public Dictionary<PropertyTaxType, int> Taxes { get; set; }

        /// <summary>
        /// Tracks upgrades specific to this property.
        /// </summary>
        public Dictionary<PropertyUpgradeType, int> Upgrades { get; set; }

        /// <summary>
        /// If true, the property will be publicly accessible.
        /// This will only be set if the PropertyType is a Building
        /// </summary>
        [Indexed]
        public bool IsPubliclyAccessible { get; set; }

        /// <summary>
        /// The layout of this property.
        /// For Starships, Apartments, and Buildings this is the interior.
        /// For Cities, this is the detail for the exterior.
        /// Structures do not use this property.
        /// </summary>
        public PropertyLayoutType Layout { get; set; }

        /// <summary>
        /// The structure type of this property.
        /// This will only be set if the PropertyType is a Structure.
        /// </summary>
        [Indexed]
        public StructureType StructureType { get; set; }

        /// <summary>
        /// If this flag is set to true, the property will be permanently removed
        /// from the database the next time the server boots up.
        /// This is delayed to avoid lag and situations where players could log into a deleted property.
        /// </summary>
        [Indexed]
        public bool IsQueuedForDeletion { get; set; }

        /// <summary>
        /// Associated item which has been serialized. Only used if the PropertyType is a Structure.
        /// </summary>
        public string SerializedItem { get; set; }

        /// <summary>
        /// The number of items that can be stored within the property.
        /// For Buildings, Apartments, and Starships, this is the maximum number of items that can be stored.
        /// For Structures, this is how much storage they increase to their parent property.
        /// </summary>
        public int ItemStorageCount { get; set; }

        /// <summary>
        /// Tracks the amount of credits contained within the property's treasury.
        /// Only applicable to City property types.
        /// </summary>
        public long Treasury { get; set; }

        /// <summary>
        /// The ids of craft station fees already credited to <see cref="Treasury"/> (<see cref="PendingStationFee.Id"/>), saved with the credit, so
        /// a retry after a crash never credits a fee twice. Records saved before this existed hold null, which means none.
        /// </summary>
        public HashSet<string> PaidCraftFees { get; set; }

        /// <summary>
        /// Credits earned from sources that cannot be withdrawn, such as City Affairs gains. Upkeep, Edicts and projects
        /// spend them before <see cref="Treasury"/>; the treasury withdrawal never touches them. Only applicable to City
        /// property types.
        /// </summary>
        public long RestrictedTreasury { get; set; }

        /// <summary>
        /// Credits held out of both balances for a bounty, tournament or project, keyed by that thing's id. Each entry
        /// remembers which balance every portion came from, so a refund returns it there.
        /// </summary>
        public Dictionary<string, CityEscrowEntry> Escrow { get; set; }

        /// <summary>
        /// Tracks the amount of credits required to pay off the upkeep for this property.
        /// Only applicable to City property types.
        /// </summary>
        public int Upkeep { get; set; }

        /// <summary>
        /// The Edicts this city holds, whatever their state (Pending or Active) and whether or not a pause covers them.
        /// Each takes one of the city's Edict slots.
        /// </summary>
        public Dictionary<EdictType, CityEdict> Edicts { get; set; }

        /// <summary>The sitting mayor's promised and broken Edict counts. Reset when the seat changes hands.</summary>
        public MayorRecord MayorRecord { get; set; }

        /// <summary>
        /// The anchor site a player station stands at (plan section 25). Empty for every other property, including
        /// planet cities; older records load empty.
        /// </summary>
        [Indexed]
        public string StationAnchorId { get; set; }

        /// <summary>
        /// The founding payment (<c>CreditPaymentMark</c>) a player station still waits on, or empty. Saved with the new
        /// station, so a login whose character file never paid it undoes the founding. Older records load empty.
        /// </summary>
        public string PendingPaymentId { get; set; }

        /// <summary>The credits <see cref="PendingPaymentId"/> takes.</summary>
        public int PendingPaymentCredits { get; set; }

        /// <summary>
        /// City-wide condition: when citizens voted No on the sitting mayor. Set when a confidence vote fails and cleared
        /// by the next election that ends in an outright win. Null when clear. Edicts pause while it is set.
        /// </summary>
        public DateTime? NoConfidenceSinceUtc { get; set; }

        /// <summary>
        /// City-wide condition: when the city's politics were found neglected (an empty second recall). Cleared by the
        /// next election that ends with at least one candidate and one vote. Null when clear. Set by the recall path.
        /// </summary>
        public DateTime? NeglectSinceUtc { get; set; }

        /// <summary>
        /// The result of the city's latest election, for the notice citizens read at their next login. Older records
        /// load with none.
        /// </summary>
        public ElectionResultRecord LastElectionResult { get; set; }

        /// <summary>
        /// The officers the mayor appointed, by player id. The limit is 1 plus 1 per city level. Cities only; older
        /// records load with none.
        /// </summary>
        public Dictionary<string, CityOfficer> Officers { get; set; }

        /// <summary>
        /// Effects City Affairs situations put on the city, until they expire. A repeat refreshes the expiry instead of
        /// stacking. Cities only; older records load with none.
        /// </summary>
        public Dictionary<CityModifierType, CityModifier> Modifiers { get; set; }

        /// <summary>
        /// How many City Affairs situations in a row expired unhandled. Any decision resets it to 0, and at 5 a recall
        /// election opens.
        /// </summary>
        public int NeglectCount { get; set; }

        /// <summary>
        /// 0 normally. 1 after a recall election ended empty: the city is marked Abandoned and the second recall is
        /// open. Cleared when the second recall ends.
        /// </summary>
        public int RecallStage { get; set; }

        /// <summary>
        /// The citizen the mayor named Acting Mayor, or empty. The term ends at <see cref="ActingMayorEndsUtc"/>, and sooner
        /// when the mayor ends it, the Acting Mayor leaves the city, or an election seats a mayor. Cities only; older
        /// records load empty.
        /// </summary>
        public string ActingMayorPlayerId { get; set; }

        /// <summary>When the Acting Mayor's term ends, or null when there is none. At most 30 days after it began.</summary>
        public DateTime? ActingMayorEndsUtc { get; set; }

        /// <summary>
        /// The mayor's step-down in progress, or null. While it is pending the mayor keeps every power; once accepted the
        /// mayor has none and the interim holder has the Acting Mayor's powers, until the snap election ends. Cities only.
        /// </summary>
        public CityStepDown StepDown { get; set; }

        /// <summary>The city's one story request for the DMs, or null. Cities only.</summary>
        public CityStoryRequest StoryRequest { get; set; }

        /// <summary>The planetary council a DM last called for the city's planet, or null. Cities only.</summary>
        public CityCouncilSummons CouncilSummons { get; set; }

        /// <summary>
        /// True when the mayor switched the city's wandering townsfolk and guards off in Manage City. They leave at once and
        /// stay away until switched back on. Cities only; older records load false, so no migration is needed.
        /// </summary>
        public bool TownsfolkHidden { get; set; }

        /// <summary>
        /// True when the mayor switched the city's street critters off in Manage City. Cities only; older records load false,
        /// so no migration is needed.
        /// </summary>
        public bool CrittersHidden { get; set; }
    }
}
