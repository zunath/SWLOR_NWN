using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class MapPinService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleClientEnter());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
        }

        private static void OnModuleClientEnter()
        {
            NWPlayer oPC = (_.GetEnteringObject());

            if (!oPC.IsPlayer) return;
            if (oPC.GetLocalInt("MAP_PINS_LOADED") == 1) return;

            List<PCMapPin> pins = DataService.PCMapPin.GetAllByPlayerID(oPC.GlobalID).ToList();

            foreach (PCMapPin pin in pins)
            {
                NWArea area = (_.GetObjectByTag(pin.AreaTag));
                SetMapPin(oPC, pin.NoteText, (float)pin.PositionX, (float)pin.PositionY, area);
            }
            
            oPC.SetLocalInt("MAP_PINS_LOADED", 1);
        }

        private static void OnModuleLeave()
        {
            NWPlayer oPC = (_.GetExitingObject());

            if (!oPC.IsPlayer) return;

            var mapPins = DataService.PCMapPin.GetAllByPlayerID(oPC.GlobalID).ToList();
            for(int x = mapPins.Count-1; x >= 0; x--)
            {
                var pin = mapPins.ElementAt(x);
                DataService.SubmitDataChange(pin, DatabaseActionType.Delete);
            }
            
            for (int x = 0; x < GetNumberOfMapPins(oPC); x++)
            {
                MapPin mapPin = GetMapPin(oPC, x);

                if (string.IsNullOrWhiteSpace(mapPin.Text)) continue;

                PCMapPin entity = new PCMapPin
                {
                    AreaTag = mapPin.Area.Tag,
                    NoteText = mapPin.Text,
                    PlayerID = oPC.GlobalID,
                    PositionX = mapPin.PositionX,
                    PositionY = mapPin.PositionY
                };

                DataService.SubmitDataChange(entity, DatabaseActionType.Insert);
            }
        }


        public static int GetNumberOfMapPins(NWPlayer oPC)
        {
            return oPC.GetLocalInt("NW_TOTAL_MAP_PINS");
        }

        public static MapPin GetMapPin(NWPlayer oPC, int index)
        {
            index++;
            MapPin mapPin = new MapPin
            {
                Text = oPC.GetLocalString("NW_MAP_PIN_NTRY_" + index),
                PositionX = oPC.GetLocalFloat("NW_MAP_PIN_XPOS_" + index),
                PositionY = oPC.GetLocalFloat("NW_MAP_PIN_YPOS_" + index),
                Area = (oPC.GetLocalObject("NW_MAP_PIN_AREA_" + index)),
                Tag = oPC.GetLocalString("CUSTOM_NW_MAP_PIN_TAG_" + index),
                Player = oPC,
                Index = index
            };
            
            return mapPin;
        }

        public static MapPin GetMapPin(NWPlayer oPC, string pinTag)
        {
            for (int index = 0; index <= GetNumberOfMapPins(oPC); index++)
            {
                string mapPinTag = oPC.GetLocalString("CUSTOM_NWN_MAP_PIN_TAG_" + index);
                if (mapPinTag == pinTag)
                {
                    return GetMapPin(oPC, index);
                }
            }

            return null; // Couldn't find a map pin by that tag.
        }

        public static void SetMapPin(NWPlayer oPC, string text, float positionX, float positionY, NWArea area, string tag)
        {
            int numberOfMapPins = GetNumberOfMapPins(oPC);
            int storeAtIndex = -1;

            for (int index = 0; index < numberOfMapPins; index++)
            {
                MapPin mapPin = GetMapPin(oPC, index);
                if (string.IsNullOrWhiteSpace(mapPin.Text))
                {
                    storeAtIndex = index;
                    break;
                }
            }

            if (storeAtIndex == -1)
            {
                numberOfMapPins++;
                storeAtIndex = numberOfMapPins - 1;
            }

            storeAtIndex++;

            oPC.SetLocalString("NW_MAP_PIN_NTRY_" + storeAtIndex, text);
            oPC.SetLocalFloat("NW_MAP_PIN_XPOS_" + storeAtIndex, positionX);
            oPC.SetLocalFloat("NW_MAP_PIN_YPOS_" + storeAtIndex, positionY);
            oPC.SetLocalObject("NW_MAP_PIN_AREA_" + storeAtIndex, area.Object);
            oPC.SetLocalInt("NW_TOTAL_MAP_PINS", numberOfMapPins);

            if (tag != null)
            {
                oPC.SetLocalString("CUSTOM_NW_MAP_PIN_TAG_" + storeAtIndex, tag);
            }
        }

        public static void SetMapPin(NWPlayer oPC, string text, float positionX, float positionY, NWArea area)
        {
            SetMapPin(oPC, text, positionX, positionY, area, null);
        }

        public static void DeleteMapPin(NWPlayer oPC, int index)
        {
            int numberOfPins = GetNumberOfMapPins(oPC);

            if (index > numberOfPins - 1) return;
            MapPin mapPin = GetMapPin(oPC, index);

            if (mapPin != null)
            {
                oPC.SetLocalString("NW_MAP_PIN_NTRY_" + index, string.Empty);
            }
        }

        public static void DeleteMapPin(NWPlayer oPC, string pinTag)
        {
            MapPin mapPin = GetMapPin(oPC, pinTag);

            if (mapPin != null)
            {
                DeleteMapPin(oPC, mapPin.Index);
            }
        }

        public static void AddWaypointMapPin(NWPlayer oPC, string waypointTag, string text, string mapPinTag)
        {
            NWObject waypoint = (_.GetWaypointByTag(waypointTag));
            SetMapPin(oPC, text, waypoint.Position.m_X, waypoint.Position.m_Y, waypoint.Area, mapPinTag);
        }

    }
}
