using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class ShipComputer : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var structureID = GetLocalString(GetArea(player), "PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(structureID))
            {
                player.SendMessage("Base structure ID not found on area.  Please report this problem.");
                return null;
            }

            var pcBaseStructureID = new Guid(structureID);
            var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

            var bSpace = SpaceService.IsLocationSpace(pcBase.ShipLocation);

            var options = new List<string>();

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
            else if (BasePermissionService.HasStructurePermission(player, structure.ID, StructurePermission.CanFlyStarship))
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

            var mainPage = new DialogPage("", options.ToArray());

            dialog.AddPage("MainPage", mainPage);

            // Hyperspace destinations.
            var responses = SpaceService.GetHyperspaceDestinationList(pcBase);
            var destinationPage = new DialogPage("Please select a destination to fly to.", responses);
            dialog.AddPage("HyperDestPage", destinationPage);

            // Landing destinations.            
            var landingspots = SpaceService.GetLandingDestinationList(player, pcBase);
            var responseList = landingspots.Keys.Cast<String>().ToList();
            var landingPage = new DialogPage("Where do you want to land?", responseList.ToArray());
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
            var structureID = new Guid(GetLocalString(GetArea(GetDialogTarget()), "PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            var baseStructure = DataService.BaseStructure.GetByID(structure.BaseStructureID);

            var bay = SpaceService.GetCargoBay(GetPC().Area, null);

            var currentReinforcedFuel = pcBase.ReinforcedFuel;
            var currentFuel = pcBase.Fuel;
            var currentResources = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(structure.ID).Count();
            var maxReinforcedFuel = BaseService.CalculateMaxReinforcedFuel(pcBase.ID) + 25 * SpaceService.GetCargoBonus(bay, ItemPropertyType.StarshipStronidiumBonus);
            var maxFuel = BaseService.CalculateMaxFuel(pcBase.ID) + 25 * SpaceService.GetCargoBonus(bay, ItemPropertyType.StarshipFuelBonus);
            var maxResources = BaseService.CalculateResourceCapacity(pcBase.ID);

            string locationDescription;

            if (SpaceService.IsLocationSpace(pcBase.ShipLocation))
            {
                locationDescription = "Ship is in orbit around " + ColorToken.Cyan(SpaceService.GetPlanetFromLocation(pcBase.ShipLocation)) + ".\n";
            }
            else if (SpaceService.IsLocationPublicStarport(pcBase.ShipLocation))
            {
                locationDescription = "Ship is docked in the public starport on " + ColorToken.Cyan(SpaceService.GetPlanetFromLocation(pcBase.ShipLocation)) + ".\n";
            }
            else
            {
                locationDescription = "Ship is docked on " + ColorToken.Cyan(SpaceService.GetPlanetFromLocation(pcBase.ShipLocation)) + ".\n";
            }

            var header = locationDescription;
            header += ColorToken.Green("Fuel: ") + currentFuel + " / " + maxFuel + "\n";
            header += ColorToken.Green("Reinforced Fuel: ") + currentReinforcedFuel + " / " + maxReinforcedFuel + "\n";
            header += ColorToken.Green("Resource Bay: ") + currentResources + " / " + maxResources + "\n";
            header += ColorToken.Green("Hull integrity: ") + structure.Durability + " / " + baseStructure.Durability + "\n";

            header += "The computer awaits your orders.";

            SetPageHeader("MainPage", header);

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var structureID = new Guid(GetLocalString(player.Area, "PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

            var page = dialog.GetPageByName(pageName);
            var response = page.Responses[responseID - 1];

            var carefulPilot = PerkService.GetCreaturePerkLevel(player, PerkType.CarefulPilot) > 0;

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
                        var shipPCBaseID = new Guid(pcBase.ShipLocation);
                        var dock = DataService.PCBaseStructure.GetByIDOrDefault(shipPCBaseID);

                        pcBase.Fuel -= 1;
                        pcBase.DateRentDue = DateTime.UtcNow.AddDays(99);
                        pcBase.ShipLocation = SpaceService.GetPlanetFromLocation(pcBase.ShipLocation) + " - Orbit";
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                        SpaceService.CreateShipInSpace(player.Area);

                        // Give the impression of movement
                        var playerArea = GetArea(player);
                        for (var creature = GetFirstObjectInArea(playerArea); GetIsObjectValid(creature); creature = GetNextObjectInArea(playerArea))
                        {
                            if (GetIsPC(creature) || GetIsDM(creature))
                            {
                                FloatingTextStringOnCreature("The ship is taking off", creature);
                            }
                        }

                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake), player);

                        // Clean up the base structure, if we were in a PC dock not public starport.
                        // Get a reference to our placeable (and door), and delete them with some VFX. 
                        if (dock != null)
                        {
                            var dockBase = DataService.PCBase.GetByID(dock.PCBaseID);

                            var areas = NWModule.Get().Areas;
                            var landingArea = GetFirstArea();

                            foreach (var area in areas)
                            {
                                if (GetResRef(area) == dockBase.AreaResref)
                                {
                                    landingArea = area;
                                }
                            }

                            List<AreaStructure> areaStructures = BaseService.GetAreaStructures(landingArea);
                            foreach (var plc in areaStructures)
                            {
                                if (plc.PCBaseStructureID == dock.ID)
                                {
                                    // Found our dock.  Clear its variable and play some VFX.
                                    plc.Structure.SetLocalInt("DOCKED_STARSHIP", 0);
                                    DoDustClouds(plc.Structure.Location);
                                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake), plc.Structure);
                                }
                                else if (plc.PCBaseStructureID == structure.ID)
                                {
                                    // found either our ship or our entrance (both are saved with our structure ID).  Delete them.
                                    // Do NOT remove the PC base structure object from the database.  We still need that. 
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
                    var bay = SpaceService.GetCargoBay(player.Area, GetPC());
                    if (bay != null) GetPC().AssignCommand(() => ActionInteractObject(bay.Object));
                    EndConversation();
                }
                else if (response.Text == "Export Starcharts")
                {
                    NWItem item = CreateItemOnObject("starcharts", player, 1, Random(10000).ToString());

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

                    var playerArea = GetArea(player);
                    // Give the impression of movement - would be great to have the actual hyperspace jump graphics here.
                    for (var creature = GetFirstObjectInArea(playerArea); GetIsObjectValid(creature); creature = GetNextObjectInArea(playerArea))
                    {
                        if (GetIsPC(creature) || GetIsDM(creature))
                        {
                            FloatingTextStringOnCreature("Making a hyperspace jump!", creature);
                            FadeToBlack(creature, 0.5f);
                            DelayCommand(1.0f, () => { FadeFromBlack(creature, 0.5f); });
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
                var starport = DataService.Starport.GetByStarportIDOrDefault(dockStructureID);
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
                        TakeGoldFromCreature(starport.Cost, player, true);

                        // Land.
                        pcBase.ShipLocation = starport.StarportID.ToString();
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
                    var dock = DataService.PCBaseStructure.GetByIDOrDefault(dockStructureID);

                    if (dock == null)
                    {
                        player.SendMessage("ERROR: Could not find landing dock by ID.  Please report this.");
                        LoggingService.Trace(TraceComponent.Space, "Could not find landing dock ID " + dockStructureID.ToString());
                        return;
                    }

                    var plc = BaseService.FindPlaceableFromStructureID(dock.ID.ToString());

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
                    structure.LocationOrientation = GetFacingFromLocation(loc);

                    DataService.SubmitDataChange(structure, DatabaseActionType.Update);

                    // And update the base to mark the parent dock as the location.
                    pcBase.ShipLocation = dock.ID.ToString();
                    DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                    // Now use the Base Service to spawn the ship exterior.
                    BaseService.SpawnStructure(plc.Area, structure.ID);

                    // Mark the dock as occupied.
                    plc.SetLocalInt("DOCKED_STARSHIP", 1);

                    // Notify PCs in the landing area.
                    var plcArea = GetArea(plc);
                    for(var creature = GetFirstObjectInArea(plcArea); GetIsObjectValid(creature); creature = GetNextObjectInArea(plcArea))
                    {
                        if (GetIsPC(creature) || GetIsDM(creature))
                        {
                            FloatingTextStringOnCreature("A ship has just landed!", creature);
                        }
                    }

                    // And shake the screen, because stuff.
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake), loc);
                    DoDustClouds(loc);
                }

                // We're landing.  Make sure any pilot or gunner get out of flight mode.  
                SpaceService.LandCrew(player.Area);

                // If we are still here, we landed successfully.  Shake the screen about and notify PCs on the ship.
                // Give the impression of movement
                var playerArea = GetArea(player);
                for(var creature = GetFirstObjectInArea(playerArea); GetIsObjectValid(creature); creature = GetNextObjectInArea(playerArea))
                {
                    if (GetIsPC(creature) || GetIsDM(creature))
                    {
                        FloatingTextStringOnCreature("The ship is landing.", creature);
                    }
                }

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake), player);
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
            var oPC = GetPC();
            var area = oPC.Area;

            NWPlaceable bay = GetLocalObject(area, "FUEL_BAY");
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

            var structureID = new Guid(GetLocalString(area, "PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            var location = oPC.Location;
            bay = CreateObject(ObjectType.Placeable, "fuel_bay", location);
            bay.AssignCommand(() => SetFacingPoint(oPC.Position));

            SetLocalObject(area, "FUEL_BAY", bay.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());

            if (isStronidium)
            {
                if (pcBase.ReinforcedFuel > 0)
                    CreateItemOnObject("stronidium", bay.Object, pcBase.ReinforcedFuel);

                bay.SetLocalInt("CONTROL_TOWER_FUEL_TYPE", 1);
            }
            else
            {
                if (pcBase.Fuel > 0)
                    CreateItemOnObject("fuel_cell", bay.Object, pcBase.Fuel);
            }

            oPC.AssignCommand(() => ActionInteractObject(bay.Object));
        }

        void DoDustClouds(NWLocation loc)
        {
            // Utility method to throw some dust clouds up around a landing ship.
            float x, y;
            Vector3 v;

            for (var i = 0; i < 6; i++)
            {
                x = loc.X + (5 - d10());
                y = loc.Y + (5 - d10());
                v = new Vector3(x, y, loc.Z);

                NWPlaceable cloud = CreateObject(ObjectType.Placeable, "plc_dustplume", Location(loc.Area, v, loc.Orientation));

                DelayCommand(10.0f, () => DestroyObject(cloud));
            }
        }

    }
}
