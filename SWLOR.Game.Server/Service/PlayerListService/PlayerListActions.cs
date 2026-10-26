namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// The state of every player list action for one viewer and selection.
    /// </summary>
    public class PlayerListActions
    {
        public PlayerListActionState SendTell { get; set; }
        public PlayerListActionState ToggleHostile { get; set; }
        public string ToggleHostileText { get; set; }
        public PlayerListActionState Call { get; set; }
        public PlayerListActionState AnswerCall { get; set; }
        public PlayerListActionState DeclineCall { get; set; }
        public PlayerListActionState EndCall { get; set; }
        public string EndCallText { get; set; }
        public PlayerListActionState Invite { get; set; }
        public PlayerListActionState Remove { get; set; }
        public PlayerListActionState MakeLeader { get; set; }
        public PlayerListActionState LeaveParty { get; set; }
        public PlayerListActionState JumpTo { get; set; }
        public PlayerListActionState JumpHere { get; set; }
        public PlayerListActionState Examine { get; set; }
        public PlayerListActionState Boot { get; set; }
    }
}
