
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public class PlayerTemporaryEffects
    {
        [ScriptHandler(ScriptName.OnModuleEnter)]
        public static void ApplyTemporaryEffects()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            ApplyCutsceneGhostToPlayer(player);
            ApplyHeight(player);
            RemoveImmobility(player);
            ReapplyBAB(player);
            ReapplySpeed(player);
        }

        private static void ApplyCutsceneGhostToPlayer(uint player)
        {
            var effect = SupernaturalEffect(EffectCutsceneGhost());
            ApplyEffectToObject(DurationType.Permanent, effect, player);
        }

        private static void ApplyHeight(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
        }

        private static void RemoveImmobility(uint player)
        {
            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectType(effect) == EffectTypeScript.CutsceneImmobilize)
                {
                    RemoveEffect(player, effect);
                }
            }
        }

        private static void ReapplyBAB(uint player)
        {
            Stat.ApplyAttacksPerRound(player, GetItemInSlot(InventorySlot.RightHand, player));
        }

        private static void ReapplySpeed(uint player)
        {
            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
        }
    }
}
