namespace SWLOR.Shared.Caching.Contracts
{
    public interface IPortraitCacheService
    {
        void LoadCache();
        int PortraitCount { get; }
        int GetPortraitByInternalId(int portraitInternalId);
        int GetPortraitInternalId(int portraitId);
        string GetPortraitResrefByInternalId(int portraitInternalId);
        int GetPortraitInternalIdByResref(string resref);
    }
}
