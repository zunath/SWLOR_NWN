namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// A single append-only audit record inside PlayerMasteryProfile.AuditLog. Written
    /// for every staff action: approve, deny, grant, revoke, reduce, quick-slot
    /// award/spend, and abandon.
    /// </summary>
    public class MasteryAuditEntry
    {
        public MasteryAuditEntry()
        {
            ActorName = string.Empty;
            ActorCDKey = string.Empty;
            Action = string.Empty;
            Reason = string.Empty;
        }

        public DateTime Date { get; set; }
        public string ActorName { get; set; }
        public string ActorCDKey { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
    }
}
