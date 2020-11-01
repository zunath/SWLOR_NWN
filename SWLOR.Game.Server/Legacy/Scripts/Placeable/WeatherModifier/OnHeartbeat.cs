using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Weather = SWLOR.Game.Server.Service.Weather;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.WeatherModifier
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
            NWObject oSelf = OBJECT_SELF;
            var nHeat = oSelf.GetLocalInt("WEATHER_HEAT");
            var nWind = oSelf.GetLocalInt("WEATHER_WIND");
            var nWet = oSelf.GetLocalInt("WEATHER_HUMIDITY");
            var nAcid = oSelf.GetLocalInt("WEATHER_ACID_RAIN");
            var nDust = oSelf.GetLocalInt("WEATHER_DUST_STORM");
            var nSand = oSelf.GetLocalInt("WEATHER_SAND_STORM");

            var oArea = GetArea(oSelf);
            Weather.SetAreaHeatModifier(oArea, nHeat);
            Weather.SetAreaHumidityModifier(oArea, nWet);
            Weather.SetAreaWindModifier(oArea, nWind);
            Weather.SetAreaAcidRain(oArea, nAcid);

            if (nDust > 0)
            {
                SetFogColor(FogType.Sun, FogColor.Brown, oArea);
                SetFogColor(FogType.Moon, FogColor.Brown, oArea);
                SetFogAmount(FogType.Sun, 80, oArea);
                SetFogAmount(FogType.Moon, 80, oArea);

                SetLocalInt(oArea, "DUST_STORM", 1);

                for(var player = GetFirstObjectInArea(oArea); GetIsObjectValid(player); player = GetNextObjectInArea(oArea))
                {
                    if (GetIsPC(player)) Weather.DoWeatherEffects(player);
                }
            }

            if (nSand > 0)
            {
                SetFogColor(FogType.Sun, FogColor.OrangeDark, oArea);
                SetFogColor(FogType.Moon, FogColor.OrangeDark, oArea);
                SetFogAmount(FogType.Sun, 80, oArea);
                SetFogAmount(FogType.Moon, 80, oArea);

                SetLocalInt(oArea, "SAND_STORM", 1);

                for (var player = GetFirstObjectInArea(oArea); GetIsObjectValid(player); player = GetNextObjectInArea(oArea))
                {
                    if (GetIsPC(player)) Weather.DoWeatherEffects(player);
                }
            }
            DestroyObject(oSelf);
        }
    }
}
