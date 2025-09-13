using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class BankLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Bank();

            return _builder.Build();
        }

        private static readonly Dictionary<uint, List<uint>> _bankWaypointsByArea = new();

        /// <summary>
        /// When a bank contained inside a property is used,
        /// ensure the user is a citizen of the city the bank is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenPropertyBank)]
        public static void OpenPropertyBank()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, "Only players can access this bank.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var bank = OBJECT_SELF;
            var cityId = GetLocalString(bank, "STORAGE_ID");

            if (dbPlayer.CitizenPropertyId != cityId)
            {
                SendMessageToPC(player, "Only citizens may use this terminal.");
                return;
            }

            if (dbPlayer.PropertyOwedTaxes > 0)
            {
                SendMessageToPC(player, $"You owe {dbPlayer.PropertyOwedTaxes} credits in taxes to this city. You cannot use its facilities until these are paid. Use the Citizenship Terminal in City Hall to pay these.");
                return;
            }

            // Execute the normal bank procedure if all these additional checks are met.
            ExecuteScript("open_bank", bank);
        }

        private void ProcessBank(uint area, uint waypoint, int storageCap, string bankId)
        {
            if (!_bankWaypointsByArea.ContainsKey(area))
                _bankWaypointsByArea[area] = new List<uint>();

            if (!_bankWaypointsByArea[area].Contains(waypoint))
            {
                _bankWaypointsByArea[area].Add(waypoint);
            }

            var placeable = GetLocalObject(waypoint, "BANK_TERMINAL_PLACEABLE");

            if (!GetIsObjectValid(placeable))
            {
                var location = GetLocation(waypoint);
                placeable = CreateObject(ObjectType.Placeable, "bank_term", location);
                SetPlotFlag(placeable, true);
                SetLocalObject(waypoint, "BANK_TERMINAL_PLACEABLE", placeable);

                SetLocalInt(placeable, "STORAGE_ITEM_LIMIT", storageCap);
                SetLocalString(placeable, "STORAGE_ID", bankId);

                SetEventScript(placeable, EventScript.Placeable_OnUsed, "open_prop_bank");
            }
        }

        private int CalculateStorageCap(int level)
        {
            return 20 + level * 20;
        }

        private void Bank()
        {
            _builder.Create(PropertyLayoutType.BankStyle1)
                .PropertyType(PropertyType.Bank)
                .Name("Bank")
                .StructureLimit(50)
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("bank")
                .OnSpawn(area =>
                {
                    var propertyId = Property.GetPropertyId(area);
                    var dbProperty = DB.Get<WorldProperty>(propertyId);
                    var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
                    var upgradeLevel = Property.GetEffectiveUpgradeLevel(dbBuilding.ParentPropertyId, PropertyUpgradeType.BankLevel);
                    var storageCap = CalculateStorageCap(upgradeLevel);
                    var bankId = dbBuilding.ParentPropertyId;

                    var count = 1;

                    var referenceObject = GetFirstObjectInArea(area);

                    // Sometimes the reference object will pick up the waypoint we're trying to spawn to.
                    // If this happens, the iteration below will ignore it and we end up with three bank terminals instead
                    // of the normal four. Handle this scenario by checking what the reference object is before proceeding.
                    if (GetTag(referenceObject) == "BANK_TERMINAL_SPAWN")
                    {
                        ProcessBank(area, referenceObject, storageCap, bankId);
                    }

                    var waypoint = GetNearestObjectByTag("BANK_TERMINAL_SPAWN", referenceObject, count);
                    while (GetIsObjectValid(waypoint))
                    {
                        ProcessBank(area, waypoint, storageCap, bankId);

                        count++;
                        waypoint = GetNearestObjectByTag("BANK_TERMINAL_SPAWN", referenceObject, count);
                    }
                })
                .OnCityUpgraded((area, upgradeType, level) =>
                {
                    if (upgradeType != PropertyUpgradeType.BankLevel)
                        return;

                    if (!_bankWaypointsByArea.ContainsKey(area))
                        return;

                    var storageCap = CalculateStorageCap(level);
                    foreach (var waypoint in _bankWaypointsByArea[area])
                    {
                        var placeable = GetLocalObject(waypoint, "BANK_TERMINAL_PLACEABLE");

                        SetLocalInt(placeable, "STORAGE_ITEM_LIMIT", storageCap);
                    }
                });
        }
    }
}
