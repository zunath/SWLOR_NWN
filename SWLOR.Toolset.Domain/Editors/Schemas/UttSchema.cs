using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for trigger blueprints (.utt). Field names and GFF types verified against
    /// the module corpus (e.g. Module\utt\anti_spawn_trigg.utt.json). Corpus verification note:
    /// trigger script slots use "ScriptOnEnter"/"ScriptOnExit"/"ScriptHeartbeat"/"ScriptUserDefine"
    /// (not the bare "OnEnter"/"OnExit"/"OnUserDefined" names used elsewhere).
    /// </summary>
    public static class UttSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utt,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Localized Name", FieldName = "LocalizedName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "ResRef", FieldName = "TemplateResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsRequired = true, Description = "Renaming this ResRef renames the blueprint and updates every placed instance." },
                            new FieldDescriptor { Label = "Comment", FieldName = "Comment", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Behavior",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Type", FieldName = "Type", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Int, LookupKey = LookupKeys.TriggerTypes, IsRequired = true, Description = "0 = generic, 1 = area transition, 2 = trap." },
                            new FieldDescriptor { Label = "Faction", FieldName = "Faction", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Dword, LookupKey = LookupKeys.Factions },
                            new FieldDescriptor { Label = "Cursor", FieldName = "Cursor", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Trap",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Trap Detectable", FieldName = "TrapDetectable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Trap Detect DC", FieldName = "TrapDetectDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Trap Disarmable", FieldName = "TrapDisarmable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Disarm DC", FieldName = "DisarmDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Trap Flag", FieldName = "TrapFlag", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Trap One Shot", FieldName = "TrapOneShot", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Trap Type", FieldName = "TrapType", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Scripts",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "On Enter", FieldName = "ScriptOnEnter", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Exit", FieldName = "ScriptOnExit", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Heartbeat", FieldName = "ScriptHeartbeat", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On User Defined", FieldName = "ScriptUserDefine", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Click", FieldName = "OnClick", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Disarm", FieldName = "OnDisarm", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Trap Triggered", FieldName = "OnTrapTriggered", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
