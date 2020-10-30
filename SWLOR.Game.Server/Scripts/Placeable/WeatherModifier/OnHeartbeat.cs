using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Scripts.Placeable.WeatherModifier
{
    public class OnHeartbeat : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject oSelf = NWScript.OBJECT_SELF;
            var nHeat = oSelf.GetLocalInt("WEATHER_HEAT");
            var nWind = oSelf.GetLocalInt("WEATHER_WIND");
            var nWet = oSelf.GetLocalInt("WEATHER_HUMIDITY");
            var nAcid = oSelf.GetLocalInt("WEATHER_ACID_RAIN");
            var nDust = oSelf.GetLocalInt("WEATHER_DUST_STORM");
            var nSand = oSelf.GetLocalInt("WEATHER_SAND_STORM");

            NWArea oArea = NWScript.GetArea(oSelf);
            WeatherService.SetAreaHeatModifier(oArea, nHeat);
            WeatherService.SetAreaHumidityModifier(oArea, nWet);
            WeatherService.SetAreaWindModifier(oArea, nWind);
            WeatherService.SetAreaAcidRain(oArea, nAcid);

            if (nDust > 0)
            {
                NWScript.SetFogColor(FogType.Sun, FogColor.Brown, oArea);
                NWScript.SetFogColor(FogType.Moon, FogColor.Brown, oArea);
                NWScript.SetFogAmount(FogType.Sun, 80, oArea);
                NWScript.SetFogAmount(FogType.Moon, 80, oArea);

                oArea.SetLocalInt("DUST_STORM", 1);

                foreach (var player in oArea.Objects)
                {
                    if (player.IsPC) WeatherService.DoWeatherEffects(player);
                }
            }

            if (nSand > 0)
            {
                NWScript.SetFogColor(FogType.Sun, FogColor.OrangeDark, oArea);
                NWScript.SetFogColor(FogType.Moon, FogColor.OrangeDark, oArea);
                NWScript.SetFogAmount(FogType.Sun, 80, oArea);
                NWScript.SetFogAmount(FogType.Moon, 80, oArea);

                oArea.SetLocalInt("SAND_STORM", 1);

                foreach (var player in oArea.Objects)
                {
                    if (player.IsPC) WeatherService.DoWeatherEffects(player);
                }
            }
            NWScript.DestroyObject(oSelf);
        }
    }
}
