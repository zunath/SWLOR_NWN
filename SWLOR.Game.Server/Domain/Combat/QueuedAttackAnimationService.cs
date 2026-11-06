using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Changes the engine's swing while a weapon ability is readied; never queues an extra attack. The logic that used
    /// to be static in <c>Service.QueuedAttackAnimation</c> is moved here unchanged; the playback and the equipment
    /// restore subscribers are this instance's own. The facade keeps the public event and forwards every call here.
    /// </summary>
    public class QueuedAttackAnimationService : IQueuedAttackAnimationService
    {
        private const string TokenVariable = "_QUEUED_ATTACK_ANIMATION";

        private readonly QueuedAttackAnimationPlayback _playback;
        private Action<uint> _restoreEquipmentAnimations;

        public QueuedAttackAnimationService()
        {
            _playback = new QueuedAttackAnimationPlayback(new NativeRuntime(),
                creature => _restoreEquipmentAnimations?.Invoke(creature));
        }

        public void AddRestoreEquipmentAnimationsHandler(Action<uint> handler) => _restoreEquipmentAnimations += handler;

        public void RemoveRestoreEquipmentAnimationsHandler(Action<uint> handler) => _restoreEquipmentAnimations -= handler;

        public void Begin(uint creature, AnimationClip clip)
        {
            _playback.Stop(creature);
            if (clip != null) _playback.Begin(creature, clip);
        }

        public void Stop(uint creature) => _playback.Stop(creature);

        public IReadOnlyCollection<string> GetActiveSwingKeys(uint creature) =>
            string.IsNullOrEmpty(GetLocalString(creature, TokenVariable))
                ? Array.Empty<string>()
                : _playback.SwingKeys;

        private sealed class NativeRuntime : INamedAnimationRuntime
        {
            public bool IsValid(uint creature) => GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;
            public string GetToken(uint creature) => GetLocalString(creature, TokenVariable);
            public void SetToken(uint creature, string token)
            {
                if (string.IsNullOrEmpty(token)) DeleteLocalString(creature, TokenVariable);
                else SetLocalString(creature, TokenVariable, token);
            }
            public void Replace(uint creature, string source, string replacement) => ReplaceObjectAnimation(creature, source, replacement);
            public ActionType CurrentAction(uint creature) => GetCurrentAction(creature);
            public void ClearActions(uint creature) => ClearAllActions(oObject: creature);
            public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
        }
    }
}
