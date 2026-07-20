using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for placeable blueprints (.utp). Field names and GFF types verified against
    /// the module corpus (e.g. Module\utp\_mdrn_chair.utp.json). ItemList (placeable inventory
    /// contents) is intentionally not exposed here; it needs its own dedicated editor.
    /// </summary>
    public static class UtpSchema
    {
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
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Name", FieldName = "LocName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "ResRef", FieldName = "TemplateResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsReadOnly = true, Description = "Blueprint resref; matches the file name." },
                            new FieldDescriptor { Label = "Comment", FieldName = "Comment", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Appearance & Behavior",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Appearance", FieldName = "Appearance", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Dword, LookupKey = LookupKeys.Placeables },
                            new FieldDescriptor { Label = "Faction", FieldName = "Faction", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Dword, LookupKey = LookupKeys.Factions },
                            new FieldDescriptor { Label = "Useable", FieldName = "Useable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Static", FieldName = "Static", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Plot", FieldName = "Plot", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Has Inventory", FieldName = "HasInventory", Kind = EditorKind.Check, FieldType = GffFieldType.Byte }
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
                            new FieldDescriptor { Label = "Close Lock DC", FieldName = "CloseLockDC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
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
                        Title = "Conversation",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Conversation", FieldName = "Conversation", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, Description = "Legacy .dlg resref; SWLOR dialogs are C# classes." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Scripts",
                        Fields = new[]
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
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
