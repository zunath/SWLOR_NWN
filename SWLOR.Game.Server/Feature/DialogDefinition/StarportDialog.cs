using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Object = SWLOR.Game.Server.Core.NWNX.Object;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportDialog: DialogBase
    {
        private class Model
        {
            public Guid SelectedShipId { get; set; }
            public bool IsConfirmingUnregister { get; set; }
            public bool IsManagingActiveShip { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ManageShipsPageId = "MANAGE_SHIPS_PAGE";
        private const string ShipDetailPageId = "SHIP_DETAIL_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ManageShipsPageId, ManageShipsPageInit)
                .AddPage(ShipDetailPageId, ShipDetailPageInit)
                .AddBackAction((oldPage, newPage) =>
                {
                    var model = GetDataModel<Model>();
                    model.IsConfirmingUnregister = false;
                    model.IsManagingActiveShip = false;
                });

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var activeShipInfo = string.Empty;
            var displayUndock = false;
            var waypointTag = GetLocalString(OBJECT_SELF, "STARPORT_TELEPORT_WAYPOINT");
            var waypoint = GetWaypointByTag(waypointTag);
            var model = GetDataModel<Model>();

            if (dbPlayer.SelectedShipId != Guid.Empty)
            {
                var selectedShip = dbPlayer.Ships[dbPlayer.SelectedShipId];
                var shipDetail = Space.GetShipDetailByType(selectedShip.Type);

                activeShipInfo = ColorToken.Green("Active Ship: ") + selectedShip.Name + $" [{shipDetail.Name}]\n";
                displayUndock = true;
            }

            page.Header = ColorToken.Green("Starport Menu") + "\n" +
                          ColorToken.Green("Ships Registered: ") + $"{dbPlayer.Ships.Count} / {Space.MaxRegisteredShips}" + "\n" +
                          activeShipInfo + "\n" +
                          "What would you like to do?";

            // Register New Ship (available only if player hasn't reached cap)
            if (dbPlayer.Ships.Count < Space.MaxRegisteredShips)
            {
                page.AddResponse("Register New Ship", () =>
                {
                    EndConversation();

                    var container = CreateObject(ObjectType.Placeable, RegistrationContainerResref, GetLocation(player));
                    AssignCommand(player, () => ActionInteractObject(container));
                });
            }

            // Manage Active Ship (available only if a ship has been selected)
            if (dbPlayer.SelectedShipId != Guid.Empty)
            {
                page.AddResponse("Manage Active Ship", () =>
                {
                    model.SelectedShipId = dbPlayer.SelectedShipId;
                    model.IsManagingActiveShip = true;
                    ChangePage(ShipDetailPageId);
                });
            }

            // Manage Ships
            page.AddResponse("Manage Ships", () =>
            {
                ChangePage(ManageShipsPageId);
            });

            // Undock (available if a waypoint is specified)
            if (displayUndock && GetIsObjectValid(waypoint))
            {
                page.AddResponse("Undock", () =>
                {
                    EndConversation();

                    Space.EnterSpaceMode(player, dbPlayer.SelectedShipId);
                    AssignCommand(player, () => ActionJumpToLocation(GetLocation(waypoint)));
                });
            }
        }

        private void ManageShipsPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var activeShipInfo = string.Empty;

            if (dbPlayer.SelectedShipId != Guid.Empty)
            {
                var selectedShip = dbPlayer.Ships[dbPlayer.SelectedShipId];
                var shipDetail = Space.GetShipDetailByType(selectedShip.Type);

                activeShipInfo = ColorToken.Green("Active Ship: ") + selectedShip.Name + $" [{shipDetail.Name}]\n";
            }

            page.Header = ColorToken.Green("Ship Management") + "\n" +
                          ColorToken.Green("Ships Registered: ") + $"{dbPlayer.Ships.Count} / {Space.MaxRegisteredShips}" + "\n" +
                          activeShipInfo + "\n" +
                          "The following is a list of your registered ships.";

            foreach (var (shipId, playerShip) in dbPlayer.Ships)
            {
                page.AddResponse(playerShip.Name, () =>
                {
                    var model = GetDataModel<Model>();
                    model.SelectedShipId = shipId;

                    ChangePage(ShipDetailPageId);
                });
            }
        }

        private void ShipDetailPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var model = GetDataModel<Model>();
            var playerShip = dbPlayer.Ships[model.SelectedShipId];
            var shipDetail = Space.GetShipDetailByType(playerShip.Type);
            var isActiveShip = dbPlayer.SelectedShipId == model.SelectedShipId;
            var highPowerModulesText = string.Empty;
            var lowPowerModulesText = string.Empty;
            var activeShipText = string.Empty;

            if (playerShip.HighPowerModules.Count <= 0)
            {
                highPowerModulesText = ColorToken.Red("None Installed");
            }
            else
            {
                foreach (var (_, module) in playerShip.HighPowerModules)
                {
                    var moduleDetail = Space.GetShipModuleDetailByType(module.Type);
                    highPowerModulesText += moduleDetail.Name + "\n";
                }
            }

            if (playerShip.LowPowerModules.Count <= 0)
            {
                lowPowerModulesText = ColorToken.Red("None Installed");
            }
            else
            {
                foreach (var (_, module) in playerShip.LowPowerModules)
                {
                    var moduleDetail = Space.GetShipModuleDetailByType(module.Type);
                    lowPowerModulesText += moduleDetail.Name + "\n";
                }
            }

            if (isActiveShip)
            {
                activeShipText = "This is your currently selected ship.";
            }

            page.Header = ColorToken.Green("Ship: ") + playerShip.Name + "\n" +
                          ColorToken.Green("Type: ") + shipDetail.Name + "\n\n" +
                          ColorToken.Green("Shields: ") + playerShip.Shield + " / " + shipDetail.MaxShield + "\n" +
                          ColorToken.Green("Hull: ") + playerShip.Hull + " / " + shipDetail.MaxHull + "\n" +
                          ColorToken.Green("Capacitor: ") + playerShip.Capacitor + " / " + shipDetail.MaxCapacitor + "\n" +
                          ColorToken.Green("Shield Recharge: ") + shipDetail.ShieldRechargeRate + "s\n" +
                          ColorToken.Green("Droid Bay: ") + (shipDetail.HasDroidBay ? "YES" : "NO") + "\n\n" +
                          ColorToken.Green("High Power Modules: \n") +
                          highPowerModulesText + "\n\n" +
                          ColorToken.Green("Low Power Modules: \n") +
                          lowPowerModulesText + "\n\n" +
                          activeShipText;

            if (!isActiveShip)
            {
                page.AddResponse("Make Active", () =>
                {
                    dbPlayer.SelectedShipId = model.SelectedShipId;
                    DB.Set(playerId, dbPlayer);
                });
            }

            page.AddResponse("Manage Modules", () =>
            {
                EndConversation();

                var container = CreateObject(ObjectType.Placeable, ModuleContainerResref, GetLocation(player));
                SetLocalString(container, "PLAYER_SHIP_ID", model.SelectedShipId.ToString());

                AssignCommand(player, () => ActionInteractObject(container));
            });

            if (model.IsConfirmingUnregister)
            {
                page.AddResponse(ColorToken.Red("CONFIRM UNREGISTER SHIP"), () =>
                {
                    if(dbPlayer.SelectedShipId == model.SelectedShipId)
                        dbPlayer.SelectedShipId = Guid.Empty;

                    dbPlayer.Ships.Remove(model.SelectedShipId);
                    DB.Set(playerId, dbPlayer);

                    CreateItemOnObject(shipDetail.ItemResref, player);

                    model.SelectedShipId = Guid.Empty;
                    model.IsConfirmingUnregister = false;

                    if (!model.IsManagingActiveShip)
                    {
                        NavigationStack.TryPop(out _);
                    }

                    ChangePage(ManageShipsPageId, false);
                });
            }
            else
            {
                page.AddResponse(ColorToken.Red("Unregister Ship"), () =>
                {
                    // Player needs to remove all modules prior to unregister.
                    if (playerShip.HighPowerModules.Count > 0 ||
                        playerShip.LowPowerModules.Count > 0)
                    {
                        FloatingTextStringOnCreature("Please uninstall all modules before unregistering your ship.", player, false);
                        return;
                    }

                    model.IsConfirmingUnregister = true;
                });
            }
        }

        private const string RegistrationContainerResref = "space_register";
        private const string ModuleContainerResref = "space_modules";

        /// <summary>
        /// When the registration container is opened, instruct the player and disable the object from being clicked.
        /// </summary>
        [NWNEventHandler("spc_reg_open")]
        public static void OpenRegistrationContainer()
        {
            var container = OBJECT_SELF;
            var player = GetLastOpenedBy();
            SendMessageToPC(player, "Please insert your ship deed to register it.");
            SetUseableFlag(container, false);
        }

        /// <summary>
        /// When the registration container is disturbed, attempt to register a ship to the player.
        /// </summary>
        [NWNEventHandler("spc_reg_disturb")]
        public static void DisturbRegistrationContainer()
        {
            var player = GetLastDisturbed();
            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();

            if (type != DisturbType.Added) return;

            var shipTypeId = GetLocalInt(item, "STARSHIP_DEED_ID");

            // Item inserted wasn't a ship deed.
            if (shipTypeId <= 0)
            {
                Item.ReturnItem(player, item);
                SendMessageToPC(player, "Only ship deeds may be placed inside.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Too many ships registered.
            if (dbPlayer.Ships.Count >= Space.MaxRegisteredShips)
            {
                Item.ReturnItem(player, item);
                SendMessageToPC(player, $"Only {Space.MaxRegisteredShips} ships may be registered at a time.");
                return;
            }

            // Validation passed. Add the ship to the player's record.
            var shipType = (ShipType)shipTypeId;
            var shipDetail = Space.GetShipDetailByType(shipType);
            var shipId = Guid.NewGuid();
            dbPlayer.Ships.Add(shipId, new PlayerShip
            {
                Type = shipType,
                Name = shipDetail.Name,
                Shield = shipDetail.MaxShield,
                Hull = shipDetail.MaxHull,
                Capacitor = shipDetail.MaxCapacitor
            });
            DB.Set(playerId, dbPlayer);

            DestroyObject(item);
            SendMessageToPC(player, ColorToken.Green("Ship registered successfully."));
        }

        /// <summary>
        /// When the registration or modules container is closed, destroy it and all items contained inside.
        /// </summary>
        [NWNEventHandler("spc_reg_close")]
        [NWNEventHandler("spc_mod_close")]
        public static void CloseRegistrationContainer()
        {
            var container = OBJECT_SELF;

            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                DestroyObject(item);
            }

            DestroyObject(container);
        }

        /// <summary>
        /// When the modules container opens, instruct the user and disable the object from being clicked.
        /// </summary>
        [NWNEventHandler("spc_mod_open")]
        public static void OpenModulesContainer()
        {
            var player = GetLastOpenedBy();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var container = OBJECT_SELF;
            var playerShipId = new Guid(GetLocalString(container, "PLAYER_SHIP_ID"));
            var playerShip = dbPlayer.Ships[playerShipId];
            var shipDetails = Space.GetShipDetailByType(playerShip.Type);

            SendMessageToPC(player, $"High Power Modules: {playerShip.HighPowerModules.Count} / {shipDetails.HighPowerNodes}");
            SendMessageToPC(player, $"Low Power Modules: {playerShip.LowPowerModules.Count} / {shipDetails.LowPowerNodes}");

            foreach (var (_, module) in playerShip.HighPowerModules)
            {
                var item = Object.Deserialize(module.SerializedItem);
                Object.AcquireItem(container, item);
            }

            foreach (var (_, module) in playerShip.LowPowerModules)
            {
                var item = Object.Deserialize(module.SerializedItem);
                Object.AcquireItem(container, item);
            }

            SetUseableFlag(container, false);
        }

        /// <summary>
        /// When the modules container is disturbed, install or uninstall the module.
        /// </summary>
        [NWNEventHandler("spc_mod_disturb")]
        public static void DisturbModulesContainer()
        {
            var player = GetLastDisturbed();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var container = OBJECT_SELF;
            var playerShipId = new Guid(GetLocalString(container, "PLAYER_SHIP_ID"));
            var playerShip = dbPlayer.Ships[playerShipId];
            var shipDetails = Space.GetShipDetailByType(playerShip.Type);

            var item = GetInventoryDisturbItem();
            var itemId = GetObjectUUID(item);
            var type = GetInventoryDisturbType();
            
            if (type == DisturbType.Added)
            {
                var moduleTypeId = GetLocalInt(item, "STARSHIP_MODULE_ID");

                // Not a valid module type.
                if (moduleTypeId <= 0)
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, "Only starship modules may be installed.");
                    return;
                }

                var moduleType = (ShipModuleType) moduleTypeId;
                var moduleDetails = Space.GetShipModuleDetailByType(moduleType);

                // No high power nodes available.
                if (moduleDetails.PowerType == ShipModulePowerType.High &&
                    playerShip.HighPowerModules.Count >= shipDetails.HighPowerNodes)
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, "No high power nodes are available.");
                    return;
                }

                // No low power nodes available.
                if (moduleDetails.PowerType == ShipModulePowerType.Low &&
                    playerShip.LowPowerModules.Count >= shipDetails.LowPowerNodes)
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, "No low power nodes are available.");
                    return;
                }

                // Add to high power modules.
                if (moduleDetails.PowerType == ShipModulePowerType.High)
                {
                    dbPlayer.Ships[playerShipId].HighPowerModules.Add(itemId, new PlayerShipModule
                    {
                        SerializedItem = Object.Serialize(item),
                        Type = moduleType,
                        RecastTime = DateTime.MinValue
                    });
                }
                // Add to low power modules.
                else if (moduleDetails.PowerType == ShipModulePowerType.Low)
                {
                    dbPlayer.Ships[playerShipId].LowPowerModules.Add(itemId, new PlayerShipModule
                    {
                        SerializedItem = Object.Serialize(item),
                        Type = moduleType,
                        RecastTime = DateTime.MinValue
                    });
                }
                // Power type not specified
                else
                {
                    Log.Write(LogGroup.Error, $"Power type is not specified for module {moduleDetails.Name}.");
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, ColorToken.Red($"This module has not been properly configured. Notify an admin."));
                    return;
                }

                moduleDetails.ModuleEquippedAction?.Invoke(player, item, playerShip);
                DB.Set(playerId, dbPlayer);
            }
            else if (type == DisturbType.Removed)
            {
                if (dbPlayer.Ships[playerShipId].HighPowerModules.ContainsKey(itemId))
                {
                    dbPlayer.Ships[playerShipId].HighPowerModules.Remove(itemId);
                }
                else if (dbPlayer.Ships[playerShipId].LowPowerModules.ContainsKey(itemId))
                {
                    dbPlayer.Ships[playerShipId].LowPowerModules.Remove(itemId);
                }
                else
                {
                    return;
                }

                var moduleTypeId = GetLocalInt(item, "STARSHIP_MODULE_ID");
                var moduleType = (ShipModuleType)moduleTypeId;
                var moduleDetails = Space.GetShipModuleDetailByType(moduleType);

                moduleDetails.ModuleUnequippedAction?.Invoke(player, item, playerShip);
                DB.Set(playerId, dbPlayer);
            }

            SendMessageToPC(player, $"High Power Modules: {dbPlayer.Ships[playerShipId].HighPowerModules.Count} / {shipDetails.HighPowerNodes}");
            SendMessageToPC(player, $"Low Power Modules: {dbPlayer.Ships[playerShipId].LowPowerModules.Count} / {shipDetails.LowPowerNodes}");
        }
    }
}
