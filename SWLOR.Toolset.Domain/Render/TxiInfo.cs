using System.Globalization;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Blending mode declared by a TXI file's <c>blending</c> key.</summary>
    public enum TxiBlendMode
    {
        /// <summary>No <c>blending</c> key present, or an unrecognized value.</summary>
        None,
        Additive,
        PunchThrough
    }

    /// <summary>
    /// Minimal parse of an NWN TXI (texture info) file: plain-text <c>key value</c> lines that
    /// accompany a texture and hint at how it should be rendered. Only the transparency-relevant
    /// renderer keys are parsed (<c>blending</c>, <c>alphamean</c>, <c>envmaptexture</c>,
    /// <c>cube</c>); every other key (procedural water/fire animation parameters, mip/filter
    /// settings, etc.) is read past but otherwise ignored.
    /// </summary>
    public sealed class TxiInfo
    {
        public TxiBlendMode Blending { get; init; } = TxiBlendMode.None;

        /// <summary>Mean alpha value for the texture, when the <c>alphamean</c> key is present.</summary>
        public float? AlphaMean { get; init; }

        /// <summary>Environment map texture resref, when the <c>envmaptexture</c> key is present.</summary>
        public string? EnvMapTexture { get; init; }

        /// <summary>Whether the <c>cube</c> key was set to a truthy (non-zero) value.</summary>
        public bool Cube { get; init; }

        /// <summary>Whether this texture declares any hint relevant to alpha/transparency handling.</summary>
        public bool HasTransparencyHint => Blending != TxiBlendMode.None || AlphaMean.HasValue;

        /// <summary>
        /// Parse TXI text. Blank lines and lines starting with <c>#</c> (comments) are skipped.
        /// Each remaining line is split into a first whitespace-delimited token (the key) and the
        /// rest of the line (the value); multi-line array values that follow unrecognized keys
        /// (e.g. <c>channelscale</c>'s four bare-number continuation lines) are harmless here
        /// since a bare number never matches one of the known keys below.
        /// </summary>
        public static TxiInfo Parse(string text)
        {
            var blending = TxiBlendMode.None;
            float? alphaMean = null;
            string? envMapTexture = null;
            var cube = false;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r').Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var spaceIndex = line.IndexOfAny(WhitespaceChars);
                var key = spaceIndex < 0 ? line : line[..spaceIndex];
                var value = spaceIndex < 0 ? string.Empty : line[(spaceIndex + 1)..].Trim();

                switch (key.ToLowerInvariant())
                {
                    case "blending":
                        blending = value.ToLowerInvariant() switch
                        {
                            "additive" => TxiBlendMode.Additive,
                            "punchthrough" => TxiBlendMode.PunchThrough,
                            _ => TxiBlendMode.None
                        };
                        break;

                    case "alphamean":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedAlphaMean))
                            alphaMean = parsedAlphaMean;
                        break;

                    case "envmaptexture":
                        envMapTexture = value.Length == 0 ? null : value;
                        break;

                    case "cube":
                        cube = value.Trim() == "1";
                        break;

                    default:
                        // Unknown keys ignored.
                        break;
                }
            }

            return new TxiInfo
            {
                Blending = blending,
                AlphaMean = alphaMean,
                EnvMapTexture = envMapTexture,
                Cube = cube
            };
        }

        private static readonly char[] WhitespaceChars = { ' ', '\t' };
    }
}
