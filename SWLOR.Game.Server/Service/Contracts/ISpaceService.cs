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
        NWPlaceable GetCargoBay(NWArea starship, NWPlayer player);
        void OnNWNXChat();
        void OnModuleLeave(NWPlayer player);
        void SetShipLocation(NWArea area, string Location);
        int GetCargoBonus(NWPlaceable bay, int stat);
        string GetShipLocation(NWArea area);
        bool IsLocationSpace(string location);
        bool IsLocationPublicStarport(string location);
        string GetPlanetFromLocation(string location);
        int PlanetToDestination(string planet);
        string DestinationToPlanet(int destination);
        string[] GetHyperspaceDestinationList(PCBase pcBase);
        Hashtable GetLandingDestinationList(NWPlayer player, PCBase pcBase);
        bool DoPilotingSkillCheck(NWPlayer player, int DC, bool reRollIfFailed = false);
        void DoFlyShip(NWPlayer player, NWArea ship);
        void DoStopFlyShip(NWPlayer player);
        void DoCrewGuns(NWPlayer player, NWArea ship);
        void DoStopCrewGuns(NWPlayer player);
        void LandCrew(NWArea ship);
        void CreateShipInSpace(NWArea ship, NWLocation location = null);
        void RemoveShipInSpace(NWArea ship);
        bool CanLandOnPlanet(NWArea ship);
        void CreateSpaceEncounter(NWObject trigger, NWPlayer player);
        void OnCreatureSpawn(NWCreature creature);
        void OnCreatureHeartbeat(NWCreature creature);
        void OnModuleItemEquipped();
        void OnPhysicalAttacked(NWCreature creature, NWCreature attacker);
        void OnPerception(NWCreature creature, NWCreature perceived);
        void OnHeartbeat(NWCreature creature);
    }
}
