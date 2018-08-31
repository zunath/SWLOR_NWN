using SWLOR.Game.Server.Placeable.CraftingForge;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Craft
{
#pragma warning disable IDE1006 // Naming Styles
    internal class cft_finish_smelt
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            App.RunEvent<FinishSmelt>();
        }
    }
}
