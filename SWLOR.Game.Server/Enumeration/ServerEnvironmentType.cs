namespace SWLOR.Game.Server.Enumeration
{
    public enum ServerEnvironmentType
    {
        Invalid = 0,
        Development = 1,
        Production = 2,

        All = Development | Production
    }
}
