using System.Collections.Generic;

namespace SWLOR.Game.Server.Caching.Contracts
{
    public interface ICache<out T>
        where T: class
    {
        IEnumerable<T> GetAll();
    }
}
