using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.WeatherService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Engine;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The weather of the outdoor areas: the logic and state that used to be static in <c>Service.Weather</c>, moved
    /// here unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class WeatherService : IWeatherService
    {
        private sealed class AreaWeather : WeatherAreaState
        {
            public DateTime NextLightningUtc { get; set; }
            public List<uint> MistPlaceables { get; } = new();
            public bool MistInitialized { get; set; }
        }

        private readonly Dictionary<uint, AreaWeather> _areas = new();
        private readonly WeatherRegionCache _regions = new();
        private readonly Dictionary<uint, WeatherExposure> _exposures = new();
        private Dictionary<PlanetType, WeatherClimate> _planetClimates;
        private Dictionary<string, WeatherClimate> _namedClimates;
        private WeatherPattern _pattern = new();

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

        private readonly IDBService _db;
        private readonly IPlanetService _planets;
        private readonly IDMPresenceService _presence;

        public WeatherService(IDBService db, IPlanetService planets, IDMPresenceService presence)
        {
            _db = db;
            _planets = planets;
            _presence = presence;
            _planetClimates = WeatherPlanetDefinitions.GetPlanetClimates();
            _namedClimates = WeatherPlanetDefinitions.GetNamedClimates(_planetClimates);
        }

        public void LoadData()
        {
            _planetClimates = WeatherPlanetDefinitions.GetPlanetClimates();
            _namedClimates = WeatherPlanetDefinitions.GetNamedClimates(_planetClimates);
            _pattern = new WeatherPattern();
            _areas.Clear();
            _regions.Clear();
            _exposures.Clear();
        }

        private WeatherClimate GetAreaClimate(uint area)
        {
            return WeatherPlanetDefinitions.ResolveClimate(_planets.GetPlanetType(area),
                GetLocalString(area, "VAR_WEATHER_CLIMATE"), _planetClimates, _namedClimates);
        }

        private bool IsWeatherArea(uint area)
        {
            return GetIsObjectValid(area) && area != GetModule() &&
                   !GetIsAreaInterior(area) && GetIsAreaAboveGround(area) &&
                   !GetLocalBool(area, "SPACE") && !GetName(area).StartsWith("Space -", StringComparison.OrdinalIgnoreCase) &&
                   GetAreaClimate(area) is { IsSheltered: false };
        }

        private bool IsHazardousWeatherArea(uint area)
        {
            if (!IsWeatherArea(area) || GetLocalBool(area, "WEATHER_HAZARD_SAFE")) return false;

            // Authored settlements opt out independently of their natural/urban
            // setting. Player-founded cities are protected while the city exists.
            var propertyId = Property.GetPropertyId(area);
            return string.IsNullOrWhiteSpace(propertyId) ||
                   _db.Get<WorldProperty>(propertyId)?.PropertyType != PropertyType.City;
        }

        public void InitializeWeather()
        {
            AdjustWeather();
        }

        public bool AdjustWeather()
        {
            if (!_pattern.TryAdvance(DateTime.UtcNow, GetCalendarMonth(), GetIsNight(), NWScript.Random))
                return false;

            // Apply once per area, including unoccupied areas, so native precipitation
            // and old fog do not survive until another creature enters.
            foreach (var area in _areas.Keys.Where(area => !GetIsObjectValid(area)).ToArray())
                _areas.Remove(area);

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
                SetWeather(area);

            return true;
        }

        public void SetWeather() => SetWeather(OBJECT_SELF);

        public void SetWeather(uint area)
        {
            if (!IsWeatherArea(area)) return;
            if (_pattern.Revision == 0)
                _pattern.TryAdvance(DateTime.UtcNow, GetCalendarMonth(), GetIsNight(), NWScript.Random);

            if (!_areas.TryGetValue(area, out var state))
            {
                state = new AreaWeather();
                _areas.Add(area, state);
            }

            // Entry and multiple occupants request the same snapshot. Maps with the
            // same climate and authored modifiers share one storm roll per front.
            if (state.Revision == _pattern.Revision) return;

            if (GetLocalInt(area, VAR_INITIALIZED) == 0)
            {
                SetLocalInt(area, VAR_SKYBOX, (int)GetSkyBox(area));
                SetLocalInt(area, VAR_FOG_SUN, GetFogAmount(FogType.Sun, area));
                SetLocalInt(area, VAR_FOG_MOON, GetFogAmount(FogType.Moon, area));
                SetLocalInt(area, VAR_FOG_C_SUN, (int)GetFogColor(FogType.Sun, area));
                SetLocalInt(area, VAR_FOG_C_MOON, (int)GetFogColor(FogType.Moon, area));
                SetLocalInt(area, VAR_INITIALIZED, 1);
            }

            var previous = state.Conditions;
            var climate = GetAreaClimate(area);
            var heatModifier = GetLocalInt(area, VAR_WEATHER_HEAT);
            var humidityModifier = GetLocalInt(area, VAR_WEATHER_HUMIDITY);
            var windModifier = GetLocalInt(area, VAR_WEATHER_WIND);
            var region = _regions.GetConditions(_pattern, climate, heatModifier, humidityModifier, windModifier, NWScript.Random);
            if (!state.TryUpdate(_pattern, climate,
                heatModifier, humidityModifier, windModifier, GetIsAreaNatural(area) != 0,
                region.Storm)) return;
            var conditions = state.Conditions;
            Log.WriteStructured(LogGroup.Server,
                "Weather updated for {AreaResref}: revision {Revision}, heat {Heat}, humidity {Humidity}, wind {Wind}, precipitation {Precipitation}, storm {Storm}",
                GetResRef(area), state.Revision, conditions.Heat, conditions.Humidity, conditions.Wind,
                conditions.Precipitation, conditions.Storm);
            if (state.TryApplyWeather(conditions.NativeWeather, NWScript.GetWeather(area)))
                NWScript.SetWeather(area, conditions.NativeWeather);

            DeleteLocalInt(area, "GS_AM_SKY_OVERRIDE");
            DeleteLocalInt(area, "DUST_STORM");
            DeleteLocalInt(area, "SAND_STORM");
            DeleteLocalInt(area, "SNOW_STORM");
            SetSkyBox((Skybox)GetLocalInt(area, VAR_SKYBOX), area);
            SetFogColor(FogType.Sun, (FogColor)GetLocalInt(area, VAR_FOG_C_SUN), area);
            SetFogColor(FogType.Moon, (FogColor)GetLocalInt(area, VAR_FOG_C_MOON), area);
            SetFogAmount(FogType.Sun, GetLocalInt(area, VAR_FOG_SUN), area);
            SetFogAmount(FogType.Moon, GetLocalInt(area, VAR_FOG_MOON), area);

            if (conditions.Storm == WeatherStorm.Thunder)
            {
                SetSkyBox(Skybox.GrassStorm, area);
                SetLocalInt(area, "GS_AM_SKY_OVERRIDE", 1);
            }
            else if (conditions.Storm == WeatherStorm.Sand || conditions.Storm == WeatherStorm.Snow)
            {
                var isSand = conditions.Storm == WeatherStorm.Sand;
                var color = isSand ? FogColor.OrangeDark : FogColor.White;
                SetLocalInt(area, isSand ? "SAND_STORM" : "SNOW_STORM", 1);
                SetFogColor(FogType.Sun, color, area);
                SetFogColor(FogType.Moon, color, area);
                SetFogAmount(FogType.Sun, 80, area);
                SetFogAmount(FogType.Moon, 80, area);
            }

            if (previous?.Precipitation != conditions.Precipitation)
                ClearMist(state);
        }

        private void ClearMist(AreaWeather state)
        {
            foreach (var placeable in state.MistPlaceables)
            {
                if (GetIsObjectValid(placeable)) DestroyObject(placeable);
            }
            state.MistPlaceables.Clear();
            state.MistInitialized = false;
        }

        private void EnsureMist(uint area, AreaWeather state)
        {
            if (state.MistInitialized || state.Conditions.Precipitation != Precipitation.Foggy) return;
            // Populate occupied areas only. Unoccupied instance templates must not
            // copy generated mist into new instances as untracked placeables.
            state.MistInitialized = true;

            var count = GetAreaSize(Dimension.Width, area) * GetAreaSize(Dimension.Height, area) / 8;
            for (var index = 0; index < count; index++)
            {
                // Use the ground at the chosen tile, not the entering creature's height.
                if (!TryGetGroundLocation(area, out var location)) continue;
                var placeable = CreateObject(ObjectType.Placeable, "x3_plc_mist", location);
                if (!GetIsObjectValid(placeable)) continue;
                SetObjectVisualTransform(placeable, ObjectVisualTransform.Scale, (200 + Random(200)) / 100f);
                state.MistPlaceables.Add(placeable);
            }
        }

        private bool TryGetGroundLocation(uint area, out Location location)
        {
            var width = GetAreaSize(Dimension.Width, area) * 10;
            var height = GetAreaSize(Dimension.Height, area) * 10;
            location = default;
            if (width <= 0 || height <= 0) return false;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var position = Vector3(Random(width) + Random(10) * 0.1f, Random(height) + Random(10) * 0.1f);
                location = Location(area, position, Random(360));
                if (GetSurfaceMaterial(location) == 0) continue;
                position.Z = GetGroundHeight(location);
                if (position.Z == -6f) continue;
                location = Location(area, position, GetFacingFromLocation(location));
                return true;
            }
            return false;
        }

        public Precipitation GetWeather() => GetWeather(OBJECT_SELF);

        public Precipitation GetWeather(uint area)
        {
            var conditions = GetConditions(area);
            if (conditions == null) return Precipitation.Invalid;
            var precipitation = NWScript.GetWeather(area);
            return precipitation == Precipitation.Clear && conditions.Precipitation == Precipitation.Foggy
                ? Precipitation.Foggy : precipitation;
        }
        public int GetHeatIndex(uint area) => GetConditions(area)?.Heat ?? Math.Clamp(_pattern.Heat, 1, 10);
        public int GetHumidity(uint area) => GetConditions(area)?.Humidity ?? Math.Clamp(_pattern.Humidity, 1, 10);
        public int GetWindStrength(uint area) => GetConditions(area)?.Wind ?? Math.Clamp(_pattern.Wind, 1, 10);

        private WeatherConditions GetConditions(uint area)
        {
            SetWeather(area);
            return IsWeatherArea(area) && _areas.TryGetValue(area, out var state) ? state.Conditions : null;
        }

        public void DoWeatherEffects(uint creature)
        {
            if (!GetIsObjectValid(creature) || !GetIsPC(creature)) return;
            var area = GetArea(creature);
            var conditions = GetConditions(area);
            if (conditions == null) return;

            var message = conditions.GetFeedback(GetAreaClimate(area), GetIsNight(),
                GetLocalInt(area, VAR_WEATHER_ACID_RAIN) == 1, NWScript.GetWeather(area),
                IsHazardousWeatherArea(area));
            SendMessageToPC(creature, message);
            ApplyWeatherDamage(creature, DateTime.UtcNow);
        }

        private void ApplyWeatherDamage(uint creature, DateTime now)
        {
            if (!_exposures.TryGetValue(creature, out var exposure))
            {
                exposure = new WeatherExposure();
                _exposures.Add(creature, exposure);
            }

            var area = GetArea(creature);
            var conditions = GetConditions(area);
            var hazard = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature) &&
                         !GetIsDead(creature) && conditions != null && IsHazardousWeatherArea(area)
                ? conditions.GetHazard(GetLocalInt(area, VAR_WEATHER_ACID_RAIN) == 1, NWScript.GetWeather(area))
                : WeatherHazard.None;
            var dice = exposure.GetDamageDice(now, hazard);
            if (dice == 0) return;

            var damageType = hazard switch
            {
                WeatherHazard.Acid => CombatDamageType.Poison,
                WeatherHazard.Snow => CombatDamageType.Ice,
                _ => CombatDamageType.Physical
            };
            AssignCommand(area, () =>
            {
                var damage = GetProtectedDamage(creature, d6(dice), damageType);
                if (damage <= 0) return;
                var nativeType = hazard == WeatherHazard.Sand ? DamageType.Bludgeoning : damageType.GetNWScriptDamageType();
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, nativeType), creature);
                if (hazard == WeatherHazard.Acid || hazard == WeatherHazard.Snow)
                {
                    var visual = hazard == WeatherHazard.Acid ? VisualEffect.Vfx_Imp_Acid_S : VisualEffect.Vfx_Imp_Frost_S;
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visual), creature);
                }
            });
        }

        // The damage pipeline (stat adjustments, resistance, damage-taken modifiers) reads the stored player or the
        // native effect list of a creature, which no unit test can reach; a test subclass answers this instead.
        protected virtual int GetProtectedDamage(uint target, int damage, CombatDamageType damageType)
        {
            // Doors/placeables hit by lightning do not have character stats.
            if (damage <= 0 || GetObjectType(target) != ObjectType.Creature) return Math.Max(0, damage);
            var typedAdjustment = damageType.IsPhysicalDamageType()
                ? Stat.GetStatAdjustment(target, StatType.PhysicalDamageTakenPercentAdjustment) : 0;
            var adjusted = Combat.ApplyTriggeredDamageTargetAdjustment(damage, typedAdjustment, null);
            damage = Resistance.ApplyResistanceToDamage(target, damageType, adjusted.Damage);
            if (damage <= 0) return 0;
            return Combat.ApplyDamageTakenModifiers(target, damage, damageType: damageType,
                targetStatusDamagePercentAdjustment: adjusted.Adjustment);
        }

        public void Thunderstorm(uint area)
        {
            if (!IsWeatherArea(area) || !_areas.TryGetValue(area, out var state) ||
                state.Conditions.Storm != WeatherStorm.Thunder || d3() != 1 ||
                !TryGetGroundLocation(area, out var location)) return;

            // The heartbeat owns strike timing. No delayed bolt can outlive a storm.
            AssignCommand(area, () => Thunderstorm(location, d100() + 10));
        }

        private void Thunderstorm(Location location, int power)
        {
            var range = Math.Clamp(power * 0.1f, 3f, 6f);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_M), location);
            if (!IsHazardousWeatherArea(GetAreaFromLocation(location))) return;

            const ObjectType targets = ObjectType.Creature | ObjectType.Door | ObjectType.Placeable;
            foreach (var target in ObjectSnapshot.InShape(Shape.Sphere, range, location, false, targets))
            {
                if (!GetIsObjectValid(target)) continue;
                var damage = WeatherConditions.GetLightningDamage(power, GetDistanceBetweenLocations(location, GetLocation(target)));
                if (damage <= 0 || GetIsDM(target) || GetIsDMPossessed(target)) continue;
                damage = GetProtectedDamage(target, damage, CombatDamageType.Electrical);
                if (damage <= 0) continue;
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
                if (GetObjectType(target) != ObjectType.Creature || GetIsDead(target)) continue;

                if (GetIsPC(target)) SendMessageToPC(target, WeatherFeedbackText.Lightning);
                PlayVoiceChat(VoiceChat.Pain1, target);
                var duration = GetKnockdownTicks(target, d6());
                if (GetIsObjectValid(target) && duration > 0 && GetKnockdownImmunity(target) <= 0)
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, duration);
            }
        }

        // The mobility resistance of a creature comes from the static Resistance service, which reads the stored
        // player or the native effect list; a test subclass answers this instead.
        protected virtual int GetKnockdownTicks(uint target, int baseTicks)
        {
            return Resistance.CalculateResistedTicks(target, ResistanceType.Mobility, baseTicks);
        }

        // The knockdown immunity of a creature comes from the static Stat service, which reads the stored player or
        // the native effect list; a test subclass answers this instead.
        protected virtual int GetKnockdownImmunity(uint target)
        {
            return Stat.GetStatAdjustment(target, StatType.KnockdownImmunity);
        }

        public void OnAreaEnter()
        {
            var area = OBJECT_SELF;
            var creature = GetEnteringObject();
            SetWeather(area);
            if (GetIsPC(creature) && _areas.TryGetValue(area, out var state)) EnsureMist(area, state);
            DoWeatherEffects(creature);
        }

        public void OnModuleHeartbeat()
        {
            var changed = AdjustWeather();
            var now = DateTime.UtcNow;
            var players = new HashSet<uint>();
            var occupiedAreas = new HashSet<uint>();
            foreach (var player in ObjectSnapshot.Players())
            {
                var viewer = _presence.GetControlledCreature(player);
                players.Add(viewer);
                occupiedAreas.Add(GetArea(viewer));
                if (changed) DoWeatherEffects(viewer);
                else ApplyWeatherDamage(viewer, now);
            }

            foreach (var player in _exposures.Keys.Where(player => !players.Contains(player)).ToArray())
                _exposures.Remove(player);

            foreach (var area in occupiedAreas)
            {
                if (!_areas.TryGetValue(area, out var state) || !IsWeatherArea(area)) continue;
                EnsureMist(area, state);
                if (state.NextLightningUtc > now) continue;
                state.NextLightningUtc = now.AddMinutes(1);
                Thunderstorm(area);
            }
        }

        private void SetAreaModifier(uint area, string variable, int modifier)
        {
            SetLocalInt(area, variable, modifier);
            if (_areas.TryGetValue(area, out var state)) state.Invalidate();
            SetWeather(area);
        }

        public void SetAreaHeatModifier(uint area, int modifier) => SetAreaModifier(area, VAR_WEATHER_HEAT, modifier);
        public void SetAreaWindModifier(uint area, int modifier) => SetAreaModifier(area, VAR_WEATHER_WIND, modifier);
        public void SetAreaHumidityModifier(uint area, int modifier) => SetAreaModifier(area, VAR_WEATHER_HUMIDITY, modifier);
        public void SetAreaAcidRain(uint area, int modifier) => SetLocalInt(area, VAR_WEATHER_ACID_RAIN, modifier);
    }
}
