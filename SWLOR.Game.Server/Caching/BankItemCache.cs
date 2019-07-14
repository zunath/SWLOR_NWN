using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BankItemCache: CacheBase<BankItem>
    {
        protected override void OnCacheObjectSet(BankItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(BankItem entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BankItem GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
