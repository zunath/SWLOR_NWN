using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISpaceService
    {
        int GetShipBaseStat(int BaseStructureID, int Stat);
        int GetCargoBonus(PCBase pcBase, int Stat);
        int GetCargoBonus(NWArea starship, int Stat);
        NWPlaceable GetCargoBay(NWArea starship, NWPlayer player);
        void OnModuleHeartbeat();
        void SetShipLocation(NWArea area, string Location);
        string GetShipLocation(NWArea area);
        bool IsLocationSpace(string location);
        bool IsLocationPublicStarport(string location);
        string GetPlanetFromLocation(string location);
        int PlanetToDestination(string planet);
        string DestinationToPlanet(int destination);
        string[] GetHyperspaceDestinationList(PCBase pcBase);
        Hashtable GetLandingDestinationList(NWPlayer player, PCBase pcBase);
        bool CanDetect(PCBase scanningShip, PCBase otherShip);
        bool DoPilotingSkillCheck(NWPlayer player, int DC);
    }
}
