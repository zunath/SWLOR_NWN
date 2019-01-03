using SWLOR.Game.Server.Service.Contracts;
using NWN;
using SWLOR.Game.Server.GameObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.WeatherModifier
{
    public class OnHeartbeat
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

            NWObject oArea = _.GetArea(oSelf);
            _weather.SetAreaHeatModifier(oSelf, nHeat);
            _weather.SetAreaHumidityModifier(oSelf, nWet);
            _weather.SetAreaWindModifier(oSelf, nWind);
            _weather.SetAreaAcidRain(oSelf, nAcid);
            _.DestroyObject(oSelf);
            return true;
        }
    }
}
