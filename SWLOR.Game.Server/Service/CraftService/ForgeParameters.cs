using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The values the Plasma Forge reads for one Smithery recipe. The working range runs from
    /// <c>RangeCenter - RangeWidth / 2</c> for <c>RangeWidth</c> points of Thermal.
    /// </summary>
    public sealed class ForgeParameters : RecipeMinigameParameters
    {
        /// <summary>The middle of the metal's working range on the 0-100 Thermal meter.</summary>
        public int RangeCenter { get; }

        /// <summary>How many Thermal points the working range spans.</summary>
        public int RangeWidth { get; }

        /// <summary>How many points of Thermal the piece loses after every action.</summary>
        public int CoolingRate { get; }

        /// <summary>The Thermal the piece starts at.</summary>
        public int StartingThermal { get; }

        /// <summary>The percentage of the work that is Shape; the rest is Finish. Armor is mostly Shape, weapons mostly Finish.</summary>
        public int ShapeShare { get; }

        public ForgeParameters(int rangeCenter, int rangeWidth, int coolingRate, int startingThermal, int shapeShare)
        {
            RangeCenter = rangeCenter;
            RangeWidth = rangeWidth;
            CoolingRate = coolingRate;
            StartingThermal = startingThermal;
            ShapeShare = shapeShare;
        }

        /// <summary>The lowest Thermal inside the working range.</summary>
        public int RangeLow => RangeCenter - RangeWidth / 2;

        /// <summary>The highest Thermal inside the working range.</summary>
        public int RangeHigh => RangeLow + RangeWidth;

        public override IReadOnlyList<RecipeParameterValue> Values => new[]
        {
            new RecipeParameterValue("rangeCenter", RangeCenter),
            new RecipeParameterValue("rangeWidth", RangeWidth),
            new RecipeParameterValue("coolingRate", CoolingRate),
            new RecipeParameterValue("startingThermal", StartingThermal),
            new RecipeParameterValue("shapeShare", ShapeShare),
        };
    }
}
