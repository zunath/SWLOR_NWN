using System;

namespace SWLOR.Game.Server.Enumeration
{
    [Flags]
    public enum ServerEnvironmentType
    {
        Invalid = 0,
        Development = 1,
        Production = 2,
        Test = 4,

        All = Development | Production | Test
    }
}
