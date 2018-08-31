using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IFoodService
    {
        PlayerCharacter RunHungerCycle(NWPlayer pc, PlayerCharacter entity);
        PlayerCharacter ApplyHungerPenalties(PlayerCharacter entity, NWPlayer pc);
        void IncreaseHungerLevel(NWPlayer oPC, int amount, bool isTainted);
        PlayerCharacter DecreaseHungerLevel(PlayerCharacter entity, NWPlayer oPC, int amount);
        void DecreaseHungerLevel(NWPlayer oPC, int amount);
    }
}
