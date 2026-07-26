using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for trigger blueprints (.utt), laid out as the base toolset's Trigger
    /// Properties dialog: one group per tab, each holding that tab's fields in the base dialog's
    /// own order. Field names and GFF types verified against the module corpus (e.g.
    /// Module\utt\anti_spawn_trigg.utt.json).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Corpus verification note: trigger script slots use "ScriptOnEnter"/"ScriptOnExit"/
    /// "ScriptHeartbeat"/"ScriptUserDefine" (not the bare "OnEnter"/"OnExit"/"OnUserDefined" names
    /// used elsewhere), while "OnClick"/"OnDisarm"/"OnTrapTriggered" carry no prefix.
    /// </para>
    /// <para>
    /// There is deliberately no Comments tab and no <c>Comment</c> field: the trigger editor writes
    /// <c>Comment</c> as an empty string on save (see <c>CommentFieldName</c>). Local variables are
    /// not a group here either — they are their own tab, driven by <see cref="EditorSchema.HasVarTable"/>.
    /// </para>
    /// <para>
    /// The trigger's dimensions are not a blueprint concern. <c>Geometry</c> is drawn per placement
    /// in the area editor and never appears on this dialog, and the same is true of the
    /// per-placement transition target (<c>LinkedTo</c>/<c>LinkedToFlags</c>).
    /// </para>
    /// </remarks>
    public static class UttSchema
    {
        /// <summary>Title of the tab that exists only while the trigger is a trap.</summary>
        public const string TrapGroupTitle = "Trap";

        /// <summary>The field the Trap tab's visibility keys off.</summary>
        public const string TypeFieldName = "Type";

        /// <summary>
        /// The <c>Type</c> value meaning "trap". 1 is the area transition and 2 is the trap, not the
        /// other way round — <c>pitfalltrap.utt.json</c> carries Type=2 with TrapFlag=1 and an empty
        /// LinkedTo, which settles it.
        /// </summary>
        public const int TrapTypeValue = 2;

        /// <summary>The blueprint comment, blanked on save because the editor has no Comments tab.</summary>
        public const string CommentFieldName = "Comment";

        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utt,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Basic",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Name", FieldName = "LocalizedName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "Trigger Type", FieldName = TypeFieldName, Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Int, LookupKey = LookupKeys.TriggerTypes, Description = "0 = generic, 1 = area transition, 2 = trap." },
                            new FieldDescriptor { Label = "Category", FieldName = "PaletteID", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte, Description = "Palette id stored in the blueprint. The toolset's own category tree is assigned from the Palette panel." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Scripts",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "OnClick", FieldName = "OnClick", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "OnEnter", FieldName = "ScriptOnEnter", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "OnExit", FieldName = "ScriptOnExit", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "OnHeartbeat", FieldName = "ScriptHeartbeat", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "OnUserDefined", FieldName = "ScriptUserDefine", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
                        }
                    },
                    new FieldGroup
                    {
                        Title = TrapGroupTitle,
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Is Trap", FieldName = "TrapFlag", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Trap Type", FieldName = "TrapType", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Detectable", FieldName = "TrapDetectable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Detect DC", FieldName = "TrapDetectDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Disarmable", FieldName = "TrapDisarmable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Disarm DC", FieldName = "DisarmDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "One Shot", FieldName = "TrapOneShot", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "OnDisarm", FieldName = "OnDisarm", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "OnTrapTriggered", FieldName = "OnTrapTriggered", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Advanced",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Auto-Remove Key", FieldName = "AutoRemoveKey", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Key Tag", FieldName = "KeyName", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "Faction", FieldName = "Faction", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Dword, LookupKey = LookupKeys.Factions },
                            new FieldDescriptor { Label = "Highlight Height", FieldName = "HighlightHeight", Kind = EditorKind.Float, FieldType = GffFieldType.Float },
                            new FieldDescriptor { Label = "Blueprint ResRef", FieldName = "TemplateResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsReadOnly = true, Description = "Blueprint resref; matches the file name." },
                            new FieldDescriptor { Label = "Cursor", FieldName = "Cursor", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte, Description = "0 = unclickable (no cursor)." },
                            new FieldDescriptor { Label = "Portrait", FieldName = "PortraitId", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Word, LookupKey = LookupKeys.Portraits }
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
