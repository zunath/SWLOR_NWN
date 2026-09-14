using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for store blueprints (.utm). Field names and GFF types verified against the
    /// module corpus (e.g. Module\utm\bartender.utm.json). Corpus verification note: stores use
    /// "ResRef" for the blueprint ResRef field, not "TemplateResRef" like every other blueprint
    /// type in this package. StoreList/WillNotBuy/WillOnlyBuy (the item lists)
    /// are intentionally not exposed here; they need their own dedicated list editor. No corpus
    /// .utm file carries a VarTable, so this schema does not offer the var-table grid.
    /// </summary>
    public static class UtmSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Utm,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Name", FieldName = "LocName", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "ResRef", FieldName = "ResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsRequired = true, Description = "Renaming this ResRef renames the blueprint and updates every placed instance; stores keep it in a field called 'ResRef' rather than 'TemplateResRef'." },
                            new FieldDescriptor { Label = "Comment", FieldName = "Comment", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Pricing",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Mark Up %", FieldName = "MarkUp", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Mark Down %", FieldName = "MarkDown", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Black Market", FieldName = "BlackMarket", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Black Market Mark Down %", FieldName = "BM_MarkDown", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Identify Price", FieldName = "IdentifyPrice", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Max Buy Price", FieldName = "MaxBuyPrice", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Store Gold", FieldName = "StoreGold", Kind = EditorKind.Integer, FieldType = GffFieldType.Int, Description = "-1 means unlimited gold." }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Scripts",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "On Open Store", FieldName = "OnOpenStore", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef },
                            new FieldDescriptor { Label = "On Store Closed", FieldName = "OnStoreClosed", Kind = EditorKind.ScriptSlot, FieldType = GffFieldType.ResRef }
                        }
                    }
                },
                HasVarTable = false
            };
        }
    }
}
