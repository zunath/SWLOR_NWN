using System.Linq;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Combat events raised by space rolls, for perk code that reacts to them. Handlers run inline and a handler
    /// that throws is logged and skipped.
    ///
    /// Ground perks keep working through the hooks they already listen to: a missed shot calls
    /// <c>Combat.TrackAvoidedAttack</c> on the target, a critical hit calls <c>Combat.ApplyCriticalHitEffects</c>
    /// for the attacker, and every weapon shot calls <c>Combat.TrackAttackActivity</c>. Anything space-only
    /// (consecutive hits, hull thresholds, ability use) subscribes here.
    ///
    /// <list type="bullet">
    /// <item><see cref="ShotEvaded"/>: a shot missed its target's evasion. Blind misses do not count.</item>
    /// <item><see cref="ShotLanded"/>: a shot hit (<c>Result.IsCritical</c> for a critical) and its damage has been applied.</item>
    /// <item><see cref="HullDamaged"/>: a ship lost hull points; ask <c>CrossedBelow(25)</c> for a threshold.</item>
    /// <item><see cref="HostileAbilityUsed"/>: a pilot's hostile space ability resolved its impact.</item>
    /// </list>
    /// </summary>
    public static class SpaceCombatEvents
    {
        public static event Action<SpaceShotEvent> ShotEvaded;
        public static event Action<SpaceShotEvent> ShotLanded;
        public static event Action<SpaceHullEvent> HullDamaged;
        public static event Action<SpaceAbilityEvent> HostileAbilityUsed;

        public static void RaiseShotEvaded(SpaceShotEvent e) => Raise(ShotEvaded, e);
        public static void RaiseShotLanded(SpaceShotEvent e) => Raise(ShotLanded, e);
        public static void RaiseHullDamaged(SpaceHullEvent e) => Raise(HullDamaged, e);
        public static void RaiseHostileAbilityUsed(SpaceAbilityEvent e) => Raise(HostileAbilityUsed, e);

        private static void Raise<T>(Action<T> handlers, T e)
        {
            if (handlers == null)
                return;

            foreach (var handler in handlers.GetInvocationList().Cast<Action<T>>())
            {
                try
                {
                    handler(e);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Space combat event handler failed: {ex}");
                }
            }
        }
    }
}
