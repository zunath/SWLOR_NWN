using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Beast incubation: using an incubator, rolling a mutation, the tester weekday overrides, creating the beast egg
    /// with the job's purities and its vendor bonus, DNA extraction objects and the removal of a property's jobs. The
    /// weekday overrides are runtime state of this instance. The perk read and the window are seams, so a unit test can
    /// answer them.
    /// </summary>
    public class BeastIncubationService : IBeastIncubationService
    {
        private const string BeastEggResref = BeastMasteryService.BeastEggResref;

        [ReloadState(ReloadStateKind.Runtime)]
        private readonly ConcurrentDictionary<string, DayOfWeek> _incubationDayOverrides = new();
        private readonly IDBService _db;
        private readonly IPropertyService _property;
        private readonly IIncubationFieldNoteService _incubationFieldNotes;
        private readonly IResistanceService _resistance;
        private readonly ICombatDamageCatalogService _damageCatalog;
        private readonly IPerkService _perk;
        private readonly IGuiService _gui;
        private readonly IBeastMasteryService _beastMastery;

        public BeastIncubationService(
            IDBService db,
            IPropertyService property,
            IIncubationFieldNoteService incubationFieldNotes,
            IResistanceService resistance,
            ICombatDamageCatalogService damageCatalog,
            IPerkService perk,
            IGuiService gui,
            IBeastMasteryService beastMastery)
        {
            _db = db;
            _property = property;
            _incubationFieldNotes = incubationFieldNotes;
            _resistance = resistance;
            _damageCatalog = damageCatalog;
            _perk = perk;
            _gui = gui;
            _beastMastery = beastMastery;
        }


        [NWNEventHandler(ScriptName.OnIncubatorTerminal)]
        public void UseIncubator()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var incubator = OBJECT_SELF;
            var dnaManipulationLevel = GetPerkLevel(player, PerkType.DNAManipulation);

            if (dnaManipulationLevel <= 0)
            {
                SendMessageToPC(player, $"Perk 'DNA Manipulation I' is required to use incubators.");
                return;
            }

            var incubatorPropertyId = _property.GetPropertyId(incubator);

            if (string.IsNullOrWhiteSpace(incubatorPropertyId))
            {
                SendMessageToPC(player, $"This incubator cannot be used.");
                return;
            }

            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), incubatorPropertyId, false);
            var incubatorJob = _db.Search(dbQuery).FirstOrDefault();

            if (incubatorJob != null && incubatorJob.PlayerId != playerId)
            {
                var now = DateTime.UtcNow;
                if (incubatorJob.DateCompleted > now)
                {
                    var delta = incubatorJob.DateCompleted - now;
                    var completionTime = Time.GetTimeLongIntervals(delta, false);
                    SendMessageToPC(player, $"Another player's incubation job is active. This job will complete in: {completionTime}.");
                }
                else
                {
                    SendMessageToPC(player, $"Another player's incubation job is active. This job has completed.");
                }

                return;
            }

            var payload = new IncubatorPayload(incubatorPropertyId, incubatorJob?.Id ?? string.Empty);
            TogglePlayerWindow(player, GuiWindowType.Incubator, payload, player);
        }

        private BeastType DetermineMutation(BeastType beastType, IncubationJob job)
        {
            if (job.IsMutationSuppressed)
                return BeastType.Invalid;

            var beast = _beastMastery.GetBeastDetail(beastType);

            // Random.Next(1000) returns 0-999, so a chance of N (tenths of a percent) succeeds N times in 1000.
            if (Random.Next(1000) < job.MutationChance)
            {
                var possibleMutations = IncubationProjection.GetEligibleMutations(beast, job);

                if (possibleMutations.Count > 0)
                {
                    var weights = possibleMutations.Select(x => x.Weight);
                    var index = Random.GetRandomWeightedIndex(weights.ToArray());

                    return possibleMutations.ElementAt(index).Type;
                }
            }

            return BeastType.Invalid;
        }

        public DayOfWeek GetIncubationServerDay(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) && _incubationDayOverrides.TryGetValue(playerId, out var day)
                ? day
                : DateTime.UtcNow.DayOfWeek;
        }

        public bool HasIncubationDayOverride(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) && _incubationDayOverrides.ContainsKey(playerId);
        }

        public void SetIncubationDayOverride(string playerId, DayOfWeek day)
        {
            _incubationDayOverrides[playerId] = day;
        }

        public void ClearIncubationDayOverride(string playerId)
        {
            _incubationDayOverrides.TryRemove(playerId, out _);
        }

        public void CreateBeastEgg(IncubationJob job, uint player)
        {
            var mutation = DetermineMutation(job.BeastDNAType, job);
            var beastType = mutation == BeastType.Invalid ? job.BeastDNAType : mutation;

            // A successful mutation reveals the field note for the beast the player just produced,
            // documenting every way to incubate it. No-op if already owned.
            if (mutation != BeastType.Invalid)
            {
                _incubationFieldNotes.GrantDiscoveredNote(player, mutation);
            }

            var egg = CreateBeastEgg(beastType, player);
            var itemProperties = new List<ItemProperty>
            {
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, job.AttackPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, job.AccuracyPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.EvasionPurity, job.EvasionPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.LearningPurity, job.LearningPurity),
            };

            foreach (var damageType in _damageCatalog.GetDefenseDamageTypes())
            {
                if (!BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(damageType, out var statType))
                    continue;

                itemProperties.Add(ItemPropertyCustom(ItemPropertyType.Incubation, (int)statType, BeastResistanceCalculator.GetDefensePurity(job, damageType)));
            }

            foreach (var resistanceType in _resistance.GetAllResistanceTypes())
            {
                if (!BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(resistanceType, out var statType))
                    continue;

                itemProperties.Add(ItemPropertyCustom(ItemPropertyType.Incubation, (int)statType, GetResistancePurity(job, resistanceType)));
            }

            itemProperties.Add(ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.XPPenalty, job.XPPenalty));

            foreach (var ip in itemProperties)
            {
                BiowareXP2.IPSafeAddItemProperty(egg, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }

            var addGoldPiece = CalculateEggVendorBonus(job);
            ItemPlugin.SetAddGoldPieceValue(egg, addGoldPiece);

            _db.Delete<IncubationJob>(job.Id);
        }

        public uint CreateBeastEgg(BeastType beastType, uint player)
        {
            var egg = CreateItemOnObject(BeastEggResref, player);
            var dnaType = ItemPropertyCustom(ItemPropertyType.DNAType, (int)beastType);
            BiowareXP2.IPSafeAddItemProperty(egg, dnaType, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            var beastDetail = _beastMastery.GetBeastDetail(beastType);
            SetName(egg, $"Beast Egg: {beastDetail.Name}");

            return egg;
        }

        private int CalculateEggVendorBonus(IncubationJob job)
        {
            const float maxPurityValue = 1000f;
            var purities = new List<int>
            {
                job.AttackPurity,
                job.AccuracyPurity,
                job.EvasionPurity,
                job.LearningPurity,
            };

            purities.AddRange(_damageCatalog.GetDefenseDamageTypes().Select(type => BeastResistanceCalculator.GetDefensePurity(job, type)));
            purities.AddRange(_resistance.GetAllResistanceTypes().Select(type => GetResistancePurity(job, type)));

            var averagePurity = purities.Average();
            var qualityPercent = averagePurity / maxPurityValue;
            var xpPenaltyAdjustment = 1f - (job.XPPenalty / maxPurityValue);

            const int baseVendorBonus = 180;
            const int qualityVendorRange = 1170;
            var addGoldPiece = (int)Math.Round(baseVendorBonus + (qualityVendorRange * qualityPercent * xpPenaltyAdjustment));

            return Math.Max(baseVendorBonus, addGoldPiece);
        }

        private int GetResistancePurity(IncubationJob job, ResistanceType type)
        {
            return job.ResistancePurities != null &&
                   job.ResistancePurities.TryGetValue(type, out var purity)
                ? purity
                : 0;
        }

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public void OnRemoveProperty()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), propertyId, false);
            var dbJobs = _db.Search(dbQuery).ToList();

            foreach (var dbJob in dbJobs)
            {
                _db.Delete<IncubationJob>(dbJob.Id);
            }
        }

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDNAExtractUsed)]
        public void UseExtractDNAObject()
        {
            var player = GetLastUsedBy();
            SendMessageToPC(player, ColorToken.Red("Use a DNA Extractor on this corpse to retrieve its DNA."));
        }

        /// <summary>
        /// A creature's perk level. <c>Perk.GetPerkLevel</c> reads the database for a player and the native engine for a
        /// droid or a beast, which a unit test cannot run, so a test overrides this.
        /// </summary>
        protected virtual int GetPerkLevel(uint creature, PerkType perkType)
        {
            return _perk.GetPerkLevel(creature, perkType);
        }

        /// <summary>
        /// Opens or closes a window for a player. The original call into <c>Gui</c>, whose window table is static state.
        /// </summary>
        protected virtual void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject)
        {
            _gui.TogglePlayerWindow(player, windowType, payload, tetherObject);
        }

    }
}
