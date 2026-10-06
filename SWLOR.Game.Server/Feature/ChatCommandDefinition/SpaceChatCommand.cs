using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SpaceChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ExitSpaceCommand();
            _builder.Create("crew").Description("Takes a docked ship station: weapons, systems, survey, leave or list.").Permissions(AuthorizationLevel.All).Action((user,target,location,args)=>
            {
                try
                {
                    var choice=args.FirstOrDefault()?.ToLowerInvariant();
                    if(choice=="leave")Space.LeaveShipStation(user);
                    else if(choice=="weapons")Space.JoinShipStation(user,Service.SpaceService.ShipCrewStation.Weapons);
                    else if(choice=="systems")Space.JoinShipStation(user,Service.SpaceService.ShipCrewStation.Systems);
                    else if(choice=="survey")Space.JoinShipStation(user,Service.SpaceService.ShipCrewStation.SurveyIndustry);
                    else SendMessageToPC(user,Space.ShipStationSummary(user,Space.GetOperatingShipId(user))+"\nUse /crew weapons|systems|survey|leave inside a docked ship.");
                }catch(InvalidOperationException ex){SendMessageToPC(user,ex.Message);}
            });
            _builder.Create("shipsupply").Description("Opens dock Standard fittings, starter grants and ore commissions.").Permissions(AuthorizationLevel.All).Action((user,target,location,args)=>
            {
                try{ShipSupply.RequireServiceDock(user);Gui.TogglePlayerWindow(user,Service.GuiService.GuiWindowType.ShipSupply,new Feature.GuiDefinition.Payload.ShipSupplyPayload());}
                catch(InvalidOperationException ex){SendMessageToPC(user,ex.Message);}
            });
            _builder.Create("cockpit").Description("Opens ship banks and prepared operating techniques.").Permissions(AuthorizationLevel.All)
                .Action((user,target,location,args)=>
                {
                    if (!Space.IsPlayerInSpaceMode(user)&&string.IsNullOrEmpty(DB.Get<Entity.Player>(GetObjectUUID(user)).CrewShipId)){SendMessageToPC(user,"Use Operations in ship management or take a crew station inside a docked ship.");return;}
                    var player=DB.Get<Entity.Player>(GetObjectUUID(user));
                    Gui.TogglePlayerWindow(user,Service.GuiService.GuiWindowType.ShipCockpit,new Feature.GuiDefinition.Payload.ShipCockpitPayload(Space.GetOperatingShipId(user)));
                });

            _builder.Create("spacejobs").Description("Opens your ship contracts.").Permissions(AuthorizationLevel.All).Action((user,target,location,args)=>
            {
                var record=DB.Get<Entity.Player>(GetObjectUUID(user));
                var shipId=Space.IsPlayerInSpaceMode(user)?record.ActiveShipId:DB.Search(new Service.DBService.DBQuery<Entity.PlayerShip>().AddFieldSearch(nameof(Entity.PlayerShip.OwnerPlayerId),record.Id,false)).FirstOrDefault()?.Id;
                if(shipId==null){SendMessageToPC(user,"Register a ship first.");return;}
                Gui.TogglePlayerWindow(user,Service.GuiService.GuiWindowType.ShipContracts,new Feature.GuiDefinition.Payload.ShipContractsPayload(shipId));
            });
            return _builder.Build();
        }

        private void ExitSpaceCommand()
        {
            _builder.Create("exit")
                .Description("Exits the pilot seat when controlling a starship.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    if (!Space.IsPlayerInSpaceMode(user))
                    {
                        SendMessageToPC(user, "This command can only be used while piloting a starship.");
                        return;
                    }

                    if (Enmity.HasEnmity(user))
                    {
                        SendMessageToPC(user, "This command cannot be used while you're targeted.");
                        return;
                    }

                    Space.WarpPlayerInsideShip(user);
                });
        }
    }
}
