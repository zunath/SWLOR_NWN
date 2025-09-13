using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.WeatherService;
using SWLOR.Game.Server.Extension;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class Weather
    {
        private static readonly Dictionary<uint, List<uint>> _areaWeatherPlaceables = new Dictionary<uint, List<uint>>();
        private static Dictionary<PlanetType, WeatherClimate> _planetClimates;
        private static readonly Dictionary<string, PlanetType> _planetsByName = new Dictionary<string, PlanetType>();

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadData()
        {
            _planetClimates = WeatherPlanetDefinitions.GetPlanetClimates();

            foreach (var @enum in Enum.GetValues(typeof(PlanetType)))
            {
                var type = (PlanetType)@enum;
                _planetsByName[type.GetDescriptionAttribute()] = type;
            }
        }

        /// <summary>
        /// Retrieves a planet's climate by its name. If the planet is not registered a default climate will be returned.
        /// </summary>
        /// <param name="planetName">The name of the planet to look for.</param>
        /// <returns>A weather climate for the specified planet.</returns>
        private static WeatherClimate GetClimateByPlanetName(string planetName)
        {
            if (!_planetsByName.ContainsKey(planetName))
            {
                return new WeatherClimate();
            }

            var planetType = _planetsByName[planetName];
            return _planetClimates[planetType];
        }

        private static WeatherClimate GetAreaClimate(uint area)
        {
            var index = GetName(area).IndexOf("-", StringComparison.Ordinal);
            if (index <= 0) return new WeatherClimate();
            var planetName = GetName(area).Substring(0, index - 1);

            return GetClimateByPlanetName(planetName);
        }

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


        public static bool AdjustWeather()
        {
            var oMod = GetModule();

            //--------------------------------------------------------------------------
            // Always change the weather the very first time
            //--------------------------------------------------------------------------
            if (GetLocalInt(oMod, VAR_INITIALIZED) == 0)
            {
                SetLocalInt(oMod, VAR_INITIALIZED, 1);
                _SetHumidity(Random(10) + 1);
            }
            else if (GetTimeHour() != GetLocalInt(oMod, VAR_WEATHER_CHANGE))
            {
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

            _SetHeatIndex(nHeat);
            _SetHumidity(nHumidity);
            _SetWindStrength(nWind);

            //--------------------------------------------------------------------------
            // Work out when to next change the weather.
            //--------------------------------------------------------------------------
            var nNextChange = GetTimeHour() + (11 - nWind);
            if (nNextChange > 23) nNextChange -= 24;
            SetLocalInt(oMod, VAR_WEATHER_CHANGE, nNextChange);

            // Update all occupied areas with the new settings.
            var oPC = GetFirstPC();
            while (GetIsObjectValid(oPC))
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

        public static void SetWeather(uint oArea)
        {

            if (GetLocalInt(oArea, VAR_INITIALIZED) == 0)
            {
                if (GetIsAreaInterior(oArea) ||
                    GetIsAreaAboveGround(oArea) == false)
                    return;
                SetLocalInt(oArea, VAR_SKYBOX, (int)GetSkyBox(oArea));
                SetLocalInt(oArea, VAR_FOG_SUN, GetFogAmount(FogType.Sun, oArea));
                SetLocalInt(oArea, VAR_FOG_MOON, GetFogAmount(FogType.Moon, oArea));
                SetLocalInt(oArea, VAR_FOG_C_SUN, (int)GetFogColor(FogType.Sun, oArea));
                SetLocalInt(oArea, VAR_FOG_C_MOON, (int)GetFogColor(FogType.Moon, oArea));
                SetLocalInt(oArea, VAR_INITIALIZED, 1);
            }

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);
            var bStormy = GetSkyBox(oArea) == Skybox.GrassStorm;
            var bDustStorm = (GetLocalInt(oArea, "DUST_STORM") == 1);
            var bSandStorm = (GetLocalInt(oArea, "SAND_STORM") == 1);
            var bSnowStorm = (GetLocalInt(oArea, "SNOW_STORM") == 1);

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
                SetSkyBox(Skybox.GrassStorm, oArea);
                Thunderstorm(oArea);
                SetLocalInt(oArea, "GS_AM_SKY_OVERRIDE", 1);
            }
            else
            {
                SetSkyBox((Skybox)GetLocalInt(oArea, VAR_SKYBOX), oArea);
                DeleteLocalInt(oArea, "GS_AM_SKY_OVERRIDE");
                bStormy = false;
            }

            // Does this area suffer from dust or sand storms?
            if (!bStormy && nWind >= 9 && d3() == 1)
            {
                // Dust storm - low visibility but no damage.
                if (GetAreaClimate(oArea).HasSandStorms)
                {
                    SetFogColor(FogType.Sun, FogColor.OrangeDark, oArea);
                    SetFogColor(FogType.Moon, FogColor.OrangeDark, oArea);
                    SetFogAmount(FogType.Sun, 80, oArea);
                    SetFogAmount(FogType.Moon, 80, oArea);

                    SetLocalInt(oArea, "SAND_STORM", 1);
                }
                else if (GetAreaClimate(oArea).HasSnowStorms)
                {
                    SetFogColor(FogType.Sun, FogColor.White, oArea);
                    SetFogColor(FogType.Moon, FogColor.White, oArea);
                    SetFogAmount(FogType.Sun, 80, oArea);
                    SetFogAmount(FogType.Moon, 80, oArea);

                    SetLocalInt(oArea, "SNOW_STORM", 1);
                }
            }
            else if (bDustStorm || bSandStorm || bSnowStorm)
            {
                // End the storm.
                DeleteLocalInt(oArea, "DUST_STORM");
                DeleteLocalInt(oArea, "SAND_STORM");
                DeleteLocalInt(oArea, "SNOW_STORM");

                SetFogColor(FogType.Sun, (FogColor)GetLocalInt(oArea, VAR_FOG_C_SUN), oArea);
                SetFogColor(FogType.Moon, (FogColor)GetLocalInt(oArea, VAR_FOG_C_MOON), oArea);
                SetFogAmount(FogType.Sun, GetLocalInt(oArea, VAR_FOG_SUN), oArea);
                SetFogAmount(FogType.Moon, GetLocalInt(oArea, VAR_FOG_MOON), oArea);
            }
        }

        public static NWN.API.NWScript.Enum.Weather GetWeather()
        {
            return GetWeather(OBJECT_SELF);
        }

        public static NWN.API.NWScript.Enum.Weather GetWeather(uint oArea)
        {
            if (GetIsAreaInterior(oArea) || GetIsAreaAboveGround(oArea) == false)
            {
                return NWN.API.NWScript.Enum.Weather.Invalid;
            }

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);

            if (nHumidity > 7 && nHeat > 3 && nHeat < 6 && nWind < 3)
            {
                return NWN.API.NWScript.Enum.Weather.Foggy;
            }

            // Rather unfortunately, the default method is also called GetWeather. 
            return NWScript.GetWeather(oArea);
        }

        public static void OnCombatRoundEnd(uint oCreature)
        {
            var oArea = GetArea(oCreature);
            if (GetLocalInt(oArea, VAR_INITIALIZED) == 0)
                return;

            var nWind = GetWindStrength(oArea);

            if (nWind > 9) _DoWindKnockdown(oCreature);
        }

        public static void ApplyAcid(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_S),
                    EffectDamage(
                        d6(),
                        DamageType.Acid));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplyAcid(oTarget, oArea); });
        }

        public static void ApplyCold(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_S),
                    EffectDamage(
                        d6(),
                        DamageType.Acid));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplyCold(oTarget, oArea); });
        }

        public static void ApplySandstorm(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

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

        public static void ApplySnowstorm(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

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

        public static void DoWeatherEffects(uint oCreature)
        {
            var oArea = GetArea(oCreature);
            if (GetIsAreaInterior(oArea) || GetIsAreaAboveGround(oArea) == false) return;

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);
            var bStormy = GetSkyBox(oArea) == Skybox.GrassStorm;
            var bIsPC = GetIsPC(oCreature);
            string sMessage;
            var climate = GetAreaClimate(oArea);

            //--------------------------------------------------------------------------
            // Apply acid rain, if applicable.  Stolen shamelessly from the Melf's Acid
            // Arrow spell.
            //--------------------------------------------------------------------------
            if (bIsPC && NWScript.GetWeather(oArea) == NWN.API.NWScript.Enum.Weather.Rain && GetLocalInt(oArea, VAR_WEATHER_ACID_RAIN) == 1)
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
            else if (bIsPC && GetLocalInt(oArea, "DUST_STORM") == 1)
            {
            }
            else if (bIsPC && GetLocalInt(oArea, "SAND_STORM") == 1)
            {
                var eEffect =
                    EffectLinkEffects(
                        EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S),
                        EffectDamage(
                            d6(2),
                            DamageType.Bludgeoning));

                ApplyEffectToObject(DurationType.Instant, eEffect, oCreature);

                DelayCommand(6.0f, () => { ApplySandstorm(oCreature, oArea); });
            }
            else if (bIsPC && GetLocalInt(oArea, "SNOW_STORM") == 1)
            {
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
                    sMessage = climate.StormText;
                }
                // Rain or mist
                else if (nHumidity > 7 && nHeat > 3)
                {
                    // Mist
                    if (nHeat < 6 && nWind < 3)
                    {
                        sMessage = climate.MistText;
                    }
                    // Humid
                    else if (nHeat > 7)
                    {
                        sMessage = climate.RainWarmText;
                    }
                    else
                    {
                        sMessage = climate.RainNormalText;
                    }
                }
                // Snow
                else if (nHumidity > 7)
                {
                    sMessage = climate.SnowText;
                }
                // Freezing
                else if (nHeat < 3)
                {
                    sMessage = climate.FreezingText;
                }
                // Boiling
                else if (nHeat > 8)
                {
                    sMessage = climate.ScorchingText;
                }
                // Cold
                else if (nHeat < 5)
                {
                    if (nWind < 5) sMessage = climate.ColdMildText;
                    else if (nWind < 8) sMessage = climate.ColdCloudyText;
                    else sMessage = climate.ColdWindyText;
                }
                // Hot
                else if (nHeat > 6)
                {
                    if (nWind < 5) sMessage = climate.WarmMildText;
                    else if (nWind < 8) sMessage = climate.WarmCloudyText;
                    else sMessage = climate.WarmWindyText;
                }
                else if (nWind < 5)
                {
                    if (GetIsNight() == false) sMessage = climate.MildText;
                    else sMessage = climate.MildNightText;
                }
                else if (nWind < 8) sMessage = climate.CloudyText;
                else sMessage = climate.WindyText;

                SendMessageToPC(oCreature, sMessage);
            }
        }

        public static int GetHeatIndex(uint oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            var oMod = GetModule();
            var nHeat = GetLocalInt(oMod, VAR_WEATHER_HEAT);
            if (GetIsObjectValid(oArea))
            {
                nHeat += GetLocalInt(oArea, VAR_WEATHER_HEAT);
                nHeat += GetAreaClimate(oArea).HeatModifier;
            }

            nHeat = (GetIsNight()) ? nHeat - 2 : nHeat + 2;

            if (nHeat > 10) nHeat = 10;
            if (nHeat < 1) nHeat = 1;

            return nHeat;
        }

        public static int GetHumidity(uint oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            var oMod = GetModule();
            var nHumidity = GetLocalInt(oMod, VAR_WEATHER_HUMIDITY);
            if (GetIsObjectValid(oArea))
            {
                nHumidity += GetLocalInt(oArea, VAR_WEATHER_HUMIDITY);
                nHumidity += GetAreaClimate(oArea).HumidityModifier;
            }

            if (nHumidity > 10) nHumidity = 10;
            if (nHumidity < 1) nHumidity = 1;

            return nHumidity;
        }

        public static int GetWindStrength(uint oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            var oMod = GetModule();
            var nWind = GetLocalInt(oMod, VAR_WEATHER_WIND);
            if (GetIsObjectValid(oArea))
            {
                nWind += GetLocalInt(oArea, VAR_WEATHER_WIND);
                nWind += GetAreaClimate(oArea).WindModifier;

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
            var oMod = GetModule();
            SetLocalInt(oMod, VAR_WEATHER_HEAT, nHeat);
        }

        private static void _SetHumidity(int nHumidity)
        {
            var oMod = GetModule();
            SetLocalInt(oMod, VAR_WEATHER_HUMIDITY, nHumidity);
        }

        private static void _SetWindStrength(int nWind)
        {
            var oMod = GetModule();
            SetLocalInt(oMod, VAR_WEATHER_WIND, nWind);
        }

        private static void _DoWindKnockdown(uint oCreature)
        {
            var nDC = (GetHitDice(oCreature) / 2) + 10;
            var nDiscipline = GetSkillRank(NWNSkillType.Discipline, oCreature);
            var nReflexSave = GetReflexSavingThrow(oCreature);
            int nSuccess;

            if (nDiscipline > nReflexSave)
                nSuccess = GetIsSkillSuccessful(oCreature, NWNSkillType.Discipline, nDC) ? 1 : 0;
            else
                nSuccess = ReflexSave(oCreature, nDC) == SavingThrowResultType.Success ? 1 : 0;

            if (nSuccess == 0)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), oCreature, 6.0f);
                FloatingTextStringOnCreature("*is unbalanced by a strong gust*", oCreature);
            }
        }

        public static void Thunderstorm(uint oArea)
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
                    Thunderstorm(Location(oArea,
                                               Vector3(fStrikeX, fStrikeY),
                                               0.0f),
                                           nPower);
                }
                         );
        }

        private static void Thunderstorm(Location lLocation, int nPower)
        {
            var fRange = IntToFloat(nPower) * 0.1f;
            // Caps on sphere of influence
            if (fRange < 3.0) fRange = 3.0f;
            if (fRange > 6.0) fRange = 6.0f;

            //Effects
            var eEffBolt = EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_M);
            ApplyEffectAtLocation(DurationType.Instant, eEffBolt, lLocation);

            var oObject = GetFirstObjectInShape(Shape.Sphere, fRange, lLocation, false, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable);
            while (GetIsObjectValid(oObject))
            {
                var nType = GetObjectType(oObject);
                if (nType == ObjectType.Creature ||
                    nType == ObjectType.Door ||
                    nType == ObjectType.Placeable)
                {
                    var eEffDam = EffectDamage(
                        FloatToInt(IntToFloat(nPower) - (GetDistanceBetweenLocations(lLocation, GetLocation(oObject)) * 10.0f)),
                        DamageType.Electrical);
                    ApplyEffectToObject(DurationType.Instant, eEffDam, oObject);

                    if (nType == ObjectType.Creature)
                    {
                        if (GetIsPC(oObject)) SendMessageToPC(oObject, WeatherFeedbackText.Lightning);

                        PlayVoiceChat(VoiceChat.Pain1, oObject);
                        var duration = IntToFloat(d6());
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), oObject, duration);
                    }
                }
                oObject = GetNextObjectInShape(Shape.Sphere, fRange, lLocation, false, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable);
            }
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            SetWeather();
            DoWeatherEffects(GetEnteringObject());

            var oArea = (OBJECT_SELF);
            var nHour = GetTimeHour();
            var nLastHour = GetLocalInt(oArea, "WEATHER_LAST_HOUR");

            if (nHour != nLastHour)
            {
                if (!_areaWeatherPlaceables.ContainsKey(oArea))
                    _areaWeatherPlaceables[oArea] = new List<uint>();

                var weatherObjects = _areaWeatherPlaceables[oArea];

                // Clean up any old weather placeables.
                for (var x = weatherObjects.Count - 1; x >= 0; x--)
                {
                    var placeable = weatherObjects.ElementAt(x);
                    DestroyObject(placeable);
                    weatherObjects.RemoveAt(x);
                }

                // Create new ones depending on the current weather.
                var nWeather = GetWeather();

                if (nWeather == NWN.API.NWScript.Enum.Weather.Foggy)
                {
                    // Get the size in tiles.
                    var nSizeX = GetAreaSize(Dimension.Width, oArea);
                    var nSizeY = GetAreaSize(Dimension.Height, oArea);

                    // We want one placeable per 8 tiles.
                    var nMax = (nSizeX * nSizeY) / 8;

                    for (var nCount = d6(); nCount < nMax; nCount++)
                    {
                        var vPosition = GetPosition(GetEnteringObject());

                        // Vectors are in meters - 10 meters to a tile. 
                        vPosition.X = IntToFloat(Random(nSizeX * 10));
                        vPosition.Y = IntToFloat(Random(nSizeY * 10));

                        var fFacing = IntToFloat(Random(360));

                        var sResRef = "x3_plc_mist";

                        var oPlaceable = CreateObject(ObjectType.Placeable, sResRef, Location(oArea, vPosition, fFacing));
                        SetObjectVisualTransform(oPlaceable, ObjectVisualTransform.Scale, IntToFloat(200 + Random(200)) / 100.0f);

                        weatherObjects.Add(oPlaceable);
                    }
                }

                _areaWeatherPlaceables[oArea] = weatherObjects;
                SetLocalInt(oArea, "WEATHER_LAST_HOUR", nHour);
            }
        }

        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void OnModuleHeartbeat()
        {
            var oMod = GetModule();
            var nHour = GetTimeHour();
            var nLastHour = GetLocalInt(oMod, "WEATHER_LAST_HOUR");

            if (nHour != nLastHour)
            {
                if (AdjustWeather())
                {
                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                    {
                        DoWeatherEffects(player);
                    }
                }

                SetLocalInt(oMod, "WEATHER_LAST_HOUR", nHour);
            }
        }

        public static void SetAreaHeatModifier(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_HEAT, nModifier);
        }

        public static void SetAreaWindModifier(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_WIND, nModifier);
        }

        public static void SetAreaHumidityModifier(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_HUMIDITY, nModifier);
        }

        public static void SetAreaAcidRain(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_ACID_RAIN, nModifier);
        }
    }
}
