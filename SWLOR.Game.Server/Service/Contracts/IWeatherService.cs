using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IWeatherService
    {
        // Call every hour in module heartbeat.  Updates the global weather settings
        // and sets the appropriate weather in all areas with PCs in.
        void AdjustWeather();
        // If the builder sets climate modifiers in the area, set them here.
        void SetAreaHeatModifier(NWObject oArea, int nModifier);
        // If the builder sets climate modifiers in the area, set them here.
        void SetAreaWindModifier(NWObject oArea, int nModifier);
        // If the builder sets climate modifiers in the area, set them here.
        void SetAreaHumidityModifier(NWObject oArea, int nModifier);
        // If the builder sets climate modifiers in the area, set them here.
        void SetAreaAcidRain(NWObject oArea, int nModifier);
        // Call in all area enter scripts.  Sets the current weather in the area to
        // reflect the global weather settings.
        void SetWeather();
        void SetWeather(NWObject oArea);
        // Retrieve the current WEATHER_* weather type.
        int GetWeather();
        int GetWeather(NWObject oArea);
        // Applies acid rain damage.
        void ApplyAcid(NWObject oTarget, NWObject oArea);
        // Call on spawned creatures to apply the weather effects for their area (e.g.
        // speed reduction if it's windy).
        void DoWeatherEffects(NWObject oCreature);
        // Retrieves the current heat index, adjusted for oArea's settings.  If oArea is
        // invalid, uses the global value.
        int GetHeatIndex();
        int GetHeatIndex(NWObject oArea);
        // Retrieves the current humidity, adjusted for oArea's settings.  If oArea is
        // invalid, uses the global value.
        int GetHumidity();
        int GetHumidity(NWObject oArea);
        // Retrieves the current wind strength, adjusted for oArea's settings.  If oArea
        // is invalid, uses the global value.
        int GetWindStrength();
        int GetWindStrength(NWObject oArea);
        // Utility wrapper to contain all area entry calls.
        void OnAreaEnter();
        // Call on creature spawn to apply any weather effects.
        void OnCreatureSpawn();
        // Call each round on PCs in combat.  If the area is very windy, characters may
        // be knocked over.
        void OnCombatRoundEnd(NWObject oPC);
        // Utility wrapper for module heartbeat processing.
        void OnModuleHeartbeat();
        // Randomly calls down a bolt to slap some creatures somewhere.
        void Thunderstorm(NWObject oArea);
    }
}
