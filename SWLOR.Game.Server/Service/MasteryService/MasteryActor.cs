namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// Identifies the staff member (or player, for self-service abandon) performing a
    /// mutating action so MasteryRules can append a properly-attributed audit entry
    /// without needing a live NWN object reference.
    /// </summary>
    public class MasteryActor
    {
        public string Name { get; set; }
        public string CDKey { get; set; }

        public MasteryActor(string name, string cdKey)
        {
            Name = name;
            CDKey = cdKey;
        }
    }
}
