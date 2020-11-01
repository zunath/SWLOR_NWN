using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
