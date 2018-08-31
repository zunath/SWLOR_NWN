using System;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class MenuService: IMenuService
    {
        private readonly IColorTokenService _color;

        public MenuService(IColorTokenService color)
        {
            _color = color;
        }

        public string BuildBar(int currentValue, int requiredValue, int numberOfBars, string colorToken = null)
        {
            if (currentValue < 0) throw new ArgumentOutOfRangeException(nameof(currentValue), "Must be zero or greater.");
            if (requiredValue <= 0) throw new ArgumentOutOfRangeException(nameof(requiredValue), "Must be one or greater.");
            if (numberOfBars <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfBars), "Must be one or greater");

            if (colorToken == null)
                colorToken = _color.TokenStart(255, 127, 0); // Orange

            string xpBar = string.Empty;
            int highlightedBars = (int)(currentValue / (float)requiredValue * numberOfBars);

            for (int bar = 1; bar <= numberOfBars; bar++)
            {
                if (bar <= highlightedBars)
                {
                    xpBar += colorToken + "|" + _color.TokenEnd();
                }
                else
                {
                    xpBar += _color.TokenStart(255, 255, 255) + "|" + _color.TokenEnd(); // White
                }
            }
            
            return xpBar;
        }
    }
}
