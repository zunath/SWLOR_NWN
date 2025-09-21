namespace SWLOR.Game.Server.Service;

public interface IPortraitCacheService
{
    int PortraitCount { get; }
    int GetPortraitByInternalId(int portraitInternalId);
    int GetPortraitInternalId(int portraitId);
    string GetPortraitResrefByInternalId(int portraitInternalId);
    int GetPortraitInternalIdByResref(string resref);
}
