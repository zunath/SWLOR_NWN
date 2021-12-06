using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void RegistrationMode()
        {
            var area = GetArea(OBJECT_SELF);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            dbCity.Dates[PropertyDateType.ElectionStart] = DateTime.UtcNow.AddDays(-1);

            DB.Set(dbCity);

            SendMessageToPC(GetLastUsedBy(), $"Election: {dbCity.Dates[PropertyDateType.ElectionStart]}");
        }

        [NWNEventHandler("test3")]
        public static void VotingMode()
        {
            var area = GetArea(OBJECT_SELF);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            dbCity.Dates[PropertyDateType.ElectionStart] = DateTime.UtcNow.AddDays(-15);

            DB.Set(dbCity);

            SendMessageToPC(GetLastUsedBy(), $"Election: {dbCity.Dates[PropertyDateType.ElectionStart]}");
        }

        [NWNEventHandler("test4")]
        public static void PastVotingMode()
        {
            var area = GetArea(OBJECT_SELF);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            dbCity.Dates[PropertyDateType.ElectionStart] = DateTime.UtcNow.AddDays(-30);

            DB.Set(dbCity);

            SendMessageToPC(GetLastUsedBy(), $"Election: {dbCity.Dates[PropertyDateType.ElectionStart]}");
        }

        [NWNEventHandler("test")]
        public static void OpenMarketListing()
        {
            KeyItem.GiveKeyItem(GetLastUsedBy(), KeyItemType.CZ220ShuttlePass);
        }

    }
}
