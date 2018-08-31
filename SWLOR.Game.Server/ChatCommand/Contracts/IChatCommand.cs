using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand.Contracts
{
    public interface IChatCommand
    {
        void DoAction(NWPlayer user, params string[] args);
    }
}
