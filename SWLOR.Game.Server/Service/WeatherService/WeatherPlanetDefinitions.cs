using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.WeatherService
{
    public static class WeatherPlanetDefinitions
    {
        // These profiles also support authored areas outside the active galaxy map.
        // Unknown overrides deliberately do not fall back to a different planet.
        public static Dictionary<string, WeatherClimate> GetNamedClimates(Dictionary<PlanetType, WeatherClimate> planets)
        {
            var climates = new Dictionary<string, WeatherClimate>(StringComparer.OrdinalIgnoreCase);
            foreach (var (planet, climate) in planets) climates.Add(planet.ToString(), climate);
            climates["Kashyyyk"] = new WeatherClimate
            {
                HeatModifier = 3, HumidityModifier = 3, MinimumHeat = 4
            };
            climates["Ossus"] = new WeatherClimate
            {
                HeatModifier = 2, HumidityModifier = -1, MinimumHeat = 4
            };
            climates["None"] = new WeatherClimate { IsSheltered = true };
            return climates;
        }

        public static WeatherClimate ResolveClimate(PlanetType planet, string climateOverride,
            IReadOnlyDictionary<PlanetType, WeatherClimate> planets, IReadOnlyDictionary<string, WeatherClimate> named)
        {
            if (!string.IsNullOrWhiteSpace(climateOverride))
                return named.TryGetValue(climateOverride.Trim(), out var custom) ? custom : null;
            return planets.TryGetValue(planet, out var climate) ? climate : null;
        }

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
                    StormText = "A storm rips in off the sea. " + WeatherFeedbackText.Storm,
                    ScorchingText = "The sun bakes the sand, and heat shimmers above the beach.",
                    ColdWindyText = "A chill wind sweeps over the isles, the moisture in the air cutting to the bone.",
                    WarmWindyText = "The wind is picking up, a warm front rolling over.  There could be a storm soon.",
                    WindyText = "A strong wind sweeps in.  The sea is choppy, waves crashing onto the beach.",
                },
                [PlanetType.Hutlar] = new WeatherClimate
                {
                    HeatModifier = -8,
                    HumidityModifier = +2,
                    MaximumHeat = 3,
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
                [PlanetType.CZ220] = new WeatherClimate { IsSheltered = true },
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
				[PlanetType.SmugglersMoon] = new WeatherClimate
				{
					HeatModifier = -1,
					HumidityModifier = +1
				},
				[PlanetType.SmugglersMoonStation] = new WeatherClimate
				{
					IsSheltered = true,
					HeatModifier = -2,
					HumidityModifier = -2,
					WindModifier = -5,
					CloudyText = "The station's sealed bulkheads keep outside weather at bay while recycled air hums through the vents.",
					ColdCloudyText = "A chill runs through the station as the life-support vents push cool, recycled air through the corridors.",
					ColdMildText = "The station air is cool and still, regulated by aging life-support systems.",
					ColdWindyText = "A cold draft slips from the station's vents, but the sealed corridors remain protected from outside weather.",
					FreezingText = "The station's life support is running cold, leaving the metal corridors uncomfortably frigid.",
					MildText = "The station's climate controls keep the interior air steady and calm.",
					MildNightText = "The station's climate controls keep the interior air steady and calm.",
					MistText = "A faint haze from overworked vents hangs in the station air.",
					WarmCloudyText = "Heat lingers in the station corridors as the ventilation system struggles to circulate the stale air.",
					WarmMildText = "The station air is warm and stale, but safely sealed away from outside weather.",
					WarmWindyText = "Warm air gusts from the station's vents as the ventilation system cycles unevenly.",
					RainNormalText = "Moisture beads along overhead pipes while the station remains sealed against the vacuum outside.",
					RainWarmText = "Warm, damp air clings to the station walls as the life-support system strains.",
					ScorchingText = "The station's temperature controls falter, leaving the corridors oppressively hot.",
					SnowText = "Frost gathers near the vents as the station's life-support system runs dangerously cold.",
					StormText = "The station creaks around you, but its sealed hull keeps the outside weather away.",
					WindyText = "A steady current of recycled air moves through the station corridors."
				},
			};
        }
    }
}
