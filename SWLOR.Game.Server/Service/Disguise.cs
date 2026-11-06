using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DisguiseService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the disguises. The logic lives in <see cref="IDisguiseService"/>; the login handler and the
    /// constants stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Disguise
    {
        public const string IdentityKeyPrefix = Domain.Character.DisguiseService.IdentityKeyPrefix;
        public const int WipeCreditCost = Domain.Character.DisguiseService.WipeCreditCost;
        public const int WipeRoleplayXPCost = Domain.Character.DisguiseService.WipeRoleplayXPCost;
        public const int ActivationDelayMinutes = Domain.Character.DisguiseService.ActivationDelayMinutes;
        public const int MinimumActivationDelayMinutes = Domain.Character.DisguiseService.MinimumActivationDelayMinutes;

        public const int MaxPrivateNameLength = Domain.Character.DisguiseService.MaxPrivateNameLength;
        public const int MaxBiographyLength = Domain.Character.DisguiseService.MaxBiographyLength;

        private static IDisguiseService Instance => FacadeAccessor.Resolve<IDisguiseService>();

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyActiveDisguiseOnEnter()
        {
            Instance.ApplyActiveDisguiseOnEnter();
        }

        public static string BuildIdentityKey(string disguiseId)
        {
            return Instance.BuildIdentityKey(disguiseId);
        }

        public static bool IsDisguiseIdentityKey(string identityKey)
        {
            return Instance.IsDisguiseIdentityKey(identityKey);
        }

        public static string GetIdentityKey(uint player)
        {
            return Instance.GetIdentityKey(player);
        }

        public static string GetDisplayDescriptor(uint player)
        {
            return Instance.GetDisplayDescriptor(player);
        }

        public static bool ShouldScrambleAccountName(uint player)
        {
            return Instance.ShouldScrambleAccountName(player);
        }

        public static PlayerDisguise GetActiveDisguise(uint player)
        {
            return Instance.GetActiveDisguise(player);
        }

        public static PlayerDisguise GetActiveDisguise(Player dbPlayer)
        {
            return Instance.GetActiveDisguise(dbPlayer);
        }

        public static List<PlayerDisguise> GetDisguises(string playerId, bool retired)
        {
            return Instance.GetDisguises(playerId, retired);
        }

        public static int GetDisguiseSlotLimit(uint player, Player dbPlayer)
        {
            return Instance.GetDisguiseSlotLimit(player, dbPlayer);
        }

        public static int CalculateDisguiseSlotLimit(int baseLimit, int additionalSlots)
        {
            return Instance.CalculateDisguiseSlotLimit(baseLimit, additionalSlots);
        }

        /// <summary>
        /// The delay this player must wait between disguise activations, after perk reductions.
        /// </summary>
        public static TimeSpan GetActivationDelay(uint player)
        {
            return Instance.GetActivationDelay(player);
        }

        /// <summary>
        /// Applies a cooldown reduction percent to the base activation delay. The result never drops
        /// below <see cref="MinimumActivationDelayMinutes"/> so that stacked reduction sources cannot
        /// remove the delay entirely and let a player cycle identities faster than they can be observed.
        /// </summary>
        public static TimeSpan CalculateActivationDelay(int reductionPercent)
        {
            return Instance.CalculateActivationDelay(reductionPercent);
        }

        public static int CountUsedSlots(string playerId)
        {
            return Instance.CountUsedSlots(playerId);
        }

        public static PlayerDisguise CreateDisguise(uint player)
        {
            return Instance.CreateDisguise(player);
        }

        public static SaveDisguiseResult SaveDisguise(
            uint player,
            string disguiseId,
            string privateName,
            string descriptor,
            string biography,
            int portraitInternalId,
            int soundSetId,
            bool scrambleAccountId)
        {
            return Instance.SaveDisguise(player, disguiseId, privateName, descriptor, biography,
                portraitInternalId, soundSetId, scrambleAccountId);
        }

        public static ActivateDisguiseResult Activate(uint player, string disguiseId)
        {
            return Instance.Activate(player, disguiseId);
        }

        public static bool Deactivate(uint player)
        {
            return Instance.Deactivate(player);
        }

        public static bool Retire(uint player, string disguiseId)
        {
            return Instance.Retire(player, disguiseId);
        }

        public static bool Unretire(uint player, string disguiseId)
        {
            return Instance.Unretire(player, disguiseId);
        }

        public static DeleteRetiredDisguiseResult DeleteRetiredDisguise(
            uint player,
            string disguiseId,
            DisguisePaymentMethod paymentMethod)
        {
            return Instance.DeleteRetiredDisguise(player, disguiseId, paymentMethod);
        }

        public static int ResetActivationCooldowns(uint player, uint actor = OBJECT_INVALID)
        {
            return Instance.ResetActivationCooldowns(player, actor);
        }

        public static string GetPortraitResref(PlayerDisguise disguise)
        {
            return Instance.GetPortraitResref(disguise);
        }

        public static string GetSoundSetName(int soundSetId)
        {
            return Instance.GetSoundSetName(soundSetId);
        }
    }
}
