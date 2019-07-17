using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;
using System.Collections.Generic;
using System.Collections;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Conversation
{
    public class ShipComputer: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            string structureID = _.GetLocalString(_.GetArea(player),"PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(structureID))
            {
                player.SendMessage("Base structure ID not found on area.  Please report this problem.");
                return null;
            }

            Guid pcBaseStructureID = new Guid(structureID);
            PCBaseStructure structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
            PCBase pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

            bool bSpace = SpaceService.IsLocationSpace(pcBase.ShipLocation);

            List<string> options = new List<string>();

            if (bSpace && BasePermissionService.HasStructurePermission(player, structure.ID, StructurePermission.CanFlyStarship))
            {
                // See if we are near enough to the planet to land.
                if (SpaceService.CanLandOnPlanet(player.Area))
                {
                    options.Add("Land");
                }

                options.Add("Pilot Ship");
                options.Add("Hyperspace Jump");
            }
            else if ( BasePermissionService.HasStructurePermission(player, structure.ID, StructurePermission.CanFlyStarship))
            {
                options.Add("Take Off");
            }

            if (!bSpace && BasePermissionService.HasBasePermission(player, structure.PCBaseID, BasePermission.CanManageBaseFuel))
            {
                options.Add("Access Fuel Bay");
                options.Add("Access Stronidium Bay");
            }

            if (BasePermissionService.HasBasePermission(player, structure.PCBaseID, BasePermission.CanAccessStructureInventory))
            {
                if (!bSpace) options.Add("Access Resource Bay");
                options.Add("Export Starcharts");
            }

            DialogPage mainPage = new DialogPage("", options.ToArray());

            dialog.AddPage("MainPage", mainPage);

            // Hyperspace destinations.
            string[] responses = SpaceService.GetHyperspaceDestinationList(pcBase);
            DialogPage destinationPage = new DialogPage("Please select a destination to fly to.", responses);
            dialog.AddPage("HyperDestPage", destinationPage);

            // Landing destinations.            
            Hashtable landingspots = SpaceService.GetLandingDestinationList(player, pcBase);
            List<String> responseList = landingspots.Keys.Cast<String>().ToList();
            DialogPage landingPage = new DialogPage("Where do you want to land?", responseList.ToArray());
            dialog.AddPage("LandingDestPage", landingPage);

            // Save off the landing responses in CustomData.  This ensures we can access the structure IDs later.
            foreach (var key in landingspots.Keys)
            {
                dialog.CustomData.Add("LAND_" + key, landingspots[key]);
            }

            return dialog;
        }
        
        public override void Initialize()
        {
            Guid structureID = new Guid(_.GetLocalString(_.GetArea(GetDialogTarget()), "PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = DataService.PCBaseStructure.GetByID(structureID); 
            PCBase pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            BaseStructure baseStructure = DataService.BaseStructure.GetByID(structure.BaseStructureID);

            NWPlaceable bay = SpaceService.GetCargoBay(GetPC().Area, null);

            int currentReinforcedFuel = pcBase.ReinforcedFuel;
            int currentFuel = pcBase.Fuel;
            int currentResources = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(structure.ID).Count();
            int maxReinforcedFuel = BaseService.CalculateMaxReinforcedFuel(pcBase.ID) + 25 * SpaceService.GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipStronidiumBonus);
            int maxFuel = BaseService.CalculateMaxFuel(pcBase.ID) + 25 * SpaceService.GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipFuelBonus);
            int maxResources = BaseService.CalculateResourceCapacity(pcBase.ID);

            string locationDescription = "";

            if (SpaceService.IsLocationSpace(pcBase.ShipLocation))
            {
                locationDescription = "Ship is in orbit around " + ColorTokenService.Cyan(SpaceService.GetPlanetFromLocation(pcBase.ShipLocation)) + ".\n";
            }
            else if (SpaceService.IsLocationPublicStarport(pcBase.ShipLocation))
            {
                locationDescription = "Ship is docked in the public starport on " + ColorTokenService.Cyan(SpaceService.GetPlanetFromLocation(pcBase.ShipLocation)) + ".\n";
            }
            else
            {
                locationDescription = "Ship is docked on " + ColorTokenService.Cyan(SpaceService.GetPlanetFromLocation(pcBase.ShipLocation)) + ".\n";
            }

            string header = locationDescription;
            header += ColorTokenService.Green("Fuel: ") + currentFuel + " / " + maxFuel + "\n";
            header += ColorTokenService.Green("Reinforced Fuel: ") + currentReinforcedFuel + " / " + maxReinforcedFuel + "\n";
            header += ColorTokenService.Green("Resource Bay: ") + currentResources + " / " + maxResources + "\n";
            header += ColorTokenService.Green("Hull integrity: ") + structure.Durability + " / " + baseStructure.Durability + "\n";
  
            header += "The computer awaits your orders.";

            SetPageHeader("MainPage", header);

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            Guid structureID = new Guid(_.GetLocalString(player.Area, "PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = DataService.PCBaseStructure.GetByID(structureID);
            PCBase pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

            DialogPage page = dialog.GetPageByName(pageName);
            DialogResponse response = page.Responses[responseID - 1];

            bool carefulPilot = PerkService.GetCreaturePerkLevel(player, PerkType.CarefulPilot) > 0;
                
            if (pageName == "MainPage")
            {
                // The number of dialog options available can vary.  So query based on the actual text of the response.
                if (response.Text == "Land")
                {
                    ChangePage("LandingDestPage");
                }
                else if (response.Text == "Pilot Ship")
                {
                    SpaceService.CreateShipInSpace(player.Area); // In case we logged in here.
                    SpaceService.DoFlyShip(GetPC(), GetPC().Area);
                    EndConversation();
                }
                else if (response.Text == "Hyperspace Jump")
                {
                    // Build the list of destinations.
                    ChangePage("HyperDestPage");
                }
                else if (response.Text == "Take Off")
                {
                    // Check fuel
                    if (pcBase.Fuel < 1)
                    {
                        GetPC().SendMessage("You don't have enough fuel! You need 1 fuel to take off.");
                        dialog.ResetPage();
                    }
                    else
                    {
                        // Fuel is good - we have liftoff.
                        if (!SpaceService.DoPilotingSkillCheck(GetPC(), 2, carefulPilot))
                        {
                            // Failed our skill check.  Deduct fuel but don't do anything else.
                            GetPC().FloatingText("The ship shudders a bit, but your awkwardness on the throttle shows, and it doesn't make it off the dock.  Try again.");
                            pcBase.Fuel -= 1;
                            DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                            return;
                        }

                        EndConversation();

                        // Save details of the current dock for later.
                        Guid shipPCBaseID = new Guid(pcBase.ShipLocation);
                        PCBaseStructure dock = DataService.PCBaseStructure.GetByIDOrDefault(shipPCBaseID);

                        pcBase.Fuel -= 1;
                        pcBase.DateRentDue = DateTime.UtcNow.AddDays(99);
                        pcBase.ShipLocation = SpaceService.GetPlanetFromLocation(pcBase.ShipLocation) + " - Orbit";
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                        SpaceService.CreateShipInSpace(player.Area);

                        // Give the impression of movement
                        foreach (var creature in player.Area.Objects)
                        {
                            if (creature.IsPC || creature.IsDM)
                            {
                                _.FloatingTextStringOnCreature("The ship is taking off", creature);
                            }
                        }

                        _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(356), player);

                        // Clean up the base structure, if we were in a PC dock not public starport.
                        // Get a reference to our placeable (and door), and delete them with some VFX. 
                        if (dock != null)
                        {
                            PCBase dockBase = DataService.PCBase.GetByID(dock.PCBaseID);

                            IEnumerable<NWArea> areas = NWModule.Get().Areas;
                            NWArea landingArea = new NWArea(_.GetFirstArea());

                            foreach (var area in areas)
                            {
                                if (_.GetResRef(area) == dockBase.AreaResref)
                                {
                                    landingArea = area;
                                }
                            }

                            List<AreaStructure> areaStructures = landingArea.Data["BASE_SERVICE_STRUCTURES"];
                            foreach (var plc in areaStructures)
                            {
                                if (plc.PCBaseStructureID == dock.ID)
                                {
                                    // Found our dock.  Clear its variable and play some VFX.
                                    plc.Structure.SetLocalInt("DOCKED_STARSHIP", 0);
                                    DoDustClouds(plc.Structure.Location);
                                    _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(356), plc.Structure);
                                }
                                else if (plc.PCBaseStructureID == structure.ID)
                                {
                                    // found either our ship or our entrance (both are saved with our structure ID).  Delete them.
                                    // Dp NOT remove the PC base structure object from the database.  We still need that. 
                                    plc.Structure.Destroy();
                                }
                            }
                        }
                    }
                }
                else if (response.Text == "Access Fuel Bay")
                {
                    OpenFuelBay(false);
                    EndConversation();
                }
                else if (response.Text == "Access Stronidium Bay")
                {
                    OpenFuelBay(true);
                    EndConversation();
                }
                else if (response.Text == "Access Resource Bay")
                {
                    NWPlaceable bay = SpaceService.GetCargoBay(player.Area, GetPC());
                    if (bay != null) GetPC().AssignCommand(() => _.ActionInteractObject(bay.Object));
                    EndConversation();
                }
                else if (response.Text == "Export Starcharts")
                {
                    NWItem item = _.CreateItemOnObject("starcharts", player, 1, _.Random(10000).ToString());

                    // Initialise the list, in case it hasn't been populated yet.
                    SpaceService.GetHyperspaceDestinationList(pcBase);

                    item.SetLocalInt("Starcharts", (int)pcBase.Starcharts);
                }
            }
            else if (pageName == "HyperDestPage")
            {
                // Check fuel
                if (pcBase.Fuel < 50)
                {
                    GetPC().SendMessage("You don't have enough fuel! You need 50 fuel to make a hyperspace jump.");
                    dialog.ResetPage();
                }
                else
                {
                    // Fuel is good - make the jump
                    if (!SpaceService.DoPilotingSkillCheck(GetPC(), 13, carefulPilot))
                    {
                        // Failed our skill check.  Deduct fuel but don't do anything else.
                        GetPC().FloatingText("Jump failed!  You forgot to whatsit the thingummyjig.");
                        pcBase.Fuel -= 50;
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                        EndConversation();
                        return;
                    }

                    // Move the ship out of the old orbit.
                    SpaceService.RemoveShipInSpace(player.Area);

                    // Fade to black for hyperspace.
                    EndConversation();
                    pcBase.Fuel -= 50;
                    pcBase.ShipLocation = response.Text + " - Orbit";
                    DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                    // Put the ship in its new orbit.
                    SpaceService.CreateShipInSpace(player.Area);

                    // Give the impression of movement - would be great to have the actual hyperspace jump graphics here.
                    foreach (var creature in player.Area.Objects)
                    {
                        if (creature.IsPC || creature.IsDM)
                        {
                            _.FloatingTextStringOnCreature("Making a hyperspace jump!", creature);
                            _.FadeToBlack(creature, 0.5f);
                            _.DelayCommand(1.0f, () => { _.FadeFromBlack(creature, 0.5f); });
                        }
                    }
                }
            }
            else if (pageName == "LandingDestPage")
            {
                // Skill check. 
                if (!SpaceService.DoPilotingSkillCheck(GetPC(), 5, carefulPilot))
                {
                    // Failed our skill check.  Land anyway but burn more fuel.
                    if (pcBase.Fuel > 0)
                    {
                        GetPC().FloatingText("You overshoot the landing spot, burning extra fuel getting your ship into position.");
                        pcBase.Fuel -= 1;
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                    }
                }

                // Get the response, then find the structure ID. 
                Guid dockStructureID = dialog.CustomData["LAND_" + response.Text];

                // This could be a public startport ID or a private dock base structure ID.  
                SpaceStarport starport = DataService.SpaceStarport.GetByIDOrDefault(dockStructureID);
                if (starport != null)
                {
                    // We have a public starport.  
                    if (player.Gold < starport.Cost)
                    {
                        player.SendMessage("You do not have enough credits to land here.");
                        return;
                    }
                    else
                    {
                        _.TakeGoldFromCreature(starport.Cost, player, 1);

                        // Land.
                        pcBase.ShipLocation = starport.ID.ToString();
                        pcBase.DateRentDue = DateTime.UtcNow.AddDays(1);
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                        // Notify PC.
                        player.SendMessage("You have paid your first day's berthing fees. Use the Base Management System to extend your lease if you plan to stay longer, or your ship will be impounded.");

                        EndConversation();
                    }
                }
                else
                {
                    LoggingService.Trace(TraceComponent.Space, "Landing in PC base dock, ID: " + dockStructureID.ToString());
                    PCBaseStructure dock = DataService.PCBaseStructure.GetByIDOrDefault(dockStructureID);

                    if (dock == null)
                    {
                        player.SendMessage("ERROR: Could not find landing dock by ID.  Please report this.");
                        LoggingService.Trace(TraceComponent.Space, "Could not find landing dock ID " + dockStructureID.ToString());
                        return;
                    }

                    NWPlaceable plc = BaseService.FindPlaceableFromStructureID(dock.ID.ToString());

                    if (plc == null)
                    {
                        LoggingService.Trace(TraceComponent.Space, "Failed to find dock placeable.");
                        player.SendMessage("ERROR: Could not find landing dock placeable.  Please report this.");
                        return; 
                    }

                    LoggingService.Trace(TraceComponent.Space, "Found dock, landing ship.");

                    // We've found our dock. Update our record of where the ship's exterior should spawn.
                    NWLocation loc = plc.Location;

                    structure.LocationX = loc.X;
                    structure.LocationY = loc.Y;
                    structure.LocationZ = loc.Z;
                    structure.LocationOrientation = _.GetFacingFromLocation(loc);

                    DataService.SubmitDataChange(structure, DatabaseActionType.Update);

                    // And update the base to mark the parent dock as the location.
                    pcBase.ShipLocation = dock.ID.ToString();
                    DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                    // Now use the Base Service to spawn the ship exterior.
                    BaseService.SpawnStructure(plc.Area, structure.ID);

                    // Mark the dock as occupied.
                    plc.SetLocalInt("DOCKED_STARSHIP", 1);

                    // Notify PCs in the landing area.
                    foreach (var creature in plc.Area.Objects)
                    {
                        if (creature.IsPC || creature.IsDM)
                        {
                            _.FloatingTextStringOnCreature("A ship has just landed!", creature);
                        }
                    }

                    // And shake the screen, because stuff.
                    _.ApplyEffectAtLocation(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(356), loc);
                    DoDustClouds(loc);            
                }

                // We're landing.  Make sure any pilot or gunner get out of flight mode.  
                SpaceService.LandCrew(player.Area);

                // If we are still here, we landed successfully.  Shake the screen about and notify PCs on the ship.
                // Give the impression of movement
                foreach (var creature in player.Area.Objects)
                {
                    if (creature.IsPC || creature.IsDM)
                    {
                        _.FloatingTextStringOnCreature("The ship is landing.", creature);
                    }
                }

                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(356), player);
                SpaceService.RemoveShipInSpace(player.Area);

                EndConversation();
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }


        private void OpenFuelBay(bool isStronidium)
        {
            NWPlayer oPC = GetPC();
            NWArea area = oPC.Area;

            NWPlaceable bay = area.GetLocalObject("FUEL_BAY");
            if (bay.IsValid)
            {
                NWObject accessor = bay.GetLocalObject("BAY_ACCESSOR");
                if (!accessor.IsValid)
                {
                    bay.Destroy();
                }
                else
                {
                    oPC.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                    return;
                }
            }

            Guid structureID = new Guid(_.GetLocalString(area, "PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            Location location = oPC.Location;
            bay = _.CreateObject(OBJECT_TYPE_PLACEABLE, "fuel_bay", location);
            bay.AssignCommand(() => _.SetFacingPoint(oPC.Position));

            area.SetLocalObject("FUEL_BAY", bay.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());

            if (isStronidium)
            {
                if(pcBase.ReinforcedFuel > 0)
                    _.CreateItemOnObject("stronidium", bay.Object, pcBase.ReinforcedFuel);

                bay.SetLocalInt("CONTROL_TOWER_FUEL_TYPE", 1);
            }
            else
            {
                if (pcBase.Fuel > 0)
                    _.CreateItemOnObject("fuel_cell", bay.Object, pcBase.Fuel);
            }

            oPC.AssignCommand(() => _.ActionInteractObject(bay.Object));
        }

        void DoDustClouds(NWLocation loc)
        {
            // Utility method to throw some dust clouds up around a landing ship.
            float x, y;
            Vector v;

            for (int i=0; i < 6; i++)
            {
                x = loc.X + (5 - _.d10());
                y = loc.Y + (5 - _.d10());
                v = new Vector(x, y, loc.Z);

                NWPlaceable cloud = _.CreateObject(OBJECT_TYPE_PLACEABLE, "plc_dustplume", _.Location(loc.Area, v, loc.Orientation));

                _.DelayCommand(10.0f, () => _.DestroyObject(cloud));
            }
        }

    }
}
