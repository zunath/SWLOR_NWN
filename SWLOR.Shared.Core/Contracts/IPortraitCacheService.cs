namespace SWLOR.Shared.Core.Contracts;

public interface IPortraitCacheService
{
    int PortraitCount { get; }
    int GetPortraitByInternalId(int portraitInternalId);
    int GetPortraitInternalId(int portraitId);
    string GetPortraitResrefByInternalId(int portraitInternalId);
    int GetPortraitInternalIdByResref(string resref);
}
