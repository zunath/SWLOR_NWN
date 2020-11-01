using System.Collections.Generic;

namespace SWLOR.Game.Server.Legacy.Caching.Contracts
{
    public interface ICache<out T>
        where T: class
    {
        IEnumerable<T> GetAll();
    }
}
