using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Waypoints
{
    public sealed class WaypointBehaviorCatalog
    {
        public const string CreatureSpawnPointId = "creature_spawn_point";
        public const string FishingPointId = "fishing_point";
        public const string MapNoteId = "map_note";
        public const string StuckRescuePointId = "stuck_rescue_point";
        public const string TransitionDestinationId = "transition_destination";
        public const string PlanetLandingId = "planet_landing";
        public const string OrbitPointId = "orbit_point";
        public const string TaxiStopId = "taxi_stop";
        public const string StarshipDockId = "starship_dock";
        public const string PropertyEntranceId = "property_entrance";
        public const string DeathRespawnId = "death_respawn";
        public const string RebuildId = "rebuild";
        public const string CustomId = "custom";

        /// <summary>
        /// Toolset metadata that preserves a transition destination selected before any inbound
        /// door or trigger exists. Runtime behavior still comes entirely from the waypoint's tag.
        /// </summary>
        public const string PersistedBehaviorLocal = "SWLOR_TOOLSET_BEHAVIOR";

        public const string StuckWaypointTag = "STUCK_WAYPOINT";
        public const string StarshipDockTag = "STARSHIP_DOCKPOINT";
        public const string PropertyEntranceTag = "PROPERTY_ENTRANCE";

        public const int BlueAppearance = 1;
        public const int RedAppearance = 2;
        public const int GreenAppearance = 3;

        private readonly HashSet<string> _spawnTableIds;
        private readonly HashSet<string> _fishingSpawnTableIds;
        private readonly HashSet<string> _transitionDestinationTags;
        private readonly HashSet<string> _landingTags;
        private readonly HashSet<string> _orbitTags;
        private readonly HashSet<string> _taxiTags;
        private readonly HashSet<string> _deathRespawnTags;
        private readonly HashSet<string> _rebuildTags;

        public IReadOnlyList<WaypointBehavior> All { get; }

        public WaypointBehavior Custom => Get(CustomId);

        public WaypointBehaviorCatalog(
            IGameCodeIndex? gameCodeIndex,
            IEnumerable<string>? transitionDestinationTags)
        {
            _fishingSpawnTableIds = Set(gameCodeIndex?.FishingSpawnTableIds);
            _spawnTableIds = Set(gameCodeIndex?.SpawnTableIds);
            _spawnTableIds.ExceptWith(_fishingSpawnTableIds);
            _transitionDestinationTags = Set(transitionDestinationTags);
            _landingTags = Set(gameCodeIndex?.PlanetLandingWaypoints.Select(value => value.Tag));
            _orbitTags = Set(gameCodeIndex?.OrbitWaypoints.Select(value => value.Tag));
            _taxiTags = Set(gameCodeIndex?.TaxiDestinations.Select(value => value.Tag));
            _deathRespawnTags = Set(gameCodeIndex?.DeathRespawnWaypointTags);
            _rebuildTags = Set(gameCodeIndex?.RebuildWaypointTags);

            All = Build(gameCodeIndex);
        }

        public WaypointBehavior Get(string id) =>
            All.FirstOrDefault(behavior => behavior.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "No such waypoint behavior.");

        /// <summary>
        /// True for runtime destination tags that must identify exactly one placed waypoint across
        /// the module. Repeatable rescue, dock, property, spawn, map-note, and generic transition
        /// markers are deliberately excluded.
        /// </summary>
        public bool IsSingletonDestinationTag(string? tag) =>
            !string.IsNullOrWhiteSpace(tag) &&
            (_landingTags.Contains(tag) ||
             _orbitTags.Contains(tag) ||
             _taxiTags.Contains(tag) ||
             _deathRespawnTags.Contains(tag) ||
             _rebuildTags.Contains(tag));

        public WaypointBehavior Classify(JsonGffStruct waypoint)
        {
            ArgumentNullException.ThrowIfNull(waypoint);

            if (string.Equals(
                    new VarTable(waypoint).GetString(PersistedBehaviorLocal),
                    TransitionDestinationId,
                    StringComparison.Ordinal))
            {
                return Get(TransitionDestinationId);
            }

            if ((waypoint.GetIntOrNull("HasMapNote") ?? 0) == 1)
                return Get(MapNoteId);

            var tag = waypoint.GetStringOrNull("Tag") ?? string.Empty;
            if (_fishingSpawnTableIds.Contains(tag))
                return Get(FishingPointId);
            if (string.Equals(tag, StuckWaypointTag, StringComparison.Ordinal))
                return Get(StuckRescuePointId);
            if (string.Equals(tag, PropertyEntranceTag, StringComparison.Ordinal))
                return Get(PropertyEntranceId);
            if (string.Equals(tag, StarshipDockTag, StringComparison.Ordinal))
                return Get(StarshipDockId);
            if (_landingTags.Contains(tag))
                return Get(PlanetLandingId);
            if (_orbitTags.Contains(tag))
                return Get(OrbitPointId);
            if (_taxiTags.Contains(tag))
                return Get(TaxiStopId);
            if (_deathRespawnTags.Contains(tag))
                return Get(DeathRespawnId);
            if (_rebuildTags.Contains(tag))
                return Get(RebuildId);
            if (_spawnTableIds.Contains(tag))
                return Get(CreatureSpawnPointId);
            if (_transitionDestinationTags.Contains(tag))
                return Get(TransitionDestinationId);

            return Custom;
        }

        private IReadOnlyList<WaypointBehavior> Build(IGameCodeIndex? gameCodeIndex)
        {
            var blue = Appearance(BlueAppearance, "Blue");
            var red = Appearance(RedAppearance, "Red");
            var green = Appearance(GreenAppearance, "Green");

            return new[]
            {
                new WaypointBehavior
                {
                    Id = CreatureSpawnPointId,
                    DisplayName = "Creature Spawn Point",
                    Group = "SPAWNING",
                    Summary = "Spawns the selected creature table at this waypoint.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Spawn table",
                            SpawnChoices(_spawnTableIds, gameCodeIndex?.SpawnTables),
                            "Tag",
                            searchable: true),
                        Statement("Marker", "Red")
                    },
                    Manages = new[] { red }
                },
                new WaypointBehavior
                {
                    Id = FishingPointId,
                    DisplayName = "Fishing Point",
                    Group = "SPAWNING",
                    Summary = "Spawns a fishing point for the selected fishing location.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Fishing location",
                            SpawnChoices(_fishingSpawnTableIds, gameCodeIndex?.FishingSpawnTables),
                            "Tag",
                            searchable: true),
                        Statement("Marker", "Green")
                    },
                    Manages = new[] { green }
                },
                new WaypointBehavior
                {
                    Id = MapNoteId,
                    DisplayName = "Map Note",
                    Group = "WORLD",
                    Summary = "Labels a location on the player's area map.",
                    Fields = new[]
                    {
                        new BehaviorFieldDefinition
                        {
                            Label = "Map note text", Name = "MapNote",
                            Kind = BehaviorFieldKind.LocalizedText,
                            FieldType = GffFieldType.CExoLocString,
                            IsRequired = true
                        },
                        Statement("Shown on map", "Always"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[]
                    {
                        new BehaviorManagedValue
                        {
                            Label = "Has Map Note", Name = "HasMapNote",
                            FieldType = GffFieldType.Byte, IntValue = 1
                        },
                        new BehaviorManagedValue
                        {
                            Label = "Shown on Map", Name = "MapNoteEnabled",
                            FieldType = GffFieldType.Byte, IntValue = 1
                        },
                        blue
                    }
                },
                new WaypointBehavior
                {
                    Id = StuckRescuePointId,
                    DisplayName = "Stuck Rescue Point",
                    Group = "WORLD",
                    Summary = "A repeatable per-area rescue destination used by the stuck command.",
                    Fields = new[]
                    {
                        Statement("Tag", StuckWaypointTag),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[]
                    {
                        Tag(StuckWaypointTag),
                        blue
                    }
                },
                new WaypointBehavior
                {
                    Id = TransitionDestinationId,
                    DisplayName = "Transition Destination",
                    Group = "MOVEMENT",
                    Summary = "Where a trigger or door area transition puts the player down.",
                    Fields = new[]
                    {
                        StringText("Destination tag", "Tag"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[] { blue }
                },
                new WaypointBehavior
                {
                    Id = PlanetLandingId,
                    DisplayName = "Planet Landing",
                    Group = "TRAVEL",
                    Summary = "A unique landing destination for one planet.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Planet",
                            Choices(gameCodeIndex?.PlanetLandingWaypoints.Select(
                                value => (value.Tag, value.DisplayName))),
                            "Tag"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[] { blue }
                },
                new WaypointBehavior
                {
                    Id = OrbitPointId,
                    DisplayName = "Orbit Point",
                    Group = "TRAVEL",
                    Summary = "A unique orbital destination for one planet.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Planet",
                            Choices(gameCodeIndex?.OrbitWaypoints.Select(
                                value => (value.Tag, value.DisplayName))),
                            "Tag"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[] { blue }
                },
                new WaypointBehavior
                {
                    Id = TaxiStopId,
                    DisplayName = "Taxi Stop",
                    Group = "TRAVEL",
                    Summary = "A unique destination offered by the taxi network.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Taxi stop",
                            Choices(gameCodeIndex?.TaxiDestinations.Select(value =>
                                (value.Tag, value.DisplayName))),
                            "Tag"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[] { blue }
                },
                new WaypointBehavior
                {
                    Id = StarshipDockId,
                    DisplayName = "Starship Dock",
                    Group = "TRAVEL",
                    Summary = "A repeatable docking point inside a starport.",
                    Fields = new[]
                    {
                        Statement("Tag", StarshipDockTag),
                        Statement("Planet", "Determined by the containing area"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[]
                    {
                        Tag(StarshipDockTag),
                        blue
                    }
                },
                new WaypointBehavior
                {
                    Id = PropertyEntranceId,
                    DisplayName = "Property Entrance",
                    Group = "TRAVEL",
                    Summary = "A repeatable entrance point inside a property layout.",
                    Fields = new[]
                    {
                        Statement("Tag", PropertyEntranceTag),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[]
                    {
                        Tag(PropertyEntranceTag),
                        blue
                    }
                },
                new WaypointBehavior
                {
                    Id = DeathRespawnId,
                    DisplayName = "Death Respawn",
                    Group = "TRAVEL",
                    Summary = "A unique fallback destination used by the death system.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Death respawn",
                            Choices(_deathRespawnTags.Select(tag => (tag, DeathRespawnName(tag)))),
                            "Tag"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[] { blue }
                },
                new WaypointBehavior
                {
                    Id = RebuildId,
                    DisplayName = "Rebuild",
                    Group = "TRAVEL",
                    Summary = "A unique destination used while entering or leaving character rebuild.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Rebuild destination",
                            Choices(_rebuildTags.Select(tag => (tag, RebuildName(tag)))),
                            "Tag"),
                        Statement("Marker", "Blue")
                    },
                    Manages = new[] { blue }
                },
                new WaypointBehavior
                {
                    Id = CustomId,
                    DisplayName = "Custom",
                    AllowsVariables = true,
                    Fields = new[]
                    {
                        new BehaviorFieldDefinition
                        {
                            Label = "Custom",
                            Name = string.Empty,
                            Kind = BehaviorFieldKind.Statement,
                            Note = "Custom exposes raw fields and local variables."
                        }
                    }.Concat(WaypointEditorLayout.Custom).ToList()
                }
            };
        }

        private static BehaviorFieldDefinition StringText(
            string label,
            string name) =>
            new()
            {
                Label = label,
                Name = name,
                Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString,
                IsRequired = true
            };

        private static BehaviorFieldDefinition StringChoice(
            string label,
            IReadOnlyList<BehaviorChoice> choices,
            string name,
            bool searchable = false) =>
            new()
            {
                Label = label,
                Name = name,
                Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.CExoString,
                IsRequired = true,
                Choices = choices,
                IsSearchable = searchable
            };

        private static BehaviorFieldDefinition Statement(string label, string note) =>
            new()
            {
                Label = label,
                Name = string.Empty,
                Kind = BehaviorFieldKind.Statement,
                Note = note
            };

        private static IReadOnlyList<BehaviorChoice> Choices(
            IEnumerable<(string Value, string Display)>? values)
        {
            if (values == null)
                return Array.Empty<BehaviorChoice>();

            return values
                .OrderBy(value => value.Display, StringComparer.OrdinalIgnoreCase)
                .Select(value => new BehaviorChoice(value.Value, value.Display))
                .ToList();
        }

        private static IReadOnlyList<BehaviorChoice> SpawnChoices(
            IEnumerable<string> ids,
            IEnumerable<SpawnTableInfo>? tables)
        {
            var names = (tables ?? Array.Empty<SpawnTableInfo>())
                .ToDictionary(table => table.Id, table => table.DisplayName, StringComparer.Ordinal);

            return Choices(ids.Select(id =>
            {
                var name = names.GetValueOrDefault(id);
                return (id, string.IsNullOrWhiteSpace(name) ? id : $"{name} ({id})");
            }));
        }

        private static string DeathRespawnName(string tag) =>
            tag == "DEATH_DEFAULT_RESPAWN_POINT"
                ? "Initial default respawn"
                : "Fallback when a saved respawn area is unavailable";

        private static string RebuildName(string tag) =>
            tag == "REBUILD_LANDING"
                ? "Enter rebuild area"
                : "Return to spending area";

        private static BehaviorManagedValue Appearance(int id, string color) =>
            new()
            {
                Label = "Appearance",
                Name = "Appearance",
                FieldType = GffFieldType.Byte,
                IntValue = id,
                Display = color
            };

        private static BehaviorManagedValue Tag(string value) =>
            new()
            {
                Label = "Tag",
                Name = "Tag",
                FieldType = GffFieldType.CExoString,
                StringValue = value
            };

        private static HashSet<string> Set(IEnumerable<string>? values) =>
            new(values ?? Array.Empty<string>(), StringComparer.Ordinal);
    }
}
