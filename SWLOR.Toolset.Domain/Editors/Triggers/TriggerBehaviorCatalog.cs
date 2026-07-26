using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// Every behavior the trigger editor offers, and the classifier that recognises which one an
    /// existing trigger already has.
    /// </summary>
    /// <remarks>
    /// Nothing here is invented: each behavior is a pattern the module already uses, and the counts
    /// in the comments are its placements across Module\git. A trigger that matches none of them
    /// classifies as Custom, which is the only behavior that exposes raw script slots and the
    /// VarTable - and which therefore also covers the do-nothing volume that used to have its own
    /// "None" entry.
    /// </remarks>
    public static class TriggerBehaviorCatalog
    {
        public const string AreaTransitionId = "area_transition";
        public const string NoSpawnZoneId = "no_spawn_zone";
        public const string ExplorationNoteId = "exploration_note";
        public const string RestZoneId = "rest_zone";
        public const string QuestId = "quest";
        public const string TrapId = "trap";
        public const string CustomId = "custom";

        /// <summary>Resref the runtime matches on to find no-spawn volumes (Walkmesh service).</summary>
        public const string NoSpawnResRef = "anti_spawn_trigg";

        private const string ExploreHandler = "explore_trigger";
        private const string RestEnterHandler = "rest_trg_enter";
        private const string RestExitHandler = "rest_trg_exit";
        private const string QuestHandler = "quest_trigger";

        private static readonly IReadOnlyList<TriggerChoice> LinkTargetChoices = new[]
        {
            new TriggerChoice(2, "Waypoint"),
            new TriggerChoice(1, "Door")
        };

        public static IReadOnlyList<TriggerBehavior> All { get; } = Build();

        public static TriggerBehavior Custom => Get(CustomId);

        public static TriggerBehavior Get(string id) =>
            All.FirstOrDefault(behavior => behavior.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "No such trigger behavior.");

        /// <summary>
        /// Recognises the behavior an existing trigger already has. Ordered most specific first: the
        /// SWLOR handlers and the no-spawn resref identify themselves exactly, so they are checked
        /// before the engine's own Type field, which is far broader.
        /// </summary>
        public static TriggerBehavior Classify(JsonGffStruct trigger)
        {
            ArgumentNullException.ThrowIfNull(trigger);

            var resRef = trigger.GetStringOrNull("TemplateResRef") ?? string.Empty;
            if (string.Equals(resRef, NoSpawnResRef, StringComparison.OrdinalIgnoreCase))
                return Get(NoSpawnZoneId);

            var onEnter = trigger.GetStringOrNull("ScriptOnEnter") ?? string.Empty;
            if (string.Equals(onEnter, ExploreHandler, StringComparison.OrdinalIgnoreCase))
                return Get(ExplorationNoteId);
            if (string.Equals(onEnter, RestEnterHandler, StringComparison.OrdinalIgnoreCase))
                return Get(RestZoneId);
            if (string.Equals(onEnter, QuestHandler, StringComparison.OrdinalIgnoreCase))
                return Get(QuestId);

            var type = trigger.GetIntOrNull("Type") ?? 0;
            var trapFlag = trigger.GetIntOrNull("TrapFlag") ?? 0;
            if (type == 2 || trapFlag == 1)
                return Get(TrapId);
            if (type == 1)
                return Get(AreaTransitionId);

            return Custom;
        }

        private static IReadOnlyList<TriggerBehavior> Build()
        {
            return new[]
            {
                // 297 placements — over half of every trigger in the module.
                new TriggerBehavior
                {
                    Id = AreaTransitionId,
                    DisplayName = "Area Transition",
                    Group = "Movement",
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "Destination Tag", Name = "LinkedTo", Kind = TriggerFieldKind.TagReference,
                            FieldType = GffFieldType.CExoString, IsRequired = true,
                            TagScope = TriggerTagScope.WaypointOrDoor
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Destination is a", Name = "LinkedToFlags", Kind = TriggerFieldKind.Choice,
                            FieldType = GffFieldType.Byte, Choices = LinkTargetChoices
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Load screen", Name = "LoadScreenID", Kind = TriggerFieldKind.Choice,
                            FieldType = GffFieldType.Word, ChoicesKey = TriggerChoiceKeys.LoadScreens,
                            Note = "Leave on Random to use the destination area's own default."
                        }
                    },
                    Manages = new[]
                    {
                        new TriggerManagedValue
                        {
                            Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int,
                            IntValue = 1, Display = "Area Transition"
                        },
                        new TriggerManagedValue
                        {
                            Label = "Cursor", Name = "Cursor", FieldType = GffFieldType.Byte,
                            IntValue = 1, Display = "Transition"
                        }
                    }
                },

                // 128 placements. Walkmesh.StoreNoSpawnZoneTriggers finds these by resref at load.
                new TriggerBehavior
                {
                    Id = NoSpawnZoneId,
                    DisplayName = "No-Spawn Zone",
                    Group = "World",
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "Nothing to set", Name = string.Empty,
                            Kind = TriggerFieldKind.Statement,
                            Note = "Uses the '" + NoSpawnResRef + "' blueprint, which is how the game "
                                 + "recognises it."
                        }
                    },
                    Summary = "Keeps NPCs and resources from spawning inside the trigger.",
                    Manages = new[]
                    {
                        // The runtime finds these by resref, so on a placement that is the field
                        // that actually makes it a no-spawn zone. A blueprint's resref is its own
                        // file name and cannot be rewritten here, hence instance-only - and it is
                        // never cleared, because a placement without a blueprint is orphaned.
                        new TriggerManagedValue
                        {
                            Label = "Blueprint", Name = "TemplateResRef", FieldType = GffFieldType.ResRef,
                            StringValue = NoSpawnResRef, IsInstanceOnly = true, ClearOnSwap = false
                        },
                        new TriggerManagedValue
                        {
                            Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int,
                            IntValue = 0, Display = "Generic"
                        }
                    }
                },

                // 41 placements, every one carrying its own DISPLAY_TEXT.
                new TriggerBehavior
                {
                    Id = ExplorationNoteId,
                    DisplayName = "Exploration Note",
                    Group = "World",
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "Message", Name = "DISPLAY_TEXT", Kind = TriggerFieldKind.Paragraph,
                            Storage = TriggerFieldStorage.Local, IsRequired = true
                        },
                    },
                    Summary = "Shows a message the first time a player enters, once per server reboot.",
                    Manages = new[]
                    {
                        new TriggerManagedValue
                        {
                            Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int,
                            IntValue = 0, Display = "Generic"
                        },
                        new TriggerManagedValue
                        {
                            Label = "OnEnter", Name = "ScriptOnEnter", FieldType = GffFieldType.ResRef,
                            StringValue = ExploreHandler
                        },
                        new TriggerManagedValue
                        {
                            Label = "Highlight Height", Name = "HighlightHeight",
                            FieldType = GffFieldType.Float, FloatValue = 3.0
                        }
                    }
                },

                // 14 placements.
                new TriggerBehavior
                {
                    Id = RestZoneId,
                    DisplayName = "Rest Zone",
                    Group = "World",
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "Nothing to set", Name = string.Empty,
                            Kind = TriggerFieldKind.Statement,
                            Note = "Entering the trigger permits rest; leaving revokes it."
                        }
                    },
                    Summary = "Lets players rest inside the trigger.",
                    Manages = new[]
                    {
                        new TriggerManagedValue
                        {
                            Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int,
                            IntValue = 0, Display = "Generic"
                        },
                        new TriggerManagedValue
                        {
                            Label = "OnEnter", Name = "ScriptOnEnter", FieldType = GffFieldType.ResRef,
                            StringValue = RestEnterHandler
                        },
                        new TriggerManagedValue
                        {
                            Label = "OnExit", Name = "ScriptOnExit", FieldType = GffFieldType.ResRef,
                            StringValue = RestExitHandler
                        }
                    }
                },

                new TriggerBehavior
                {
                    Id = QuestId,
                    DisplayName = "Quest Trigger",
                    Group = "Progression",
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "Quest", Name = "QUEST_ID", Kind = TriggerFieldKind.Text,
                            Storage = TriggerFieldStorage.Local, IsRequired = true
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Advance to state", Name = "QUEST_STATE", Kind = TriggerFieldKind.Integer,
                            Storage = TriggerFieldStorage.Local
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Message", Name = "QUEST_MESSAGE", Kind = TriggerFieldKind.Paragraph,
                            Storage = TriggerFieldStorage.Local
                        }
                    },
                    Manages = new[]
                    {
                        new TriggerManagedValue
                        {
                            Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int,
                            IntValue = 0, Display = "Generic"
                        },
                        new TriggerManagedValue
                        {
                            Label = "OnEnter", Name = "ScriptOnEnter", FieldType = GffFieldType.ResRef,
                            StringValue = QuestHandler
                        }
                    }
                },

                // 14 placements.
                new TriggerBehavior
                {
                    Id = TrapId,
                    DisplayName = "Trap",
                    Group = "Hazard",
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "Trap type", Name = "TrapType", Kind = TriggerFieldKind.Choice,
                            FieldType = GffFieldType.Byte, ChoicesKey = TriggerChoiceKeys.TrapTypes
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Detectable", Name = "TrapDetectable", Kind = TriggerFieldKind.Check,
                            FieldType = GffFieldType.Byte
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Detect DC", Name = "TrapDetectDC", Kind = TriggerFieldKind.Integer,
                            FieldType = GffFieldType.Byte
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Disarmable", Name = "TrapDisarmable", Kind = TriggerFieldKind.Check,
                            FieldType = GffFieldType.Byte
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Disarm DC", Name = "DisarmDC", Kind = TriggerFieldKind.Integer,
                            FieldType = GffFieldType.Byte
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "Fires once", Name = "TrapOneShot", Kind = TriggerFieldKind.Check,
                            FieldType = GffFieldType.Byte
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnTrapTriggered", Name = "OnTrapTriggered", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnDisarm", Name = "OnDisarm", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        }
                    },
                    Manages = new[]
                    {
                        new TriggerManagedValue
                        {
                            Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int,
                            IntValue = 2, Display = "Trap"
                        },
                        new TriggerManagedValue
                        {
                            Label = "Trap Flag", Name = "TrapFlag", FieldType = GffFieldType.Byte,
                            IntValue = 1, Display = "set"
                        }
                    }
                },

                new TriggerBehavior
                {
                    Id = CustomId,
                    DisplayName = "Custom",
                    AllowsVariables = true,
                    Fields = new[]
                    {
                        new TriggerFieldDefinition
                        {
                            Label = "OnClick", Name = "OnClick", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnDisarm", Name = "OnDisarm", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnTrapTriggered", Name = "OnTrapTriggered", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnEnter", Name = "ScriptOnEnter", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnExit", Name = "ScriptOnExit", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnHeartbeat", Name = "ScriptHeartbeat", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        },
                        new TriggerFieldDefinition
                        {
                            Label = "OnUserDefined", Name = "ScriptUserDefine", Kind = TriggerFieldKind.Script,
                            FieldType = GffFieldType.ResRef
                        }
                    }
                }
            };
        }
    }
}
