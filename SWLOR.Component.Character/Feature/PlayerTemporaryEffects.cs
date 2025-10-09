using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Feature
{
    public class PlayerTemporaryEffects
    {
        private readonly IDatabaseService _db;
        private readonly ICreaturePluginService _creaturePlugin;
        

        public PlayerTemporaryEffects(
            IDatabaseService db, 
            ICreaturePluginService creaturePlugin)
        {
            _db = db;
            _creaturePlugin = creaturePlugin;
        }

        [ScriptHandler<OnModuleEnter>]
        public void ApplyTemporaryEffects()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            ApplyCutsceneGhostToPlayer(player);
            ApplyHeight(player);
            RemoveImmobility(player);
            ReapplySpeed(player);
        }

        private void ApplyCutsceneGhostToPlayer(uint player)
        {
            var effect = SupernaturalEffect(EffectCutsceneGhost());
            ApplyEffectToObject(DurationType.Permanent, effect, player);
        }

        private void ApplyHeight(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            SetObjectVisualTransform(player, ObjectVisualTransformType.Scale, dbPlayer.AppearanceScale);
        }

        private void RemoveImmobility(uint player)
        {
            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectType(effect) == EffectScriptType.CutsceneImmobilize)
                {
                    RemoveEffect(player, effect);
                }
            }
        }


        private void ReapplySpeed(uint player)
        {
            _creaturePlugin.SetMovementRate(player, MovementRateType.PC);
        }
    }
}
