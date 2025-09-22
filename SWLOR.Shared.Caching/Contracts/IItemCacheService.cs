namespace SWLOR.Shared.Core.Contracts
{
    public interface IItemCacheService
    {
        void CacheItemNamesByResref();
        string GetItemNameByResref(string resref);
    }
}
