namespace SWLOR.Shared.Abstractions.Contracts;

public interface IItemCacheService
{
    void CacheItemNamesByResref();
    string GetItemNameByResref(string resref);
}
