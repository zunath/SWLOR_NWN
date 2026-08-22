using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Editor schema for the static area properties in an .are file (not the instance data in the
    /// paired .git/.gic files). Field names and GFF types verified against the module corpus
    /// (Module\are\bank.are.json). Tileset/Width/Height are read-only: they describe the area's
    /// fixed tileset layout, not adjustable gameplay settings. No corpus .are file carries a
    /// VarTable field, so this schema does not offer the var-table grid.
    /// </summary>
    public static class AreSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Area,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Name", FieldName = "Name", Kind = EditorKind.LocString, FieldType = GffFieldType.CExoLocString },
                            new FieldDescriptor { Label = "Tag", FieldName = "Tag", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString },
                            new FieldDescriptor { Label = "ResRef", FieldName = "ResRef", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsReadOnly = true, Description = "The ResRef. Matches the file name." },
                            new FieldDescriptor { Label = "Tileset", FieldName = "Tileset", Kind = EditorKind.ResRef, FieldType = GffFieldType.ResRef, IsReadOnly = true },
                            new FieldDescriptor { Label = "Width", FieldName = "Width", Kind = EditorKind.Integer, FieldType = GffFieldType.Int, IsReadOnly = true },
                            new FieldDescriptor { Label = "Height", FieldName = "Height", Kind = EditorKind.Integer, FieldType = GffFieldType.Int, IsReadOnly = true },
                            new FieldDescriptor { Label = "Comments", FieldName = "Comments", Kind = EditorKind.Text, FieldType = GffFieldType.CExoString }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Flags",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Flags", FieldName = "Flags", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword, Description = "Area type bitmask (interior/underground/natural)." },
                            new FieldDescriptor { Label = "No Rest", FieldName = "NoRest", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Player vs Player", FieldName = "PlayerVsPlayer", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Lighting",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Lighting Scheme", FieldName = "LightingScheme", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Sky Box", FieldName = "SkyBox", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Day/Night Cycle", FieldName = "DayNightCycle", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Is Night", FieldName = "IsNight", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Sun Ambient Color", FieldName = "SunAmbientColor", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword },
                            new FieldDescriptor { Label = "Sun Diffuse Color", FieldName = "SunDiffuseColor", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword },
                            new FieldDescriptor { Label = "Sun Shadows", FieldName = "SunShadows", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Sun Fog Amount", FieldName = "SunFogAmount", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Moon Ambient Color", FieldName = "MoonAmbientColor", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword },
                            new FieldDescriptor { Label = "Moon Diffuse Color", FieldName = "MoonDiffuseColor", Kind = EditorKind.Integer, FieldType = GffFieldType.Dword },
                            new FieldDescriptor { Label = "Moon Shadows", FieldName = "MoonShadows", Kind = EditorKind.Check, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Moon Fog Amount", FieldName = "MoonFogAmount", Kind = EditorKind.Integer, FieldType = GffFieldType.Byte },
                            new FieldDescriptor { Label = "Fog Clip Distance", FieldName = "FogClipDist", Kind = EditorKind.Float, FieldType = GffFieldType.Float }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Weather",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Chance of Rain", FieldName = "ChanceRain", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Chance of Snow", FieldName = "ChanceSnow", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Chance of Lightning", FieldName = "ChanceLightning", Kind = EditorKind.Integer, FieldType = GffFieldType.Int },
                            new FieldDescriptor { Label = "Wind Power", FieldName = "WindPower", Kind = EditorKind.Integer, FieldType = GffFieldType.Int }
                        }
                    },
                    new FieldGroup
                    {
                        Title = "Loading",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "Load Screen", FieldName = "LoadScreenID", Kind = EditorKind.Integer, FieldType = GffFieldType.Word }
                        }
                    }
                },
                HasVarTable = false
            };
        }
    }
}
