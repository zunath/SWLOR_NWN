namespace SWLOR.Game.Server.Service.LogService
{
    public enum ServerEnvironment
    {
        Invalid = 0,
        Development = 1,
        Production = 2,

        All = Development | Production
    }
}
