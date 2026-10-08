using Nwn.Authoring.Areas.Properties;
using Nwn.Authoring.Behaviors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Schemas
{
    /// <summary>
    /// Projects the shared native ARE field schema into SWLOR's localized editor descriptors.
    /// ResRef, Tileset, Width and Height describe native identity/layout and remain read-only.
    /// </summary>
    public static class AreSchema
    {
        public static EditorSchema Build()
        {
            return new EditorSchema
            {
                ResourceType = ResourceType.Area,
                Groups = AreaPropertyCatalog.Groups.Select(group => new FieldGroup
                {
                    Title = GroupTitle(group.Id),
                    Fields = group.Fields.Select(field => new FieldDescriptor
                    {
                        Label = FieldLabel(field.Id),
                        FieldName = field.NativeName,
                        Kind = EditorKindFor(field.Kind),
                        FieldType = field.FieldType,
                        IsReadOnly = field.IsReadOnly,
                        Description = field.Id == AreaPropertyFieldId.ResRef
                            ? "The ResRef. Matches the file name."
                            : field.Id == AreaPropertyFieldId.Flags
                                ? "Area type bitmask (interior/underground/natural)."
                                : null,
                    }).ToArray(),
                }).ToArray(),
                HasVarTable = false,
            };
        }

        private static EditorKind EditorKindFor(BehaviorFieldKind kind) => kind switch
        {
            BehaviorFieldKind.LocalizedText => EditorKind.LocString,
            BehaviorFieldKind.Text => EditorKind.Text,
            BehaviorFieldKind.Integer => EditorKind.Integer,
            BehaviorFieldKind.Float => EditorKind.Float,
            BehaviorFieldKind.Check => EditorKind.Check,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        private static string GroupTitle(AreaPropertyGroupId id) => id switch
        {
            AreaPropertyGroupId.Identity => "Identity",
            AreaPropertyGroupId.Flags => "Flags",
            AreaPropertyGroupId.Lighting => "Lighting",
            AreaPropertyGroupId.Weather => "Weather",
            AreaPropertyGroupId.Loading => "Loading",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };

        private static string FieldLabel(AreaPropertyFieldId id) => id switch
        {
            AreaPropertyFieldId.Name => "Name",
            AreaPropertyFieldId.Tag => "Tag",
            AreaPropertyFieldId.ResRef => "ResRef",
            AreaPropertyFieldId.Tileset => "Tileset",
            AreaPropertyFieldId.Width => "Width",
            AreaPropertyFieldId.Height => "Height",
            AreaPropertyFieldId.Comments => "Comments",
            AreaPropertyFieldId.Flags => "Flags",
            AreaPropertyFieldId.NoRest => "No Rest",
            AreaPropertyFieldId.PlayerVsPlayer => "Player vs Player",
            AreaPropertyFieldId.LightingScheme => "Lighting Scheme",
            AreaPropertyFieldId.SkyBox => "Sky Box",
            AreaPropertyFieldId.DayNightCycle => "Day/Night Cycle",
            AreaPropertyFieldId.IsNight => "Is Night",
            AreaPropertyFieldId.SunAmbientColor => "Sun Ambient Color",
            AreaPropertyFieldId.SunDiffuseColor => "Sun Diffuse Color",
            AreaPropertyFieldId.SunShadows => "Sun Shadows",
            AreaPropertyFieldId.SunFogAmount => "Sun Fog Amount",
            AreaPropertyFieldId.MoonAmbientColor => "Moon Ambient Color",
            AreaPropertyFieldId.MoonDiffuseColor => "Moon Diffuse Color",
            AreaPropertyFieldId.MoonShadows => "Moon Shadows",
            AreaPropertyFieldId.MoonFogAmount => "Moon Fog Amount",
            AreaPropertyFieldId.FogClipDist => "Fog Clip Distance",
            AreaPropertyFieldId.ChanceRain => "Chance of Rain",
            AreaPropertyFieldId.ChanceSnow => "Chance of Snow",
            AreaPropertyFieldId.ChanceLightning => "Chance of Lightning",
            AreaPropertyFieldId.WindPower => "Wind Power",
            AreaPropertyFieldId.LoadScreenID => "Load Screen",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
    }
}
