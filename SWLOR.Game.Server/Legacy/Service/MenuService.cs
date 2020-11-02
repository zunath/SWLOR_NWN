using System;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class MenuService
    {
        public static string BuildBar(int currentValue, int requiredValue, int numberOfBars, string colorToken = null)
        {
            if (currentValue < 0) throw new ArgumentOutOfRangeException(nameof(currentValue), "Must be zero or greater.");
            if (requiredValue <= 0) throw new ArgumentOutOfRangeException(nameof(requiredValue), "Must be one or greater.");
            if (numberOfBars <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfBars), "Must be one or greater");

            if (colorToken == null)
                colorToken = ColorToken.TokenStart(255, 127, 0); // Orange

            var xpBar = string.Empty;
            var highlightedBars = (int)(currentValue / (float)requiredValue * numberOfBars);

            for (var bar = 1; bar <= numberOfBars; bar++)
            {
                if (bar <= highlightedBars)
                {
                    xpBar += colorToken + "|" + ColorToken.TokenEnd();
                }
                else
                {
                    xpBar += ColorToken.TokenStart(255, 255, 255) + "|" + ColorToken.TokenEnd(); // White
                }
            }
            
            return xpBar;
        }
    }
}
