using NWN.Native.API;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Composition.GameServices;
using static SWLOR.Game.Server.Native.ResolveAttackRoll;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The combat log of a native weapon roll: the attack message each side sees, and the deflection message.
    /// </summary>
    public static class AttackRollFeedback
    {
        internal static void SendAttackFeedback(CNWSCreature attacker, CNWSCreature defender,
            CNWSCombatAttackData pAttackData, int hitRate, SkillType weaponSkillType, DeflectionSource deflectionSource)
        {
            Log.Write(LogGroup.Attack, $"Building combat log message");
            var attackerMessage = BuildAttackFeedbackMessage(
                attacker.m_idSelf,
                attacker,
                defender,
                pAttackData.m_nAttackResult,
                hitRate,
                weaponSkillType,
                deflectionSource);
            var defenderMessage = BuildAttackFeedbackMessage(
                defender.m_idSelf,
                attacker,
                defender,
                pAttackData.m_nAttackResult,
                hitRate,
                weaponSkillType,
                deflectionSource);
            attacker.SendFeedbackString(new CExoString(attackerMessage));
            defender.SendFeedbackString(new CExoString(defenderMessage));
        }

        private static string BuildAttackFeedbackMessage(
            uint observer,
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int hitRate,
            SkillType weaponSkillType,
            DeflectionSource deflectionSource)
        {
            if (IsSuccessfulAttackResult(attackResultType) &&
                UsePerkFeat.TryGetQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, out var queuedAbility, attacker.m_pcCombatRound.m_nCurrentAttack))
            {
                return GameServices.CombatFeedback.BuildAbilityCombatLogMessage(
                    observer,
                    attacker.m_idSelf,
                    defender.m_idSelf,
                    queuedAbility.Name,
                    attackResultType,
                    hitRate);
            }

            return GameServices.CombatFeedback.BuildCombatLogMessageNative(
                observer,
                attacker,
                defender,
                attackResultType,
                hitRate,
                deflectionSource);
        }

        internal static string BuildDeflectionFeedback(uint observer, CNWSCreature attacker, CNWSCreature defender, string deflectionName)
        {
            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker.m_idSelf);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender.m_idSelf);

            return ColorToken.Combat($"{defenderName}'s {deflectionName} negates {attackerName}'s attack.");
        }
    }
}
