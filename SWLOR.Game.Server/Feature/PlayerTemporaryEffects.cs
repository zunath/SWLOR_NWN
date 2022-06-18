using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public class PlayerTemporaryEffects
    {
        [NWNEventHandler("mod_enter")]
        public static void ApplyTemporaryEffects()
        {
            var player = GetEnteringObject();
            ApplyCutsceneGhostToPlayer(player);
            ApplyHeight(player);
            RemoveImmobility(player);
        }

        private static void ApplyCutsceneGhostToPlayer(uint player)
        {
            var effect = SupernaturalEffect(EffectCutsceneGhost());
            ApplyEffectToObject(DurationType.Permanent, effect, player);
        }

        private static void ApplyHeight(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

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

        /// <summary>
        /// Whenever a player unequips an item, if they have any action modes enabled, disable them.
        /// This works around issues 
        /// </summary>
        [NWNEventHandler("mod_unequip")]
        public static void RemoveEquipmentEffects()
        {
            DisableRapidShotMode();
        }

        private static void DisableRapidShotMode()
        {
            var player = GetPCItemLastUnequippedBy();
            if (!GetIsPC(player))
                return;

            if(GetActionMode(player, ActionMode.RapidShot))
                SetActionMode(player, ActionMode.RapidShot, false);
        }
    }
}
