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
        public const string RespawnPointId = "respawn_point";
        public const string CustomId = "custom";

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
        private readonly HashSet<string> _respawnTags;

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
            _respawnTags = Set(gameCodeIndex?.RespawnWaypointTags);

            All = Build(gameCodeIndex);
        }

        public WaypointBehavior Get(string id) =>
            All.FirstOrDefault(behavior => behavior.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "No such waypoint behavior.");

        public WaypointBehavior Classify(JsonGffStruct waypoint)
        {
            ArgumentNullException.ThrowIfNull(waypoint);

            if ((waypoint.GetIntOrNull("HasMapNote") ?? 0) == 1)
                return Get(MapNoteId);

            var tag = waypoint.GetStringOrNull("Tag") ?? string.Empty;
            if (_fishingSpawnTableIds.Contains(tag))
                return Get(FishingPointId);
            if (string.Equals(tag, StuckWaypointTag, StringComparison.Ordinal))
                return Get(StuckRescuePointId);
            if (_transitionDestinationTags.Contains(tag))
                return Get(TransitionDestinationId);
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
            if (_respawnTags.Contains(tag))
                return Get(RespawnPointId);
            if (_spawnTableIds.Contains(tag))
                return Get(CreatureSpawnPointId);

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
                            Choices(_spawnTableIds.Select(id => (id, id))),
                            "Tag"),
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
                            Choices(_fishingSpawnTableIds.Select(id => (id, id))),
                            "Tag"),
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
                        new BehaviorFieldDefinition
                        {
                            Label = "Shown on map", Name = "MapNoteEnabled",
                            Kind = BehaviorFieldKind.Check,
                            FieldType = GffFieldType.Byte
                        },
                        Statement("Marker", "Blue")
                    },
                    Manages = new[]
                    {
                        new BehaviorManagedValue
                        {
                            Label = "Has Map Note", Name = "HasMapNote",
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
                        StringChoice(
                            "Destination tag",
                            Choices(_transitionDestinationTags.Select(tag => (tag, tag))),
                            "Tag"),
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
                                (value.Tag, $"{value.DisplayName} — region {value.RegionId}, {value.Price} credits"))),
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
                    Id = RespawnPointId,
                    DisplayName = "Respawn Point",
                    Group = "TRAVEL",
                    Summary = "A unique destination used by death or rebuild recovery.",
                    Fields = new[]
                    {
                        StringChoice(
                            "Respawn point",
                            Choices(_respawnTags.Select(tag => (tag, tag))),
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
                            Label = "Legacy waypoint",
                            Name = string.Empty,
                            Kind = BehaviorFieldKind.Statement,
                            Note = "Custom exposes raw fields and local variables. Existing locals may be consumed by legacy scripts."
                        }
                    }
                }
            };
        }

        private static BehaviorFieldDefinition StringChoice(
            string label,
            IReadOnlyList<BehaviorChoice> choices,
            string name) =>
            new()
            {
                Label = label,
                Name = name,
                Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.CExoString,
                IsRequired = true,
                Choices = choices
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
