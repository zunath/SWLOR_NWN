using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// What the player list knows about one online character before it is resolved for a particular viewer.
    /// Built once per refresh so viewers share the database reads.
    /// </summary>
    public class PlayerListSubject
    {
        public uint Player { get; set; }
        public bool IsDM { get; set; }
        public string CanonicalName { get; set; }
        public string Descriptor { get; set; }
        public bool IsDisguised { get; set; }
        public IReadOnlyCollection<PlayerListFlagType> Flags { get; set; }
        public uint PartyLeader { get; set; }
        public int PartyPlayerCount { get; set; }
    }
}
