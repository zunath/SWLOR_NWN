using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Declares one parameter of a minigame: its builder argument name, its Bible column header, the
    /// bounds every recipe's value must stay inside, and how far the generator may move a recipe's
    /// value away from the profile's base value.
    /// </summary>
    public sealed class RecipeParameterDefinition
    {
        private const double Tolerance = 1e-9;
        private static readonly Regex NamePattern = new Regex("^[a-z][A-Za-z0-9]*$", RegexOptions.Compiled);
        private static readonly Regex HeaderWhitespace = new Regex(@"\s+", RegexOptions.Compiled);

        private readonly HashSet<int> _enumValues;

        /// <summary>The constructor and builder argument name, in camelCase.</summary>
        public string Name { get; }

        /// <summary>The header of the Bible recipe tab column holding this value, with runs of whitespace collapsed to one space.</summary>
        public string Header { get; }

        public double Min { get; }
        public double Max { get; }

        /// <summary>Digits after the decimal point; values sit on a grid of 10^-Decimals.</summary>
        public int Decimals { get; }

        /// <summary>The largest distance the generator may move a recipe's value from the base value, in whole steps.</summary>
        public double Variation { get; }

        /// <summary>The enum whose members are the allowed values, or null for numeric parameters.</summary>
        public Type EnumType { get; }

        public double Step => Math.Pow(10, -Decimals);

        private RecipeParameterDefinition(
            string name,
            string header,
            double min,
            double max,
            int decimals,
            double variation,
            Type enumType)
        {
            if (string.IsNullOrEmpty(name) || !NamePattern.IsMatch(name))
                throw new ArgumentException($"Parameter name '{name}' must be a camelCase identifier.", nameof(name));
            if (string.IsNullOrWhiteSpace(header))
                throw new ArgumentException($"Parameter {name} needs a Bible header.", nameof(header));
            if (decimals < 0 || decimals > 6)
                throw new ArgumentOutOfRangeException(nameof(decimals));
            if (min > max)
                throw new ArgumentException($"Parameter {name} has min {min} above max {max}.");
            if (variation < 0)
                throw new ArgumentOutOfRangeException(nameof(variation));

            Name = name;
            Header = HeaderWhitespace.Replace(header, " ").Trim();
            Min = min;
            Max = max;
            Decimals = decimals;
            Variation = variation;
            EnumType = enumType;

            if (!IsOnGrid(min) || !IsOnGrid(max))
                throw new ArgumentException($"Parameter {name} has bounds off its {decimals}-decimal grid.");

            if (enumType != null)
            {
                _enumValues = new HashSet<int>(Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt32));
            }
        }

        /// <summary>A whole-number parameter.</summary>
        public static RecipeParameterDefinition Integer(string name, string header, int min, int max, int variation = 0)
        {
            return new RecipeParameterDefinition(name, header, min, max, 0, variation, null);
        }

        /// <summary>A parameter with a fixed number of decimal places. The constructor argument is a double.</summary>
        public static RecipeParameterDefinition Decimal(
            string name,
            string header,
            double min,
            double max,
            int decimals,
            double variation = 0)
        {
            return new RecipeParameterDefinition(name, header, min, max, decimals, variation, null);
        }

        /// <summary>
        /// A parameter chosen from the members of an enum. The enum must be declared where recipe
        /// definitions can see it, and its members are never varied.
        /// </summary>
        public static RecipeParameterDefinition Enumeration<TEnum>(string name, string header)
            where TEnum : struct, Enum
        {
            var values = Enum.GetValues(typeof(TEnum)).Cast<object>().Select(Convert.ToInt32).ToList();
            return new RecipeParameterDefinition(name, header, values.Min(), values.Max(), 0, 0, typeof(TEnum));
        }

        /// <summary>Rounds a value to this parameter's grid and clamps it to the bounds.</summary>
        public double Snap(double value)
        {
            var rounded = Math.Round(value, Decimals, MidpointRounding.AwayFromZero);
            return Math.Min(Max, Math.Max(Min, rounded)) + 0.0;
        }

        public bool IsInBounds(double value)
        {
            return value >= Min - Tolerance && value <= Max + Tolerance;
        }

        /// <summary>True when the value is on the grid, or is a member of the enum.</summary>
        public bool IsOnGrid(double value)
        {
            if (_enumValues != null)
                return Math.Abs(value - Math.Round(value)) < Tolerance && _enumValues.Contains((int)Math.Round(value));

            return Math.Abs(value - Math.Round(value, Decimals, MidpointRounding.AwayFromZero)) < Tolerance;
        }

        /// <summary>
        /// Moves a value by a deterministic number of steps chosen by <paramref name="unit"/>, a value in [0, 1).
        /// </summary>
        public double Vary(double baseValue, double unit)
        {
            var spanSteps = (int)Math.Floor(Variation / Step + Tolerance);
            if (spanSteps <= 0)
                return baseValue;

            var offset = (int)Math.Floor(unit * (2 * spanSteps + 1)) - spanSteps;
            return Snap(baseValue + offset * Step);
        }

        /// <summary>The text written into recipe source for a value.</summary>
        public string FormatLiteral(double value)
        {
            if (EnumType != null)
                return $"{EnumType.Name}.{Enum.GetName(EnumType, Enum.ToObject(EnumType, (int)Math.Round(value)))}";

            return FormatNumber(value);
        }

        /// <summary>The text written into the Bible cell for a value.</summary>
        public string FormatDisplay(double value)
        {
            if (EnumType != null)
                return Enum.GetName(EnumType, Enum.ToObject(EnumType, (int)Math.Round(value)));

            return FormatNumber(value);
        }

        private string FormatNumber(double value)
        {
            var rounded = Math.Round(value, Decimals, MidpointRounding.AwayFromZero) + 0.0;
            return rounded.ToString("F" + Decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }
    }
}
