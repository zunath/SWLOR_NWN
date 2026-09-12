using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Service.WeatherService
{
    public enum WeatherStorm
    {
        None,
        Thunder,
        Sand,
        Snow
    }

    public enum WeatherHazard
    {
        None,
        Acid,
        Sand,
        Snow
    }

    public sealed record WeatherConditions(int Heat, int Humidity, int Wind, Precipitation Precipitation, WeatherStorm Storm)
    {
        public static WeatherConditions Create(
            int heat, int humidity, int wind, WeatherClimate climate,
            int heatModifier, int humidityModifier, int windModifier, bool isNatural,
            WeatherStorm previousStorm, Func<int, int> random)
        {
            heat = Math.Clamp(heat + climate.HeatModifier + heatModifier, climate.MinimumHeat, climate.MaximumHeat);
            humidity = Math.Clamp(humidity + climate.HumidityModifier + humidityModifier, 1, 10);
            wind = Math.Clamp(wind + climate.WindModifier + windModifier - (isNatural ? 0 : 1), 1, 10);

            var precipitation = Precipitation.Clear;
            if (humidity > 7)
            {
                precipitation = heat <= 3 ? Precipitation.Snow :
                    heat < 6 && wind < 3 ? Precipitation.Foggy : Precipitation.Rain;
            }

            var storm = WeatherStorm.None;
            if (precipitation == Precipitation.Rain && heat > 4 &&
                (previousStorm == WeatherStorm.Thunder ? random(3) == 0 : random(20) < wind))
            {
                storm = WeatherStorm.Thunder;
            }
            else if (wind >= 9 && (climate.HasSandStorms || climate.HasSnowStorms) && random(3) == 0)
            {
                storm = climate.HasSandStorms ? WeatherStorm.Sand : WeatherStorm.Snow;
            }

            return new WeatherConditions(heat, humidity, wind, precipitation, storm);
        }

        public WeatherHazard GetHazard(bool acidRain, Precipitation? actualPrecipitation = null)
        {
            if (acidRain && (actualPrecipitation ?? Precipitation) == Precipitation.Rain) return WeatherHazard.Acid;
            if (Storm == WeatherStorm.Sand) return WeatherHazard.Sand;
            if (Storm == WeatherStorm.Snow) return WeatherHazard.Snow;
            return WeatherHazard.None;
        }

        public string GetFeedback(WeatherClimate climate, bool isNight, bool acidRain, Precipitation? actualPrecipitation = null)
        {
            switch (GetHazard(acidRain, actualPrecipitation))
            {
                case WeatherHazard.Acid: return WeatherFeedbackText.AcidRain;
                case WeatherHazard.Sand: return WeatherFeedbackText.SandStorm;
                case WeatherHazard.Snow: return WeatherFeedbackText.SnowStorm;
            }

            if (Storm == WeatherStorm.Thunder) return climate.StormText;
            if (Precipitation == Precipitation.Foggy) return climate.MistText;
            if (Precipitation == Precipitation.Rain) return Heat > 7 ? climate.RainWarmText : climate.RainNormalText;
            if (Precipitation == Precipitation.Snow) return climate.SnowText;
            if (Heat < 3) return climate.FreezingText;
            if (Heat > 8) return climate.ScorchingText;
            if (Heat < 5) return Wind < 5 ? climate.ColdMildText : Wind < 8 ? climate.ColdCloudyText : climate.ColdWindyText;
            if (Heat > 6) return Wind < 5 ? climate.WarmMildText : Wind < 8 ? climate.WarmCloudyText : climate.WarmWindyText;
            if (Wind < 5) return isNight ? climate.MildNightText : climate.MildText;
            return Wind < 8 ? climate.CloudyText : climate.WindyText;
        }

        public static int GetLightningDamage(int power, float distance)
        {
            return Math.Max(0, (int)(power - distance * 10f));
        }
    }
}
