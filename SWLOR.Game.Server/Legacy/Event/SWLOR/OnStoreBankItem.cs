using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
