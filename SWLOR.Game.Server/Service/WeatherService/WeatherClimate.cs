namespace SWLOR.Game.Server.Service.WeatherService
{
    public class WeatherClimate
    {
        public int HeatModifier { get; set; } = 0;
        public int HumidityModifier { get; set; } = 0;
        public int WindModifier { get; set; } = 0;

        public bool HasSandStorms { get; set; } = false;
        public bool HasSnowStorms { get; set; } = false;

        // Allow overrides of text on different planets.
        public string CloudyText { get; set; } = WeatherFeedbackText.Cloudy;
        public string ColdCloudyText { get; set; } = WeatherFeedbackText.ColdCloudy;
        public string ColdMildText { get; set; } = WeatherFeedbackText.ColdMild;
        public string ColdWindyText { get; set; } = WeatherFeedbackText.ColdWindy;
        public string FreezingText { get; set; } = WeatherFeedbackText.Freezing;
        public string MildText { get; set; } = WeatherFeedbackText.Mild;
        public string MildNightText { get; set; } = WeatherFeedbackText.MildNight;
        public string MistText { get; set; } = WeatherFeedbackText.Mist;
        public string WarmCloudyText { get; set; } = WeatherFeedbackText.WarmCloudy;
        public string WarmMildText { get; set; } = WeatherFeedbackText.WarmMild;
        public string WarmWindyText { get; set; } = WeatherFeedbackText.WarmWindy;
        public string RainNormalText { get; set; } = WeatherFeedbackText.RainNormal;
        public string RainWarmText { get; set; } = WeatherFeedbackText.RainWarm;
        public string ScorchingText { get; set; } = WeatherFeedbackText.Scorching;
        public string SnowText { get; set; } = WeatherFeedbackText.Snow;
        public string StormText { get; set; } = WeatherFeedbackText.Storm;
        public string WindyText { get; set; } = WeatherFeedbackText.Windy;
    }
}
