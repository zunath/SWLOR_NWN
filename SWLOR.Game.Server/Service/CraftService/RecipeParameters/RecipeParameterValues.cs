using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// The generated values of one recipe, by parameter name, handed to a profile to build its parameter type.
    /// </summary>
    public sealed class RecipeParameterValues
    {
        private readonly IReadOnlyDictionary<string, double> _values;

        public RecipeParameterValues(IReadOnlyDictionary<string, double> values)
        {
            _values = values;
        }

        public double Double(string name)
        {
            if (!_values.TryGetValue(name, out var value))
                throw new KeyNotFoundException($"No generated value named '{name}'.");

            return value;
        }

        public int Int(string name)
        {
            return (int)Math.Round(Double(name));
        }

        public TEnum EnumValue<TEnum>(string name)
            where TEnum : struct, Enum
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), Int(name));
        }
    }
}
