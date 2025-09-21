namespace SWLOR.Game.Server.Service;

public interface IItemCacheService
{
    void CacheItemNamesByResref();
    string GetItemNameByResref(string resref);
}
