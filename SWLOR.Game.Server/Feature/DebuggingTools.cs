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

        [NWNEventHandler("test5")]
        public static void UpdateCharacterCreationDate()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.DateCreated = DateTime.UtcNow.AddDays(-60);
            dbPlayer.TotalSPAcquired = 100;
            DB.Set(dbPlayer);
            SendMessageToPC(player, "Creation date updated");
        }

        [NWNEventHandler("test6")]
        public static void UpdateUpkeepDate()
        {
            var area = GetArea(OBJECT_SELF);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            dbCity.Dates[PropertyDateType.Upkeep] = DateTime.UtcNow;

            DB.Set(dbCity);
            SendMessageToPC(GetLastUsedBy(), "Updated upkeep date on city");
        }

        [NWNEventHandler("test7")]
        public static void UpdateDestructionDate()
        {
            var area = GetArea(OBJECT_SELF);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            dbCity.Dates[PropertyDateType.DisrepairDestruction] = DateTime.UtcNow;

            DB.Set(dbCity);
            SendMessageToPC(GetLastUsedBy(), "Updated disrepair destruction date on city");
        }

        [NWNEventHandler("test")]
        public static void OpenMarketListing()
        {
            KeyItem.GiveKeyItem(GetLastUsedBy(), KeyItemType.CZ220ShuttlePass);
        }

    }
}
