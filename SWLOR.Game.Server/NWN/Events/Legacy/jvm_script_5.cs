using SWLOR.Game.Server.Event.Legacy;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Legacy
{
#pragma warning disable IDE1006 // Naming Styles
    internal class jvm_script_5
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<LegacyJVMEvent>("JAVA_SCRIPT_5");
        }
    }
}