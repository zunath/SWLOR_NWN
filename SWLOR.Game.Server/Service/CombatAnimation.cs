using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Handles the lightsaber "Forms" combat animation system used by the /combatgui panel.
    ///
    /// IMPORTANT (see handoff): the real pose animations are NOT all produced yet. Animations are
    /// invoked through an animation slot override (ReplaceObjectAnimation) + play, and reverted by
    /// clearing the override on that slot. Every variant/resref is only ever played if it is present
    /// in <see cref="ActiveResrefs"/> (a runtime whitelist). Anything not whitelisted is a safe no-op
    /// that simply informs the player it is not available yet, so we never try to play a resref that
    /// may not exist in the module.
    /// </summary>
    public static class CombatAnimation
    {
        // Base animation slot that gets overridden. The same scheme is reused by other systems
        // (see KatarAnimationRemap) so we stay consistent with the existing override approach.
        private const string BaseSlot = "pause1";

        // Local variable used to track which slots have been overridden for a given player so that
        // the RESET button can revert every touched slot, not just the last one.
        private const string OverriddenSlotsVariable = "COMBAT_ANIM_SLOTS";

        // The base animation that plays on the overridden slot.
        private const Animation SlotAnimation = Animation.LoopingPause;

        /// <summary>
        /// Runtime whitelist of animation resrefs that actually exist in the module and are safe to
        /// play. This is intentionally EMPTY until real pose animations are shipped. To activate a
        /// form/role once its .mdl assets exist, add the corresponding resrefs here (or verify them
        /// at runtime). While empty, every combat form behaves as a selectable placeholder.
        /// </summary>
        public static readonly HashSet<string> ActiveResrefs = new(StringComparer.OrdinalIgnoreCase)
        {
            // Example (kept commented until Makashi assets are actually present in the module):
            // "MAK_GRD1", "MAK_GRD2", "MAK_GRD3",
            // "MAK_ATK1", "MAK_ATK2", "MAK_ATK3",
            // "MAK_DEF1", "MAK_DEF2", "MAK_DEF3",
        };

        public enum CombatRole
        {
            Stance = 0,
            Attack = 1,
            Defense = 2
        }

        public class CombatFormDetail
        {
            public string Name { get; set; }
            public string ShortName { get; set; }
            public string Code { get; set; }
            public List<string> StanceVariants { get; set; } = new();
            public List<string> AttackVariants { get; set; } = new();
            public List<string> DefenseVariants { get; set; } = new();

            public List<string> GetVariants(CombatRole role)
            {
                return role switch
                {
                    CombatRole.Stance => StanceVariants,
                    CombatRole.Attack => AttackVariants,
                    CombatRole.Defense => DefenseVariants,
                    _ => new List<string>()
                };
            }
        }

        // The 9 lightsaber Forms. Variant names follow the <FORM>_<ROLE><VAR> convention (<= 16 chars).
        // 3 variants per role are declared so the UI/random-roll flow can be validated even before the
        // matching assets exist.
        public static readonly List<CombatFormDetail> Forms = BuildForms();

        private static List<CombatFormDetail> BuildForms()
        {
            var definitions = new (string Name, string Short, string Code)[]
            {
                ("Shii-Cho", "Shii", "SHC"),
                ("Makashi", "Mak", "MAK"),
                ("Soresu", "Sor", "SOR"),
                ("Ataru", "Ata", "ATA"),
                ("Shien", "Shie", "SHI"),
                ("Djem So", "Djem", "DJS"),
                ("Niman", "Nim", "NIM"),
                ("Juyo", "Juyo", "JUY"),
                ("Vaapad", "Vaap", "VAA"),
            };

            var forms = new List<CombatFormDetail>();
            foreach (var (name, shortName, code) in definitions)
            {
                var form = new CombatFormDetail
                {
                    Name = name,
                    ShortName = shortName,
                    Code = code
                };

                for (var i = 1; i <= 3; i++)
                {
                    form.StanceVariants.Add($"{code}_GRD{i}");
                    form.AttackVariants.Add($"{code}_ATK{i}");
                    form.DefenseVariants.Add($"{code}_DEF{i}");
                }

                forms.Add(form);
            }

            return forms;
        }

        public static string GetRoleName(CombatRole role)
        {
            return role switch
            {
                CombatRole.Stance => "Stance",
                CombatRole.Attack => "Attack",
                CombatRole.Defense => "Defense",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns true only if the resref is present in the runtime whitelist and therefore safe to play.
        /// </summary>
        public static bool IsResrefAvailable(string resref)
        {
            return !string.IsNullOrWhiteSpace(resref) && ActiveResrefs.Contains(resref);
        }

        /// <summary>
        /// Starts a custom combat animation by overriding the base slot and playing it. If the resref is
        /// not whitelisted (asset not shipped yet), this is a safe no-op that tells the player it is not
        /// available. The overridden slot is tracked so RESET can revert it later.
        /// </summary>
        public static void PlayCombat(uint player, string resref)
        {
            if (!IsResrefAvailable(resref))
            {
                SendMessageToPC(player, $"L'animazione '{resref}' non è ancora disponibile.");
                return;
            }

            // Start: override + play.
            ReplaceObjectAnimation(player, BaseSlot, resref);
            TrackOverriddenSlot(player, BaseSlot);

            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionPlayAnimation(SlotAnimation, 1f, 9999.9f);
            });
        }

        /// <summary>
        /// Plays a standard (built-in) looping animation, used by the Guns branch where the animations
        /// are regular game animations rather than custom slot overrides.
        /// </summary>
        public static void PlayStandardLooping(uint player, Animation animation)
        {
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionPlayAnimation(animation, 1f, 9999.9f);
            });
        }

        /// <summary>
        /// Emergency RESET: reverts every slot that was overridden for this player and returns them to
        /// the default idle. Works regardless of what the player has (or hasn't) done in the panel.
        /// </summary>
        public static void ResetAll(uint player)
        {
            var slots = GetOverriddenSlots(player);
            foreach (var slot in slots)
            {
                // Stop: revert the base slot override.
                ReplaceObjectAnimation(player, slot, string.Empty);
            }

            DeleteLocalString(player, OverriddenSlotsVariable);

            AssignCommand(player, () =>
            {
                ClearAllActions(true);
                ActionPlayAnimation(Animation.LoopingPause, 1f, 1f);
            });
        }

        private static IEnumerable<string> GetOverriddenSlots(uint player)
        {
            var raw = GetLocalString(player, OverriddenSlotsVariable);
            if (string.IsNullOrWhiteSpace(raw))
                return Enumerable.Empty<string>();

            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct();
        }

        private static void TrackOverriddenSlot(uint player, string slot)
        {
            var slots = GetOverriddenSlots(player).ToList();
            if (!slots.Contains(slot))
            {
                slots.Add(slot);
                SetLocalString(player, OverriddenSlotsVariable, string.Join(",", slots));
            }
        }
    }
}
