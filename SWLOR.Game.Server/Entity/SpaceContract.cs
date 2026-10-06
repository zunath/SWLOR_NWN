using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Entity
{
    public enum SpaceContractState { Recruiting, Loading, Active, Unloading, Settling, Completed, Cancelled }
    public sealed record SpaceRoutePoint(double X,double Y,double Z);
    public sealed class SpaceContract : EntityBase
    {
        [Indexed] public bool Closed { get; set; }
        [Indexed] public SpaceContractState State { get; set; }
        public string Profile { get; set; }
        public string LeaderId { get; set; }
        public string OriginPlanet { get; set; }
        public string DestinationPlanet { get; set; }
        public string AreaResref { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime TransitionAt { get; set; }
        public bool RewardJournalComplete { get; set; }
        public bool DepositPaid { get; set; }
        public int FreightFee { get; set; }
        public Dictionary<string,string> ShipsByPlayer { get; set; }=new();
        public List<SpaceRoutePoint> Route { get; set; }=new();
        public int CompletedLegs { get; set; }
        public HashSet<string> Kills { get; set; }=new();
        public HashSet<string> Surveys { get; set; }=new();
        public HashSet<string> Receipts { get; set; }=new();
        public HashSet<string> Sites { get; set; }=new();
        public Dictionary<string,string> EncounterObjectives { get; set; }=new();
        public double RecoveredOre { get; set; }
        public double RecoveredBulk { get; set; }
        public double UsedAttempts { get; set; }
        public double RescueRecovery { get; set; }
        public bool Boarded { get; set; }
        public string BoardingEncounterId { get; set; }
        public DateTime BoardingStartedAt { get; set; }
        public DateTime BoardingEndsAt { get; set; }
        public HashSet<string> BoardingPlayers { get; set; }=new();
        public Dictionary<int,string> BoardingConsoleOperators {get;set;}=new();
        public HashSet<int> BoardingConsoles {get;set;}=new();
        public Dictionary<string,SpaceRoutePoint> BoardingReturnPositions {get;set;}=new();
        public SpaceContributionLedger Contributions { get; set; }=new();
    }
}
