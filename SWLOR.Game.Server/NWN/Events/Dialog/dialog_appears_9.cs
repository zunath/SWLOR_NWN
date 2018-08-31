using SWLOR.Game.Server.Event.Dialog;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Dialog
{
#pragma warning disable IDE1006 // Naming Styles
    internal class dialog_appears_9
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static int Main()
        {
            return App.RunEvent<AppearsWhen>(2, 9) ? 1 : 0;
        }
    }
}