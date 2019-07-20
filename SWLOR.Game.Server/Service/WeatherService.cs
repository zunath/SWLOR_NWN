using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.ValueObject;

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
namespace SWLOR.Game.Server.Service
{
    public static class WeatherService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnAreaEnter>(message => OnAreaEnter());
            MessageHub.Instance.Subscribe<OnModuleHeartbeat>(message => OnModuleHeartbeat());
        }

        // Feedback texts.
        const string FB_T_WEATHER_LIGHTNING = "You were hit by the bolt of lightning!";
        const string FB_T_WEATHER_MESSAGE_CLOUDY = "Clouds move across the sky at a brisk pace, driven by strong wind.";
        const string FB_T_WEATHER_MESSAGE_COLD_CLOUDY = "Cold air is punctuated by an overcast sky, the clouds thick and dark.";
        const string FB_T_WEATHER_MESSAGE_COLD_WINDY = "A chill wind is in the air, cutting like a knife.";
        const string FB_T_WEATHER_MESSAGE_COLD_MILD = "It is quite cold out. Wrap up warm by wearing winter clothing.";
        const string FB_T_WEATHER_MESSAGE_FREEZING = "The air is bitingly cold right now. Make sure you are wrapped up warm and have plenty of rations.";
        const string FB_T_WEATHER_MESSAGE_MILD = "It is lovely and sunny here.";
        const string FB_T_WEATHER_MESSAGE_MILD_NIGHT = "The weather is fine tonight.";
        const string FB_T_WEATHER_MESSAGE_MIST = "It is still and very humid, the mist hangs in the air about you.";
        const string FB_T_WEATHER_MESSAGE_WARM_CLOUDY = "It is hot, and clouds are dotted around. Travels will be tiring - you should wear light clothing.";
        const string FB_T_WEATHER_MESSAGE_WARM_MILD = "It is warm and calm here. Make sure you have enough to drink in the extra heat, and wear light clothing.";
        const string FB_T_WEATHER_MESSAGE_WARM_WINDY = "Warm gusts of wind ripple the air here, and there are a worrying number of clouds casting shadows over the earth. We might experience thunderstorms, so be careful.";
        const string FB_T_WEATHER_MESSAGE_RAIN_NORMAL = "It is raining. Your travels will be a little difficult because of it.";
        const string FB_T_WEATHER_MESSAGE_RAIN_WARM = "It is raining, and the air is humid. Thunderstorms are likely, and it will be more difficult to make progress on your journey.";
        const string FB_T_WEATHER_MESSAGE_SCORCHING = "The heat is blazing here! You should wear something to protect your face and hands, if you can.";
        const string FB_T_WEATHER_MESSAGE_SNOW = "It is snowing right now! Remember to wrap up warm and pack extra provisions.";
        const string FB_T_WEATHER_MESSAGE_STORM = "There is a thunderstorm at the moment. It will be quite dangerous out.";
        const string FB_T_WEATHER_MESSAGE_WINDY = "The wind is very strong, and there are many clouds in the sky. The weather could shift at any time.";

        const string FB_T_WEATHER_DUST_STORM = "There is a dust storm!  Visibility is drastically reduced.";
        const string FB_T_WEATHER_SAND_STORM = "There is a sand storm! Take cover immediately, these storms are very dangerous!";

        // Module and area variables.
        const string VAR_WEATHER_CHANGE = "VAR_WEATHER_CHANGE";
        const string VAR_WEATHER_HEAT = "VAR_WEATHER_HEAT";
        const string VAR_WEATHER_HUMIDITY = "VAR_WEATHER_HUMIDITY";
        const string VAR_WEATHER_MIST = "VAR_WEATHER_MIST";
        const string VAR_WEATHER_WIND = "VAR_WEATHER_WIND";
        const string VAR_WEATHER_ACID_RAIN = "VAR_WEATHER_ACID_RAIN";
        const string VAR_EFFECTOR = "VAR_WH_EFFECTOR";
        const string VAR_INITIALIZED = "VAR_WH_INITIALIZED";
        const string VAR_SKYBOX = "VAR_WH_SKYBOX";
        const string VAR_TIMESTAMP = "VAR_WH_TIMESTAMP";
        const string VAR_FOG_SUN = "VAR_WH_FOG_SUN";
        const string VAR_FOG_MOON = "VAR_WH_FOG_MOON";
        const string VAR_FOG_C_SUN = "VAR_WH_FOG_C_SUN";
        const string VAR_FOG_C_MOON = "VAR_WH_FOG_C_MOON";

        // Flag variables - used to modify climate per area.
        const int CLIMATE_HEAT_VERY_COLD = -8;
        const int CLIMATE_HEAT_COLD = -4;
        const int CLIMATE_HEAT_NORMAL = 0;
        const int CLIMATE_HEAT_HOT = 4;
        const int CLIMATE_HEAT_VERY_HOT = 8;

        const int CLIMATE_HUMIDITY_VERY_WET = 6;
        const int CLIMATE_HUMIDITY_WET = 2;
        const int CLIMATE_HUMIDITY_NORMAL = 0;
        const int CLIMATE_HUMIDITY_DRY = -2;
        const int CLIMATE_HUMIDITY_VERY_DRY = -6;

        const int CLIMATE_WIND_VERY_SHELTERED = -6;
        const int CLIMATE_WIND_SHELTERED = -2;
        const int CLIMATE_WIND_NORMAL = 0;
        const int CLIMATE_WIND_EXPOSED = 1;
        const int CLIMATE_WIND_VERY_EXPOSED = 4;

        // WEATHER_CLEAR = 0, WEATHER_RAIN = 1, WEATHER_SNOW = 2
        const int WEATHER_FOGGY = 3;
        
        struct PlanetaryClimate
        {
            public int Heat_Modifier;
            public int Humidity_Modifier;
            public int Wind_Modifier;

            // Does the planet suffer from unusual weather?
            public bool Acid_Rain;
            public bool Dust_Storm;
            public bool Sand_Storm;

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
            PlanetaryClimate climate = new PlanetaryClimate();

            //--------------------------------------------------------------------------
            // This line depends on the naming scheme PlanetName - AreaName.  Change it
            // if the area naming scheme changes!
            //--------------------------------------------------------------------------
            int index = _.GetName(oArea).IndexOf("-");
            if (index <= 0) return climate;
            string planetName = _.GetName(oArea).Substring(0, index - 1);

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

            return climate;
        }

        public static bool AdjustWeather()
        {
            LoggingService.Trace(TraceComponent.Weather, "Adjusting module weather");
            NWObject oMod = _.GetModule();

            //--------------------------------------------------------------------------
            // Always change the weather the very first time
            //--------------------------------------------------------------------------
            if (oMod.GetLocalInt(VAR_INITIALIZED) == 0)
            {
                oMod.SetLocalInt(VAR_INITIALIZED, 1);
                _SetHumidity(_.Random(10) + 1);
            }
            else if (_.GetTimeHour() != oMod.GetLocalInt(VAR_WEATHER_CHANGE))
            {
                LoggingService.Trace(TraceComponent.Weather, "No change needed... yet.");
                return false;
            }

            //--------------------------------------------------------------------------
            // Adjust the indices.  Only humidity is affected by the current values.
            //--------------------------------------------------------------------------
            int nHeat = GetHeatIndex();
            int nHumidity = GetHumidity();
            int nWind = GetWindStrength();

            //--------------------------------------------------------------------------
            // Heat is affected by time of year.
            //--------------------------------------------------------------------------
            nHeat = _.Random(5) + (6 - _.abs(_.GetCalendarMonth() - 6)); // (0-4 + 0-6)
            if (nHeat < 1) nHeat = 1;

            //--------------------------------------------------------------------------
            // Humidity is random but moves slowly.
            //--------------------------------------------------------------------------
            nHumidity = nHumidity + (_.Random(2 * nWind + 1) - nWind);
            if (nHumidity > 10) nHumidity = 20 - nHumidity;
            if (nHumidity < 1) nHumidity = 1 - nHumidity;

            //--------------------------------------------------------------------------
            // Wind is more likely to be calm, but can change quickly.
            //--------------------------------------------------------------------------
            nWind = _.d10(2) - 10;
            if (nWind < 1) nWind = 1 - nWind;

            LoggingService.Trace(TraceComponent.Weather, "New weather settings: heat - " + _.IntToString(nHeat) +
                                           ", humidity - " + _.IntToString(nHumidity) +
                                               ", wind - " + _.IntToString(nWind));

            _SetHeatIndex(nHeat);
            _SetHumidity(nHumidity);
            _SetWindStrength(nWind);

            //--------------------------------------------------------------------------
            // Work out when to next change the weather.
            //--------------------------------------------------------------------------
            int nNextChange = _.GetTimeHour() + (11 - nWind);
            if (nNextChange > 23) nNextChange -= 24;
            LoggingService.Trace(TraceComponent.Weather, "Change the weather next at hour " + _.IntToString(nNextChange));
            oMod.SetLocalInt(VAR_WEATHER_CHANGE, nNextChange);

            // Update all occupied areas with the new settings.
            NWObject oPC = _.GetFirstPC();
            while (_.GetIsObjectValid(oPC) == 1)
            {
                SetWeather(_.GetArea(oPC));
                oPC = _.GetNextPC();
            }

            return true;
        }

        public static void SetWeather()
        {
            SetWeather(NWGameObject.OBJECT_SELF);
        }

        public static void SetWeather(NWObject oArea)
        {
            
            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
            {
                if (_.GetIsAreaInterior(oArea) == 1 ||
                    _.GetIsAreaAboveGround(oArea) == 0)
                    return;
                oArea.SetLocalInt(VAR_SKYBOX, _.GetSkyBox(oArea));
                oArea.SetLocalInt(VAR_FOG_SUN, _.GetFogAmount(_.FOG_TYPE_SUN, oArea));
                oArea.SetLocalInt(VAR_FOG_MOON, _.GetFogAmount(_.FOG_TYPE_MOON, oArea));
                oArea.SetLocalInt(VAR_FOG_C_SUN, _.GetFogColor(_.FOG_TYPE_SUN, oArea));
                oArea.SetLocalInt(VAR_FOG_C_MOON, _.GetFogColor(_.FOG_TYPE_MOON, oArea));
                oArea.SetLocalInt(VAR_INITIALIZED, 1);
            }

            int nHeat = GetHeatIndex(oArea);
            int nHumidity = GetHumidity(oArea);
            int nWind = GetWindStrength(oArea);
            bool bStormy = _.GetSkyBox(oArea) == _.SKYBOX_GRASS_STORM;
            bool bDustStorm = (oArea.GetLocalInt("DUST_STORM") == 1);
            bool bSandStorm = (oArea.GetLocalInt("SAND_STORM") == 1);

            //--------------------------------------------------------------------------
            // Process weather rules for this area.
            //--------------------------------------------------------------------------
            if (nHumidity > 7 && nHeat > 3)
            {
                if (nHeat < 6 && nWind < 3)
                {
                    _.SetWeather(oArea, _.WEATHER_CLEAR);
                }
                else _.SetWeather(oArea, _.WEATHER_RAIN);
            }
            else if (nHumidity > 7) _.SetWeather(oArea, _.WEATHER_SNOW);
            else _.SetWeather(oArea, _.WEATHER_CLEAR);

            //--------------------------------------------------------------------------
            // Stormy if heat is greater than 4 only; if already stormy then 2 in 3
            // chance of storm clearing, otherwise x in 20 chance of storm starting,
            // where x is the wind level.
            //--------------------------------------------------------------------------
            if (nHeat > 4 && nHumidity > 7 &&
                ((bStormy && _.d20() - nWind < 1) || (bStormy && _.d3() == 1)))
            {
                LoggingService.Trace(TraceComponent.Weather, "A thunderstorm is now raging in " + _.GetName(oArea));
                _.SetSkyBox(_.SKYBOX_GRASS_STORM, oArea);
                Thunderstorm(oArea);
                oArea.SetLocalInt("GS_AM_SKY_OVERRIDE", 1);
                bStormy = true;
            }
            else
            {
                _.SetSkyBox(oArea.GetLocalInt(VAR_SKYBOX), oArea);
                oArea.DeleteLocalInt("GS_AM_SKY_OVERRIDE");
                bStormy = false;
            }

            // Does this area suffer from dust or sand storms?
            if (!bStormy && nWind >= 9 && _.d3() == 1)
            {
                // Dust storm - low visibility but no damage.
                if (_GetClimate(oArea).Dust_Storm)
                {
                    _.SetFogColor(_.FOG_TYPE_SUN, _.FOG_COLOR_BROWN, oArea);
                    _.SetFogColor(_.FOG_TYPE_MOON, _.FOG_COLOR_BROWN, oArea);
                    _.SetFogAmount(_.FOG_TYPE_SUN, 80, oArea);
                    _.SetFogAmount(_.FOG_TYPE_MOON, 80, oArea);

                    oArea.SetLocalInt("DUST_STORM", 1);
                    bDustStorm = true;
                }
                else if (_GetClimate(oArea).Sand_Storm)
                {
                    _.SetFogColor(_.FOG_TYPE_SUN, _.FOG_COLOR_ORANGE_DARK, oArea);
                    _.SetFogColor(_.FOG_TYPE_MOON, _.FOG_COLOR_ORANGE_DARK, oArea);
                    _.SetFogAmount(_.FOG_TYPE_SUN, 80, oArea);
                    _.SetFogAmount(_.FOG_TYPE_MOON, 80, oArea);

                    oArea.SetLocalInt("SAND_STORM", 1);
                    bSandStorm = true;
                }
            }
            else if (bDustStorm || bSandStorm)
            {
                // End the storm.
                oArea.DeleteLocalInt("DUST_STORM");
                oArea.DeleteLocalInt("SAND_STORM");

                _.SetFogColor(_.FOG_TYPE_SUN, oArea.GetLocalInt(VAR_FOG_C_SUN), oArea);
                _.SetFogColor(_.FOG_TYPE_MOON, oArea.GetLocalInt(VAR_FOG_C_MOON), oArea);
                _.SetFogAmount(_.FOG_TYPE_SUN, oArea.GetLocalInt(VAR_FOG_SUN), oArea);
                _.SetFogAmount(_.FOG_TYPE_MOON, oArea.GetLocalInt(VAR_FOG_MOON), oArea);
                bSandStorm = false;
                bDustStorm = false;
            }

            LoggingService.Trace(TraceComponent.Weather, "Area weather settings for area: " + _.GetName(oArea) +
                                                  ", heat - " + _.IntToString(nHeat) +
                                              ", humidity - " + _.IntToString(nHumidity) +
                                                  ", wind - " + _.IntToString(nWind) +
                                                 ", thunderstorm - " + bStormy.ToString() +
                                                 ", sand storm - " + bSandStorm.ToString() +
                                                 ", dust storm - " + bDustStorm.ToString());
        }

        public static int GetWeather()
        {
            return GetWeather(NWGameObject.OBJECT_SELF);
        }

        public static int GetWeather(NWObject oArea)
        {
            LoggingService.Trace(TraceComponent.Weather, "Getting current weather for area: " + _.GetName(oArea));

            if (_.GetIsAreaInterior(oArea) == 1 || _.GetIsAreaAboveGround(oArea) == 0)
            {
                return _.WEATHER_INVALID;
            }

            int nHeat = GetHeatIndex(oArea);
            int nHumidity = GetHumidity(oArea);
            int nWind = GetWindStrength(oArea);

            if (nHumidity > 7 && nHeat > 3 && nHeat < 6 && nWind < 3)
            {
                return WEATHER_FOGGY;
            }

            // Rather unfortunately, the default method is also called GetWeather. 
            return _.GetWeather(oArea);
        }

        public static void OnCombatRoundEnd(NWObject oCreature)
        {
            NWObject oArea = _.GetArea(oCreature);
            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
                return;

            int nWind = GetWindStrength(oArea);

            if (nWind > 9) _DoWindKnockdown(oCreature);
        }

        public static void ApplyAcid(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)_.GetArea(oTarget) != oArea) return;
            if (_.GetIsDead(oTarget) == 1) return;
            if (_.GetIsPC(oTarget) == 1 && _.GetIsPC(_.GetMaster(oTarget)) == 0) return;

            //apply
            Effect eEffect =
                _.EffectLinkEffects(
                    _.EffectVisualEffect(_.VFX_IMP_ACID_S),
                    _.EffectDamage(
                        _.d6(1),
                        _.DAMAGE_TYPE_ACID));

            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, eEffect, oTarget);

            _.DelayCommand(6.0f, () => { ApplyAcid(oTarget, oArea); });
        }

        public static void ApplySandstorm(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)_.GetArea(oTarget) != oArea) return;
            if (_.GetIsDead(oTarget) == 1) return;
            if (_.GetIsPC(oTarget) == 1 && _.GetIsPC(_.GetMaster(oTarget)) == 0) return;

            //apply
            Effect eEffect =
                _.EffectLinkEffects(
                    _.EffectVisualEffect(_.VFX_IMP_FLAME_S),
                    _.EffectDamage(
                        _.d6(2),
                        _.DAMAGE_TYPE_BLUDGEONING));

            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, eEffect, oTarget);

            _.DelayCommand(6.0f, () => { ApplySandstorm(oTarget, oArea); });
        }

        public static void DoWeatherEffects(NWObject oCreature)
        {
            NWObject oArea = _.GetArea(oCreature);
            if (_.GetIsAreaInterior(oArea) == 1 || _.GetIsAreaAboveGround(oArea) == 0) return;

            int nHeat = GetHeatIndex(oArea);
            int nHumidity = GetHumidity(oArea);
            int nWind = GetWindStrength(oArea);
            bool bStormy = _.GetSkyBox(oArea) == _.SKYBOX_GRASS_STORM;
            bool bIsPC  = (_.GetIsPC(oCreature) == 1);
            string sMessage = "";
            PlanetaryClimate climate = _GetClimate(oArea);

            //--------------------------------------------------------------------------
            // Apply acid rain, if applicable.  Stolen shamelessly from the Melf's Acid
            // Arrow spell.
            //--------------------------------------------------------------------------
            if (bIsPC && _.GetWeather(oArea) == _.WEATHER_RAIN && oArea.GetLocalInt(VAR_WEATHER_ACID_RAIN) == 1)
            {
                Effect eEffect =
                  _.EffectLinkEffects(
                      _.EffectVisualEffect(_.VFX_IMP_ACID_S),
                      _.EffectDamage(
                          _.d6(2),
                          _.DAMAGE_TYPE_ACID));

                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, eEffect, oCreature);

                _.DelayCommand(6.0f, () => { ApplyAcid(oCreature, oArea); });
            }
            else if (bIsPC && oArea.GetLocalInt("DUST_STORM") == 1)
            {
                sMessage = FB_T_WEATHER_DUST_STORM;
            }
            else if (bIsPC && oArea.GetLocalInt("SAND_STORM") == 1)
            {
                sMessage = FB_T_WEATHER_SAND_STORM;
                Effect eEffect =
                    _.EffectLinkEffects(
                        _.EffectVisualEffect(_.VFX_IMP_FLAME_S),
                        _.EffectDamage(
                            _.d6(2),
                            _.DAMAGE_TYPE_BLUDGEONING));

                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, eEffect, oCreature);

                _.DelayCommand(6.0f, () => { ApplySandstorm(oCreature, oArea); });
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
                    if (_.GetIsNight() == 0) sMessage = string.IsNullOrWhiteSpace(climate.special_mild) ? FB_T_WEATHER_MESSAGE_MILD : climate.special_mild;
                    else sMessage = string.IsNullOrWhiteSpace(climate.special_mild_night) ? FB_T_WEATHER_MESSAGE_MILD_NIGHT : climate.special_mild_night;
                }
                else if (nWind < 8) sMessage = string.IsNullOrWhiteSpace(climate.special_cloudy) ? FB_T_WEATHER_MESSAGE_CLOUDY : climate.special_cloudy;
                else sMessage = string.IsNullOrWhiteSpace(climate.special_windy) ? FB_T_WEATHER_MESSAGE_WINDY : climate.special_windy;

                _.SendMessageToPC(oCreature, sMessage);
            }
        }
        
        public static int GetHeatIndex()
        {
            return GetHeatIndex(NWGameObject.OBJECT_SELF);
        }

        public static int GetHeatIndex(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = _.GetModule();
            int nHeat = oMod.GetLocalInt(VAR_WEATHER_HEAT);
            if (oArea.IsValid)
            {
                nHeat += oArea.GetLocalInt(VAR_WEATHER_HEAT);
                nHeat += _GetClimate(oArea).Heat_Modifier;
            }

            nHeat = (_.GetIsNight() == 1) ? nHeat - 2 : nHeat + 2;

            if (nHeat > 10) nHeat = 10;
            if (nHeat < 1) nHeat = 1;

            return nHeat;
        }

        public static int GetHumidity()
        {
            return GetHumidity(NWGameObject.OBJECT_SELF);
        }

        public static int GetHumidity(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = _.GetModule();
            int nHumidity = oMod.GetLocalInt(VAR_WEATHER_HUMIDITY);
            if (oArea.IsValid)
            {
                nHumidity += oArea.GetLocalInt(VAR_WEATHER_HUMIDITY);
                nHumidity += _GetClimate(oArea).Humidity_Modifier;
            }

            if (nHumidity > 10) nHumidity = 10;
            if (nHumidity < 1) nHumidity = 1;

            return nHumidity;
        }

        public static int GetWindStrength()
        {
            return GetWindStrength(NWGameObject.OBJECT_SELF);
        }

        public static int GetWindStrength(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = _.GetModule();
            int nWind = oMod.GetLocalInt(VAR_WEATHER_WIND);
            if (oArea.IsValid)
            {
                nWind += oArea.GetLocalInt(VAR_WEATHER_WIND);
                nWind += _GetClimate(oArea).Wind_Modifier;

                //----------------------------------------------------------------------
                // Automatic cover bonus for artificial areas such as cities (lots of
                // buildings).
                //----------------------------------------------------------------------
                if (_.GetIsAreaNatural(oArea) == 0) nWind -= 1;
            }

            if (nWind > 10) nWind = 10;
            if (nWind < 1) nWind = 1;

            return nWind;
        }

        private static void _SetHeatIndex(int nHeat)
        {
            NWObject oMod = _.GetModule();
            oMod.SetLocalInt(VAR_WEATHER_HEAT, nHeat);
        }

        private static void _SetHumidity(int nHumidity)
        {
            NWObject oMod = _.GetModule();
            oMod.SetLocalInt(VAR_WEATHER_HUMIDITY, nHumidity);
        }

        private static void _SetWindStrength(int nWind)
        {
            NWObject oMod = _.GetModule();
            oMod.SetLocalInt(VAR_WEATHER_WIND, nWind);
        }

        private static void _DoWindKnockdown(NWObject oCreature)
        {
            LoggingService.Trace(TraceComponent.Weather, "Checking whether " + _.GetName(oCreature) + " is blown over");
            int nDC = (_.GetHitDice(oCreature) / 2) + 10;
            int nDiscipline = _.GetSkillRank(_.SKILL_DISCIPLINE, oCreature);
            int nReflexSave = _.GetReflexSavingThrow(oCreature);
            int nSuccess;

            if (nDiscipline > nReflexSave)
                nSuccess = _.GetIsSkillSuccessful(oCreature, _.SKILL_DISCIPLINE, nDC);
            else
                nSuccess = _.ReflexSave(oCreature, nDC);

            if (nSuccess == 0)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY,
                                      _.EffectKnockdown(),
                                      oCreature,
                                      6.0f);
                _.FloatingTextStringOnCreature("*is unbalanced by a strong gust*", oCreature);
            }
        }

        public static void Thunderstorm(NWObject oArea)
        {
            // 1 in 3 chance of a bolt.
            if (_.d3() != 1) return;

            // Pick a spot. Any spot.
            int nWidth = _.GetAreaSize(_.AREA_WIDTH, oArea);
            int nHeight = _.GetAreaSize(_.AREA_HEIGHT, oArea);
            int nPointWide = _.Random(nWidth * 10);
            int nPointHigh = _.Random(nHeight * 10);
            float fStrikeX = _.IntToFloat(nPointWide) + (_.IntToFloat(_.Random(10)) * 0.1f);
            float fStrikeY = _.IntToFloat(nPointHigh) + (_.IntToFloat(_.Random(10)) * 0.1f);

            // Now find out the power
            int nPower = _.d100() + 10;

            // Fire ze thunderboltz!
            _.DelayCommand(_.IntToFloat(_.Random(60)),
                () =>
                {
                    _Thunderstorm(_.Location(oArea,
                                               _.Vector(fStrikeX, fStrikeY),
                                               0.0f),
                                           nPower);
                }
                         );
        }

        private static void _Thunderstorm(NWLocation lLocation, int nPower)
        {
            float fRange = _.IntToFloat(nPower) * 0.1f;
            // Caps on sphere of influence
            if (fRange < 3.0) fRange = 3.0f;
            if (fRange > 6.0) fRange = 6.0f;

            //Effects
            Effect eEffBolt = _.EffectVisualEffect(_.VFX_IMP_LIGHTNING_M);
            Effect eEffKnock = _.EffectKnockdown();
            _.ApplyEffectAtLocation(_.DURATION_TYPE_INSTANT, eEffBolt, lLocation);

            Effect eEffDam;
            int nType;
            NWObject oObject = _.GetFirstObjectInShape(_.SHAPE_SPHERE, fRange, lLocation, 0, _.OBJECT_TYPE_CREATURE | _.OBJECT_TYPE_DOOR | _.OBJECT_TYPE_PLACEABLE);
            while (_.GetIsObjectValid(oObject) == 1)
            {
                nType = _.GetObjectType(oObject);
                if ((nType & (_.OBJECT_TYPE_CREATURE | _.OBJECT_TYPE_DOOR | _.OBJECT_TYPE_PLACEABLE)) == 1)
                {
                    eEffDam = _.EffectDamage(
                        _.FloatToInt(_.IntToFloat(nPower) - (_.GetDistanceBetweenLocations(lLocation, _.GetLocation(oObject)) * 10.0f)),
                        _.DAMAGE_TYPE_ELECTRICAL);
                    _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, eEffDam, oObject);

                    if (nType == _.OBJECT_TYPE_CREATURE)
                    {
                        if (_.GetIsPC(oObject) == 1) _.SendMessageToPC(oObject, FB_T_WEATHER_LIGHTNING);

                        _.PlayVoiceChat(_.VOICE_CHAT_PAIN1, oObject);
                        _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, eEffKnock, oObject, _.IntToFloat(_.d6(1)));
                    }
                }
                oObject = _.GetNextObjectInShape(_.SHAPE_SPHERE, fRange, lLocation, 0, _.OBJECT_TYPE_CREATURE | _.OBJECT_TYPE_DOOR | _.OBJECT_TYPE_PLACEABLE);
            }
        }

        private static void OnAreaEnter()
        {
            using (new Profiler("WeatherService.OnAreaEnter"))
            {
                SetWeather();

                LoggingService.Trace(TraceComponent.Weather, "Applying weather to creature: " + _.GetName(_.GetEnteringObject()));

                DoWeatherEffects(_.GetEnteringObject());

                NWArea oArea = (NWGameObject.OBJECT_SELF);
                int nHour = _.GetTimeHour();
                int nLastHour = oArea.GetLocalInt("WEATHER_LAST_HOUR");

                if (nHour != nLastHour)
                {
                    if (!oArea.Data.ContainsKey("WEATHER_OBJECTS"))
                        oArea.Data["WEATHER_OBJECTS"] = new List<NWPlaceable>();
                    List<NWPlaceable> weatherObjects = oArea.Data["WEATHER_OBJECTS"];

                    LoggingService.Trace(TraceComponent.Weather, "Cleaning up old weather");

                    // Clean up any old weather placeables.
                    for (int x = weatherObjects.Count - 1; x >= 0; x--)
                    {
                        var placeable = weatherObjects.ElementAt(x);
                        placeable.Destroy();
                        weatherObjects.RemoveAt(x);
                    }

                    // Create new ones depending on the current weather.
                    int nWeather = GetWeather();
                    LoggingService.Trace(TraceComponent.Weather, "Current weather: " + nWeather.ToString());

                    if (nWeather == WEATHER_FOGGY)
                    {
                        // Get the size in tiles.
                        int nSizeX = _.GetAreaSize(_.AREA_WIDTH, oArea);
                        int nSizeY = _.GetAreaSize(_.AREA_HEIGHT, oArea);

                        // We want one placeable per 8 tiles.
                        int nMax = (nSizeX * nSizeY) / 8;
                        LoggingService.Trace(TraceComponent.Weather, "Creating up to " + nMax.ToString() + " mist objects.");

                        for (int nCount = _.d6(); nCount < nMax; nCount++)
                        {
                            Vector vPosition = _.GetPosition(_.GetEnteringObject());

                            // Vectors are in meters - 10 meters to a tile. 
                            vPosition.m_X = _.IntToFloat(_.Random(nSizeX * 10));
                            vPosition.m_Y = _.IntToFloat(_.Random(nSizeY * 10));

                            float fFacing = _.IntToFloat(_.Random(360));

                            string sResRef = "x3_plc_mist";

                            NWPlaceable oPlaceable = _.CreateObject(_.OBJECT_TYPE_PLACEABLE, sResRef, _.Location(oArea, vPosition, fFacing));
                            _.SetObjectVisualTransform(oPlaceable, _.OBJECT_VISUAL_TRANSFORM_SCALE, _.IntToFloat(200 + _.Random(200)) / 100.0f);

                            weatherObjects.Add(oPlaceable);
                        }
                    }

                    oArea.Data["WEATHER_OBJECTS"] = weatherObjects;
                    oArea.SetLocalInt("WEATHER_LAST_HOUR", nHour);
                }
            }
        }

        private static void OnModuleHeartbeat()
        {
            NWObject oMod = _.GetModule();
            int nHour = _.GetTimeHour();
            int nLastHour = oMod.GetLocalInt("WEATHER_LAST_HOUR");

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

        public static void OnCreatureSpawn()
        {
            DoWeatherEffects(NWGameObject.OBJECT_SELF);
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
