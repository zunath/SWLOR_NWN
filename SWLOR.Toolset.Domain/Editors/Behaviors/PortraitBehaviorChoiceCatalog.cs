using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// Builds the one portrait catalog shared by every behavior editor. Keeping the visual choice
    /// metadata here means creature and door portrait pickers cannot drift into different filters,
    /// labels, or ordering behavior.
    /// </summary>
    public static class PortraitBehaviorChoiceCatalog
    {
        public const string GenderFacetKey = "gender";
        public const string RaceFacetKey = "race";
        public const string SubjectFacetKey = "subject";

        private const string UnspecifiedKey = "unspecified";

        public static IReadOnlyList<BehaviorChoice> Build(
            IReadOnlyList<PortraitRow> portraits,
            IReadOnlyDictionary<int, string> genderNames,
            IReadOnlyDictionary<int, string> raceNames)
        {
            ArgumentNullException.ThrowIfNull(portraits);
            ArgumentNullException.ThrowIfNull(genderNames);
            ArgumentNullException.ThrowIfNull(raceNames);

            return portraits.Select(row =>
                new BehaviorChoice(
                    row.Id,
                    $"{row.BaseResRef} ({row.Id})",
                    PortraitService.GetTgaVariants(row.BaseResRef).Medium)
                {
                    ImageCrop = BehaviorChoiceImageCrop.NeverwinterPortrait,
                    GalleryFacets =
                    [
                        LookupFacet(
                            GenderFacetKey,
                            "Gender",
                            row.Sex,
                            genderNames,
                            row.Sex ?? int.MaxValue),
                        LookupFacet(
                            RaceFacetKey,
                            "Race",
                            row.Race,
                            raceNames),
                        row.InanimateType.HasValue
                            ? new BehaviorChoiceFacet(
                                SubjectFacetKey, "Subject", "inanimate", "Inanimate", 1)
                            : new BehaviorChoiceFacet(
                                SubjectFacetKey, "Subject", "creature", "Creature", 0)
                    ]
                }).ToList();
        }

        private static BehaviorChoiceFacet LookupFacet(
            string groupKey,
            string groupLabel,
            int? value,
            IReadOnlyDictionary<int, string> names,
            int order = 0)
        {
            if (value.HasValue && names.TryGetValue(value.Value, out var display) &&
                !string.IsNullOrWhiteSpace(display))
            {
                return new BehaviorChoiceFacet(
                    groupKey,
                    groupLabel,
                    value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    display,
                    order);
            }

            // A custom value absent from the authoritative lookup is not a new gender or race the
            // editor should name. Keep it findable without presenting invalid engine data as valid.
            return new BehaviorChoiceFacet(
                groupKey, groupLabel, UnspecifiedKey, "Unspecified", int.MaxValue);
        }
    }
}
