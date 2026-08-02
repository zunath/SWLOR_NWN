using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for ambient sound blueprints (.uts). Field names and GFF types verified
    /// against the module corpus (both files: Module\uts\night_bazzarnois.uts.json and
    /// Module\uts\night_metalsound.uts.json). The "Sounds" list (the actual sound resref entries)
    /// is intentionally not exposed here; it needs its own dedicated list editor. Neither corpus
    /// file carries a VarTable, so this schema does not offer the var-table grid.
    /// </summary>
    public static class UtsSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Uts,
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
                        Title = "Playback",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Active", FieldName = "Active", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Continuous", FieldName = "Continuous", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Looping", FieldName = "Looping", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Positional", FieldName = "Positional", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Random Position", FieldName = "RandomPosition", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Priority", FieldName = "Priority", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Volume",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Volume", FieldName = "Volume", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Volume Variation", FieldName = "VolumeVrtn", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Timing",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Interval", FieldName = "Interval", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword },
                            new FieldDescriptor { Label = "Interval Variation", FieldName = "IntervalVrtn", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Distance",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Min Distance", FieldName = "MinDistance", Kind = EditorKind.Float, FieldType = GffFieldType.Float },
                            new FieldDescriptor { Label = "Max Distance", FieldName = "MaxDistance", Kind = EditorKind.Float, FieldType = GffFieldType.Float }
                        }
                    }
                },
                HasVarTable = false
            };
        }
    }
}
