using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnRemoveBankItem
    {
        public NWPlayer Player { get; set; }
        public BankItem Entity { get; set; }

        public OnRemoveBankItem(NWPlayer player, BankItem entity)
        {
            Player = player;
            Entity = entity;
        }
    }
}
