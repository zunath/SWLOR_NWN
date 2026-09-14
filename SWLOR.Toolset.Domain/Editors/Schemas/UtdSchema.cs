using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for door blueprints (.utd). Field names and GFF types verified against the
    /// module corpus (e.g. Module\utd\_mdrn_dt_alien1.utd.json).
    ///
    /// Door appearance is deliberately absent from this generic schema. The dedicated door editor
    /// owns Appearance and GenericType_New as one combined model gallery: Appearance = 0 disables
    /// the doortypes lookup and selects GenericType_New instead. Declaring those paired fields as
    /// independent dropdowns makes the generic lookup guard reject every ordinary generic door.
    /// </summary>
    public static class UtdSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utd,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Name", FieldName = "LocName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
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
                            new FieldDescriptor { Label = "Faction", FieldName = "Faction", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Dword, LookupKey = LookupKeys.Factions }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Lock",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Locked", FieldName = "Locked", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Lockable", FieldName = "Lockable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Open Lock DC", FieldName = "OpenLockDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Close Lock DC", FieldName = "CloseLockDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Key Required", FieldName = "KeyRequired", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Key Tag", FieldName = "KeyName", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "Auto Remove Key", FieldName = "AutoRemoveKey", Kind = EditorKind.Check, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Combat",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Hit Points", FieldName = "HP", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Hardness", FieldName = "Hardness", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
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
                        Title = "Conversation",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Conversation", FieldName = "Conversation", Kind = EditorKind.ResourcePicker, LookupKey = "dlg", FieldType = GffFieldType.ResRef, Description = "The .dlg this object talks from. For a C# dialog class, leave this blank and set the CONVERSATION local variable instead." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Scripts",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "On Click", FieldName = "OnClick", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Closed", FieldName = "OnClosed", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Damaged", FieldName = "OnDamaged", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Death", FieldName = "OnDeath", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Disarm", FieldName = "OnDisarm", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Fail To Open", FieldName = "OnFailToOpen", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Heartbeat", FieldName = "OnHeartbeat", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Lock", FieldName = "OnLock", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Melee Attacked", FieldName = "OnMeleeAttacked", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Open", FieldName = "OnOpen", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Spell Cast At", FieldName = "OnSpellCastAt", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Trap Triggered", FieldName = "OnTrapTriggered", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Unlock", FieldName = "OnUnlock", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On User Defined", FieldName = "OnUserDefined", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
