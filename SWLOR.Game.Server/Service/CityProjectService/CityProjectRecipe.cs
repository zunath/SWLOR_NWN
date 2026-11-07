using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>What one City Project asks for besides the treasury's credits: the materials anyone may donate.</summary>
    public sealed class CityProjectRecipe
    {
        public CityProjectRecipe(CityProjectType type, int targetLevel, IReadOnlyDictionary<string, int> materials)
        {
            Type = type;
            TargetLevel = targetLevel;
            Materials = materials;
        }

        public CityProjectType Type { get; }

        /// <summary>The facility level the project builds, or 0 for Home Ground.</summary>
        public int TargetLevel { get; }

        /// <summary>Item resref to the number the project needs.</summary>
        public IReadOnlyDictionary<string, int> Materials { get; }
    }
}
