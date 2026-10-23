using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

namespace SWLOR.Game.Server.Feature
{
    public class PlayerTemporaryEffects
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyTemporaryEffects()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            ApplyCutsceneGhostToPlayer(player);
            ApplyHeight(player);
            ApplyHeadScale(player);
            RemoveStaleActivityImmobility(player);
            ReapplySpeed(player);
        }

        /// <summary>Lets other creatures walk through the player; the engine ignores it on an already ghosted player.</summary>
        public static void ApplyCutsceneGhostToPlayer(uint player)
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

        private static void ApplyHeadScale(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var headScale = dbPlayer.HeadAppearanceScale <= 0f ? 1.0f : dbPlayer.HeadAppearanceScale;

            HelmetModelRenderer.SetHeadScale(player, headScale);
        }

        private static void RemoveStaleActivityImmobility(uint player)
        {
            RemoveEffectByTag(player, PlayerActivityEffectTag.CraftingImmobilize);
            RemoveEffectByTag(player, PlayerActivityEffectTag.HoloComImmobilize);
            RemoveEffectByTag(player, PlayerActivityEffectTag.RefiningImmobilize);

            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectType(effect) == EffectTypeScript.CutsceneImmobilize &&
                    string.IsNullOrWhiteSpace(GetEffectTag(effect)))
                {
                    RemoveEffect(player, effect);
                }
            }
        }

        private static void ReapplySpeed(uint player)
        {
            Stat.ApplyCreatureMovementRate(player);
        }

        /// <summary>
        /// Death strips every effect; restores the login ghost and the crafting styles the player selected once the revive settles.
        /// Every revive (respawn, subdual, Resuscitation, a DM raise) calls this.
        /// </summary>
        public static void RestoreGhostAfterRevive(uint creature)
        {
            DelayCommand(0.1f, () =>
            {
                if (ShouldHaveGhost(creature) && !GetIsDead(creature))
                {
                    ApplyCutsceneGhostToPlayer(creature);
                    CraftingStyleSelection.Restore(creature);
                }
            });
        }

        /// <summary>Entering space mode strips every effect, so the login ghost returns on leaving it.</summary>
        [NWNEventHandler(ScriptName.OnSpaceExit)]
        public static void RestoreGhostOnSpaceExit()
        {
            // A pilot killed with their ship leaves space mode dead; their revive restores the ghost.
            var player = OBJECT_SELF;
            if (ShouldHaveGhost(player) && !GetIsDead(player))
                ApplyCutsceneGhostToPlayer(player);
        }

        private static bool ShouldHaveGhost(uint creature)
        {
            return GetIsObjectValid(creature) && GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
        }
    }
}
