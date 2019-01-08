using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Event;
using NWN;
using SWLOR.Game.Server.GameObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.WeatherModifier
{
    public class OnHeartbeat : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IWeatherService _weather;

        public OnHeartbeat(INWScript script, IWeatherService weather)
        {
            _ = script;
            _weather = weather;
        }

        public bool Run(params object[] args)
        {
            NWObject oSelf = Object.OBJECT_SELF;
            int nHeat = oSelf.GetLocalInt("WEATHER_HEAT");
            int nWind = oSelf.GetLocalInt("WEATHER_WIND");
            int nWet = oSelf.GetLocalInt("WEATHER_HUMIDITY");
            int nAcid = oSelf.GetLocalInt("WEATHER_ACID_RAIN");
            int nDust = oSelf.GetLocalInt("WEATHER_DUST_STORM");
            int nSand = oSelf.GetLocalInt("WEATHER_SAND_STORM");

            NWArea oArea = _.GetArea(oSelf);
            _weather.SetAreaHeatModifier(oArea, nHeat);
            _weather.SetAreaHumidityModifier(oArea, nWet);
            _weather.SetAreaWindModifier(oArea, nWind);
            _weather.SetAreaAcidRain(oArea, nAcid);

            if (nDust > 0)
            {
                _.SetFogColor(NWScript.FOG_TYPE_SUN, NWScript.FOG_COLOR_BROWN, oArea);
                _.SetFogColor(NWScript.FOG_TYPE_MOON, NWScript.FOG_COLOR_BROWN, oArea);
                _.SetFogAmount(NWScript.FOG_TYPE_SUN, 80, oArea);
                _.SetFogAmount(NWScript.FOG_TYPE_MOON, 80, oArea);

                oArea.SetLocalInt("DUST_STORM", 1);

                foreach (NWObject player in oArea.Objects)
                {
                    if (player.IsPC) _weather.DoWeatherEffects(player);
                }
            }

            if (nSand > 0)
            {
                _.SetFogColor(NWScript.FOG_TYPE_SUN, NWScript.FOG_COLOR_ORANGE_DARK, oArea);
                _.SetFogColor(NWScript.FOG_TYPE_MOON, NWScript.FOG_COLOR_ORANGE_DARK, oArea);
                _.SetFogAmount(NWScript.FOG_TYPE_SUN, 80, oArea);
                _.SetFogAmount(NWScript.FOG_TYPE_MOON, 80, oArea);

                oArea.SetLocalInt("SAND_STORM", 1);

                foreach (NWObject player in oArea.Objects)
                {
                    if (player.IsPC) _weather.DoWeatherEffects(player);
                }
            }
            _.DestroyObject(oSelf);
            return true;
        }
    }
}
