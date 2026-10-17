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
        public PlayerListActionState Invite { get; set; }
        public PlayerListActionState Remove { get; set; }
        public PlayerListActionState MakeLeader { get; set; }
        public PlayerListActionState LeaveParty { get; set; }
    }
}
