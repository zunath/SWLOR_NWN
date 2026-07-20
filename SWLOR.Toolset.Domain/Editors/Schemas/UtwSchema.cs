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
                            new FieldDescriptor { Label = "ResRef", FieldName = "TemplateResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsReadOnly = true, Description = "Blueprint resref; matches the file name." },
                            new FieldDescriptor { Label = "Comment", FieldName = "Comment", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
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
