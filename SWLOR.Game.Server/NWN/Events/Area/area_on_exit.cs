using SWLOR.Game.Server.Event.Area;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Area
{
#pragma warning disable IDE1006 // Naming Styles
    internal class area_on_exit
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            App.RunEvent<OnAreaExit>();
        }
    }
}
