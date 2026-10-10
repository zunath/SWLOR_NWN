using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition
{
    public static class ModelResource
    {
        public static bool Exists(string model) => Exists(model,
            (name, type) => !string.IsNullOrEmpty(ResManGetAliasFor(name, type)),
            name => ResManGetFileContents(name, (int)ResType.LOD));

        public static bool Exists(string model, Func<string, ResType, bool> hasResource,
            Func<string, string> readLod)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (IsResRef(model) && visited.Count < 16 && visited.Add(model))
            {
                // The client resolves the first LOD model before looking for an MDL.
                if (!hasResource(model, ResType.LOD))
                    return hasResource(model, ResType.MDL);

                var contents = readLod(model);
                if (string.IsNullOrWhiteSpace(contents))
                    return false;
                model = contents.Split('\n')[0].Trim();
            }
            return false;
        }

        private static bool IsResRef(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 16)
                return false;
            foreach (var character in name)
                if (!(character >= 'a' && character <= 'z') &&
                    !(character >= 'A' && character <= 'Z') &&
                    !(character >= '0' && character <= '9') && character != '_')
                    return false;
            return true;
        }
    }
}
