using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.StorageContainer
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly IStorageService _storage;

        public OnDisturbed(IStorageService storage)
        {
            _storage = storage;
        }

        public bool Run(params object[] args)
        {
            _storage.OnChestDisturbed((Object.OBJECT_SELF));
            return true;
        }
    }
}
