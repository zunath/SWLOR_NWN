using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>Named categories from the ambient-sound palette.</summary>
    public static class SoundPaletteCategoryReader
    {
        public static IReadOnlyList<BehaviorChoice> Read(
            ItpDocument palette,
            Func<uint, string?>? resolveStrRef = null)
        {
            return PaletteCategoryReader.Read(palette, resolveStrRef)
                .Cast<BehaviorChoice>()
                .ToList();
        }
    }
}
