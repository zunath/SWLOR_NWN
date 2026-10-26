namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// How the player viewing the list relates to the selected player, for deciding which actions apply.
    /// </summary>
    public class PlayerListRelation
    {
        public bool ViewerIsDM { get; set; }
        public bool ViewerIsInParty { get; set; }
        public bool ViewerIsPartyLeader { get; set; }
        public bool ViewerIsInSpace { get; set; }
        public PlayerListCallState ViewerCallState { get; set; }

        public bool HasTarget { get; set; }
        public bool TargetIsSelf { get; set; }
        public bool TargetIsDM { get; set; }
        public bool TargetIsInViewerParty { get; set; }
        public bool TargetIsInOtherParty { get; set; }
        public bool TargetIsPartyLeader { get; set; }
        public bool ViewerDislikesTarget { get; set; }
        public bool TargetIsInSpace { get; set; }
        public bool TargetIsInCall { get; set; }
    }
}
