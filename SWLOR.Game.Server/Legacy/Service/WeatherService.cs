using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Profiler = SWLOR.Game.Server.Legacy.ValueObject.Profiler;

/*
    Name: WeatherService
    Author: Mithreas
    Date: 29 May 08, ported to cs 1 Jan 2019
    Description:  Implements a module wide weather system.

    Integration:
    * Call OnModuleHeartbeat() from heartbeat script.  Calls:
    * Call OnAreaEnter from area entry script.
    * Call OnCombatRoundEnd() for each PC or NPC in combat, each round
    * Call OnCreatureSpawn() in the creature spawn script. 
    * Optional: set all areas to 0% rain/snow (their weather will be overridden)
    * Optional: place area property objects as follows.
    * Climate: Wet or Climate: Dry for especially damp/dry areas
    * Climate: Hot or Climate: Cold for especially warm/cool areas
    * Climate: Sheltered or Climate: Exposed ...etc.

    Functionality:
    All three scales range from a score of 1 to 10.  Depending on the score
    different effects occur as follows.
    Heat
    rain turns to snow when heat < 4
    Humidity
    water rate drops by additional 0.5 per humidity point under 5
    rain/snow happens if humidity > 7
    Wind
    All creatures get 10% speed reduction when wind > 8
    Characters in combat must pass a discipline check or reflex save vs a DC
    of 10 + character level every round or be KD'd when wind == 10.
    Weather changes faster when wind is higher.

    Weather changes every 11 game hours minus the wind score.

    Multiple planet support
    - Areas are named PlanetName - areaname
    - Planets have distinct climates, implemented as +/- on each scale
    - Different climates can have different hazards - acid rain, sandstorms etc.
*/
namespace SWLOR.Game.Server.Legacy.Service
{
    public static class WeatherService
    {
        private static readonly Dictionary<uint, List<uint>> _areaWeatherPlaceables = new Dictionary<uint, List<uint>>();

        // Feedback texts.
        private const string FB_T_WEATHER_LIGHTNING = "You were hit by the bolt of lightning!";
        private const string FB_T_WEATHER_MESSAGE_CLOUDY = "Clouds move across the sky at a brisk pace, driven by strong wind.";
        private const string FB_T_WEATHER_MESSAGE_COLD_CLOUDY = "Cold air is punctuated by an overcast sky, the clouds thick and dark.";
        private const string FB_T_WEATHER_MESSAGE_COLD_WINDY = "A chill wind is in the air, cutting like a knife.";
        private const string FB_T_WEATHER_MESSAGE_COLD_MILD = "It is quite cold out. Wrap up warm by wearing winter clothing.";
        private const string FB_T_WEATHER_MESSAGE_FREEZING = "The air is bitingly cold right now. Make sure you are wrapped up warm and have plenty of rations.";
        private const string FB_T_WEATHER_MESSAGE_MILD = "It is lovely and sunny here.";
        private const string FB_T_WEATHER_MESSAGE_MILD_NIGHT = "The weather is fine tonight.";
        private const string FB_T_WEATHER_MESSAGE_MIST = "It is still and very humid, the mist hangs in the air about you.";
        private const string FB_T_WEATHER_MESSAGE_WARM_CLOUDY = "It is hot, and clouds are dotted around. Travels will be tiring - you should wear light clothing.";
        private const string FB_T_WEATHER_MESSAGE_WARM_MILD = "It is warm and calm here. Make sure you have enough to drink in the extra heat, and wear light clothing.";
        private const string FB_T_WEATHER_MESSAGE_WARM_WINDY = "Warm gusts of wind ripple the air here, and there are a worrying number of clouds casting shadows over the earth. We might experience thunderstorms, so be careful.";
        private const string FB_T_WEATHER_MESSAGE_RAIN_NORMAL = "It is raining. Your travels will be a little difficult because of it.";
        private const string FB_T_WEATHER_MESSAGE_RAIN_WARM = "It is raining, and the air is humid. Thunderstorms are likely, and it will be more difficult to make progress on your journey.";
        private const string FB_T_WEATHER_MESSAGE_SCORCHING = "The heat is blazing here! You should wear something to protect your face and hands, if you can.";
        private const string FB_T_WEATHER_MESSAGE_SNOW = "It is snowing right now! Remember to wrap up warm and pack extra provisions.";
        private const string FB_T_WEATHER_MESSAGE_STORM = "There is a thunderstorm at the moment. It will be quite dangerous out.";
        private const string FB_T_WEATHER_MESSAGE_WINDY = "The wind is very strong, and there are many clouds in the sky. The weather could shift at any time.";

        const string FB_T_WEATHER_DUST_STORM = "There is a dust storm!  Visibility is drastically reduced.";
        const string FB_T_WEATHER_SAND_STORM = "There is a sand storm! Take cover immediately, these storms are very dangerous!";
        const string FB_T_WEATHER_SNOW_STORM = "There is a snow storm! Visibility is reduced.";

        // Module and area variables.
        private const string VAR_WEATHER_CHANGE = "VAR_WEATHER_CHANGE";
        private const string VAR_WEATHER_HEAT = "VAR_WEATHER_HEAT";
        private const string VAR_WEATHER_HUMIDITY = "VAR_WEATHER_HUMIDITY";
        private const string VAR_WEATHER_WIND = "VAR_WEATHER_WIND";
        private const string VAR_WEATHER_ACID_RAIN = "VAR_WEATHER_ACID_RAIN";
        private const string VAR_INITIALIZED = "VAR_WH_INITIALIZED";
        private const string VAR_SKYBOX = "VAR_WH_SKYBOX";
        private const string VAR_FOG_SUN = "VAR_WH_FOG_SUN";
        private const string VAR_FOG_MOON = "VAR_WH_FOG_MOON";
        private const string VAR_FOG_C_SUN = "VAR_WH_FOG_C_SUN";
        private const string VAR_FOG_C_MOON = "VAR_WH_FOG_C_MOON";

        private struct PlanetaryClimate
        {
            public int Heat_Modifier;
            public int Humidity_Modifier;
            public int Wind_Modifier;

            public bool Sand_Storm;
            public bool Snow_Storm;

            // Allow overrides of text on different planets.
            public string special_cloudy;
            public string special_cold_cloudy;
            public string special_cold_mild;
            public string special_cold_windy;
            public string special_freezing;
            public string special_mild;
            public string special_mild_night;
            public string special_mist;
            public string special_warm_cloudy;
            public string special_warm_mild;
            public string special_warm_windy;
            public string special_rain_normal;
            public string special_rain_warm;
            public string special_scorching;
            public string special_snow;
            public string special_storm;
            public string special_windy;
        }

        private static PlanetaryClimate _GetClimate(NWObject oArea)
        {
            var climate = new PlanetaryClimate();

            //--------------------------------------------------------------------------
            // This line depends on the naming scheme PlanetName - AreaName.  Change it
            // if the area naming scheme changes!
            //--------------------------------------------------------------------------
            var index = GetName(oArea).IndexOf("-");
            if (index <= 0) return climate;
            var planetName = GetName(oArea).Substring(0, index - 1);

            if (planetName == "Viscara")
            {
                LoggingService.Trace(TraceComponent.Weather, "Planet is Viscara.");
                climate.Heat_Modifier = -2;
                climate.Humidity_Modifier = +2;
            }
            else if (planetName == "Tatooine")
            {
                LoggingService.Trace(TraceComponent.Weather, "Planet is Tatooine.");
                climate.Heat_Modifier = +5;
                climate.Humidity_Modifier = -8;

                climate.Sand_Storm = true;

                climate.special_cloudy = "A dusty wind sweeps through the desert; sparse clouds speed overhead.";
                climate.special_mild = "The sun shines brilliantly, but not oppressively, over the desert; the sky is clear.";
                climate.special_mild_night = "A clear night sky casts the desert in pale hues.";
                climate.special_warm_cloudy = "The shade of an overcast sky provides only minor relief to the sweltering temperatures.";
                climate.special_scorching = "The desert is baked with pervasive, inescapable heat; a haze blurs the horizon.";
                climate.special_warm_windy = "The hot wind wears at your face like a sandblaster.  A sand storm seems likely.";
                climate.special_windy = "A scouring wind sweeps across the desert, a sand storm cannot be far away.";
            }
            else if (planetName == "Mon Cala")
            {
                LoggingService.Trace(TraceComponent.Weather, "Planet is Mon Cala.");
                climate.Humidity_Modifier = 0;
                climate.Wind_Modifier = +1;
                climate.Heat_Modifier = +1;

                climate.special_cloudy = "Clouds build over the ocean, and the wind starts to pick up.  A storm could be brewing.";
                climate.special_cold_cloudy = "Thick clouds fill the sky, and a keen wind blows in off the ocean, exciting the waves.";
                climate.special_cold_mild = "It is cool, but calm.  The ocean is calm and beautiful.";
                climate.special_freezing = "A wave of cold air rolls in, stinging exposed flesh.";
                climate.special_mild = "The sea is calm, a faint breeze rippling through the trees.";
                climate.special_mild_night = "The sea is calm, and the sky towards the Galactic Core is full of stars.  In other directions, you see only a deep, unending black.";
                climate.special_mist = "A mist has blown in off the sea, moisture hanging heavy in the air.";
                climate.special_warm_cloudy = "The sea is choppy and the wind has picked up. An array of clouds marshals on the horizon, ready to sweep over you.";
                climate.special_warm_mild = "It is a beautiful day, warm and calm, though quite humid.";
                climate.special_rain_normal = "The ocean, affronted by the existence of patches of non-ocean on the surface of the planet, is attempting to reclaim the land by air drop.  In other words, it's raining.";
                climate.special_rain_warm = "A heavy rain shower is passing over, but is doing little to dispel the humidity in the air.";
                climate.special_snow = "It's snowing!  The local flora seems most surprised at this turn of events.";
                climate.special_storm = "A storm rips in off the sea, filling the sky with dramatic flashes.";
                climate.special_scorching = "The sun bakes the sand, making it extremely uncomfortable to those without insulated boots.";
                climate.special_cold_windy = "A chill wind sweeps over the isles, the moisture in the air cutting to the bone.";
                climate.special_warm_windy = "The wind is picking up, a warm front rolling over.  There could be a storm soon.";
                climate.special_windy = "A strong wind sweeps in.  The sea is choppy, waves crashing onto the beach.";
            }
            else if (planetName == "Hutlar")
            {
                LoggingService.Trace(TraceComponent.Weather, "Planet is Hutlar.");
                climate.Heat_Modifier = -5;
                climate.Humidity_Modifier = -8;

                climate.Snow_Storm = true;

                climate.special_freezing = "A wave of cold air rolls in, stinging exposed flesh.";
                climate.special_snow = "It's snowing... again...";
                climate.special_windy = "A cold wind sweeps in.";
                climate.special_cold_windy = "A freezing wind stings exposed flesh";
                climate.special_cloudy = "Clouds build over head, and there is a occasional strong gust of wind.";
                climate.special_cold_cloudy = "The clouds over head build, a cold wind stings exposed flesh. Looks like it is going to snow.";
                climate.special_cold_mild = "It is cold, the sky is clear, and there is a gentle breeze.";

            }

            return climate;
        }

        public static bool AdjustWeather()
        {
            LoggingService.Trace(TraceComponent.Weather, "Adjusting module weather");
            NWObject oMod = GetModule();

            //--------------------------------------------------------------------------
            // Always change the weather the very first time
            //--------------------------------------------------------------------------
            if (oMod.GetLocalInt(VAR_INITIALIZED) == 0)
            {
                oMod.SetLocalInt(VAR_INITIALIZED, 1);
                _SetHumidity(Random(10) + 1);
            }
            else if (GetTimeHour() != oMod.GetLocalInt(VAR_WEATHER_CHANGE))
            {
                LoggingService.Trace(TraceComponent.Weather, "No change needed... yet.");
                return false;
            }

            //--------------------------------------------------------------------------
            // Adjust the indices.  Only humidity is affected by the current values.
            //--------------------------------------------------------------------------
            var nHumidity = GetHumidity(OBJECT_SELF);
            var nWind = GetWindStrength(OBJECT_SELF);

            //--------------------------------------------------------------------------
            // Heat is affected by time of year.
            //--------------------------------------------------------------------------
            var nHeat = Random(5) + (6 - abs(GetCalendarMonth() - 6));
            if (nHeat < 1) nHeat = 1;

            //--------------------------------------------------------------------------
            // Humidity is random but moves slowly.
            //--------------------------------------------------------------------------
            nHumidity = nHumidity + (Random(2 * nWind + 1) - nWind);
            if (nHumidity > 10) nHumidity = 20 - nHumidity;
            if (nHumidity < 1) nHumidity = 1 - nHumidity;

            //--------------------------------------------------------------------------
            // Wind is more likely to be calm, but can change quickly.
            //--------------------------------------------------------------------------
            nWind = d10(2) - 10;
            if (nWind < 1) nWind = 1 - nWind;

            LoggingService.Trace(TraceComponent.Weather, "New weather settings: heat - " + IntToString(nHeat) +
                                           ", humidity - " + IntToString(nHumidity) +
                                               ", wind - " + IntToString(nWind));

            _SetHeatIndex(nHeat);
            _SetHumidity(nHumidity);
            _SetWindStrength(nWind);

            //--------------------------------------------------------------------------
            // Work out when to next change the weather.
            //--------------------------------------------------------------------------
            var nNextChange = GetTimeHour() + (11 - nWind);
            if (nNextChange > 23) nNextChange -= 24;
            LoggingService.Trace(TraceComponent.Weather, "Change the weather next at hour " + IntToString(nNextChange));
            oMod.SetLocalInt(VAR_WEATHER_CHANGE, nNextChange);

            // Update all occupied areas with the new settings.
            NWObject oPC = GetFirstPC();
            while (GetIsObjectValid(oPC) == true)
            {
                SetWeather(GetArea(oPC));
                oPC = GetNextPC();
            }

            return true;
        }

        public static void SetWeather()
        {
            SetWeather(OBJECT_SELF);
        }

        public static void SetWeather(NWObject oArea)
        {
            
            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
            {
                if (GetIsAreaInterior(oArea) == true ||
                    GetIsAreaAboveGround(oArea) == false)
                    return;
                oArea.SetLocalInt(VAR_SKYBOX, (int)GetSkyBox(oArea));
                oArea.SetLocalInt(VAR_FOG_SUN, GetFogAmount(FogType.Sun, oArea));
                oArea.SetLocalInt(VAR_FOG_MOON, GetFogAmount(FogType.Moon, oArea));
                oArea.SetLocalInt(VAR_FOG_C_SUN, (int)GetFogColor(FogType.Sun, oArea));
                oArea.SetLocalInt(VAR_FOG_C_MOON, (int)GetFogColor(FogType.Moon, oArea));
                oArea.SetLocalInt(VAR_INITIALIZED, 1);
            }

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);
            var bStormy = GetSkyBox(oArea) == Skybox.GrassStorm;
            var bDustStorm = (oArea.GetLocalInt("DUST_STORM") == 1);
            var bSandStorm = (oArea.GetLocalInt("SAND_STORM") == 1);
            var bSnowStorm = (oArea.GetLocalInt("SNOW_STORM") == 1);

            //--------------------------------------------------------------------------
            // Process weather rules for this area.
            //--------------------------------------------------------------------------
            if (nHumidity > 7 && nHeat > 3)
            {
                if (nHeat < 6 && nWind < 3)
                {
                    NWScript.SetWeather(oArea, WeatherType.Clear);
                }
                else NWScript.SetWeather(oArea, WeatherType.Rain);
            }
            else if (nHumidity > 7) NWScript.SetWeather(oArea, WeatherType.Snow);
            else NWScript.SetWeather(oArea, WeatherType.Clear);

            //--------------------------------------------------------------------------
            // Stormy if heat is greater than 4 only; if already stormy then 2 in 3
            // chance of storm clearing, otherwise x in 20 chance of storm starting,
            // where x is the wind level.
            //--------------------------------------------------------------------------
            if (nHeat > 4 && nHumidity > 7 &&
                ((bStormy && d20() - nWind < 1) || (bStormy && d3() == 1)))
            {
                LoggingService.Trace(TraceComponent.Weather, "A thunderstorm is now raging in " + GetName(oArea));
                SetSkyBox(Skybox.GrassStorm, oArea);
                Thunderstorm(oArea);
                oArea.SetLocalInt("GS_AM_SKY_OVERRIDE", 1);
                bStormy = true;
            }
            else
            {
                SetSkyBox((Skybox)oArea.GetLocalInt(VAR_SKYBOX), oArea);
                oArea.DeleteLocalInt("GS_AM_SKY_OVERRIDE");
                bStormy = false;
            }

            // Does this area suffer from dust or sand storms?
            if (!bStormy && nWind >= 9 && d3() == 1)
            {
                // Dust storm - low visibility but no damage.
                if (_GetClimate(oArea).Sand_Storm)
                {
                    SetFogColor(FogType.Sun, FogColor.OrangeDark, oArea);
                    SetFogColor(FogType.Moon, FogColor.OrangeDark, oArea);
                    SetFogAmount(FogType.Sun, 80, oArea);
                    SetFogAmount(FogType.Moon, 80, oArea);

                    oArea.SetLocalInt("SAND_STORM", 1);
                    bSandStorm = true;
                }
                else if (_GetClimate(oArea).Snow_Storm)
                {
                    SetFogColor(FogType.Sun, FogColor.White, oArea);
                    SetFogColor(FogType.Moon, FogColor.White, oArea);
                    SetFogAmount(FogType.Sun, 80, oArea);
                    SetFogAmount(FogType.Moon, 80, oArea);

                    oArea.SetLocalInt("SNOW_STORM", 1);
                    bSnowStorm = true;
                }
            }
            else if (bDustStorm || bSandStorm || bSnowStorm)
            {
                // End the storm.
                oArea.DeleteLocalInt("DUST_STORM");
                oArea.DeleteLocalInt("SAND_STORM");
                oArea.DeleteLocalInt("SNOW_STORM");

                SetFogColor(FogType.Sun, (FogColor)oArea.GetLocalInt(VAR_FOG_C_SUN), oArea);
                SetFogColor(FogType.Moon, (FogColor)oArea.GetLocalInt(VAR_FOG_C_MOON), oArea);
                SetFogAmount(FogType.Sun, oArea.GetLocalInt(VAR_FOG_SUN), oArea);
                SetFogAmount(FogType.Moon, oArea.GetLocalInt(VAR_FOG_MOON), oArea);
                bSandStorm = false;
                bDustStorm = false;
                bSnowStorm = false;
            }

            LoggingService.Trace(TraceComponent.Weather, "Area weather settings for area: " + GetName(oArea) +
                                                  ", heat - " + IntToString(nHeat) +
                                              ", humidity - " + IntToString(nHumidity) +
                                                  ", wind - " + IntToString(nWind) +
                                                 ", thunderstorm - " + bStormy.ToString() +
                                                 ", sand storm - " + bSandStorm.ToString() +
                                                 ", snow storm - " + bSnowStorm.ToString() +
                                                 ", dust storm - " + bDustStorm.ToString());
        }

        public static Weather GetWeather()
        {
            return GetWeather(OBJECT_SELF);
        }

        public static Weather GetWeather(NWObject oArea)
        {
            LoggingService.Trace(TraceComponent.Weather, "Getting current weather for area: " + GetName(oArea));

            if (GetIsAreaInterior(oArea) == true || GetIsAreaAboveGround(oArea) == false)
            {
                return Weather.Invalid;
            }

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);

            if (nHumidity > 7 && nHeat > 3 && nHeat < 6 && nWind < 3)
            {
                return Weather.Foggy;
            }

            // Rather unfortunately, the default method is also called GetWeather. 
            return NWScript.GetWeather(oArea);
        }

        public static void OnCombatRoundEnd(NWObject oCreature)
        {
            NWObject oArea = GetArea(oCreature);
            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
                return;

            var nWind = GetWindStrength(oArea);

            if (nWind > 9) _DoWindKnockdown(oCreature);
        }

        public static void ApplyAcid(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget) == true) return;
            if (GetIsPC(oTarget) == true && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_S),
                    EffectDamage(
                        d6(1),
                        DamageType.Acid));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplyAcid(oTarget, oArea); });
        }

        public static void ApplyCold(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget) == true) return;
            if (GetIsPC(oTarget) == true && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_S),
                    EffectDamage(
                        d6(1),
                        DamageType.Acid));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplyCold(oTarget, oArea); });
        }

        public static void ApplySandstorm(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget) == true) return;
            if (GetIsPC(oTarget) == true && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S),
                    EffectDamage(
                        d6(2),
                        DamageType.Bludgeoning));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplySandstorm(oTarget, oArea); });
        }

        public static void ApplySnowstorm(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget) == true) return;
            if (GetIsPC(oTarget) == true && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffect.Vfx_Dur_Iceskin),
                    EffectDamage(
                        d6(2),
                        DamageType.Cold));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplySnowstorm(oTarget, oArea); });
        }

        public static void DoWeatherEffects(NWObject oCreature)
        {
            NWObject oArea = GetArea(oCreature);
            if (GetIsAreaInterior(oArea) || GetIsAreaAboveGround(oArea) == false) return;

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);
            var bStormy = GetSkyBox(oArea) == Skybox.GrassStorm;
            var bIsPC  = (GetIsPC(oCreature) == true);
            var sMessage = "";
            var climate = _GetClimate(oArea);

            //--------------------------------------------------------------------------
            // Apply acid rain, if applicable.  Stolen shamelessly from the Melf's Acid
            // Arrow spell.
            //--------------------------------------------------------------------------
            if (bIsPC && NWScript.GetWeather(oArea) == Weather.Rain && oArea.GetLocalInt(VAR_WEATHER_ACID_RAIN) == 1)
            {
                var eEffect =
                  EffectLinkEffects(
                      EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_S),
                      EffectDamage(
                          d6(2),
                          DamageType.Acid));

                ApplyEffectToObject(DurationType.Instant, eEffect, oCreature);

                DelayCommand(6.0f, () => { ApplyAcid(oCreature, oArea); });
            }
            else if (bIsPC && oArea.GetLocalInt("DUST_STORM") == 1)
            {
                sMessage = FB_T_WEATHER_DUST_STORM;
            }
            else if (bIsPC && oArea.GetLocalInt("SAND_STORM") == 1)
            {
                sMessage = FB_T_WEATHER_SAND_STORM;
                var eEffect =
                    EffectLinkEffects(
                        EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S),
                        EffectDamage(
                            d6(2),
                            DamageType.Bludgeoning));

                ApplyEffectToObject(DurationType.Instant, eEffect, oCreature);

                DelayCommand(6.0f, () => { ApplySandstorm(oCreature, oArea); });
            }
            else if (bIsPC && oArea.GetLocalInt("SNOW_STORM") == 1)
            {
                sMessage = FB_T_WEATHER_SNOW_STORM;
                var eEffect =
                    EffectLinkEffects(
                        EffectVisualEffect(VisualEffect.Vfx_Dur_Iceskin),
                        EffectDamage(
                            d6(2),
                            DamageType.Cold));

                ApplyEffectToObject(DurationType.Instant, eEffect, oCreature);

                DelayCommand(6.0f, () => { ApplySnowstorm(oCreature, oArea); });
            }
            else if (bIsPC)
            {
                // Stormy weather
                if (bStormy)
                {
                    sMessage = string.IsNullOrWhiteSpace(climate.special_storm) ? FB_T_WEATHER_MESSAGE_STORM : climate.special_storm;
                }
                // Rain or mist
                else if (nHumidity > 7 && nHeat > 3)
                {
                    // Mist
                    if (nHeat < 6 && nWind < 3)
                    {
                        sMessage = string.IsNullOrWhiteSpace(climate.special_mist) ? FB_T_WEATHER_MESSAGE_MIST : climate.special_mist;
                    }
                    // Humid
                    else if (nHeat > 7)
                    {
                        sMessage = string.IsNullOrWhiteSpace(climate.special_rain_warm) ? FB_T_WEATHER_MESSAGE_RAIN_WARM : climate.special_rain_warm;
                    }
                    else
                    {
                        sMessage = string.IsNullOrWhiteSpace(climate.special_rain_normal) ? FB_T_WEATHER_MESSAGE_RAIN_NORMAL : climate.special_rain_normal;
                    }
                }
                // Snow
                else if (nHumidity > 7)
                {
                    sMessage = string.IsNullOrWhiteSpace(climate.special_snow) ? FB_T_WEATHER_MESSAGE_SNOW : climate.special_snow;
                }
                // Freezing
                else if (nHeat < 3)
                {
                    sMessage = string.IsNullOrWhiteSpace(climate.special_freezing) ? FB_T_WEATHER_MESSAGE_FREEZING : climate.special_freezing;
                }
                // Boiling
                else if (nHeat > 8)
                {
                    sMessage = string.IsNullOrWhiteSpace(climate.special_scorching) ? FB_T_WEATHER_MESSAGE_SCORCHING : climate.special_scorching;
                }
                // Cold
                else if (nHeat < 5)
                {
                    if (nWind < 5) sMessage = string.IsNullOrWhiteSpace(climate.special_cold_mild) ? FB_T_WEATHER_MESSAGE_COLD_MILD : climate.special_cold_mild;
                    else if (nWind < 8) sMessage = string.IsNullOrWhiteSpace(climate.special_cold_cloudy) ? FB_T_WEATHER_MESSAGE_COLD_CLOUDY : climate.special_cold_cloudy;
                    else sMessage = string.IsNullOrWhiteSpace(climate.special_cold_windy) ? FB_T_WEATHER_MESSAGE_COLD_WINDY : climate.special_cold_windy;
                }
                // Hot
                else if (nHeat > 6)
                {
                    if (nWind < 5) sMessage = string.IsNullOrWhiteSpace(climate.special_warm_mild) ? FB_T_WEATHER_MESSAGE_WARM_MILD : climate.special_warm_mild;
                    else if (nWind < 8) sMessage = string.IsNullOrWhiteSpace(climate.special_warm_cloudy) ? FB_T_WEATHER_MESSAGE_WARM_CLOUDY : climate.special_warm_cloudy;
                    else sMessage = string.IsNullOrWhiteSpace(climate.special_warm_windy) ? FB_T_WEATHER_MESSAGE_WARM_WINDY : climate.special_warm_windy;
                }
                else if (nWind < 5)
                {
                    if (GetIsNight() == false) sMessage = string.IsNullOrWhiteSpace(climate.special_mild) ? FB_T_WEATHER_MESSAGE_MILD : climate.special_mild;
                    else sMessage = string.IsNullOrWhiteSpace(climate.special_mild_night) ? FB_T_WEATHER_MESSAGE_MILD_NIGHT : climate.special_mild_night;
                }
                else if (nWind < 8) sMessage = string.IsNullOrWhiteSpace(climate.special_cloudy) ? FB_T_WEATHER_MESSAGE_CLOUDY : climate.special_cloudy;
                else sMessage = string.IsNullOrWhiteSpace(climate.special_windy) ? FB_T_WEATHER_MESSAGE_WINDY : climate.special_windy;

                SendMessageToPC(oCreature, sMessage);
            }
        }
        
        public static int GetHeatIndex(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = GetModule();
            var nHeat = oMod.GetLocalInt(VAR_WEATHER_HEAT);
            if (oArea.IsValid)
            {
                nHeat += oArea.GetLocalInt(VAR_WEATHER_HEAT);
                nHeat += _GetClimate(oArea).Heat_Modifier;
            }

            nHeat = (GetIsNight()) ? nHeat - 2 : nHeat + 2;

            if (nHeat > 10) nHeat = 10;
            if (nHeat < 1) nHeat = 1;

            return nHeat;
        }

        public static int GetHumidity(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = GetModule();
            var nHumidity = oMod.GetLocalInt(VAR_WEATHER_HUMIDITY);
            if (oArea.IsValid)
            {
                nHumidity += oArea.GetLocalInt(VAR_WEATHER_HUMIDITY);
                nHumidity += _GetClimate(oArea).Humidity_Modifier;
            }

            if (nHumidity > 10) nHumidity = 10;
            if (nHumidity < 1) nHumidity = 1;

            return nHumidity;
        }

        public static int GetWindStrength(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = GetModule();
            var nWind = oMod.GetLocalInt(VAR_WEATHER_WIND);
            if (oArea.IsValid)
            {
                nWind += oArea.GetLocalInt(VAR_WEATHER_WIND);
                nWind += _GetClimate(oArea).Wind_Modifier;

                //----------------------------------------------------------------------
                // Automatic cover bonus for artificial areas such as cities (lots of
                // buildings).
                //----------------------------------------------------------------------
                if (GetIsAreaNatural(oArea) == 0) nWind -= 1;
            }

            if (nWind > 10) nWind = 10;
            if (nWind < 1) nWind = 1;

            return nWind;
        }

        private static void _SetHeatIndex(int nHeat)
        {
            NWObject oMod = GetModule();
            oMod.SetLocalInt(VAR_WEATHER_HEAT, nHeat);
        }

        private static void _SetHumidity(int nHumidity)
        {
            NWObject oMod = GetModule();
            oMod.SetLocalInt(VAR_WEATHER_HUMIDITY, nHumidity);
        }

        private static void _SetWindStrength(int nWind)
        {
            NWObject oMod = GetModule();
            oMod.SetLocalInt(VAR_WEATHER_WIND, nWind);
        }

        private static void _DoWindKnockdown(NWObject oCreature)
        {
            LoggingService.Trace(TraceComponent.Weather, "Checking whether " + GetName(oCreature) + " is blown over");
            var nDC = (GetHitDice(oCreature) / 2) + 10;
            var nDiscipline = GetSkillRank(Skill.Discipline, oCreature);
            var nReflexSave = GetReflexSavingThrow(oCreature);
            int nSuccess;

            if (nDiscipline > nReflexSave)
                nSuccess = GetIsSkillSuccessful(oCreature, Skill.Discipline, nDC) ? 1 : 0;
            else
                nSuccess = ReflexSave(oCreature, nDC) == SaveReturn.Success ? 1 : 0;

            if (nSuccess == 0)
            {
                ApplyEffectToObject(DurationType.Temporary,
                                      AbilityService.EffectKnockdown(oCreature, 6.0f),
                                      oCreature,
                                      6.0f);
                FloatingTextStringOnCreature("*is unbalanced by a strong gust*", oCreature);
            }
        }

        public static void Thunderstorm(NWObject oArea)
        {
            // 1 in 3 chance of a bolt.
            if (d3() != 1) return;

            // Pick a spot. Any spot.
            var nWidth = GetAreaSize(Dimension.Width, oArea);
            var nHeight = GetAreaSize(Dimension.Height, oArea);
            var nPointWide = Random(nWidth * 10);
            var nPointHigh = Random(nHeight * 10);
            var fStrikeX = IntToFloat(nPointWide) + (IntToFloat(Random(10)) * 0.1f);
            var fStrikeY = IntToFloat(nPointHigh) + (IntToFloat(Random(10)) * 0.1f);

            // Now find out the power
            var nPower = d100() + 10;

            // Fire ze thunderboltz!
            DelayCommand(IntToFloat(Random(60)),
                () =>
                {
                    _Thunderstorm(Location(oArea,
                                               Vector3(fStrikeX, fStrikeY),
                                               0.0f),
                                           nPower);
                }
                         );
        }

        private static void _Thunderstorm(NWLocation lLocation, int nPower)
        {
            var fRange = IntToFloat(nPower) * 0.1f;
            // Caps on sphere of influence
            if (fRange < 3.0) fRange = 3.0f;
            if (fRange > 6.0) fRange = 6.0f;

            //Effects
            var eEffBolt = EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_M);
            ApplyEffectAtLocation(DurationType.Instant, eEffBolt, lLocation);

            Effect eEffDam;
            ObjectType nType;
            NWObject oObject = GetFirstObjectInShape(Shape.Sphere, fRange, lLocation, false, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable);
            while (GetIsObjectValid(oObject))
            {
                nType = GetObjectType(oObject);
                if (nType == ObjectType.Creature ||
                    nType == ObjectType.Door ||
                    nType == ObjectType.Placeable)
                {
                    eEffDam = EffectDamage(
                        FloatToInt(IntToFloat(nPower) - (GetDistanceBetweenLocations(lLocation, GetLocation(oObject)) * 10.0f)),
                        DamageType.Electrical);
                    ApplyEffectToObject(DurationType.Instant, eEffDam, oObject);

                    if (nType == ObjectType.Creature)
                    {
                        if (GetIsPC(oObject)) SendMessageToPC(oObject, FB_T_WEATHER_LIGHTNING);

                        PlayVoiceChat(VoiceChat.Pain1, oObject);
                        var duration = IntToFloat(d6(1));
                        ApplyEffectToObject(DurationType.Temporary, AbilityService.EffectKnockdown(oObject, duration), oObject, duration);
                    }
                }
                oObject = GetNextObjectInShape(Shape.Sphere, fRange, lLocation, false, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable);
            }
        }

        [NWNEventHandler("area_enter")]
        private static void OnAreaEnter()
        {
            using (new Profiler("WeatherService.OnAreaEnter"))
            {
                SetWeather();

                LoggingService.Trace(TraceComponent.Weather, "Applying weather to creature: " + GetName(GetEnteringObject()));

                DoWeatherEffects(GetEnteringObject());

                var oArea = (OBJECT_SELF);
                var nHour = GetTimeHour();
                var nLastHour = GetLocalInt(oArea, "WEATHER_LAST_HOUR");

                if (nHour != nLastHour)
                {
                    if (!_areaWeatherPlaceables.ContainsKey(oArea))
                        _areaWeatherPlaceables[oArea] = new List<uint>();

                    var weatherObjects = _areaWeatherPlaceables[oArea];

                    LoggingService.Trace(TraceComponent.Weather, "Cleaning up old weather");

                    // Clean up any old weather placeables.
                    for (var x = weatherObjects.Count - 1; x >= 0; x--)
                    {
                        var placeable = weatherObjects.ElementAt(x);
                        DestroyObject(placeable);
                        weatherObjects.RemoveAt(x);
                    }

                    // Create new ones depending on the current weather.
                    var nWeather = GetWeather();
                    LoggingService.Trace(TraceComponent.Weather, "Current weather: " + nWeather.ToString());

                    if (nWeather == Weather.Foggy)
                    {
                        // Get the size in tiles.
                        var nSizeX = GetAreaSize(Dimension.Width, oArea);
                        var nSizeY = GetAreaSize(Dimension.Height, oArea);

                        // We want one placeable per 8 tiles.
                        var nMax = (nSizeX * nSizeY) / 8;
                        LoggingService.Trace(TraceComponent.Weather, "Creating up to " + nMax.ToString() + " mist objects.");

                        for (var nCount = d6(); nCount < nMax; nCount++)
                        {
                            var vPosition = GetPosition(GetEnteringObject());

                            // Vectors are in meters - 10 meters to a tile. 
                            vPosition.X = IntToFloat(Random(nSizeX * 10));
                            vPosition.Y = IntToFloat(Random(nSizeY * 10));

                            var fFacing = IntToFloat(Random(360));

                            var sResRef = "x3_plc_mist";

                            NWPlaceable oPlaceable = CreateObject(ObjectType.Placeable, sResRef, Location(oArea, vPosition, fFacing));
                            SetObjectVisualTransform(oPlaceable, ObjectVisualTransform.Scale, IntToFloat(200 + Random(200)) / 100.0f);

                            weatherObjects.Add(oPlaceable);
                        }
                    }

                    _areaWeatherPlaceables[oArea] = weatherObjects;
                    SetLocalInt(oArea, "WEATHER_LAST_HOUR", nHour);
                }
            }
        }

        [NWNEventHandler("mod_heartbeat")]
        private static void OnModuleHeartbeat()
        {
            NWObject oMod = GetModule();
            var nHour = GetTimeHour();
            var nLastHour = oMod.GetLocalInt("WEATHER_LAST_HOUR");

            if (nHour != nLastHour)
            {
                if (AdjustWeather())
                {
                    foreach (var player in NWModule.Get().Players)
                    {
                        DoWeatherEffects(player);
                    }
                }

                oMod.SetLocalInt("WEATHER_LAST_HOUR", nHour);
            }
        }

        public static void SetAreaHeatModifier(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_HEAT, nModifier);
        }

        public static void SetAreaWindModifier(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_WIND, nModifier);
        }

        public static void SetAreaHumidityModifier(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_HUMIDITY, nModifier);
        }

        public static void SetAreaAcidRain(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_ACID_RAIN, nModifier);
        }
    }
}
