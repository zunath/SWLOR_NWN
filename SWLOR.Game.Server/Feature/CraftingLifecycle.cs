using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Feature
{
    public static class CraftingLifecycle
    {
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            if (GetIsPC(player))
                CraftViewModel.CloseForPlayer(player);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath() => CraftViewModel.CloseForPlayer(GetLastPlayerDied());
    }
}
