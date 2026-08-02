using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>Audio ResRefs available to an ambient sound's Sounds list.</summary>
    public static class SoundResourceCatalog
    {
        public static IReadOnlyList<string> Read(ResourceIndex? resources)
        {
            if (resources == null)
                return Array.Empty<string>();

            var wavType = ResourceIdentity.TypeFromExtension("wav");
            return resources.EnumerateResources(wavType)
                .Select(resource => resource.ResRef)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
