namespace SWLOR.Shared.Caching.Contracts
{
    public interface IItemCacheService
    {
        void CacheItemNamesByResref();
        string GetItemNameByResref(string resref);
    }
}
