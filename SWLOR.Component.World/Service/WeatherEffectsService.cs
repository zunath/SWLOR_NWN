using System;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Model;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using WeatherType = SWLOR.NWN.API.NWScript.Enum.WeatherType;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service for applying weather effects to creatures and handling weather-related damage.
    /// </summary>
    public class WeatherEffectsService : IWeatherEffectsService
    {
        private readonly IWeatherCalculationService _weatherCalculationService;
        private readonly IWeatherClimateService _weatherClimateService;

        // Module and area variables.
        private const string VAR_WEATHER_ACID_RAIN = "VAR_WEATHER_ACID_RAIN";
        private const string VAR_INITIALIZED = "VAR_WH_INITIALIZED";

        public WeatherEffectsService(
            IWeatherCalculationService weatherCalculationService,
            IWeatherClimateService weatherClimateService)
        {
            _weatherCalculationService = weatherCalculationService;
            _weatherClimateService = weatherClimateService;
        }

        /// <summary>
        /// Applies weather effects to a creature.
        /// </summary>
        /// <param name="oCreature">The creature to apply effects to.</param>
        public void DoWeatherEffects(uint oCreature)
        {
            var oArea = GetArea(oCreature);
            if (GetIsAreaInterior(oArea) || GetIsAreaAboveGround(oArea) == false) return;

            var nHeat = _weatherCalculationService.GetHeatIndex(oArea);
            var nHumidity = _weatherCalculationService.GetHumidity(oArea);
            var nWind = _weatherCalculationService.GetWindStrength(oArea);
            var bStormy = GetSkyBox(oArea) == SkyboxType.GrassStorm;
            var bIsPC = GetIsPC(oCreature);
            string sMessage;
            var climate = _weatherClimateService.GetAreaClimate(oArea);

            //--------------------------------------------------------------------------
            // Apply acid rain, if applicable.  Stolen shamelessly from the Melf's Acid
            // Arrow spell.
            //--------------------------------------------------------------------------
            if (bIsPC && GetWeather(oArea) == WeatherType.Rain && GetLocalInt(oArea, VAR_WEATHER_ACID_RAIN) == 1)
            {
                var eEffect =
                  EffectLinkEffects(
                      EffectVisualEffect(VisualEffectType.Vfx_Imp_Acid_S),
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
                        EffectVisualEffect(VisualEffectType.Vfx_Imp_Flame_S),
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
                        EffectVisualEffect(VisualEffectType.Vfx_Dur_Iceskin),
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

        /// <summary>
        /// Applies acid rain damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply acid damage to.</param>
        /// <param name="oArea">The area where the acid rain is occurring.</param>
        public void ApplyAcid(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffectType.Vfx_Imp_Acid_S),
                    EffectDamage(
                        d6(),
                        DamageType.Acid));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplyAcid(oTarget, oArea); });
        }

        /// <summary>
        /// Applies cold damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply cold damage to.</param>
        /// <param name="oArea">The area where the cold is occurring.</param>
        public void ApplyCold(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffectType.Vfx_Imp_Acid_S),
                    EffectDamage(
                        d6(),
                        DamageType.Acid));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplyCold(oTarget, oArea); });
        }

        /// <summary>
        /// Applies sandstorm damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply sandstorm damage to.</param>
        /// <param name="oArea">The area where the sandstorm is occurring.</param>
        public void ApplySandstorm(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffectType.Vfx_Imp_Flame_S),
                    EffectDamage(
                        d6(2),
                        DamageType.Bludgeoning));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplySandstorm(oTarget, oArea); });
        }

        /// <summary>
        /// Applies snowstorm damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply snowstorm damage to.</param>
        /// <param name="oArea">The area where the snowstorm is occurring.</param>
        public void ApplySnowstorm(uint oTarget, uint oArea)
        {
            if (GetArea(oTarget) != oArea) return;
            if (GetIsDead(oTarget)) return;
            if (GetIsPC(oTarget) && GetIsPC(GetMaster(oTarget)) == false) return;

            //apply
            var eEffect =
                EffectLinkEffects(
                    EffectVisualEffect(VisualEffectType.Vfx_Dur_Iceskin),
                    EffectDamage(
                        d6(2),
                        DamageType.Cold));

            ApplyEffectToObject(DurationType.Instant, eEffect, oTarget);

            DelayCommand(6.0f, () => { ApplySnowstorm(oTarget, oArea); });
        }

        /// <summary>
        /// Handles wind knockdown effects on combat round end.
        /// </summary>
        /// <param name="oCreature">The creature to check for wind effects.</param>
        public void OnCombatRoundEnd(uint oCreature)
        {
            var oArea = GetArea(oCreature);
            if (GetLocalInt(oArea, VAR_INITIALIZED) == 0)
                return;

            var nWind = _weatherCalculationService.GetWindStrength(oArea);

            if (nWind > 9) _DoWindKnockdown(oCreature);
        }

        /// <summary>
        /// Creates a thunderstorm effect in an area.
        /// </summary>
        /// <param name="oArea">The area to create thunderstorm in.</param>
        public void Thunderstorm(uint oArea)
        {
            // 1 in 3 chance of a bolt.
            if (d3() != 1) return;

            // Pick a spot. Any spot.
            var nWidth = GetAreaSize(AreaDimensionType.Width, oArea);
            var nHeight = GetAreaSize(AreaDimensionType.Height, oArea);
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

        private void Thunderstorm(Location lLocation, int nPower)
        {
            var fRange = IntToFloat(nPower) * 0.1f;
            // Caps on sphere of influence
            if (fRange < 3.0) fRange = 3.0f;
            if (fRange > 6.0) fRange = 6.0f;

            //Effects
            var eEffBolt = EffectVisualEffect(VisualEffectType.Vfx_Imp_Lightning_M);
            ApplyEffectAtLocation(DurationType.Instant, eEffBolt, lLocation);

            var oObject = GetFirstObjectInShape(ShapeType.Sphere, fRange, lLocation, false, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable);
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

                        PlayVoiceChat(VoiceChatType.Pain1, oObject);
                        var duration = IntToFloat(d6());
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), oObject, duration);
                    }
                }
                oObject = GetNextObjectInShape(ShapeType.Sphere, fRange, lLocation, false, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable);
            }
        }

        private void _DoWindKnockdown(uint oCreature)
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
    }
}
