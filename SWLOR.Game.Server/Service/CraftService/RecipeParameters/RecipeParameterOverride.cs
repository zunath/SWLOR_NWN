using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Fixed values that replace the generated ones, with the reason a designer wants them.
    /// </summary>
    public sealed class RecipeParameterOverride
    {
        public string Reason { get; }
        public IReadOnlyDictionary<string, double> Values { get; }

        public RecipeParameterOverride(string reason, IReadOnlyDictionary<string, double> values)
        {
            Reason = reason;
            Values = values;
        }
    }
}
