using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>Fills a toolset-generated treasure container once, when it is first opened.</summary>
    public static class ProceduralTreasure
    {
        private const string FilledVariable = "PROCEDURAL_TREASURE_FILLED";

        [NWNEventHandler(ScriptName.OnProceduralTreasureOpened)]
        public static void OnOpened()
        {
            var container = OBJECT_SELF;
            if (GetLocalBool(container, FilledVariable))
                return;

            SetLocalBool(container, FilledVariable, true);
            Loot.SpawnLoot(container, container, "LOOT_TABLE_");
        }
    }
}
