using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>One selectable option of a 2DA-backed dropdown.</summary>
    public sealed record LookupOption(long Id, string Display, bool ShowId = true)
    {
        /// <summary>
        /// Behavior editors put an optional id after the readable name; generic dropdowns put it
        /// before. Both honor the same presentation rule so a lookup cannot drift between editors.
        /// </summary>
        public string BehaviorDisplay => ShowId ? $"{Display} ({Id})" : Display;

        public override string ToString() => ShowId ? $"{Id}: {Display}" : Display;
    }

    /// <summary>
    /// Maps schema LookupKeys to option lists built from the lookup services. Every service is
    /// optional — a missing service (or unknown key) yields an empty list, and the editor
    /// degrades that field to a plain numeric box.
    /// </summary>
    /// <remarks>
    /// Note that an unhandled key degrades SILENTLY to a numeric box, which is how Door Type,
    /// placeable Appearance, and ambient Sound spent a while rendering as raw ids even though their
    /// schemas already declared dropdowns and their services already existed — the switch below
    /// simply had no case for them. When adding a lookup key, wire it here too; the
    /// LookupOptionProviderTests coverage exists to catch exactly that omission.
    /// </remarks>
    public sealed class LookupOptionProvider
    {
        private readonly AppearanceService? _appearances;
        private readonly PortraitService? _portraits;
        private readonly PlaceableAppearanceService? _placeables;
        private readonly DoorTypeService? _doorTypes;
        private readonly SoundService? _sounds;
        private readonly TwoDaLookupService? _twoDaLookups;
        private readonly WorkspaceContext _workspaceContext;
        private readonly Dictionary<string, IReadOnlyList<LookupOption>> _cache = new(StringComparer.OrdinalIgnoreCase);

        public LookupOptionProvider(
            WorkspaceContext workspaceContext,
            AppearanceService? appearances = null,
            PortraitService? portraits = null,
            PlaceableAppearanceService? placeables = null,
            DoorTypeService? doorTypes = null,
            SoundService? sounds = null,
            TwoDaLookupService? twoDaLookups = null,
            WaypointAppearanceService? waypointAppearances = null)
        {
            _workspaceContext = workspaceContext;
            _appearances = appearances;
            _portraits = portraits;
            _placeables = placeables;
            _doorTypes = doorTypes;
            _sounds = sounds;
            _twoDaLookups = twoDaLookups;
            _waypointAppearances = waypointAppearances;
        }

        private readonly WaypointAppearanceService? _waypointAppearances;

        public IReadOnlyList<LookupOption> GetOptions(string? lookupKey)
        {
            if (string.IsNullOrEmpty(lookupKey))
                return Array.Empty<LookupOption>();

            if (_cache.TryGetValue(lookupKey, out var cached))
                return cached;

            var options = Build(lookupKey);
            _cache[lookupKey] = options;
            return options;
        }

        /// <summary>Discards lists derived from the active HAK/2DA/TLK stack.</summary>
        public void Invalidate() => _cache.Clear();

        private IReadOnlyList<LookupOption> Build(string lookupKey)
        {
            try
            {
                switch (lookupKey)
                {
                    case LookupKeys.Appearance when _appearances != null:
                        return _appearances.GetAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.Portraits when _portraits != null:
                        return _portraits.GetAll()
                            .Select(row => new LookupOption(row.Id, row.BaseResRef))
                            .ToList();
                    case LookupKeys.Placeables when _placeables != null:
                        return _placeables.GetAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.DoorTypes when _doorTypes != null:
                        return _doorTypes.GetAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.GenericDoors when _doorTypes != null:
                        return _doorTypes.GetGenericAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.AmbientSounds when _sounds != null:
                        return _sounds.GetAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.Gender:
                        return FromTable(TwoDaLookupTables.Gender, showId: false);
                    case LookupKeys.Phenotype:
                        return FromTable(TwoDaLookupTables.Phenotype, showId: false);
                    case LookupKeys.SoundSets:
                        return FromTable(TwoDaLookupTables.SoundSet, showId: false);
                    case LookupKeys.BaseItems:
                        return FromTable(TwoDaLookupTables.BaseItem);
                    case LookupKeys.LoadScreens:
                        return FromTable(TwoDaLookupTables.LoadScreen);
                    case LookupKeys.Races:
                        return FromTable(TwoDaLookupTables.Race, showId: false);
                    case LookupKeys.CreatureMovementRates:
                        return FromTable(TwoDaLookupTables.CreatureSpeed, showId: false);
                    case LookupKeys.TriggerTypes:
                        return TriggerTypeOptions;
                    case LookupKeys.WaypointAppearances when _waypointAppearances != null:
                        return _waypointAppearances.GetAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.Factions:
                        return BuildFactions();
                    default:
                        return Array.Empty<LookupOption>();
                }
            }
            catch (Exception)
            {
                // A malformed lookup source must never break the editor; degrade to numeric.
                return Array.Empty<LookupOption>();
            }
        }

        private IReadOnlyList<LookupOption> FromTable(TwoDaLookupTable table, bool showId = true)
        {
            if (_twoDaLookups == null)
                return Array.Empty<LookupOption>();

            return _twoDaLookups.GetRows(table)
                .Select(row => new LookupOption(row.Id, row.DisplayName, showId))
                .ToList();
        }

        /// <summary>Trigger Type is a fixed engine enum rather than a 2DA table.</summary>
        /// <remarks>
        /// 1 is the area transition and 2 is the trap, not the other way round. These were reversed,
        /// which is worse than a wrong label: picking "Trap" wrote 1 and turned the trigger into an
        /// area transition, and picking "Area Transition" wrote 2 and made it a trap - so editing this
        /// dropdown changed the object into the opposite of what was chosen.
        /// <para>
        /// The module's own data settles it. <c>pitfalltrap.utt.json</c> carries <c>Type=2</c> with
        /// <c>TrapFlag=1</c>, <c>TrapDetectable=1</c> and <c>TrapType=122</c>, and an empty
        /// <c>LinkedTo</c> - a trap in every field but the one this list was naming. The other six
        /// checked-in triggers are all <c>Type=0</c>.
        /// </para>
        /// </remarks>
        private static readonly IReadOnlyList<LookupOption> TriggerTypeOptions = new[]
        {
            new LookupOption(0, "Generic"),
            new LookupOption(1, "Area Transition"),
            new LookupOption(2, "Trap")
        };

        /// <summary>Faction ids come from the module's repute.fac: FactionList index = id.</summary>
        private IReadOnlyList<LookupOption> BuildFactions()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<LookupOption>();

            var facPath = Path.Combine(workspace.ModuleRoot, "fac", "repute.fac.json");
            if (!File.Exists(facPath))
                return Array.Empty<LookupOption>();

            var document = JsonGffDocument.Load(facPath);
            var factionList = document.Root.GetOrNull("FactionList");
            if (factionList?.Elements == null)
                return Array.Empty<LookupOption>();

            var options = new List<LookupOption>(factionList.Elements.Count);
            for (var i = 0; i < factionList.Elements.Count; i++)
            {
                var name = factionList.Elements[i].GetOrNull("FactionName")?.GetString() ?? $"Faction {i}";
                options.Add(new LookupOption(i, name, ShowId: false));
            }

            return options;
        }
    }
}
