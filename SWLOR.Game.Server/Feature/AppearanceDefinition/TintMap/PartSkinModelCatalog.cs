using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Robe bodies of one family share a skeleton layout, so partlayout.2da maps each body to a layout
    /// and partrender.2da lists the remapped external skins of each layout.
    /// </summary>
    public sealed class PartSkinModelCatalog
    {
        public static readonly IReadOnlyDictionary<AppearanceArmor, string> PartNames = new Dictionary<AppearanceArmor, string>
        {
            { AppearanceArmor.RightFoot, "footr" }, { AppearanceArmor.LeftFoot, "footl" },
            { AppearanceArmor.RightShin, "shinr" }, { AppearanceArmor.LeftShin, "shinl" },
            { AppearanceArmor.LeftThigh, "legl" }, { AppearanceArmor.RightThigh, "legr" },
            { AppearanceArmor.Pelvis, "pelvis" }, { AppearanceArmor.Torso, "chest" },
            { AppearanceArmor.Belt, "belt" },
            { AppearanceArmor.RightForearm, "forer" }, { AppearanceArmor.LeftForearm, "forel" },
            { AppearanceArmor.RightBicep, "bicepr" }, { AppearanceArmor.LeftBicep, "bicepl" },
            { AppearanceArmor.RightShoulder, "shor" }, { AppearanceArmor.LeftShoulder, "shol" },
            { AppearanceArmor.RightHand, "handr" }, { AppearanceArmor.LeftHand, "handl" }
        };

        private static readonly Dictionary<string, AppearanceArmor> Parts =
            PartNames.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);
        private static readonly string PartPattern = string.Join("|", PartNames.Values);
        private static readonly Regex Source = new($@"^(p[fm][a-z])\d+_({PartPattern})(\d{{3}})$", RegexOptions.IgnoreCase);
        private static readonly Regex Render = new($@"^(p[fm][a-z])0_({PartPattern})(\d{{4,5}})$", RegexOptions.IgnoreCase);
        private static readonly Regex Body = new(@"^(p[fm][a-z])\d+$", RegexOptions.IgnoreCase);

        private readonly Dictionary<string, string> _layouts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string Layout, AppearanceArmor Part, ushort Style), ushort> _renders = new();

        public PartSkinModelCatalog(IEnumerable<(string Body, string Layout)> layouts,
            IEnumerable<(string Layout, string Source, string Render)> renders)
        {
            var families = new Dictionary<string, string>();
            foreach (var (body, layout) in layouts)
            {
                var match = Body.Match(body ?? string.Empty);
                if (!match.Success || string.IsNullOrWhiteSpace(layout) || !_layouts.TryAdd(body, layout) ||
                    families.TryGetValue(layout, out var family) &&
                    !family.Equals(match.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Invalid or duplicate part skin layout row.");
                families[layout] = match.Groups[1].Value;
            }

            var authored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (layout, source, render) in renders)
            {
                var sourceMatch = Source.Match(source ?? string.Empty);
                var renderMatch = Render.Match(render ?? string.Empty);
                if (layout == null || !families.TryGetValue(layout, out var family) ||
                    !sourceMatch.Success || !renderMatch.Success ||
                    !family.Equals(sourceMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                    !family.Equals(renderMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                    !sourceMatch.Groups[2].Value.Equals(renderMatch.Groups[2].Value, StringComparison.OrdinalIgnoreCase) ||
                    !ushort.TryParse(renderMatch.Groups[3].Value, out var id) || id < 1000 ||
                    !_renders.TryAdd((layout, Parts[sourceMatch.Groups[2].Value],
                        ushort.Parse(sourceMatch.Groups[3].Value)), id) ||
                    authored.TryGetValue(render, out var previous) &&
                    !previous.Equals(source, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Invalid or duplicate part skin rendering row.");
                authored[render] = source;
            }
        }

        public ushort Resolve(string body, AppearanceArmor part, ushort style) =>
            style != 0 && body != null && _layouts.TryGetValue(body, out var layout)
                ? _renders.GetValueOrDefault((layout, part, style), style)
                : style;
    }
}
