using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public sealed class NeckModelCatalog
    {
        private readonly Dictionary<string, ushort> _models = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ushort> _authoredStyles = new(StringComparer.OrdinalIgnoreCase);

        public NeckModelCatalog(IEnumerable<(string Body, string Source, string Render)> rows)
        {
            foreach (var (body, source, render) in rows)
            {
                var sourceMatch = Regex.Match(source ?? string.Empty, @"^(p[fm][a-z])\d+_neck(\d+)$", RegexOptions.IgnoreCase);
                var renderMatch = Regex.Match(render ?? string.Empty, @"^(p[fm][a-z])0_neck(\d+)$", RegexOptions.IgnoreCase);
                if (!Regex.IsMatch(body ?? string.Empty, @"^p[fm][a-z]\d+$", RegexOptions.IgnoreCase) ||
                    !sourceMatch.Success || !renderMatch.Success ||
                    !body.StartsWith(sourceMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                    !renderMatch.Groups[1].Value.Equals(sourceMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                    !ushort.TryParse(renderMatch.Groups[2].Value, out var id) || id < 1000 ||
                    !ushort.TryParse(sourceMatch.Groups[2].Value, out var sourceId) || sourceId >= 1000 ||
                    !_models.TryAdd(body + "/" + source, id) ||
                    _authoredStyles.TryGetValue(render, out var authored) && authored != sourceId)
                    throw new ArgumentException("Invalid or duplicate neck rendering row.");
                _authoredStyles[render] = sourceId;
            }
        }

        public ushort Resolve(string body, string source, ushort original) =>
            _models.GetValueOrDefault(body + "/" + source, original);

        // A projected alias read back from a creature must not stick after its body changes.
        public ushort GetAuthoredStyle(string body, ushort style) =>
            style >= 1000 && body?.Length >= 3 &&
            _authoredStyles.TryGetValue($"{body[..3]}0_neck{style}", out var authored)
                ? authored
                : style;
    }
}
