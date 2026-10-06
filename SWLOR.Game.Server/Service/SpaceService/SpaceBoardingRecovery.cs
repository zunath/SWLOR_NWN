using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class SpaceBoardingRecovery
    {
        public static bool GrantConsole(SpaceContract contract,SpaceActivityProfile profile,ShipStatus ship,int index,string playerId,DateTime now)
        {
            if(!profile.Boarding||contract.State!=SpaceContractState.Active||!contract.BoardingPlayers.Contains(playerId)||!contract.ShipsByPlayer.ContainsKey(playerId)||now>=contract.BoardingEndsAt||index<0||index>=profile.BoardingConsoles)throw new InvalidOperationException("The boarding recovery is unavailable.");
            if(contract.BoardingConsoleOperators.TryGetValue(index,out var owner)&&owner!=playerId)throw new InvalidOperationException("That console recovery belongs to its original operator.");
            if(contract.BoardingConsoles.Contains(index))return false;
            var receipt="boarding/"+contract.Id+"/"+index;
            if(!ship.Cargo.ContainsKey(receipt+"/precision")&&!ShipCargo.CanAdd(ship,profile.BoardingPrecisionPerConsole+profile.BoardingOrePerConsole))throw new InvalidOperationException("Reserve room in your ship hold for the recovered components.");
            contract.BoardingConsoleOperators[index]=playerId;
            ShipCargo.Add(ship,"prec_assembly",profile.BoardingPrecisionPerConsole,receipt+"/precision");ShipCargo.Add(ship,"ore_tilarium",profile.BoardingOrePerConsole,receipt+"/ore");
            contract.BoardingConsoles.Add(index);
            if(contract.Receipts.Add(receipt)){contract.Contributions.Credit(playerId,SkillType.ShipSystems,profile.BoardingConsoleSeconds);contract.Contributions.Credit(playerId,SkillType.SpaceIndustry,profile.BoardingPrecisionPerConsole+profile.BoardingOrePerConsole);}
            contract.Boarded=contract.BoardingConsoles.Count==profile.BoardingConsoles;return true;
        }
    }
}
