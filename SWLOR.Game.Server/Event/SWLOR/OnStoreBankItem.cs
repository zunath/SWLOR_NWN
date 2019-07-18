using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnStoreBankItem
    {
        public NWPlayer Player { get; set; }
        public BankItem Entity { get; set; }

        public OnStoreBankItem(NWPlayer player, BankItem entity)
        {
            Player = player;
            Entity = entity;
        }
    }
}
