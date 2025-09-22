using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Feature
{
    public class PlayerTemporaryEffects
    {
        private readonly IDatabaseService _db;
        private readonly IStatService _statService;

        public PlayerTemporaryEffects(IDatabaseService db, IStatService statService)
        {
            _db = db;
            _statService = statService;
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
            ReapplyBAB(player);
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

            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
        }

        private void RemoveImmobility(uint player)
        {
            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectType(effect) == EffectTypeScript.CutsceneImmobilize)
                {
                    RemoveEffect(player, effect);
                }
            }
        }

        private void ReapplyBAB(uint player)
        {
            _statService.ApplyAttacksPerRound(player, GetItemInSlot(InventorySlot.RightHand, player));
        }

        private void ReapplySpeed(uint player)
        {
            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
        }
    }
}
