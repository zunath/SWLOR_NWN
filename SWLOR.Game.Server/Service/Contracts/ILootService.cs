using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ILootService
    {
        ItemVO PickRandomItemFromLootTable(int lootTableID);
        void OnCreatureDeath(NWCreature creature);
    }
}
