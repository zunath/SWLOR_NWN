using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for waypoint blueprints (.utw). Field names and GFF types verified against
    /// the module corpus (e.g. Module\utw\beetle_spwn001.utw.json).
    /// </summary>
    public static class UtwSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utw,
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
                        Title = "Appearance",
                        Fields = new[]
                        {
                            // Every checked-in waypoint carries this byte, the template factory writes it,
                            // and AreaSceneBuilder resolves it through WaypointAppearanceService to choose
                            // the marker drawn in the area view - so it was the one thing about a waypoint
                            // the editor could not change.
                            new FieldDescriptor { Label = "Appearance", FieldName = "Appearance", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Byte, LookupKey = LookupKeys.WaypointAppearances, IsRequired = true, Description = "Row index into waypoint.2da - the marker drawn in the area view." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Map Note",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Has Map Note", FieldName = "HasMapNote", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Map Note", FieldName = "MapNote", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Map Note Enabled", FieldName = "MapNoteEnabled", Kind = EditorKind.Check, FieldType = GffFieldType.Byte }
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
