using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Feature
{
    public static class CraftingLifecycle
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnPlayerEnter()
        {
            var player = GetEnteringObject();
            if (GetIsPC(player) && !GetIsDM(player)) DelayCommand(1f, () => CraftViewModel.RecoverForPlayer(player));
        }

        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void OnPlayerRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            DelayCommand(1f, () => CraftViewModel.RecoverForPlayer(player));
        }

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
