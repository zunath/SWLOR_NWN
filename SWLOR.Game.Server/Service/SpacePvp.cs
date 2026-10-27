using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Player ships fighting player ships (plan section 9, PvP). A player ship may attack another player ship only
    /// when the ground rules allow it: the attacker has the target marked hostile in the player list
    /// (<see cref="PlayerList.IsHostileTo"/>) and the area's PvP setting allows players to fight. Ships against NPC
    /// ships keep the engine's reaction. The PvP damage ramp and the halved hard crowd control are the ground's:
    /// the damage hook calls <see cref="PvpCombat"/> and statuses go through <c>StatusEffect</c>.
    /// </summary>
    public static partial class Space
    {
        private const string KillerVariable = "SPACE_SHIP_KILLER";

        /// <summary>
        /// The PvP decision for an attack by one ship on another. <see cref="SpacePvpDecision.NotPlayerVersusPlayer"/>
        /// when either side is not a player.
        /// </summary>
        public static SpacePvpDecision GetPvpDecision(uint attacker, uint target)
        {
            var isPvp = PvpCombat.IsPlayerAgainstPlayer(attacker, target);
            if (!isPvp)
                return SpacePvpDecision.NotPlayerVersusPlayer;

            var decision = SpacePvpRules.CanAttack(
                true,
                PlayerList.IsHostileTo(attacker, target),
                GetEffectivePvpSetting(GetArea(target)),
                Party.IsInParty(attacker, target));

            // The ground ability check (reputation, the area PvP setting and the Like/Dislike marks) must agree.
            return decision == SpacePvpDecision.Allowed && !GetIsReactionTypeHostile(target, attacker)
                ? SpacePvpDecision.NotHostile
                : decision;
        }

        /// <summary>Whether a ship may attack another ship. Always true unless two players' ships are involved.</summary>
        public static bool CanShipAttack(uint attacker, uint target)
        {
            return SpacePvpRules.IsAllowed(GetPvpDecision(attacker, target));
        }

        /// <summary>
        /// Whether the source treats the target ship as an enemy it can attack: the engine's reaction for NPCs, and
        /// the PvP rules between two players.
        /// </summary>
        public static bool IsShipEnemy(uint source, uint target)
        {
            return PvpCombat.IsPlayerAgainstPlayer(source, target)
                ? CanShipAttack(source, target)
                : GetIsEnemy(target, source);
        }

        /// <summary>
        /// Whether a support effect (remote repair, capacitor transfer, Command orders) must skip the target: an
        /// enemy, or a player either side has marked hostile.
        /// </summary>
        public static bool IsShipHostile(uint source, uint target)
        {
            var isPvp = PvpCombat.IsPlayerAgainstPlayer(source, target);
            return SpacePvpRules.IsHostile(
                isPvp,
                GetIsEnemy(target, source),
                isPvp && PlayerList.IsHostileTo(source, target),
                isPvp && PlayerList.IsHostileTo(target, source));
        }

        /// <summary>The message that explains why an attack was stopped, or an empty string when it is allowed.</summary>
        public static string GetPvpRefusal(uint attacker, uint target)
        {
            return SpacePvpRules.GetRefusal(GetPvpDecision(attacker, target));
        }

        /// <summary>
        /// Applies the PvP damage ramp to ship damage a player's ship deals to another player's ship, and keeps
        /// both players in PvP combat, as the ground damage pipeline does.
        /// </summary>
        private static int ApplyPvpDamageRamp(uint attacker, uint target, int amount)
        {
            if (amount <= 0 || !PvpCombat.IsPlayerAgainstPlayer(attacker, target))
                return amount;

            PvpCombat.RecordHit(attacker, target);
            return SpacePvpRules.ApplyRamp(amount, true, PvpCombat.GetDamageIncreasePercent(target));
        }

        /// <summary>The area's PvP setting; a server-default area takes the server's.</summary>
        private static PvPSetting GetEffectivePvpSetting(uint area)
        {
            var areaSetting = GetIsObjectValid(area) ? AreaPlugin.GetPVPSetting(area) : PvPSetting.ServerDefault;
            var serverSetting = (PvPSetting)AdministrationPlugin.GetPlayOptionValue(AdministrationOption.PvpSetting);
            return SpacePvpRules.ResolveAreaSetting(areaSetting, serverSetting);
        }

        /// <summary>Remembers who delivered the blow that destroyed a ship, for the death rules.</summary>
        private static void RecordShipKiller(uint target, uint attacker)
        {
            if (!GetIsPC(target) || !GetIsObjectValid(attacker))
                return;

            SetLocalObject(target, KillerVariable, attacker);
        }

        /// <summary>The creature that destroyed a ship: the recorded killing blow, otherwise the last hostile actor.</summary>
        private static uint TakeShipKiller(uint target)
        {
            var killer = GetLocalObject(target, KillerVariable);
            DeleteLocalObject(target, KillerVariable);
            return GetIsObjectValid(killer) ? killer : GetLastHostileActor(target);
        }
    }
}
