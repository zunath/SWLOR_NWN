using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>The six ambient-sound behaviors and their raw-field classifier.</summary>
    public static class SoundBehaviorCatalog
    {
        public const string PointLoopId = "point_loop";
        public const string PointAmbienceId = "point_ambience";
        public const string AreaLoopId = "area_loop";
        public const string AreaAmbienceId = "area_ambience";
        public const string ScatteredAmbienceId = "scattered_ambience";
        public const string CustomId = "custom";

        private static readonly IReadOnlyList<BehaviorChoice> PlayOrderChoices = new[]
        {
            new BehaviorChoice(0, "Sequential"),
            new BehaviorChoice(1, "Random")
        };

        private static readonly IReadOnlyList<BehaviorChoice> TimeChoices = new[]
        {
            new BehaviorChoice(1, "Day"),
            new BehaviorChoice(2, "Night"),
            new BehaviorChoice(3, "Both")
        };

        public static IReadOnlyList<SoundBehavior> All { get; } = Build();

        public static SoundBehavior Custom => Get(CustomId);

        public static SoundBehavior Get(string id) =>
            All.FirstOrDefault(behavior => behavior.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "No such sound behavior.");

        /// <summary>Classifies in engine-significant order, including the unsupported play-once mode.</summary>
        public static SoundBehavior Classify(JsonGffStruct sound)
        {
            ArgumentNullException.ThrowIfNull(sound);

            var continuous = sound.GetIntOrNull("Continuous") ?? 0;
            var looping = sound.GetIntOrNull("Looping") ?? 0;
            if (continuous == 0 && looping == 0)
                return Custom;

            var positional = sound.GetIntOrNull("Positional") ?? 0;
            if (positional == 0)
                return Get(looping == 1 ? AreaLoopId : AreaAmbienceId);

            var randomPosition = sound.GetIntOrNull("RandomPosition") ?? 0;
            if (randomPosition == 1)
                return Get(ScatteredAmbienceId);

            return Get(looping == 1 ? PointLoopId : PointAmbienceId);
        }

        private static IReadOnlyList<SoundBehavior> Build()
        {
            return new[]
            {
                new SoundBehavior
                {
                    Id = PointLoopId,
                    DisplayName = "Point Loop",
                    Group = "POINT",
                    Fields = new[]
                    {
                        Sounds(maxItems: 1),
                        Integer("Volume", "Volume", GffFieldType.Byte),
                        Float("Audible within", "MaxDistance"),
                        Float("Full volume within", "MinDistance"),
                        Float("Height", "Elevation"),
                        Times()
                    },
                    Manages = new[]
                    {
                        Byte("Positional", 1),
                        Byte("RandomPosition", 0),
                        Byte("Looping", 1),
                        Byte("Continuous", 0),
                        Dword("Interval", 0),
                        Dword("IntervalVrtn", 0),
                        FloatValue("PitchVariation", 0),
                        Byte("Priority", 3)
                    }
                },
                new SoundBehavior
                {
                    Id = PointAmbienceId,
                    DisplayName = "Point Ambience",
                    Group = "POINT",
                    Fields = new[]
                    {
                        Sounds(),
                        PlayOrder(),
                        Integer("Play every (seconds)", "Interval", GffFieldType.Dword),
                        Integer("Variation (seconds)", "IntervalVrtn", GffFieldType.Dword),
                        Float("Pitch variation", "PitchVariation"),
                        Integer("Volume", "Volume", GffFieldType.Byte),
                        Float("Audible within", "MaxDistance"),
                        Float("Height", "Elevation"),
                        Times()
                    },
                    Manages = new[]
                    {
                        Byte("Positional", 1),
                        Byte("RandomPosition", 0),
                        Byte("Continuous", 1),
                        Byte("Looping", 0),
                        Byte("Priority", 20)
                    }
                },
                new SoundBehavior
                {
                    Id = AreaLoopId,
                    DisplayName = "Area Loop",
                    Group = "AREA",
                    Fields = new[]
                    {
                        Sounds(maxItems: 1),
                        Integer("Volume", "Volume", GffFieldType.Byte),
                        Times()
                    },
                    Manages = new[]
                    {
                        Byte("Positional", 0),
                        Byte("Looping", 1),
                        Byte("Continuous", 0),
                        Dword("Interval", 0),
                        Dword("IntervalVrtn", 0),
                        FloatValue("PitchVariation", 0),
                        Byte("Priority", 2)
                    }
                },
                new SoundBehavior
                {
                    Id = AreaAmbienceId,
                    DisplayName = "Area Ambience",
                    Group = "AREA",
                    Fields = new[]
                    {
                        Sounds(),
                        PlayOrder(),
                        Integer("Play every (seconds)", "Interval", GffFieldType.Dword),
                        Integer("Variation (seconds)", "IntervalVrtn", GffFieldType.Dword),
                        Float("Pitch variation", "PitchVariation"),
                        Integer("Volume", "Volume", GffFieldType.Byte),
                        Times()
                    },
                    Manages = new[]
                    {
                        Byte("Positional", 0),
                        Byte("Continuous", 1),
                        Byte("Looping", 0),
                        Byte("Priority", 19)
                    }
                },
                new SoundBehavior
                {
                    Id = ScatteredAmbienceId,
                    DisplayName = "Scattered Ambience",
                    Group = "AREA",
                    Fields = new[]
                    {
                        Sounds(),
                        Integer("Play every (seconds)", "Interval", GffFieldType.Dword),
                        Integer("Variation (seconds)", "IntervalVrtn", GffFieldType.Dword),
                        Float("W-E scatter range", "RandomRangeX"),
                        Float("N-S scatter range", "RandomRangeY"),
                        Float("Pitch variation", "PitchVariation"),
                        Integer("Volume", "Volume", GffFieldType.Byte),
                        Float("Audible within", "MaxDistance"),
                        Float("Height", "Elevation"),
                        Times()
                    },
                    Manages = new[]
                    {
                        Byte("Positional", 1),
                        Byte("RandomPosition", 1),
                        Byte("Continuous", 1),
                        Byte("Looping", 0),
                        Byte("Random", 1),
                        Byte("Priority", 20)
                    }
                },
                new SoundBehavior
                {
                    Id = CustomId,
                    DisplayName = "Custom",
                    Fields = new[]
                    {
                        Sounds(),
                        PlayOrder(),
                        Integer("Interval variation (seconds)", "IntervalVrtn", GffFieldType.Dword),
                        Float("Pitch variation", "PitchVariation"),
                        Integer("Volume", "Volume", GffFieldType.Byte),
                        Float("Audible within", "MaxDistance"),
                        Float("Full volume within", "MinDistance"),
                        Float("Height", "Elevation"),
                        Float("W-E scatter range", "RandomRangeX"),
                        Float("N-S scatter range", "RandomRangeY"),
                        Times()
                    }.Concat(SoundEditorLayout.RawPlaybackFields).ToList(),
                    AllowsVariables = true
                }
            };
        }

        private static BehaviorFieldDefinition Sounds(int maxItems = 0) => new()
        {
            Label = maxItems == 1 ? "Sound" : "Sounds",
            Name = SoundValueStore.SoundsField,
            Kind = BehaviorFieldKind.SoundList,
            FieldType = GffFieldType.List,
            IsRequired = true,
            MaxItems = maxItems
        };

        private static BehaviorFieldDefinition PlayOrder() => new()
        {
            Label = "Play order",
            Name = "Random",
            Kind = BehaviorFieldKind.Choice,
            FieldType = GffFieldType.Byte,
            Choices = PlayOrderChoices
        };

        private static BehaviorFieldDefinition Times() => new()
        {
            Label = "When it plays",
            Name = "Times",
            Kind = BehaviorFieldKind.Choice,
            FieldType = GffFieldType.Byte,
            Choices = TimeChoices
        };

        private static BehaviorFieldDefinition Integer(string label, string name, GffFieldType type) => new()
        {
            Label = label,
            Name = name,
            Kind = BehaviorFieldKind.Integer,
            FieldType = type
        };

        private static BehaviorFieldDefinition Float(string label, string name) => new()
        {
            Label = label,
            Name = name,
            Kind = BehaviorFieldKind.Float,
            FieldType = GffFieldType.Float
        };

        private static BehaviorManagedValue Byte(string name, long value) => new()
        {
            Label = name,
            Name = name,
            FieldType = GffFieldType.Byte,
            IntValue = value
        };

        private static BehaviorManagedValue Dword(string name, long value) => new()
        {
            Label = name,
            Name = name,
            FieldType = GffFieldType.Dword,
            IntValue = value
        };

        private static BehaviorManagedValue FloatValue(string name, double value) => new()
        {
            Label = name,
            Name = name,
            FieldType = GffFieldType.Float,
            FloatValue = value
        };
    }
}
