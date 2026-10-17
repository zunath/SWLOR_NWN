using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public sealed class ShoulderModelCatalog
    {
        private readonly Dictionary<string, ushort> _models = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ushort> _authoredStyles = new(StringComparer.OrdinalIgnoreCase);

        public ShoulderModelCatalog(IEnumerable<(string Body, string Source, string Render)> rows)
        {
            foreach (var (body, source, render) in rows)
            {
                var sourceMatch = Regex.Match(source ?? string.Empty, @"^(pmh)0_sho([lr])(\d+)$", RegexOptions.IgnoreCase);
                var renderMatch = Regex.Match(render ?? string.Empty, @"^(pmh)0_sho([lr])(\d+)$", RegexOptions.IgnoreCase);
                if (!Regex.IsMatch(body ?? string.Empty, @"^pmh\d+$", RegexOptions.IgnoreCase) ||
                    !sourceMatch.Success || !renderMatch.Success ||
                    !sourceMatch.Groups[2].Value.Equals(renderMatch.Groups[2].Value, StringComparison.OrdinalIgnoreCase) ||
                    !ushort.TryParse(renderMatch.Groups[3].Value, out var id) || id < 1000 ||
                    !ushort.TryParse(sourceMatch.Groups[3].Value, out var sourceId) || sourceId >= 1000 ||
                    !_models.TryAdd(body + "/" + source, id) ||
                    _authoredStyles.TryGetValue(render, out var authored) && authored != sourceId)
                    throw new ArgumentException("Invalid or duplicate shoulder rendering row.");
                _authoredStyles[render] = sourceId;
            }
        }

        public ushort Resolve(string body, string source, ushort original) =>
            _models.GetValueOrDefault(body + "/" + source, original);

        public ushort GetAuthoredStyle(string body, AppearanceArmor part, ushort style)
        {
            var side = part == AppearanceArmor.LeftShoulder ? 'l' : 'r';
            return style >= 1000 && body?.StartsWith("pmh", StringComparison.OrdinalIgnoreCase) == true &&
                   part is AppearanceArmor.LeftShoulder or AppearanceArmor.RightShoulder &&
                   _authoredStyles.TryGetValue($"pmh0_sho{side}{style}", out var authored)
                ? authored : style;
        }
    }
}
