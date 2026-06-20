namespace SWLOR.Game.Server.Service.PropertyService
{
    public class PropertyLoadDiagnostic
    {
        public string PropertyId { get; set; }
        public string Name { get; set; }
        public string OwnerPlayerId { get; set; }
        public PropertyType PropertyType { get; set; }
        public PropertyLoadType LoadType { get; set; }
        public PropertyLoadState State { get; set; }
        public string QueuePriority { get; set; }
        public int SpawnedChildCount { get; set; }
        public int ExpectedChildCount { get; set; }
        public bool IsLoadedAreaValid { get; set; }
        public string LastPhase { get; set; }
        public string Failure { get; set; }
        public int WaiterCount { get; set; }
        public bool IsQueued { get; set; }
    }
}
