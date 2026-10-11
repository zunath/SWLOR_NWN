using System.Text;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>Resolves the first model of an EE LOD resource, following the client's precedence.</summary>
    public static class ModelResourceResolver
    {
        public static bool TryResolve(ResourceIndex resources, string resRef, out ResourceHandle handle)
        {
            handle = null!;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                while (IsResRef(resRef) && visited.Count < 16 && visited.Add(resRef))
                {
                    if (!resources.TryLookup(ResourceIdentity.FromFileName(resRef + ".lod"), out var lod))
                        return resources.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out handle);

                    resRef = Encoding.UTF8.GetString(lod.GetBytes(65536)).Split('\n')[0].Trim();
                }
            }
            catch (Exception)
            {
                // Broken optional artwork must not prevent previews or catalogs from loading.
            }
            return false;
        }

        private static bool IsResRef(string name) =>
            !string.IsNullOrEmpty(name) && name.Length <= 16 &&
            name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
    }
}
