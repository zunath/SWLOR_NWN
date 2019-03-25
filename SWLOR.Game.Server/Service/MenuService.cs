using System;


namespace SWLOR.Game.Server.Service
{
    public static class MenuService
    {
        public static string BuildBar(int currentValue, int requiredValue, int numberOfBars, string colorToken = null)
        {
            if (currentValue < 0) throw new ArgumentOutOfRangeException(nameof(currentValue), "Must be zero or greater.");
            if (requiredValue <= 0) throw new ArgumentOutOfRangeException(nameof(requiredValue), "Must be one or greater.");
            if (numberOfBars <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfBars), "Must be one or greater");

            if (colorToken == null)
                colorToken = ColorTokenService.TokenStart(255, 127, 0); // Orange

            string xpBar = string.Empty;
            int highlightedBars = (int)(currentValue / (float)requiredValue * numberOfBars);

            for (int bar = 1; bar <= numberOfBars; bar++)
            {
                if (bar <= highlightedBars)
                {
                    xpBar += colorToken + "|" + ColorTokenService.TokenEnd();
                }
                else
                {
                    xpBar += ColorTokenService.TokenStart(255, 255, 255) + "|" + ColorTokenService.TokenEnd(); // White
                }
            }
            
            return xpBar;
        }
    }
}
