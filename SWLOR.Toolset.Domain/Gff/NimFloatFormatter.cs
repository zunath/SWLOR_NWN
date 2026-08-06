using System.Globalization;
using System.Numerics;
using System.Text;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Formats floating-point values exactly the way nwn_gff prints them, so newly written
    /// values are indistinguishable from values the packer would produce.
    ///
    /// nwn_gff (Nim on Windows) prints doubles via C sprintf("%.16g") with MSVCRT semantics,
    /// then appends ".0" when the result contains neither '.' nor 'e'. That means:
    /// 16 significant digits with exact-tie rounding half away from zero, %g trailing-zero
    /// stripping, scientific notation outside the fixed range, and 3-digit zero-padded
    /// exponents (e.g. 9.18e-039). Implemented over the exact decimal expansion of the double
    /// (every double has a finite one) rather than .NET formatting, whose shortest-round-trip
    /// and tie-to-even behavior differ. Conformance against every float literal in the module
    /// corpus is enforced by tests.
    /// </summary>
    public static class NimFloatFormatter
    {
        private const int SignificantDigits = 16;

        public static string Format(double value)
        {
            if (double.IsNaN(value))
                return "nan";
            if (double.IsPositiveInfinity(value))
                return "inf";
            if (double.IsNegativeInfinity(value))
                return "-inf";

            var negative = double.IsNegative(value);
            string text;
            if (value == 0.0)
            {
                text = "0";
            }
            else
            {
                var (digits, decimalExponent) = RoundSignificant(Math.Abs(value));
                text = FormatGeneral(digits, decimalExponent);
            }

            if (negative)
                text = "-" + text;

            if (!text.Contains('.') && !text.Contains('e'))
                text += ".0";

            return text;
        }

        /// <summary>Formats a 32-bit float by widening to double first, matching nwn_gff.</summary>
        public static string Format(float value)
        {
            return Format((double)value);
        }

        public static double Parse(string text)
        {
            return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Produces the value's significant digits rounded to 16 (half away from zero on exact
        /// ties, which only occur because doubles have finite decimal expansions), plus the
        /// decimal exponent of the first digit (value = d.ddd × 10^exponent).
        /// </summary>
        private static (string Digits, int Exponent) RoundSignificant(double value)
        {
            var bits = BitConverter.DoubleToUInt64Bits(value);
            var biasedExponent = (int)((bits >> 52) & 0x7FF);
            var mantissa = bits & 0xF_FFFF_FFFF_FFFF;

            int binaryExponent;
            BigInteger significand;
            if (biasedExponent == 0)
            {
                binaryExponent = -1074;
                significand = mantissa;
            }
            else
            {
                binaryExponent = biasedExponent - 1075;
                significand = mantissa | 0x10_0000_0000_0000;
            }

            // Exact decimal digit string: value = significand * 2^binaryExponent.
            string exact;
            int pointPosition; // number of digits left of the decimal point
            if (binaryExponent >= 0)
            {
                exact = (significand << binaryExponent).ToString();
                pointPosition = exact.Length;
            }
            else
            {
                var scale = -binaryExponent;
                exact = (significand * BigInteger.Pow(5, scale)).ToString();
                pointPosition = exact.Length - scale;
            }

            var exponent = pointPosition - 1;

            // MSVCRT (the C runtime behind nwn_gff's printf) double-rounds: the exact
            // expansion is first rounded to a 17-significant-digit intermediate, and that
            // intermediate is then rounded to the requested 16 digits — both stages half away
            // from zero. Pinned by the corpus conformance test, which round-trips float-typed
            // literals through the float32 funnel the pack/unpack pipeline actually uses.
            var kept = RoundToDigits(exact, SignificantDigits + 1, ref exponent);
            kept = RoundToDigits(kept, SignificantDigits, ref exponent);

            kept = kept.TrimEnd('0');
            if (kept.Length == 0)
                kept = "0";

            return (kept, exponent);
        }

        private static string RoundToDigits(string digits, int keep, ref int exponent)
        {
            if (digits.Length <= keep)
                return digits;

            var kept = digits[..keep];

            // The remainder's leading digit decides: >= '5' means at-least-half of an exact
            // (or already-rounded) expansion, and ties round away from zero.
            if (digits[keep] < '5')
                return kept;

            var buffer = kept.ToCharArray();
            var i = buffer.Length - 1;
            while (i >= 0)
            {
                if (buffer[i] != '9')
                {
                    buffer[i]++;
                    break;
                }

                buffer[i] = '0';
                i--;
            }

            if (i < 0)
            {
                exponent++;
                return "1" + new string('0', keep - 1);
            }

            return new string(buffer);
        }

        /// <summary>C %g presentation: fixed inside [-4, 16), scientific with a 3-digit
        /// zero-padded exponent outside.</summary>
        private static string FormatGeneral(string digits, int exponent)
        {
            if (exponent < -4 || exponent >= SignificantDigits)
            {
                var mantissa = digits.Length == 1 ? digits : $"{digits[0]}.{digits[1..]}";
                var sign = exponent < 0 ? '-' : '+';
                return $"{mantissa}e{sign}{Math.Abs(exponent):000}";
            }

            if (exponent >= 0)
            {
                var integerLength = exponent + 1;
                if (digits.Length <= integerLength)
                    return digits + new string('0', integerLength - digits.Length);

                return $"{digits[..integerLength]}.{digits[integerLength..]}";
            }

            return "0." + new string('0', -exponent - 1) + digits;
        }
    }
}
