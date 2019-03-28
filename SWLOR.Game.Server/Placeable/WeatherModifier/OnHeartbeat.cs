
using SWLOR.Game.Server.Event;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.WeatherModifier
{
    public class OnHeartbeat : IRegisteredEvent
    {
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
            WeatherService.SetAreaHeatModifier(oArea, nHeat);
            WeatherService.SetAreaHumidityModifier(oArea, nWet);
            WeatherService.SetAreaWindModifier(oArea, nWind);
            WeatherService.SetAreaAcidRain(oArea, nAcid);

            if (nDust > 0)
            {
                _.SetFogColor(_.FOG_TYPE_SUN, _.FOG_COLOR_BROWN, oArea);
                _.SetFogColor(_.FOG_TYPE_MOON, _.FOG_COLOR_BROWN, oArea);
                _.SetFogAmount(_.FOG_TYPE_SUN, 80, oArea);
                _.SetFogAmount(_.FOG_TYPE_MOON, 80, oArea);

                oArea.SetLocalInt("DUST_STORM", 1);

                foreach (NWObject player in oArea.Objects)
                {
                    if (player.IsPC) WeatherService.DoWeatherEffects(player);
                }
            }

            if (nSand > 0)
            {
                _.SetFogColor(_.FOG_TYPE_SUN, _.FOG_COLOR_ORANGE_DARK, oArea);
                _.SetFogColor(_.FOG_TYPE_MOON, _.FOG_COLOR_ORANGE_DARK, oArea);
                _.SetFogAmount(_.FOG_TYPE_SUN, 80, oArea);
                _.SetFogAmount(_.FOG_TYPE_MOON, 80, oArea);

                oArea.SetLocalInt("SAND_STORM", 1);

                foreach (NWObject player in oArea.Objects)
                {
                    if (player.IsPC) WeatherService.DoWeatherEffects(player);
                }
            }
            _.DestroyObject(oSelf);
            return true;
        }
    }
}
