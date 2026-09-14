using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for item blueprints (.uti). Field names and GFF types verified against the
    /// module corpus (e.g. Module\uti\001.uti.json). PropertiesList (the item-property list) is
    /// intentionally not exposed here: item properties need their own dedicated editor and are
    /// out of scope for this schema.
    /// </summary>
    public static class UtiSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Uti,
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
                            new FieldDescriptor { Label = "Description", FieldName = "DescIdentified", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString, Description = "The text players read. GetDescription returns the identified description by default." },
                            new FieldDescriptor { Label = "Comment", FieldName = "Comment", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Item Type",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Base Item", FieldName = "BaseItem", Kind = EditorKind.TwoDaDropdown, FieldType = GffFieldType.Int, LookupKey = LookupKeys.BaseItems, IsRequired = true, Description = "Row index into baseitems.2da." },
                            new FieldDescriptor { Label = "Stack Size", FieldName = "StackSize", Kind = EditorKind.Integer, FieldType = GffFieldType.Word },
                            new FieldDescriptor { Label = "Charges", FieldName = "Charges", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Economy",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Cost", FieldName = "Cost", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword },
                            new FieldDescriptor { Label = "Add Cost", FieldName = "AddCost", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Flags",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Plot", FieldName = "Plot", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Stolen", FieldName = "Stolen", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Cursed", FieldName = "Cursed", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Identified", FieldName = "Identified", Kind = EditorKind.Check, FieldType = GffFieldType.Byte }
                        }
                    }
                },
                HasVarTable = true
            };
        }
    }
}
