using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for creature blueprints (.utc). Field names and GFF types verified
    /// against the module corpus (e.g. Module\utc\zomb_guard.utc.json). This is the reference
    /// schema the other blueprint schemas are stamped from.
    /// </summary>
    public static class UtcSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utc,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "First Name", FieldName = "FirstName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Last Name", FieldName = "LastName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "ResRef", FieldName = "TemplateResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsRequired = true, Description = "Renaming this ResRef renames the blueprint and updates every placed instance." },
                            new FieldDescriptor { Label = "Description", FieldName = "Description", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            // Not "legacy": 352 of the module's conversations are hand-authored .dlg
                            // files and only 19 are C# classes. The C# route is the local CONVERSATION
                            // variable, not this field.
                            new FieldDescriptor { Label = "Conversation", FieldName = "Conversation", Kind = EditorKind.ResourcePicker, LookupKey = "dlg", FieldType = GffFieldType.ResRef, Description = "The .dlg this creature talks from. For a C# dialog class, leave this blank and set the CONVERSATION local variable instead." },
                            new FieldDescriptor { Label = "Comment", FieldName = "Comment", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Appearance",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Appearance", FieldName = "Appearance_Type", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Word, LookupKey = LookupKeys.Appearance },
                            new FieldDescriptor { Label = "Portrait", FieldName = "PortraitId", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Word, LookupKey = LookupKeys.Portraits },
                            new FieldDescriptor { Label = "Gender", FieldName = "Gender", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Byte, LookupKey = LookupKeys.Gender },
                            new FieldDescriptor { Label = "Phenotype", FieldName = "Phenotype", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Int, LookupKey = LookupKeys.Phenotype },
                            new FieldDescriptor { Label = "Sound Set", FieldName = "SoundSetFile", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Word, LookupKey = LookupKeys.SoundSets }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Statistics",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "STR", FieldName = "Str", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "DEX", FieldName = "Dex", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "CON", FieldName = "Con", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "INT", FieldName = "Int", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "WIS", FieldName = "Wis", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "CHA", FieldName = "Cha", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Hit Points", FieldName = "HitPoints", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Current HP", FieldName = "CurrentHitPoints", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Max HP", FieldName = "MaxHitPoints", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Natural AC", FieldName = "NaturalAC", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Challenge Rating", FieldName = "ChallengeRating", Kind = EditorKind.Float, FieldType = GffFieldType.Float },
                            new FieldDescriptor { Label = "Fortitude Bonus", FieldName = "fortbonus", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Reflex Bonus", FieldName = "refbonus", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Will Bonus", FieldName = "willbonus", Kind = EditorKind.Integer, FieldType = GffFieldType.Short },
                            new FieldDescriptor { Label = "Walk Rate", FieldName = "WalkRate", Kind = EditorKind.Integer, FieldType = GffFieldType.Int }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Behavior",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Faction", FieldName = "FactionID", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Word, LookupKey = LookupKeys.Factions },
                            new FieldDescriptor { Label = "Plot", FieldName = "Plot", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Immortal", FieldName = "IsImmortal", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "No Permanent Death", FieldName = "NoPermDeath", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Disarmable", FieldName = "Disarmable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Lootable", FieldName = "Lootable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Interruptable", FieldName = "Interruptable", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Perception Range", FieldName = "PerceptionRange", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Scripts",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "On Attacked", FieldName = "ScriptAttacked", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Damaged", FieldName = "ScriptDamaged", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Death", FieldName = "ScriptDeath", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Dialogue", FieldName = "ScriptDialogue", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Disturbed", FieldName = "ScriptDisturbed", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Combat Round End", FieldName = "ScriptEndRound", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Heartbeat", FieldName = "ScriptHeartbeat", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Blocked", FieldName = "ScriptOnBlocked", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Perception", FieldName = "ScriptOnNotice", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Rested", FieldName = "ScriptRested", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Spawn", FieldName = "ScriptSpawn", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Spell Cast At", FieldName = "ScriptSpellAt", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On User Defined", FieldName = "ScriptUserDefine", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
