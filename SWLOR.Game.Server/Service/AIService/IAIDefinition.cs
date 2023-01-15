using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public interface IAIDefinition
    {
        /// <summary>
        /// Runs any AI pre-processing which is used by DeterminePerkAbility.
        /// This should be used to aggregate/update any cached data.
        /// </summary>
        /// <param name="self">The creature</param>
        /// <param name="target">The target</param>
        /// <param name="allies">Allies associated with this creature. Should also include this creature.</param>
        void PreProcessAI(uint self, uint target, List<uint> allies);

        /// <summary>
        /// Determines which perk ability to use.
        /// </summary>
        /// <returns>A feat and target</returns>
        (FeatType, uint) DeterminePerkAbility();
    }
}
