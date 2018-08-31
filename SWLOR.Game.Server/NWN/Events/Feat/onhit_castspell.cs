using SWLOR.Game.Server.Event.Feat;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Feat
{
#pragma warning disable IDE1006 // Naming Styles
    internal class onhit_castspell
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<OnHitCastSpell>();
        }
    }
}