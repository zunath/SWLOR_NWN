namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// Every behavior a placeable can have, declared in the order the editor lists them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The set was derived from the module rather than invented: 94% of the 8,355 placeable
    /// blueprints set no script at all, and the ones that do use only 77 distinct script sets,
    /// of which the top twenty cover 88%. Instances are the same story - 93% carry no script and
    /// twelve sets cover 93% of the rest. So a short list of named behaviors plus a Custom escape
    /// hatch describes practically the whole corpus.
    /// </para>
    /// <para>
    /// Script resrefs and variable names here must match <c>SWLOR.Game.Server</c> exactly; they are
    /// pinned by <c>PlaceableBehaviorCatalogTests</c> against the module corpus so a rename in the
    /// game code cannot silently orphan a behavior.
    /// </para>
    /// </remarks>
    public static class PlaceableBehaviorCatalog
    {
        public const string NoneId = "none";
        public const string CustomId = "custom";

        private static readonly IReadOnlyList<PlaceableBehavior> All = Build();

        private static readonly Dictionary<string, PlaceableBehavior> ById =
            All.ToDictionary(behavior => behavior.Id, StringComparer.Ordinal);

        /// <summary>Every behavior, in list order.</summary>
        public static IReadOnlyList<PlaceableBehavior> Behaviors => All;

        /// <summary>The decor default: no scripts, no variables, nothing managed.</summary>
        public static PlaceableBehavior None => ById[NoneId];

        /// <summary>Wiring no declaration covers; unlocks the Variables tab and the raw script slots.</summary>
        public static PlaceableBehavior Custom => ById[CustomId];

        public static PlaceableBehavior? FindById(string? id) =>
            id != null && ById.TryGetValue(id, out var behavior) ? behavior : null;

        private static IReadOnlyList<PlaceableBehavior> Build()
        {
            return new[]
            {
                new PlaceableBehavior
                {
                    Id = NoneId,
                    Name = "Decor",
                    Group = string.Empty,
                    IsSentinel = true,
                    EditableFlags = new[]
                    {
                        new PlaceableBehaviorEditableFlag(
                            "Static",
                            "Static",
                            "Treat this decor as part of the area geometry instead of an interactive object.")
                    }
                },

                // ---- Gathering ------------------------------------------------------------
                new PlaceableBehavior
                {
                    Id = "scavenge_point",
                    Name = "Scavenge Point",
                    Group = "Gathering",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnOpen"] = "scav_opened",
                        ["OnClosed"] = "scav_closed",
                        ["OnInvDisturbed"] = "scav_disturbed"
                    },
                    Flags = UsableFlags(new PlaceableBehaviorFlag("HasInventory", true)),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "SCAVENGE_POINT_LOOT_TABLE_NAME",
                            Label = "Loot table",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.LootTables,
                            IsRequired = true
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "SCAVENGE_POINT_LEVEL",
                            Label = "Scavenging level required",
                            Kind = PlaceableFieldKind.Integer,
                            IsRequired = true,
                            Minimum = 1,
                            Maximum = 5,
                            DefaultIntValue = 1
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "harvest_node",
                    Name = "Harvest Node",
                    Group = "Gathering",
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "HARVESTING_LOOT_TABLE",
                            Label = "Loot table",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.LootTables,
                            IsRequired = true
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "HARVESTER_REQUIRED_LEVEL",
                            Label = "Harvesting level required",
                            Kind = PlaceableFieldKind.Integer,
                            IsRequired = true,
                            Minimum = 1,
                            Maximum = 5,
                            DefaultIntValue = 1
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "RESOURCE_COUNT",
                            Label = "Charges",
                            Kind = PlaceableFieldKind.Integer,
                            Description = "How many times it can be harvested before it is used up.",
                            Minimum = 1,
                            DefaultIntValue = 4
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "resource_node",
                    Name = "Resource Node",
                    Group = "Gathering",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "res_used",
                        ["OnHeartbeat"] = "res_heartbeat"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "RESOURCE_PROP",
                            Label = "Prop",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.PlaceableBlueprints,
                            EmptyChoiceLabel = "No prop",
                            ClearChoiceLabel = "Remove prop",
                            Description = "Optional. Select a visual-only prop to appear over the resource node, " +
                                          "most often a tree. Players cannot select or interact with the prop. " +
                                          "Remove it to use only the resource node's own appearance."
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "RESOURCE_SPAWN_TABLE_ID",
                            Label = "Spawn table",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.SpawnTables
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "RESOURCE_SPAWN_COUNT",
                            Label = "Charges",
                            Kind = PlaceableFieldKind.Integer,
                            Minimum = 1,
                            DefaultIntValue = 4
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "asteroid",
                    Name = "Asteroid",
                    Group = "Gathering",
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "ASTEROID_LOOT_TABLE_ID",
                            Label = "Mining loot table",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.LootTables,
                            IsRequired = true
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "STRIPMINE_LOOT_TABLE_ID",
                            Label = "Strip mine loot table",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.LootTables
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "ASTEROID_TIER",
                            Label = "Tier",
                            Kind = PlaceableFieldKind.Integer,
                            IsRequired = true,
                            Minimum = 1,
                            Maximum = 5,
                            DefaultIntValue = 1
                        }
                    }
                },

                // ---- Terminals ------------------------------------------------------------
                new PlaceableBehavior
                {
                    Id = "slicing_terminal",
                    Name = "Slicing Terminal",
                    Group = "Terminals",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "slice_terminal"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "SLICING_TIER",
                            Label = "Tier",
                            Kind = PlaceableFieldKind.Integer,
                            IsRequired = true,
                            Minimum = 1,
                            Maximum = 5,
                            DefaultIntValue = 1
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "SLICING_INTEGRITY",
                            Label = "Integrity",
                            Kind = PlaceableFieldKind.Integer,
                            IsRequired = true,
                            Minimum = 1,
                            Maximum = 100,
                            DefaultIntValue = 100,
                            Description = "Starting structural integrity (1–100). Failed slicing attempts reduce it; at 0 the terminal is destroyed or respawned."
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "KEY_ITEM_ID",
                            Label = "Key item awarded",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.KeyItems
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "market_terminal",
                    Name = "Market Terminal",
                    Group = "Terminals",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "generic_convo"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "CONVERSATION",
                            Label = "Dialog",
                            Kind = PlaceableFieldKind.Text,
                            IsRequired = true,
                            DefaultStringValue = "MarketDialog",
                            IsVisible = false
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "MARKET_ID",
                            Label = "Market region",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.MarketRegions,
                            IsRequired = true,
                            DefaultIntValue = 1
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "workbench",
                    Name = "Workbench",
                    Group = "Terminals",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "craft_on_used"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "CRAFTING_SKILL_TYPE_ID",
                            Label = "Crafting skill",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.SkillTypes,
                            IsRequired = true
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "conversation",
                    Name = "Conversation",
                    Group = "Terminals",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "generic_convo"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "CONVERSATION",
                            Label = "C# dynamic dialog",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.Dialogs,
                            IsRequired = true,
                            Description = "Select the C# dynamic dialog class to start. This does not use an NWN .dlg conversation."
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "TARGET_PC",
                            Label = "Player talks to self",
                            Kind = PlaceableFieldKind.Toggle,
                            Description = "Use the player as the conversation target instead of the placeable."
                        }
                    }
                },

                // ---- World ----------------------------------------------------------------
                new PlaceableBehavior
                {
                    Id = "teleporter",
                    Name = "Teleporter",
                    Group = "World",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "teleport"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "DESTINATION",
                            Label = "Destination",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.ObjectTags,
                            IsRequired = true,
                            Description = "Tag of the waypoint the player arrives at."
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "TELEPORT_PARTY_MEMBERS",
                            Label = "Teleport party members",
                            Kind = PlaceableFieldKind.Toggle
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "KEY_ITEM_ID",
                            Label = "Required key item",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.KeyItems
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "MISSING_KEY_ITEM_MESSAGE",
                            Label = "Message when missing it",
                            Kind = PlaceableFieldKind.Text
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "VISUAL_EFFECT",
                            Label = "Visual effect",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.VisualEffects
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "quest_activator",
                    Name = "Quest Activator",
                    Group = "World",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "quest_enc"
                    },
                    Flags = UsableFlags(),
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_ENCOUNTER_ID",
                            Label = "Encounter id",
                            Kind = PlaceableFieldKind.Text,
                            IsRequired = true
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_ENCOUNTER_RESREF",
                            Label = "Creature",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.CreatureBlueprints,
                            IsRequired = true
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_ENCOUNTER_WAYPOINT",
                            Label = "Spawn waypoint tag",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.ObjectTags
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_ID",
                            Label = "Quest",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.Quests
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_STATE",
                            Label = "Quest state",
                            Kind = PlaceableFieldKind.Integer
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_ENCOUNTER_COOLDOWN_MINUTES",
                            Label = "Cooldown (minutes)",
                            Kind = PlaceableFieldKind.Integer,
                            Minimum = 1,
                            DefaultIntValue = 60,
                            Description = "How long a player must wait before starting this encounter again."
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "QUEST_ENCOUNTER_IDLE_MINUTES",
                            Label = "Idle despawn (minutes)",
                            Kind = PlaceableFieldKind.Integer,
                            Minimum = 1,
                            DefaultIntValue = 10,
                            Description = "How long the spawned creature may remain uncontested before it despawns."
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "visibility_gated",
                    Name = "Visibility-gated",
                    Group = "World",
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "VISIBILITY_OBJECT_ID",
                            Label = "Visibility object id",
                            Kind = PlaceableFieldKind.Text,
                            IsRequired = true
                        },
                        new PlaceableBehaviorField
                        {
                            VariableName = "VISIBILITY_HIDDEN_DEFAULT",
                            Label = "Hidden by default",
                            Kind = PlaceableFieldKind.Toggle
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "permanent_vfx",
                    Name = "Permanent VFX",
                    Group = "World",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnHeartbeat"] = "permanent_vfx"
                    },
                    Fields = new[]
                    {
                        new PlaceableBehaviorField
                        {
                            VariableName = "PERMANENT_VFX_ID",
                            Label = "Visual effect",
                            Kind = PlaceableFieldKind.Choice,
                            Source = PlaceableValueSource.VisualEffects,
                            IsRequired = true
                        }
                    }
                },
                new PlaceableBehavior
                {
                    Id = "space_target",
                    Name = "Space Dock",
                    Group = "World",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnClick"] = "spc_target"
                    },
                    Flags = UsableFlags()
                },

                // ---- Props ----------------------------------------------------------------
                new PlaceableBehavior
                {
                    Id = "chair",
                    Name = "Chair",
                    Group = "Props",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "sit"
                    },
                    AlternateScripts = new[] { "x0_o2_use_chair", "x2_plc_used_sit", "zep_use_chair" },
                    Flags = UsableFlags()
                },
                new PlaceableBehavior
                {
                    Id = "light_torch",
                    Name = "Light / Torch",
                    Group = "Props",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "zep_torch",
                        ["OnHeartbeat"] = "zep_torchspawn"
                    },
                    Flags = UsableFlags()
                },
                new PlaceableBehavior
                {
                    Id = "on_off_switch",
                    Name = "On / Off Switch",
                    Group = "Props",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "zep_onoff"
                    },
                    AlternateScripts = new[] { "nw_02_onoff" },
                    Flags = UsableFlags()
                },
                new PlaceableBehavior
                {
                    Id = "door_blocker",
                    Name = "Door Blocker",
                    Group = "Props",
                    Scripts = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["OnUsed"] = "zep_openclose",
                        ["OnHeartbeat"] = "zep_doorspawn",
                        ["OnDeath"] = "zep_doorkill"
                    },
                    Flags = UsableFlags()
                },

                new PlaceableBehavior
                {
                    Id = CustomId,
                    Name = "Custom",
                    Group = "Custom",
                    IsSentinel = true,
                    AllowsRawEditing = true
                }
            };
        }

        /// <summary>
        /// A usable placeable must be dynamic. Static placeables cannot receive normal interaction,
        /// so every behavior that enables Useable also owns Static = false.
        /// </summary>
        private static IReadOnlyList<PlaceableBehaviorFlag> UsableFlags(
            params PlaceableBehaviorFlag[] additional)
        {
            return new[]
                {
                    new PlaceableBehaviorFlag("Useable", true),
                    new PlaceableBehaviorFlag("Static", false)
                }
                .Concat(additional)
                .ToArray();
        }
    }
}
