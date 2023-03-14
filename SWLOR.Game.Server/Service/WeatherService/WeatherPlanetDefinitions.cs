using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.WeatherService
{
    public static class WeatherPlanetDefinitions
    {
        public static Dictionary<PlanetType, WeatherClimate> GetPlanetClimates()
        {
            return new Dictionary<PlanetType, WeatherClimate>
            {
                [PlanetType.Viscara] = new WeatherClimate
                {
                    HeatModifier = -2,
                    HumidityModifier = +2
                },
                [PlanetType.Tatooine] = new WeatherClimate
                {
                    HeatModifier = +5,
                    HumidityModifier = -8,
                    HasSandStorms = true,
                    CloudyText = "A dusty wind sweeps through the desert; sparse clouds speed overhead.",
                    MildText = "The sun shines brilliantly, but not oppressively, over the desert; the sky is clear.",
                    MildNightText = "A clear night sky casts the desert in pale hues.",
                    WarmCloudyText =
                        "The shade of an overcast sky provides only minor relief to the sweltering temperatures.",
                    ScorchingText = "The desert is baked with pervasive, inescapable heat; a haze blurs the horizon.",
                    WarmWindyText = "The hot wind wears at your face like a sandblaster.  A sand storm seems likely.",
                    WindyText = "A scouring wind sweeps across the desert, a sand storm cannot be far away.",
                },
                [PlanetType.MonCala] = new WeatherClimate
                {
                    WindModifier = +1,
                    HeatModifier = +1,
                    CloudyText = "Clouds build over the ocean, and the wind starts to pick up.  A storm could be brewing.",
                    ColdCloudyText = "Thick clouds fill the sky, and a keen wind blows in off the ocean, exciting the waves.",
                    ColdMildText = "It is cool, but calm.  The ocean is calm and beautiful.",
                    FreezingText = "A wave of cold air rolls in, stinging exposed flesh.",
                    MildText = "The sea is calm, a faint breeze rippling through the trees.",
                    MildNightText = "The sea is calm, and the sky towards the Galactic Core is full of stars.  In other directions, you see only a deep, unending black.",
                    MistText = "A mist has blown in off the sea, moisture hanging heavy in the air.",
                    WarmCloudyText = "The sea is choppy and the wind has picked up. An array of clouds marshals on the horizon, ready to sweep over you.",
                    WarmMildText = "It is a beautiful day, warm and calm, though quite humid.",
                    RainNormalText = "The ocean, affronted by the existence of patches of non-ocean on the surface of the planet, is attempting to reclaim the land by air drop.  In other words, it's raining.",
                    RainWarmText = "A heavy rain shower is passing over, but is doing little to dispel the humidity in the air.",
                    SnowText = "It's snowing!  The local flora seems most surprised at this turn of events.",
                    StormText = "A storm rips in off the sea, filling the sky with dramatic flashes.",
                    ScorchingText = "The sun bakes the sand, making it extremely uncomfortable to those without insulated boots.",
                    ColdWindyText = "A chill wind sweeps over the isles, the moisture in the air cutting to the bone.",
                    WarmWindyText = "The wind is picking up, a warm front rolling over.  There could be a storm soon.",
                    WindyText = "A strong wind sweeps in.  The sea is choppy, waves crashing onto the beach.",
                },
                [PlanetType.Hutlar] = new WeatherClimate
                {
                    HeatModifier = -8,
                    HumidityModifier = -8,
                    HasSnowStorms = true,
                    FreezingText = "A wave of cold air rolls in, stinging exposed flesh.",
                    SnowText = "It's snowing... again...",
                    WindyText = "A cold wind sweeps in.",
                    ColdWindyText = "A freezing wind stings exposed flesh",
                    CloudyText = "Clouds build over head, and there is a occasional strong gust of wind.",
                    ColdCloudyText = "The clouds over head build, a cold wind stings exposed flesh. Looks like it is going to snow.",
                    MildText = "It is cold, the sky is clear, and there is a gentle breeze.",
                    WarmCloudyText = "It is cold."
                },
                [PlanetType.Korriban] = new WeatherClimate
                {
                    HeatModifier = +3,
                    HumidityModifier = -5
                },
                [PlanetType.Dathomir] = new WeatherClimate
                {
                    HeatModifier = -1,
                    HumidityModifier = +1
                },
                [PlanetType.Dantooine] = new WeatherClimate
                 {
                     HeatModifier = -1,
                     HumidityModifier = +1
                 },
            };
        }
    }
}
