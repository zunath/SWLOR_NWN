using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Keeps a droid's saved tint overrides in step with the colours its controller item records. Static callers
    /// reach the container's instance through <c>GameServices.TintMapDroidOverride</c>.
    /// </summary>
    public interface ITintMapDroidOverrideService
    {
        /// <summary>Removes the named overrides from the droid's saved record.</summary>
        void RemoveDroidOverrides(uint creature, IReadOnlyList<string> variableNames);

        /// <summary>Saves one colour for each named override on the droid's record.</summary>
        void SaveDroidOverrides(uint creature, IReadOnlyList<string> variableNames, int savedColor);

        /// <summary>
        /// Saves one material colour: on the droid's own record when the droid holds the palette, otherwise by
        /// refreshing the equipped item's snapshot.
        /// </summary>
        void SaveDroidOverride(uint creature, TintMapMaterialSelection selection, TintMapLayerType layer,
            string variableName, int savedColor);
    }
}
