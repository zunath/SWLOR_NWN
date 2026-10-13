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

        public CombatRecordAttribution(uint actor, CombatRecordSourceKind kind, string name, int code, int rank)
        {
            Actor = actor;
            Kind = kind;
            Name = name;
            Code = code;
            Rank = rank;
        }
    }
}
