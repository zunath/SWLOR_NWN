namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Who and what a recorded change belongs to: the acting creature plus the ability, status
    /// effect, item or rider that caused it.
    /// </summary>
    public struct CombatRecordAttribution
    {
        public uint Actor;
        public CombatRecordSourceKind Kind;
        public string Name;
        public int Code;
        public int Rank;

        /// <summary>
        /// For a status effect, the creature carrying it. Its reactions belong to the status even
        /// when another creature applied it.
        /// </summary>
        public uint Owner;

        /// <summary>
        /// The recording that captured this attribution, so one stored for later (a trap) is not
        /// replayed into a later recording.
        /// </summary>
        public int Recording;

        public CombatRecordAttribution(uint actor, CombatRecordSourceKind kind, string name, int code, int rank, uint owner = 0)
        {
            Actor = actor;
            Kind = kind;
            Name = name;
            Code = code;
            Rank = rank;
            Owner = owner;
            Recording = 0;
        }
    }
}
