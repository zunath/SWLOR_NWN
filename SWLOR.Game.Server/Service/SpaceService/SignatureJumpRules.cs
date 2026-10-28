namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Jump to Signature (plan section 24): a micro-jump to an open site after a spool. Damage breaks the spool, and so
    /// does an Interdictor once it holds ships.
    /// </summary>
    public static class SignatureJumpRules
    {
        /// <summary>Why a pilot cannot start a jump, or None.</summary>
        public static SignatureJumpRefusal GetStartRefusal(
            bool piloting,
            ScanSignature signature,
            string pilotAreaResref,
            bool partyCanJump,
            bool alreadySpooling)
        {
            if (!piloting) return SignatureJumpRefusal.NotPiloting;
            if (signature == null) return SignatureJumpRefusal.NotFound;
            if (signature.AreaResref != pilotAreaResref) return SignatureJumpRefusal.OtherArea;
            if (!partyCanJump) return SignatureJumpRefusal.NotLocated;
            if (alreadySpooling) return SignatureJumpRefusal.AlreadySpooling;
            return SignatureJumpRefusal.None;
        }

        /// <summary>Whether the spool has run its full time.</summary>
        public static bool IsSpoolDone(DateTime startedAt, DateTime now) =>
            now >= startedAt.AddSeconds(ScanningTuning.JumpSpoolSeconds);

        /// <summary>Why a spool breaks off when it ends, or None: damage, an Interdictor, or the site is gone.</summary>
        public static SignatureJumpRefusal GetCompletionRefusal(bool damagedDuringSpool, bool interdicted, bool siteOpen, bool sameArea)
        {
            if (damagedDuringSpool) return SignatureJumpRefusal.Damaged;
            if (interdicted) return SignatureJumpRefusal.Interdicted;
            if (!siteOpen || !sameArea) return SignatureJumpRefusal.SiteClosed;
            return SignatureJumpRefusal.None;
        }

        /// <summary>
        /// Where the ship drops out: <see cref="ScanningTuning.JumpArrivalMeters"/> from the site, on the side it came
        /// from.
        /// </summary>
        public static (float X, float Y) Arrival(float fromX, float fromY, float siteX, float siteY)
        {
            var dx = fromX - siteX;
            var dy = fromY - siteY;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.01)
                return (siteX + ScanningTuning.JumpArrivalMeters, siteY);

            var scale = ScanningTuning.JumpArrivalMeters / length;
            return ((float)(siteX + dx * scale), (float)(siteY + dy * scale));
        }

        /// <summary>The message for a refusal.</summary>
        public static string Message(SignatureJumpRefusal refusal) => refusal switch
        {
            SignatureJumpRefusal.NotPiloting => "Only the pilot can jump the ship.",
            SignatureJumpRefusal.NotFound => "That signature has faded.",
            SignatureJumpRefusal.OtherArea => "That signature is in another system.",
            SignatureJumpRefusal.NotLocated => "Your party has not located that signature. Scan it to 100% first.",
            SignatureJumpRefusal.AlreadySpooling => "The jump drive is already spooling.",
            SignatureJumpRefusal.Damaged => "Taking fire broke off the jump.",
            SignatureJumpRefusal.Interdicted => "An interdiction field holds your ship. The jump fails.",
            SignatureJumpRefusal.SiteClosed => "The site is gone. The jump is cancelled.",
            _ => string.Empty,
        };
    }
}
