using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>Every door behavior and the ordered classifier for existing content.</summary>
    public static class DoorBehaviorCatalog
    {
        public const string AreaTransitionId = "area_transition";
        public const string LockedDoorId = "locked_door";
        public const string KeyItemDoorId = "key_item_door";
        public const string SealedDoorId = "sealed_door";
        public const string TrappedDoorId = "trapped_door";
        public const string CustomId = "custom";

        public const string DefaultDeathScript = "x2_door_death";
        public const string LockedDoorConversation = "LockedDoorDialog";

        private static readonly string[] ScriptFields =
        {
            "OnClick",
            "OnClosed",
            "OnDamaged",
            "OnDeath",
            "OnDisarm",
            "OnFailToOpen",
            "OnHeartbeat",
            "OnLock",
            "OnMeleeAttacked",
            "OnOpen",
            "OnSpellCastAt",
            "OnTrapTriggered",
            "OnUnlock",
            "OnUserDefined"
        };

        private static readonly IReadOnlyList<BehaviorChoice> LinkTargetChoices = new[]
        {
            new BehaviorChoice(1, "Door"),
            new BehaviorChoice(2, "Waypoint")
        };

        public static IReadOnlyList<DoorBehavior> All { get; } = Build();

        public static DoorBehavior Custom => Get(CustomId);

        public static DoorBehavior Get(string id) =>
            All.FirstOrDefault(behavior => behavior.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "No such door behavior.");

        public static DoorBehavior Classify(JsonGffStruct door)
        {
            ArgumentNullException.ThrowIfNull(door);
            var values = new DoorValueStore(door);

            if (values.HasRequiredKeyItemLocals)
                return Get(KeyItemDoorId);

            if (door.GetIntOrNull("TrapFlag") == 1)
                return Get(TrappedDoorId);

            if (!string.IsNullOrWhiteSpace(door.GetStringOrNull("LinkedTo")))
                return Get(AreaTransitionId);

            if (door.GetIntOrNull("Locked") == 1 || door.GetIntOrNull("KeyRequired") == 1)
                return Get(LockedDoorId);

            if (HasCustomScript(door) ||
                !string.IsNullOrWhiteSpace(door.GetStringOrNull("Conversation")) ||
                values.Locals.Any())
            {
                return Custom;
            }

            if (door.GetIntOrNull("Plot") == 1)
                return Get(SealedDoorId);

            return Custom;
        }

        private static bool HasCustomScript(JsonGffStruct door)
        {
            foreach (var field in ScriptFields)
            {
                var script = door.GetStringOrNull(field);
                if (string.IsNullOrWhiteSpace(script))
                    continue;

                if (field == "OnDeath" &&
                    string.Equals(script, DefaultDeathScript, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (field == "OnOpen" && DoorValueStore.IsKnownCloser(script))
                    continue;

                return true;
            }

            return false;
        }

        private static IReadOnlyList<DoorBehavior> Build()
        {
            var conditionalLockFields = LockFields(conditional: true);
            var customFields = CustomFields();

            return new[]
            {
                new DoorBehavior
                {
                    Id = AreaTransitionId,
                    DisplayName = "Area Transition",
                    Group = "Movement",
                    Summary = "Walking through moves the player to another door or waypoint.",
                    Fields = new[]
                    {
                        new DoorFieldDefinition
                        {
                            Label = "Destination", Name = "LinkedTo", Kind = BehaviorFieldKind.TagReference,
                            FieldType = GffFieldType.CExoString, IsRequired = true,
                            TagScope = BehaviorTagScope.WaypointOrDoor
                        },
                        new DoorFieldDefinition
                        {
                            Label = "Destination is a", Name = "LinkedToFlags", Kind = BehaviorFieldKind.Choice,
                            FieldType = GffFieldType.Byte, Choices = LinkTargetChoices
                        },
                        new DoorFieldDefinition
                        {
                            Label = "Load screen", Name = "LoadScreenID", Kind = BehaviorFieldKind.Choice,
                            FieldType = GffFieldType.Word, ChoicesKey = DoorChoiceKeys.LoadScreens
                        },
                        new DoorFieldDefinition
                        {
                            Label = "Locked", Name = "Locked", Kind = BehaviorFieldKind.Check,
                            FieldType = GffFieldType.Byte
                        }
                    }.Concat(conditionalLockFields).ToList(),
                    Manages = new[]
                    {
                        Owned("Destination type", "LinkedToFlags", GffFieldType.Byte),
                        Owned("Key required", "KeyRequired", GffFieldType.Byte)
                    }
                },
                new DoorBehavior
                {
                    Id = LockedDoorId,
                    DisplayName = "Locked Door",
                    Group = "Access",
                    Summary = "The engine's lock: picked open, or opened by an item with the matching tag.",
                    Fields = LockFields(conditional: false),
                    Manages = new[]
                    {
                        Pinned("Locked", "Locked", GffFieldType.Byte, 1),
                        Owned("Key required", "KeyRequired", GffFieldType.Byte)
                    }
                },
                new DoorBehavior
                {
                    Id = KeyItemDoorId,
                    DisplayName = "Key-Item Door",
                    Group = "Access",
                    Summary = "SWLOR's gate: talking to the door checks key items and moves the player inside.",
                    Fields = new[]
                    {
                        new DoorFieldDefinition
                        {
                            Label = "Required key items", Name = DoorValueStore.RequiredKeyItemPrefix,
                            Kind = BehaviorFieldKind.MultiChoice, Storage = BehaviorFieldStorage.Local,
                            FieldType = GffFieldType.Int, IsRequired = true,
                            Special = DoorFieldSpecial.KeyItemSequence
                        },
                        new DoorFieldDefinition
                        {
                            Label = "Prompt", Name = "DOOR_DIALOGUE", Kind = BehaviorFieldKind.Paragraph,
                            Storage = BehaviorFieldStorage.Local, FieldType = GffFieldType.CExoString
                        },
                        new DoorFieldDefinition
                        {
                            Label = "Destination waypoint tag", Name = "LOCKED_DOOR_INSIDE_WP",
                            Kind = BehaviorFieldKind.TagReference, Storage = BehaviorFieldStorage.Local,
                            FieldType = GffFieldType.CExoString, IsRequired = true,
                            TagScope = BehaviorTagScope.Waypoint,
                            Note = "After the key-item check succeeds, the player and henchman move to this waypoint."
                        }
                    },
                    Manages = new[]
                    {
                        new BehaviorManagedValue
                        {
                            Label = "Conversation", Name = "CONVERSATION",
                            Storage = BehaviorFieldStorage.Local, FieldType = GffFieldType.CExoString,
                            StringValue = LockedDoorConversation
                        },
                        Pinned("Locked", "Locked", GffFieldType.Byte, 1)
                    },
                    OwnedLocalPrefixes = new[] { DoorValueStore.RequiredKeyItemPrefix }
                },
                new DoorBehavior
                {
                    Id = SealedDoorId,
                    DisplayName = "Sealed Door",
                    Group = "Access",
                    Summary = "Scenery that never opens and cannot be destroyed.",
                    Fields = new[]
                    {
                        new DoorFieldDefinition
                        {
                            Label = "Nothing to set", Name = string.Empty,
                            Kind = BehaviorFieldKind.Statement,
                            Note = "Plot is set; lock, key requirement, and area link are cleared."
                        }
                    },
                    Manages = new[]
                    {
                        Pinned("Plot", "Plot", GffFieldType.Byte, 1),
                        Pinned("Locked", "Locked", GffFieldType.Byte, 0),
                        Pinned("Key required", "KeyRequired", GffFieldType.Byte, 0),
                        PinnedText("Linked to", "LinkedTo", GffFieldType.CExoString, string.Empty)
                    }
                },
                new DoorBehavior
                {
                    Id = TrappedDoorId,
                    DisplayName = "Trapped Door",
                    Group = "Hazard",
                    Summary = "A trap fires when the door is opened.",
                    Fields = new[]
                    {
                        new DoorFieldDefinition
                        {
                            Label = "Trap type", Name = "TrapType", Kind = BehaviorFieldKind.Choice,
                            FieldType = GffFieldType.Byte, ChoicesKey = DoorChoiceKeys.TrapTypes
                        },
                        Integer("Detect DC", "TrapDetectDC", GffFieldType.Byte),
                        Integer("Disarm DC", "DisarmDC", GffFieldType.Byte),
                        Check("One shot", "TrapOneShot"),
                        Check("Detectable", "TrapDetectable"),
                        Check("Disarmable", "TrapDisarmable"),
                        Script("OnTrapTriggered", "OnTrapTriggered"),
                        Script("OnDisarm", "OnDisarm")
                    },
                    Manages = new[]
                    {
                        Pinned("Trap flag", "TrapFlag", GffFieldType.Byte, 1)
                    }
                },
                new DoorBehavior
                {
                    Id = CustomId,
                    DisplayName = "Custom",
                    Fields = customFields,
                    AllowsVariables = true
                }
            };
        }

        private static IReadOnlyList<DoorFieldDefinition> LockFields(bool conditional)
        {
            DoorFieldDefinition Conditional(DoorFieldDefinition field)
            {
                if (!conditional)
                    return field;

                return new DoorFieldDefinition
                {
                    Label = field.Label,
                    Name = field.Name,
                    Kind = field.Kind,
                    Storage = field.Storage,
                    FieldType = field.FieldType,
                    IsRequired = field.IsRequired,
                    MaxLength = field.MaxLength,
                    Choices = field.Choices,
                    ChoicesKey = field.ChoicesKey,
                    Note = field.Note,
                    TagScope = field.TagScope,
                    NonEmptySetsField = field.NonEmptySetsField,
                    VisibleWhenField = "Locked"
                };
            }

            return new[]
            {
                Conditional(new DoorFieldDefinition
                {
                    Label = "Opens with key", Name = "KeyName", Kind = BehaviorFieldKind.TagReference,
                    FieldType = GffFieldType.CExoString, TagScope = BehaviorTagScope.Item,
                    NonEmptySetsField = "KeyRequired"
                }),
                Conditional(Check("Key is used up", "AutoRemoveKey")),
                Conditional(Integer("Pick lock DC", "OpenLockDC", GffFieldType.Byte)),
                Conditional(Integer("Relock DC", "CloseLockDC", GffFieldType.Byte)),
                Conditional(Check("Can be relocked", "Lockable"))
            };
        }

        private static IReadOnlyList<DoorFieldDefinition> CustomFields()
        {
            var fields = new List<DoorFieldDefinition>
            {
                new DoorFieldDefinition
                {
                    Label = "Conversation", Name = "Conversation", Kind = BehaviorFieldKind.Text,
                    FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength
                },
                Check("Plot", "Plot"),
                Check("Locked", "Locked"),
                Check("Key Required", "KeyRequired"),
                new DoorFieldDefinition
                {
                    Label = "Linked To", Name = "LinkedTo", Kind = BehaviorFieldKind.Text,
                    FieldType = GffFieldType.CExoString
                },
                Integer("Linked To Flags", "LinkedToFlags", GffFieldType.Byte),
                Check("Trap Flag", "TrapFlag"),
                Integer("Trap Type", "TrapType", GffFieldType.Byte),
                Check("Trap Detectable", "TrapDetectable"),
                Integer("Trap Detect DC", "TrapDetectDC", GffFieldType.Byte),
                Check("Trap Disarmable", "TrapDisarmable"),
                Integer("Trap Disarm DC", "DisarmDC", GffFieldType.Byte),
                Check("Trap One Shot", "TrapOneShot"),
                Check("Auto Remove Key", "AutoRemoveKey"),
                Integer("Relock DC", "CloseLockDC", GffFieldType.Byte),
                Integer("Current hit points", "CurrentHP", GffFieldType.Short),
                Integer("Hardness", "Hardness", GffFieldType.Byte),
                Integer("Hit points", "HP", GffFieldType.Short),
                Integer("Fortitude save", "Fort", GffFieldType.Byte),
                Integer("Reflex save", "Ref", GffFieldType.Byte),
                Integer("Will save", "Will", GffFieldType.Byte),
                new DoorFieldDefinition
                {
                    Label = "Key tag", Name = "KeyName", Kind = BehaviorFieldKind.TagReference,
                    FieldType = GffFieldType.CExoString, TagScope = BehaviorTagScope.Item
                },
                Check("Lockable", "Lockable"),
                Integer("Pick lock DC", "OpenLockDC", GffFieldType.Byte),
                new DoorFieldDefinition
                {
                    Label = "Load screen", Name = "LoadScreenID", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Word, ChoicesKey = DoorChoiceKeys.LoadScreens
                }
            };

            fields.AddRange(ScriptFields.Select(name => Script(name, name)));
            return fields;
        }

        private static DoorFieldDefinition Integer(string label, string name, GffFieldType type) =>
            new()
            {
                Label = label, Name = name, Kind = BehaviorFieldKind.Integer, FieldType = type
            };

        private static DoorFieldDefinition Check(string label, string name) =>
            new()
            {
                Label = label, Name = name, Kind = BehaviorFieldKind.Check, FieldType = GffFieldType.Byte
            };

        private static DoorFieldDefinition Script(string label, string name) =>
            new()
            {
                Label = label, Name = name, Kind = BehaviorFieldKind.Script,
                FieldType = GffFieldType.ResRef, MaxLength = NwnResRef.MaxLength
            };

        private static BehaviorManagedValue Pinned(
            string label,
            string name,
            GffFieldType type,
            long value) =>
            new()
            {
                Label = label, Name = name, FieldType = type, IntValue = value
            };

        private static BehaviorManagedValue PinnedText(
            string label,
            string name,
            GffFieldType type,
            string value) =>
            new()
            {
                Label = label, Name = name, FieldType = type, StringValue = value
            };

        private static BehaviorManagedValue Owned(string label, string name, GffFieldType type) =>
            new()
            {
                Label = label, Name = name, FieldType = type
            };
    }
}
