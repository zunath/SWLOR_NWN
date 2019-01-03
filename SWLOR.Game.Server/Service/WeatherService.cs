using NWN;
using System;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Collections.Generic;
using System.Text;
using Object = NWN.Object;
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
*/
namespace SWLOR.Game.Server.Service
{
    public class WeatherService : IWeatherService
    {
        // Other services.
        private readonly INWScript _;
        private readonly IErrorService _error;

        // Feedback texts.
        const string FB_T_WEATHER_LIGHTNING = "You were hit by the bolt of lightning!";
        const string FB_T_WEATHER_MESSAGE_CLOUDY = "There are a fair few clouds overhead, and it is quite windy.";
        const string FB_T_WEATHER_MESSAGE_COLD_CLOUDY = "It is cold out, and clouds are prominent overhead. Wrapping up warm is advised.";
        const string FB_T_WEATHER_MESSAGE_COLD_FOGGY = "The wind is strong, and pretty cold. Make sure you are warm enough, and be wary of rain.";
        const string FB_T_WEATHER_MESSAGE_COLD_MILD = "It is quite cold out. Wrap up warm by wearing winter clothing.";
        const string FB_T_WEATHER_MESSAGE_FREEZING = "The air is bitingly cold right now. Make sure you are wrapped up warm and have plenty of rations.";
        const string FB_T_WEATHER_MESSAGE_FOGGY = "The wind is very strong, and there are many clouds in the sky. It is likely to rain soon.";
        const string FB_T_WEATHER_MESSAGE_MILD = "It is lovely and sunny here.";
        const string FB_T_WEATHER_MESSAGE_MILD_NIGHT = "The weather is fine tonight.";
        const string FB_T_WEATHER_MESSAGE_MIST = "It is still and very humid, the mist hangs in the air about you.";
        const string FB_T_WEATHER_MESSAGE_WARM_CLOUDY = "It is hot, and clouds are dotted around. Travels will be tiring - you should wear light clothing.";
        const string FB_T_WEATHER_MESSAGE_WARM_FOGGY = "Warm gusts of wind ripple the air here, and there are a worrying number of clouds casting shadows over the earth. We might experience thunderstorms, so be careful.";
        const string FB_T_WEATHER_MESSAGE_WARM_MILD = "It is warm and calm here. Make sure you have enough to drink in the extra heat, and wear light clothing.";
        const string FB_T_WEATHER_MESSAGE_RAIN_NORMAL = "It is raining. Your travels will be a little difficult because of it.";
        const string FB_T_WEATHER_MESSAGE_RAIN_WARM = "It is raining, and the air is humid. Thunderstorms are likely, and it will be more difficult to make progress on your journey.";
        const string FB_T_WEATHER_MESSAGE_SCORCHING = "The heat is blazing here! You should wear something to protect your face and hands, if you can.";
        const string FB_T_WEATHER_MESSAGE_SNOW = "It is snowing right now! Remember to wrap up warm and pack extra provisions.";
        const string FB_T_WEATHER_MESSAGE_STORM = "There is a thunderstorm at the moment. It will be quite dangerous out.";
        
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

        const string WEATHER = "WEATHER"; // for tracing

        public WeatherService(INWScript script,
                              IErrorService error
            )
        {
            _ = script;
            _error = error;
        }

        public void AdjustWeather()
        {
            _error.Trace(WEATHER, "Adjusting module weather");
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
                _error.Trace(WEATHER, "No change needed... yet.");
                return;
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

            _error.Trace(WEATHER, "New weather settings: heat - " + _.IntToString(nHeat) +
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
            _error.Trace(WEATHER, "Change the weather next at hour " + _.IntToString(nNextChange));
            oMod.SetLocalInt(VAR_WEATHER_CHANGE, nNextChange);

            // Update all occupied areas with the new settings.
            NWObject oPC = _.GetFirstPC();
            while (_.GetIsObjectValid(oPC) == 1)
            {
                SetWeather(_.GetArea(oPC));
                oPC = _.GetNextPC();
            }
        }

        public void SetWeather()
        {
            SetWeather(Object.OBJECT_SELF);
        }

        public void SetWeather(NWObject oArea)
        {
            
            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
            {
                if (_.GetIsAreaInterior(oArea) == 1 ||
                    _.GetIsAreaAboveGround(oArea) == 0)
                    return;
                oArea.SetLocalInt(VAR_SKYBOX, _.GetSkyBox(oArea));
                oArea.SetLocalInt(VAR_FOG_SUN, _.GetFogAmount(NWScript.FOG_TYPE_SUN, oArea));
                oArea.SetLocalInt(VAR_FOG_MOON, _.GetFogAmount(NWScript.FOG_TYPE_MOON, oArea));
                oArea.SetLocalInt(VAR_FOG_C_SUN, _.GetFogColor(NWScript.FOG_TYPE_SUN, oArea));
                oArea.SetLocalInt(VAR_FOG_C_MOON, _.GetFogColor(NWScript.FOG_TYPE_MOON, oArea));
                oArea.SetLocalInt(VAR_INITIALIZED, 1);
            }

            int nHeat = GetHeatIndex(oArea);
            int nHumidity = GetHumidity(oArea);
            int nWind = GetWindStrength(oArea);
            bool bStormy = _.GetSkyBox(oArea) == NWScript.SKYBOX_GRASS_STORM;

            _error.Trace(WEATHER, "Area weather settings for area: " + _.GetName(oArea) +
                                                  ", heat - " + _.IntToString(nHeat) +
                                              ", humidity - " + _.IntToString(nHumidity) +
                                                  ", wind - " + _.IntToString(nWind) +
                                                 ", storm - " + bStormy.ToString());

            //--------------------------------------------------------------------------
            // Process weather rules for this area.
            //--------------------------------------------------------------------------
            if (nHumidity > 7 && nHeat > 3)
            {
                if (nHeat < 6 && nWind < 3)
                {
                    _.SetWeather(oArea, NWScript.WEATHER_CLEAR);
                }
                else _.SetWeather(oArea, NWScript.WEATHER_RAIN);
            }
            else if (nHumidity > 7) _.SetWeather(oArea, NWScript.WEATHER_SNOW);
            else _.SetWeather(oArea, NWScript.WEATHER_CLEAR);

            //--------------------------------------------------------------------------
            // Stormy if heat is greater than 4 only; if already stormy then 2 in 3
            // chance of storm clearing, otherwise x in 20 chance of storm starting,
            // where x is the wind level.
            //--------------------------------------------------------------------------
            if (nHeat > 4 && nHumidity > 7 &&
                ((bStormy && _.d20() - nWind < 1) || (bStormy && _.d3() == 1)))
            {
                _error.Trace(WEATHER, "A thunderstorm is now raging in " + _.GetName(oArea));
                _.SetSkyBox(NWScript.SKYBOX_GRASS_STORM, oArea);
                Thunderstorm(oArea);
                oArea.SetLocalInt("GS_AM_SKY_OVERRIDE", 1);
            }
            else
            {
                _.SetSkyBox(oArea.GetLocalInt(VAR_SKYBOX), oArea);
                oArea.DeleteLocalInt("GS_AM_SKY_OVERRIDE");
            }

            // Replaced by code in a_enter to spawn mist placeables. TODO (and rescale them)
            /*	
            if (bFoggy)
            {
                SetFogAmount(FOG_TYPE_ALL, 35, oArea);
                SetFogColor(FOG_TYPE_ALL, 0xEEEEEE, oArea);
                SetLocalInt(oArea, "GS_AM_FOG_OVERRIDE", TRUE);
            }
            else
            {
                SetFogAmount(FOG_TYPE_SUN, GetLocalInt(oArea,VAR_FOG_SUN), oArea);
                SetFogAmount(FOG_TYPE_MOON, GetLocalInt(oArea,VAR_FOG_MOON), oArea);
                SetFogColor(FOG_TYPE_SUN, GetLocalInt(oArea,VAR_FOG_C_SUN), oArea);
                SetFogColor(FOG_TYPE_MOON, GetLocalInt(oArea,VAR_FOG_C_MOON), oArea);
                DeleteLocalInt(oArea, "GS_AM_FOG_OVERRIDE");
            }
            */
        }

        public int GetWeather()
        {
            return GetWeather(Object.OBJECT_SELF);
        }

        public int GetWeather(NWObject oArea)
        {
            if (_.GetIsAreaInterior(oArea) == 1 || _.GetIsAreaAboveGround(oArea) == 0)
            {
                return NWScript.WEATHER_INVALID;
            }

            int nHeat = GetHeatIndex(oArea);
            int nHumidity = GetHumidity(oArea);
            int nWind = GetWindStrength(oArea);

            if (nHumidity > 7 && nHeat > 3 && nHeat < 6 && nWind < 3)
            {
                return WEATHER_FOGGY;
            }

            return GetWeather(oArea);
        }

        public void OnCombatRoundEnd(NWObject oCreature)
        {
            NWObject oArea = _.GetArea(oCreature);
            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
                return;

            int nWind = GetWindStrength(oArea);

            if (nWind > 9) _DoWindKnockdown(oCreature);
        }

        public void ApplyAcid(NWObject oTarget, NWObject oArea)
        {
            if ((NWObject)_.GetArea(oTarget) != oArea) return;
            if (_.GetIsDead(oTarget) == 1) return;
            if (_.GetIsPC(oTarget) == 1 && _.GetIsPC(_.GetMaster(oTarget)) == 0) return;

            //apply
            Effect eEffect =
                _.EffectLinkEffects(
                    _.EffectVisualEffect(NWScript.VFX_IMP_ACID_S),
                    _.EffectDamage(
                        _.d6(1),
                        NWScript.DAMAGE_TYPE_ACID));

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, eEffect, oTarget);

            _.DelayCommand(6.0f, () => { ApplyAcid(oTarget, oArea); });
        }

        public void DoWeatherEffects(NWObject oCreature)
        {
            NWObject oArea = _.GetArea(oCreature);
            if (_.GetIsAreaInterior(oArea) == 1 || _.GetIsAreaAboveGround(oArea) == 0) return;

            if (oArea.GetLocalInt(VAR_INITIALIZED) == 0)
            {
                _DoWeatherEffects(oCreature);
            }
            int nHeat = GetHeatIndex(oArea);
            int nHumidity = GetHumidity(oArea);
            int nWind = GetWindStrength(oArea);
            bool bStormy = _.GetSkyBox(oArea) == NWScript.SKYBOX_GRASS_STORM;

            //--------------------------------------------------------------------------
            // Apply bonuses/penalties based on clothing
            //--------------------------------------------------------------------------
            //object oBoots = GetItemInSlot(INVENTORY_SLOT_BOOTS, oCreature);
            //object oCloak = GetItemInSlot(INVENTORY_SLOT_CLOAK, oCreature);
            //if (GetIsObjectValid(oCloak))
            //    nHeat += 1;
            //if (!GetIsObjectValid(oBoots))
            //    nHeat -= 1;

            // Apply acid rain, if applicable.  Stolen shamelessly from the Melf's Acid
            // Arrow spell.
            if (_.GetWeather(oArea) == NWScript.WEATHER_RAIN && oArea.GetLocalInt(VAR_WEATHER_ACID_RAIN) == 1)
            {
                Effect eEffect =
                  _.EffectLinkEffects(
                      _.EffectVisualEffect(NWScript.VFX_IMP_ACID_S),
                      _.EffectDamage(
                          _.d6(2),
                          NWScript.DAMAGE_TYPE_ACID));

                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, eEffect, oCreature);

                _.DelayCommand(6.0f, () => { ApplyAcid(oCreature, oArea); });
            }

            int bIsPC = _.GetIsPC(oCreature);

            //--------------------------------------------------------------------------
            // Apply modifiers to this creature.
            //--------------------------------------------------------------------------
            if (nWind > 8)
            {
                oCreature.SetLocalString(VAR_EFFECTOR, _.GetTag(oArea));
                //----------------------------------------------------------------------
                // Apply move penalty - removed - too buggy. e.g. stacks.
                //----------------------------------------------------------------------
                //ApplyEffectToObject(DURATION_TYPE_PERMANENT,
                //                    SupernaturalEffect(EffectMovementSpeedDecrease(10)),
                //                    oCreature);
            }
            else
            {
                _DoWeatherEffects(oCreature);
            }

            if (nHeat > 8)
            {
                //ApplyEffectToObject(DURATION_TYPE_INSTANT,
                //                    EffectDamage(nHeat - 8, DAMAGE_TYPE_FIRE),
                //                    oCreature);
            }
            else if (nHeat < 3)
            {
                //ApplyEffectToObject(DURATION_TYPE_INSTANT,
                //                    EffectDamage(3 - nHeat, DAMAGE_TYPE_COLD),
                //                    oCreature);
            }

            if (bIsPC == 1)
            {
                string sMessage = "";
                // Stormy weather
                if (bStormy)
                {
                    sMessage = FB_T_WEATHER_MESSAGE_STORM;
                }
                // Rain or mist
                else if (nHumidity > 7 && nHeat > 3)
                {
                    // Mist
                    if (nHeat < 6 && nWind < 3)
                    {
                        sMessage = FB_T_WEATHER_MESSAGE_MIST;
                    }
                    // Humid
                    else if (nHeat > 7)
                    {
                        sMessage = FB_T_WEATHER_MESSAGE_RAIN_WARM;
                    }
                    else
                    {
                        sMessage = FB_T_WEATHER_MESSAGE_RAIN_NORMAL;
                    }
                }
                // Snow
                else if (nHumidity > 7)
                {
                    sMessage = FB_T_WEATHER_MESSAGE_SNOW;
                }
                // Freezing
                else if (nHeat < 3)
                {
                    sMessage = FB_T_WEATHER_MESSAGE_FREEZING;
                }
                // Boiling
                else if (nHeat > 8)
                {
                    sMessage = FB_T_WEATHER_MESSAGE_SCORCHING;
                }
                // Cold
                else if (nHeat < 5)
                {
                    if (nWind < 5) sMessage = FB_T_WEATHER_MESSAGE_COLD_MILD;
                    else if (nWind < 8) sMessage = FB_T_WEATHER_MESSAGE_COLD_CLOUDY;
                    else sMessage = FB_T_WEATHER_MESSAGE_COLD_FOGGY;
                }
                // Hot
                else if (nHeat > 6)
                {
                    if (nWind < 5) sMessage = FB_T_WEATHER_MESSAGE_WARM_MILD;
                    else if (nWind < 8) sMessage = FB_T_WEATHER_MESSAGE_WARM_CLOUDY;
                    else sMessage = FB_T_WEATHER_MESSAGE_WARM_FOGGY;
                }
                else if (nWind < 5)
                {
                    if (_.GetIsNight() == 0) sMessage = FB_T_WEATHER_MESSAGE_MILD;
                    else sMessage = FB_T_WEATHER_MESSAGE_MILD_NIGHT;
                }
                else if (nWind < 8) sMessage = FB_T_WEATHER_MESSAGE_CLOUDY;
                else sMessage = FB_T_WEATHER_MESSAGE_FOGGY;

                _.SendMessageToPC(oCreature, sMessage);
            }
        }

        void _DoWeatherEffects(NWObject oCreature)
        {
            //----------------------------------------------------------------------
            // Remove all previous penalties
            //----------------------------------------------------------------------
            NWObject oOldArea = _.GetObjectByTag(oCreature.GetLocalString(VAR_EFFECTOR));
            if (_.GetIsObjectValid(oOldArea) == 1)
            {
                Effect eBad = _.GetFirstEffect(oCreature);
                while (_.GetIsEffectValid(eBad) == 1)
                {
                    if ((NWObject)_.GetEffectCreator(eBad) == oOldArea)
                        _.RemoveEffect(oCreature, eBad);
                    eBad = _.GetNextEffect(oCreature);
                }
            }
        }

        public int GetHeatIndex()
        {
            return GetHeatIndex(Object.OBJECT_SELF);
        }

        public int GetHeatIndex(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = _.GetModule();
            int nHeat = oMod.GetLocalInt(VAR_WEATHER_HEAT);
            nHeat += oArea.GetLocalInt(VAR_WEATHER_HEAT);
            nHeat = (_.GetIsNight() == 1) ? nHeat - 1 : nHeat + 1;
            return nHeat;
        }

        public int GetHumidity()
        {
            return GetHumidity(Object.OBJECT_SELF);
        }

        public int GetHumidity(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = _.GetModule();
            int nHumidity = oMod.GetLocalInt(VAR_WEATHER_HUMIDITY);
            nHumidity += oArea.GetLocalInt(VAR_WEATHER_HUMIDITY);
            return nHumidity;
        }

        public int GetWindStrength()
        {
            return GetWindStrength(Object.OBJECT_SELF);
        }

        public int GetWindStrength(NWObject oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            NWObject oMod = _.GetModule();
            int nWind = oMod.GetLocalInt(VAR_WEATHER_WIND);
            nWind += oArea.GetLocalInt(VAR_WEATHER_WIND);
            //--------------------------------------------------------------------------
            // Automatic cover bonus for artificial areas such as cities (lots of
            // buildings).
            //--------------------------------------------------------------------------
            if (_.GetIsAreaNatural(oArea) == 0) nWind -= 1;
            return nWind;
        }

        void _SetHeatIndex(int nHeat)
        {
            NWObject oMod = _.GetModule();
            oMod.SetLocalInt(VAR_WEATHER_HEAT, nHeat);
        }

        void _SetHumidity(int nHumidity)
        {
            NWObject oMod = _.GetModule();
            oMod.SetLocalInt(VAR_WEATHER_HUMIDITY, nHumidity);
        }

        void _SetWindStrength(int nWind)
        {
            NWObject oMod = _.GetModule();
            oMod.SetLocalInt(VAR_WEATHER_WIND, nWind);
        }

        void _DoWindKnockdown(NWObject oCreature)
        {
            _error.Trace(WEATHER, "Checking whether " + _.GetName(oCreature) + " is blown over");
            int nDC = (_.GetHitDice(oCreature) / 2) + 10;
            int nDiscipline = _.GetSkillRank(NWScript.SKILL_DISCIPLINE, oCreature);
            int nReflexSave = _.GetReflexSavingThrow(oCreature);
            int nSuccess;

            if (nDiscipline > nReflexSave)
                nSuccess = _.GetIsSkillSuccessful(oCreature, NWScript.SKILL_DISCIPLINE, nDC);
            else
                nSuccess = _.ReflexSave(oCreature, nDC);

            if (nSuccess == 0)
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY,
                                      _.EffectKnockdown(),
                                      oCreature,
                                      6.0f);
                _.FloatingTextStringOnCreature("*is unbalanced by a strong gust*", oCreature);
            }
        }

        public void Thunderstorm(NWObject oArea)
        {
            // 1 in 3 chance of a bolt.
            if (_.d3() != 1) return;

            // Pick a spot. Any spot.
            int nWidth = _.GetAreaSize(NWScript.AREA_WIDTH, oArea);
            int nHeight = _.GetAreaSize(NWScript.AREA_HEIGHT, oArea);
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

        void _Thunderstorm(NWLocation lLocation, int nPower)
        {
            float fRange = _.IntToFloat(nPower) * 0.1f;
            // Caps on sphere of influence
            if (fRange < 3.0) fRange = 3.0f;
            if (fRange > 6.0) fRange = 6.0f;

            //Effects
            Effect eEffBolt = _.EffectVisualEffect(NWScript.VFX_IMP_LIGHTNING_M);
            Effect eEffKnock = _.EffectKnockdown();
            _.ApplyEffectAtLocation(NWScript.DURATION_TYPE_INSTANT, eEffBolt, lLocation);

            Effect eEffDam;
            int nType;
            NWObject oObject = _.GetFirstObjectInShape(NWScript.SHAPE_SPHERE, fRange, lLocation, 0, NWScript.OBJECT_TYPE_CREATURE | NWScript.OBJECT_TYPE_DOOR | NWScript.OBJECT_TYPE_PLACEABLE);
            while (_.GetIsObjectValid(oObject) == 1)
            {
                nType = _.GetObjectType(oObject);
                if ((nType & (NWScript.OBJECT_TYPE_CREATURE | NWScript.OBJECT_TYPE_DOOR | NWScript.OBJECT_TYPE_PLACEABLE)) == 1)
                {
                    eEffDam = _.EffectDamage(
                        _.FloatToInt(_.IntToFloat(nPower) - (_.GetDistanceBetweenLocations(lLocation, _.GetLocation(oObject)) * 10.0f)),
                        NWScript.DAMAGE_TYPE_ELECTRICAL);
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, eEffDam, oObject);

                    if (nType == NWScript.OBJECT_TYPE_CREATURE)
                    {
                        if (_.GetIsPC(oObject) == 1) _.SendMessageToPC(oObject, FB_T_WEATHER_LIGHTNING);

                        _.PlayVoiceChat(NWScript.VOICE_CHAT_PAIN1, oObject);
                        _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, eEffKnock, oObject, _.IntToFloat(_.d6(1)));
                    }
                }
                oObject = _.GetNextObjectInShape(NWScript.SHAPE_SPHERE, fRange, lLocation, 0, NWScript.OBJECT_TYPE_CREATURE | NWScript.OBJECT_TYPE_DOOR | NWScript.OBJECT_TYPE_PLACEABLE);
            }
        }

        public void OnAreaEnter()
        {
            SetWeather();
            DoWeatherEffects(_.GetEnteringObject());

            NWObject oArea = Object.OBJECT_SELF;
            int nHour = _.GetTimeHour();
            int nLastHour = oArea.GetLocalInt("WEATHER_LAST_HOUR");

            if (nHour != nLastHour)

            {
                // Clean up any old weather placeables.
                NWObject oPlaceable = _.GetFirstObjectInArea(oArea);
                while (_.GetIsObjectValid(oPlaceable) == 1)
                {
                    if(oPlaceable.GetLocalInt("WEATHER") == 1)
                    {
                        _.DestroyObject(oPlaceable);
                    }
                }

                // Create new ones depending on the current weather.
                int nWeather = GetWeather();

                if (nWeather == WEATHER_FOGGY)
                {
                    for (int nCount = _.d6() ; nCount < 9; nCount++)
                    {
                        int nSizeX = _.GetAreaSize(NWScript.AREA_WIDTH, oArea);
                        int nSizeY = _.GetAreaSize(NWScript.AREA_HEIGHT, oArea);
                        Vector vPosition = _.GetPosition(_.GetEnteringObject());

                        vPosition.m_X = _.IntToFloat(_.Random(nSizeX));
                        vPosition.m_Y = _.IntToFloat(_.Random(nSizeY));

                        float fFacing = _.IntToFloat(_.Random(360));

                        string sResRef = "x3_plc_mist";

                        oPlaceable = _.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sResRef, _.Location(oArea, vPosition, fFacing));
                        _.SetObjectVisualTransform(oPlaceable, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, _.IntToFloat(100 + _.Random(300)) / 100.0f);
                        oPlaceable.SetLocalInt("WEATHER", 1);
                    }
                }

                oArea.SetLocalInt("WEATHER_LAST_HOUR", nHour);
            }
        }

        public void OnModuleHeartbeat()
        {
            NWObject oMod = _.GetModule();
            int nHour = _.GetTimeHour();
            int nLastHour = oMod.GetLocalInt("WEATHER_LAST_HOUR");

            if (nHour != nLastHour)
            {
                AdjustWeather();

                foreach (var player in NWModule.Get().Players)
                {
                    DoWeatherEffects(player);
                }
                oMod.SetLocalInt("WEATHER_LAST_HOUR", nHour);
            }
        }

        public void OnCreatureSpawn()
        {
            DoWeatherEffects(Object.OBJECT_SELF);
        }
        
        public void SetAreaHeatModifier(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_HEAT, nModifier);
        }

        public void SetAreaWindModifier(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_WIND, nModifier);
        }

        public void SetAreaHumidityModifier(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_HUMIDITY, nModifier);
        }

        public void SetAreaAcidRain(NWObject oArea, int nModifier)
        {
            oArea.SetLocalInt(VAR_WEATHER_ACID_RAIN, nModifier);
        }
    }
}
