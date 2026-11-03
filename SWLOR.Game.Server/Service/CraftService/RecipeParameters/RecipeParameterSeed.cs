using System.Globalization;
using System.Text;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Stable per-recipe randomness. It hashes names with FNV-1a and never uses string.GetHashCode,
    /// which changes between runs, so generated values are the same on every machine and every run.
    /// </summary>
    public static class RecipeParameterSeed
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        /// <summary>The 64-bit FNV-1a hash of the UTF-8 bytes of the text.</summary>
        public static ulong Hash(string text)
        {
            var hash = FnvOffsetBasis;
            foreach (var value in Encoding.UTF8.GetBytes(text))
            {
                hash ^= value;
                hash = unchecked(hash * FnvPrime);
            }

            return hash;
        }

        /// <summary>
        /// A value in [0, 1) that depends only on the four inputs. The recipe name is the
        /// <see cref="RecipeType"/> member name, and each parameter draws independently, so adding a
        /// parameter to a profile never changes the others.
        /// </summary>
        public static double Unit(string profileKey, string recipeName, string parameterName, int attempt)
        {
            var text = string.Join("|", profileKey, recipeName, parameterName, attempt.ToString(CultureInfo.InvariantCulture));
            return (Mix(Hash(text)) >> 11) * (1.0 / 9007199254740992.0);
        }

        private static ulong Mix(ulong value)
        {
            unchecked
            {
                value ^= value >> 33;
                value *= 0xff51afd7ed558ccdUL;
                value ^= value >> 33;
                value *= 0xc4ceb9fe1a85ec53UL;
                value ^= value >> 33;
                return value;
            }
        }
    }
}
