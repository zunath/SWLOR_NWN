using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for placeable blueprints (.utp). Field names and GFF types verified against
    /// the module corpus (e.g. Module\utp\_mdrn_chair.utp.json). ItemList (placeable inventory
    /// contents) is intentionally not exposed here; it needs its own dedicated editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The placeable editor is three tabs plus Variables. Basic comes from this schema; Appearance
    /// and Behavior are views of their own - a model grid and a behavior list - rather than lists of
    /// fields, and Variables appears only when there is something raw left to edit. There is no
    /// Advanced tab: the raw conversation slot and event scripts belong to the Custom behavior, and
    /// they are shown there.
    /// </para>
    /// <para>
    /// A lot of what Aurora showed is deliberately absent, in every case because the corpus says it
    /// is not authored: no trap fields (0 trapped blueprints and 2 trapped instances module-wide),
    /// no saving throws (8,353 of 8,355 blueprints carry Aurora's untouched 16/0/0), no hardness
    /// (8,199 carry the default 5), no portrait (6,295 have none, and a portrait is the wrong
    /// artwork for a placeable anyway), no body bag (0 on all 98,856 instances), no faction (two
    /// values cover 8,329 blueprints), and no lock fields (2 locked blueprints, 63 locked instances,
    /// key-required on two objects).
    /// </para>
    /// <para>
    /// The .dlg conversation slot is NOT one of those omissions, and an earlier version of this
    /// schema dropped it on the grounds that "SWLOR dialogs are C# classes". That is not true: the
    /// module ships 352 hand-authored .dlg conversations against 19 C# DialogBase classes. The
    /// Behavior tab's Conversation behavior writes the CONVERSATION variable, which can only name
    /// one of those 19 - so without the raw slot the majority route is unreachable and the 32
    /// placed placeables already using it cannot be edited here. It is shown under Custom.
    /// </para>
    /// <para>
    /// Absent from the UI is not absent from the file: every one of those fields is written back
    /// exactly as stored. The editor never normalizes what it does not show.
    /// </para>
    /// <para>
    /// Appearance is not declared here on purpose. As a dropdown it forced the editor to refuse to
    /// open the 2,982 blueprints whose appearance row is blank in placeables.2da, because a combo
    /// box cannot represent a value it has no option for. The Appearance tab keeps and marks the
    /// stored row instead.
    /// </para>
    /// </remarks>
    public static class UtpSchema
    {
        public const string BasicTab = "Basic";

        public const string ConversationGroupTitle = "Conversation";
        public const string ScriptSlotsGroupTitle = "Script slots";

        /// <summary>
        /// Group titles a fully initialized placeable editor takes off Basic and shows under Custom
        /// on the Behavior tab instead. They stay in the schema as the no-game-data fallback: with
        /// no 2DA layer there is no Behavior tab to move them to.
        /// </summary>
        public static IReadOnlyList<string> CustomBehaviorGroupTitles { get; } = new[]
        {
            ConversationGroupTitle,
            ScriptSlotsGroupTitle
        };

        /// <summary>The raw .dlg slot, shown below Custom's scripts on the Behavior tab.</summary>
        public static IReadOnlyList<FieldDescriptor> CustomBehaviorConversationFields { get; } =
            new[]
            {
                new FieldDescriptor { Label = "Conversation", FieldName = "Conversation", Kind = EditorKind.ResourcePicker, LookupKey = "dlg", FieldType = GffFieldType.ResRef, Description = "The .dlg this placeable talks from. For a C# dialog class, use the Conversation behavior instead." }
            };

        /// <summary>
        /// Raw placeable flags. Named behaviors manage the flags they require, so these are exposed
        /// only by Custom in the Behavior tab instead of competing with behavior selection on Basic.
        /// </summary>
        public static IReadOnlyList<FieldDescriptor> CustomBehaviorFlagFields { get; } =
            new[]
            {
                new FieldDescriptor { Label = "Useable", FieldName = "Useable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                new FieldDescriptor { Label = "Has Inventory", FieldName = "HasInventory", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                new FieldDescriptor { Label = "Static", FieldName = "Static", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                new FieldDescriptor { Label = "Plot", FieldName = "Plot", Kind = EditorKind.Check, FieldType = GffFieldType.Byte }
            };

        /// <summary>Raw event wiring shown below Custom's flags on the Behavior tab.</summary>
        public static IReadOnlyList<FieldDescriptor> CustomBehaviorScriptFields { get; } =
            new[]
            {
                new FieldDescriptor { Label = "On Used", FieldName = "OnUsed", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Click", FieldName = "OnClick", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Closed", FieldName = "OnClosed", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Damaged", FieldName = "OnDamaged", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Death", FieldName = "OnDeath", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Disarm", FieldName = "OnDisarm", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Heartbeat", FieldName = "OnHeartbeat", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Inventory Disturbed", FieldName = "OnInvDisturbed", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Lock", FieldName = "OnLock", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Melee Attacked", FieldName = "OnMeleeAttacked", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Open", FieldName = "OnOpen", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Spell Cast At", FieldName = "OnSpellCastAt", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Trap Triggered", FieldName = "OnTrapTriggered", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On Unlock", FieldName = "OnUnlock", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                new FieldDescriptor { Label = "On User Defined", FieldName = "OnUserDefined", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
            };

        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utp,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Tab = BasicTab,
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Name", FieldName = "LocName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "ResRef", FieldName = "TemplateResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsRequired = true, Description = "Renaming this ResRef renames the blueprint and updates every placed instance." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Durability",
                        Tab = BasicTab,
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Hit Points", FieldName = "HP", Kind = EditorKind.Integer, FieldType = GffFieldType.Short, Description = "Only matters while Plot is off." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Description",
                        Tab = BasicTab,
                        Fields = new[]
                        {
                            new FieldDescriptor
                            {
                                Label = "Description",
                                FieldName = "Description",
                                Kind = EditorKind.LocString,
                                FieldType = GffFieldType.CExoLocString,
                                Description = "Shown when a player examines it.",
                                IsMultiline = true
                            }
                        }
                    },
                    new FieldGroup
                    {
                        // The Behavior tab's Conversation behavior covers the C# route, which writes
                        // the CONVERSATION variable and can only name one of the 19 DialogBase
                        // classes. This is the other route, and it is the one most of the module
                        // uses: 352 of the 371 conversations SWLOR ships are hand-authored .dlg
                        // files. Without this field a placeable that talks from one cannot be
                        // expressed here at all - and 32 placed placeables already do.
                        Title = ConversationGroupTitle,
                        Tab = BasicTab,
                        Fields = CustomBehaviorConversationFields
                    },
                    new FieldGroup
                    {
                        // Kept in the schema as the no-game-data fallback. A fully initialized
                        // placeable editor filters this group off Basic and shows these same
                        // descriptors under Custom on the Behavior tab.
                        Title = ScriptSlotsGroupTitle,
                        Tab = BasicTab,
                        Fields = CustomBehaviorScriptFields
                    }
                },
                HasVarTable = true
            };
        }
    }
}
